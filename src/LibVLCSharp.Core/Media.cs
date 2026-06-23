using System;
using System.Collections.Generic;
using System.IO;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>
    /// An abstract representation of a playable media (<c>libvlc_media_t</c>): a media location
    /// (MRL) plus various optional meta data.
    /// </summary>
    public unsafe class Media : NativeReference
    {
        private EventManager? _events;

        // Rooted for the media's lifetime when created via FromCallbacks; libvlc holds their pointers.
        // Stored as the alias delegates directly — they carry [UnmanagedFunctionPointer(Cdecl)], so the
        // call site marshals them straight to function pointers (no interop-twin conversion).
        private MediaOpenCallback? _openCb;
        private MediaReadCallback? _readCb;
        private MediaSeekCallback? _seekCb;
        private MediaCloseCallback? _closeCb;

        // Owned by the media (disposed on release) when created via FromStream without leaveOpen.
        private Stream? _stream;

        /// <summary>Wraps an existing native handle.</summary>
        /// <param name="handle">Native <c>libvlc_media_t*</c>.</param>
        public Media(IntPtr handle) : base(handle) { }

        /// <summary>
        /// Wraps a native handle. When <paramref name="addRef"/> is <c>false</c> the media is a borrowed
        /// view that is never released and is valid only for as long as its owner keeps it alive
        /// (e.g. a media obtained from <see cref="MediaEventArgs.GetMedia"/> with
        /// <c>addRef: false</c>, valid only inside the event handler). Do not dispose a borrowed media.
        /// </summary>
        /// <param name="handle">Native <c>libvlc_media_t*</c>.</param>
        /// <param name="addRef"><c>true</c> to retain ownership and release on dispose; <c>false</c> for a borrowed view.</param>
        public Media(IntPtr handle, bool addRef) : base(handle, addRef) { }

        /// <summary>
        /// Creates a media from a media resource location, for instance a valid URL.
        /// <c>libvlc_media_new_location</c>.
        /// </summary>
        /// <param name="mrl">The media location (MRL).</param>
        /// <returns>The newly created media.</returns>
        /// <remarks>
        /// To refer to a local file with this function, the <c>file://...</c> URI syntax <b>must</b>
        /// be used (IETF RFC 3986); prefer <see cref="FromPath"/> when dealing with local files.
        /// libvlc returns NULL on error, in which case construction throws.
        /// </remarks>
        public static Media FromLocation(string mrl)
        {
            using var u = new Utf8Buffer(mrl);
            return new Media((IntPtr)libvlc_media_new_location(u));
        }

        /// <summary>Creates a media for a certain file path. <c>libvlc_media_new_path</c>.</summary>
        /// <param name="path">Local filesystem path.</param>
        /// <returns>The newly created media.</returns>
        /// <remarks>libvlc returns NULL on error, in which case construction throws.</remarks>
        public static Media FromPath(string path)
        {
            using var u = new Utf8Buffer(path);
            return new Media((IntPtr)libvlc_media_new_path(u));
        }

        /// <summary>
        /// Creates a media for an already open file descriptor, which shall be open for reading (or
        /// reading and writing). <c>libvlc_media_new_fd</c>.
        /// </summary>
        /// <param name="fd">Open file descriptor.</param>
        /// <returns>The newly created media.</returns>
        /// <remarks>
        /// Regular files, pipe read descriptors and character devices (including TTYs) are supported on
        /// all platforms; block devices where available; directory descriptors on systems providing
        /// <c>fdopendir()</c>; sockets everywhere they are file descriptors (i.e. all except Windows).
        /// This library will <b>not</b> close the descriptor under any circumstance. A descriptor can
        /// usually only be rendered once; to render it again, rewind it to the beginning with
        /// <c>lseek()</c>. Available since LibVLC 1.1.5.
        /// </remarks>
        public static Media FromFd(int fd) => new Media((IntPtr)libvlc_media_new_fd(fd));

        /// <summary>
        /// Creates a media as an empty node (a named container for subitems).
        /// <c>libvlc_media_new_as_node</c>.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <returns>The new empty media.</returns>
        public static Media AsNode(string name)
        {
            using var u = new Utf8Buffer(name);
            return new Media((IntPtr)libvlc_media_new_as_node(u));
        }

        /// <summary>
        /// Creates media with custom callbacks to read the data from. <c>libvlc_media_new_callbacks</c>.
        /// </summary>
        /// <param name="open">
        /// Opens the data source and reports its size, or null. <c>libvlc_media_open_cb</c>. When null,
        /// <paramref name="opaque"/> is passed straight to the other callbacks and the stream size is
        /// treated as unknown.
        /// </param>
        /// <param name="read">Reads bytes into the supplied buffer (must not be null). <c>libvlc_media_read_cb</c>.</param>
        /// <param name="seek">Seeks to a byte offset, or null if seeking is not supported. <c>libvlc_media_seek_cb</c>.</param>
        /// <param name="close">Closes the data source, or null if unnecessary. <c>libvlc_media_close_cb</c>.</param>
        /// <param name="opaque">Data pointer passed to the open callback (or to the others when <paramref name="open"/> is null).</param>
        /// <returns>The newly created media.</returns>
        /// <remarks>
        /// The callbacks may be called asynchronously (from another thread). A single stream instance
        /// need not be reentrant, but <paramref name="open"/> must be reentrant if the media is used by
        /// multiple player instances. <b>Warning:</b> the callbacks may be used until all player
        /// instances supplied the media are stopped, so keep the returned <see cref="Media"/> (which
        /// roots the delegates) alive for that long. Available since LibVLC 3.0.0.
        /// </remarks>
        public static Media FromCallbacks(
            MediaOpenCallback? open,
            MediaReadCallback read,
            MediaSeekCallback? seek,
            MediaCloseCallback? close,
            IntPtr opaque = default)
        {
            // The alias delegates carry [UnmanagedFunctionPointer(Cdecl)], so they marshal straight to
            // native function pointers; root them in fields to keep the caller's handlers alive.
            var media = new Media((IntPtr)libvlc_media_new_callbacks(
                open.ToFunctionPointer(), read.ToFunctionPointer(), seek.ToFunctionPointer(), close.ToFunctionPointer(), opaque))
            {
                _openCb = open,
                _readCb = read,
                _seekCb = seek,
                _closeCb = close
            };
            return media;
        }

        /// <summary>
        /// Creates media that reads from a managed <see cref="Stream"/>, wiring it to the libvlc media
        /// callbacks (a convenience over <see cref="FromCallbacks"/>). A seekable stream reports its
        /// length and supports seeking; a non-seekable stream is read with unknown length and no seek.
        /// </summary>
        /// <param name="stream">The source stream; must be readable. Read on libvlc's own threads.</param>
        /// <param name="leaveOpen">
        /// When <c>false</c> (default) the stream is disposed together with the returned
        /// <see cref="Media"/>; when <c>true</c> the caller keeps ownership. Either way the stream is
        /// kept alive for the media's lifetime.
        /// </param>
        public static Media FromStream(Stream stream, bool leaveOpen = false)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new ArgumentException("Stream must be readable.", nameof(stream));

            // libvlc may open/read/close more than once (e.g. parse then playback), so reset position
            // on open and never dispose from the close callback. The stream is captured directly, so
            // the libvlc opaque pointer is unused.
            MediaOpenCallback open = (IntPtr opaque, IntPtr* datap, ulong* sizep) =>
            {
                *datap = IntPtr.Zero;
                try
                {
                    if (stream.CanSeek)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        *sizep = (ulong)stream.Length;
                    }
                    else
                    {
                        *sizep = ulong.MaxValue; // unknown length
                    }
                    return 0;
                }
                catch { return -1; }
            };

#if NETSTANDARD2_0
            byte[]? scratch = null; // staging buffer: ns2.0 has no Stream.Read(Span<byte>)
#endif
            MediaReadCallback read = (IntPtr opaque, byte* buf, UIntPtr len) =>
            {
                try
                {
                    int toRead = (int)Math.Min((ulong)len, int.MaxValue);
                    if (toRead == 0) return IntPtr.Zero;
#if NETSTANDARD2_0
                    if (scratch is null || scratch.Length < toRead) scratch = new byte[toRead];
                    int n = stream.Read(scratch, 0, toRead);
                    if (n > 0) new ReadOnlySpan<byte>(scratch, 0, n).CopyTo(new Span<byte>(buf, n));
#else
                    int n = stream.Read(new Span<byte>(buf, toRead));
#endif
                    return (IntPtr)n; // 0 = end of stream
                }
                catch { return (IntPtr)(-1); }
            };

            MediaSeekCallback? seek = stream.CanSeek ? (IntPtr opaque, ulong offset) =>
            {
                try { stream.Seek((long)offset, SeekOrigin.Begin); return 0; }
                catch { return -1; }
            }
            : null;

            MediaCloseCallback close = (IntPtr opaque) =>
            {
                try { if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin); } catch { /* best effort */ }
            };

            var media = FromCallbacks(open, read, seek, close);
            if (!leaveOpen) media._stream = stream;
            return media;
        }

        /// <summary>Implicit conversion to the native <c>libvlc_media_t*</c> (null for a null media).</summary>
        public static implicit operator libvlc_media_t*(Media? media) =>
            media is null ? null : (libvlc_media_t*)media.NativeHandle;

        protected override void Dispose(bool disposing)
        {
            if (disposing) _events?.Dispose();   // detach events before the native release
            base.Dispose(disposing);             // native release (see Release)
            if (disposing) _stream?.Dispose();   // owned stream from FromStream(leaveOpen: false): close AFTER release so a late read callback can't hit a disposed stream
        }

        protected override void Release(IntPtr handle)
        {
            libvlc_media_release((libvlc_media_t*)handle);

            // Keep the FromCallbacks delegates rooted until the media is released (libvlc holds their
            // function pointers); this is also the only read of these fields.
            GC.KeepAlive(_openCb);
            GC.KeepAlive(_readCb);
            GC.KeepAlive(_seekCb);
            GC.KeepAlive(_closeCb);
        }

        /// <summary>The media resource locator. <c>libvlc_media_get_mrl</c> (heap string; freed here).</summary>
        /// <remarks>Null when the underlying input item has no URI.</remarks>
        public string? Mrl => ((IntPtr)libvlc_media_get_mrl(this)).GetUtf8(freeNativeData: true);

        /// <summary>Duration in milliseconds, or -1 on error. <c>libvlc_media_get_duration</c>.</summary>
        /// <remarks>
        /// You must call <see cref="ParseRequest"/> or play the media at least once before reading
        /// this; otherwise the result is undefined.
        /// </remarks>
        public long Duration => libvlc_media_get_duration(this);

        /// <summary>Current parse status. <c>libvlc_media_get_parsed_status</c>. Since LibVLC 3.0.0.</summary>
        public MediaParsedStatus ParsedStatus => (MediaParsedStatus)libvlc_media_get_parsed_status(this);

        private MediaList? _subitems;

        /// <summary>
        /// The media's subitems as a <see cref="MediaList"/>, or null if none. Populated after
        /// parsing, e.g. for a playlist or directory. <c>libvlc_media_subitems</c> — returns the
        /// cached instance while the native pointer is unchanged (the extra reference the getter
        /// takes is released); a new owning wrapper if it changed. Owned by this <see cref="Media"/>.
        /// </summary>
        public MediaList? Subitems
        {
            get => Reconcile(ref _subitems, (IntPtr)libvlc_media_subitems(this), // +1 ref, or null
                static h => libvlc_media_list_release((libvlc_media_list_t*)h),
                static h => new MediaList(h));
        }

        /// <summary>
        /// The media's tracks of a given type as a new owned <see cref="MediaTrackList"/> (dispose it),
        /// or null in case of error. <c>libvlc_media_get_tracklist</c>. Since LibVLC 4.0.0.
        /// </summary>
        /// <param name="type">Type of the track list to request.</param>
        /// <returns>
        /// A new owned track list (dispose it), or null on error. When there is no track for the
        /// category, the list has a size of 0.
        /// </returns>
        /// <remarks>
        /// You must call <see cref="ParseRequest"/> or play the media at least once first; otherwise
        /// the list is empty.
        /// </remarks>
        public MediaTrackList? GetTracks(TrackType type)
        {
            var list = libvlc_media_get_tracklist(this, (libvlc_track_type_t)type);
            return list == null ? null : new MediaTrackList((IntPtr)list);
        }

        /// <summary>
        /// Adds an option to the media (e.g. ":network-caching=300"), used to determine how the media
        /// player reads it — VLC's advanced reading/streaming options on a per-media basis.
        /// <c>libvlc_media_add_option</c>.
        /// </summary>
        /// <param name="option">The option (as a string).</param>
        /// <remarks>
        /// Options are listed in <c>vlc --longhelp</c>; available options and their semantics vary
        /// across LibVLC versions. <b>Warning:</b> not all options affect a single media — most audio
        /// and video options (e.g. text renderer options) have no effect here and must be set through
        /// <see cref="LibVLC"/> (<c>libvlc_new</c>) instead.
        /// </remarks>
        public void AddOption(string option)
        {
            using var u = new Utf8Buffer(option);
            libvlc_media_add_option(this, u);
        }

        /// <summary>
        /// Parses the media asynchronously, fetching (local or network) art, meta data and/or track
        /// information. Track progress via the <see cref="ParsedChanged"/> event.
        /// <c>libvlc_media_parse_request</c>. Since LibVLC 4.0.0.
        /// </summary>
        /// <param name="libVLC">The LibVLC instance that is to parse the media.</param>
        /// <param name="flags">Parse options (combinable). By default media is parsed if it is a local file.</param>
        /// <param name="timeoutMs">
        /// Maximum preparse time: -1 uses the default "preparse-timeout" option, 0 waits indefinitely,
        /// and a positive value is a timeout in milliseconds.
        /// </param>
        /// <returns>0 on success, -1 if parsing could not be requested (no events are sent on error).</returns>
        /// <remarks>Parsing can be aborted with <see cref="ParseStop"/>.</remarks>
        public int ParseRequest(LibVLC libVLC, MediaParseFlag flags = 0, int timeoutMs = -1) =>
            libvlc_media_parse_request(libVLC, this, (libvlc_media_parse_flag_t)flags, timeoutMs);

        /// <summary>
        /// Stops a pending parse. <c>libvlc_media_parse_stop</c>. Since LibVLC 3.0.0.
        /// </summary>
        /// <param name="libVLC">The LibVLC instance that is to cease parsing the media.</param>
        /// <remarks>
        /// When parsing is stopped, the <see cref="ParsedChanged"/> event is sent with the
        /// <see cref="MediaParsedStatus"/> timeout status.
        /// </remarks>
        public void ParseStop(LibVLC libVLC) => libvlc_media_parse_stop(libVLC, this);

        /// <summary>
        /// Adds an option to the media with configurable flags (e.g. trusted/unique).
        /// <c>libvlc_media_add_option_flag</c>.
        /// </summary>
        /// <param name="options">The options (as a string).</param>
        /// <param name="flags">The flags for this option.</param>
        /// <remarks>See <see cref="AddOption(string)"/> for the option semantics and limitations.</remarks>
        public void AddOption(string options, MediaOptionFlag flags)
        {
            using var u = new Utf8Buffer(options);
            libvlc_media_add_option_flag(this, u, (uint)flags);
        }

        /// <summary>Duplicates the media into a new owned instance. <c>libvlc_media_duplicate</c>.</summary>
        /// <returns>The duplicated media, or null on allocation failure.</returns>
        /// <remarks>The duplicate does not share forthcoming updates from the original.</remarks>
        public Media? Duplicate()
        {
            var m = libvlc_media_duplicate(this);
            return m == null ? null : new Media((IntPtr)m);
        }

        /// <summary>Media type (file/stream/disc/...). <c>libvlc_media_get_type</c>. Since LibVLC 3.0.0.</summary>
        public MediaType Type => (MediaType)libvlc_media_get_type(this);

        /// <summary>Reads a metadata field. <c>libvlc_media_get_meta</c>.</summary>
        /// <param name="meta">The meta field to read.</param>
        /// <returns>The media's meta value, or null if the media has not been parsed yet.</returns>
        /// <remarks>
        /// Call <see cref="ParseRequest"/> or play the media at least once first. Changes are signalled
        /// by the <see cref="MetaChanged"/> event.
        /// </remarks>
        public string? GetMeta(Meta meta) => ((IntPtr)libvlc_media_get_meta(this, (libvlc_meta_t)meta)).GetUtf8();

        /// <summary>
        /// Sets a metadata field. This does not save the meta — call <see cref="SaveMeta"/> to persist
        /// it. <c>libvlc_media_set_meta</c>.
        /// </summary>
        /// <param name="meta">The meta field to write.</param>
        /// <param name="value">The meta value.</param>
        public void SetMeta(Meta meta, string value)
        {
            using var u = new Utf8Buffer(value);
            libvlc_media_set_meta(this, (libvlc_meta_t)meta, u);
        }

        /// <summary>Saves the meta previously set. <c>libvlc_media_save_meta</c>.</summary>
        /// <param name="vlc">The LibVLC instance.</param>
        /// <returns>True if the write operation was successful.</returns>
        public bool SaveMeta(LibVLC vlc) => libvlc_media_save_meta(vlc, this) != 0;

        /// <summary>Reads a non-standard (extra) metadata field. <c>libvlc_media_get_meta_extra</c>.</summary>
        /// <param name="name">The meta extra name to read.</param>
        /// <returns>The media's meta extra value, or null (e.g. if the media has not been parsed yet).</returns>
        public string? GetMetaExtra(string name)
        {
            using var u = new Utf8Buffer(name);
            return ((IntPtr)libvlc_media_get_meta_extra(this, u)).GetUtf8(true);
        }

        /// <summary>
        /// Sets a non-standard (extra) metadata field. This does not save the meta — call
        /// <see cref="SaveMeta"/> to persist it. <c>libvlc_media_set_meta_extra</c>.
        /// </summary>
        /// <param name="name">The meta extra name to write.</param>
        /// <param name="value">The meta extra value.</param>
        public void SetMetaExtra(string name, string value)
        {
            using var uName = new Utf8Buffer(name);
            using var uValue = new Utf8Buffer(value);
            libvlc_media_set_meta_extra(this, uName, uValue);
        }

        /// <summary>
        /// Names of the non-standard (extra) metadata fields (count known + indexable).
        /// <c>libvlc_media_get_meta_extra_names</c>.
        /// </summary>
        public IReadOnlyList<string?> MetaExtraNames
        {
            get
            {
                byte** names;
                uint n = libvlc_media_get_meta_extra_names(this, &names);
                if (n == 0) return Array.Empty<string?>();
                try
                {
                    var result = new string?[n];
                    for (uint i = 0; i < n; i++) result[i] = ((IntPtr)names[i]).GetUtf8();
                    return result;
                }
                finally { libvlc_media_meta_extra_names_release(names, n); }
            }
        }

        /// <summary>
        /// The media descriptor's user data — specialized data accessed by the host application.
        /// <c>libvlc_media_get_user_data</c> / <c>libvlc_media_set_user_data</c>.
        /// </summary>
        public IntPtr UserData
        {
            get => libvlc_media_get_user_data(this);
            set => libvlc_media_set_user_data(this, value);
        }

        /// <summary>Current statistics about the media, or null if unavailable. <c>libvlc_media_get_stats</c>.</summary>
        public MediaStats? Stats
        {
            get
            {
                libvlc_media_stats_t s;
                return libvlc_media_get_stats(this, &s).ToBool() ? new MediaStats(&s) : null;
            }
        }

        /// <summary>
        /// Reads a file-stat value (mtime/size) of the media, or null if not found.
        /// <c>libvlc_media_get_filestat</c>. Since LibVLC 4.0.0.
        /// </summary>
        /// <param name="type">A valid file-stat kind (mtime/size).</param>
        /// <returns>The value, or null if not found / on error.</returns>
        /// <remarks>
        /// File-stat values are currently only parsed by directory accesses — only sub-medias of a
        /// directory media parsed with <see cref="ParseRequest"/> can have valid file-stat properties.
        /// </remarks>
        public ulong? GetFileStat(MediaFileStat type)
        {
            ulong value;
            return libvlc_media_get_filestat(this, (uint)type, &value) == 1 ? value : (ulong?)null;
        }

        /// <summary>
        /// Adds a slave (external input source) — an additional subtitle track (e.g. a .srt) or audio
        /// track (e.g. a .ac3) — by URI. <c>libvlc_media_slaves_add</c>. Since LibVLC 3.0.0.
        /// </summary>
        /// <param name="type">Subtitle or audio.</param>
        /// <param name="priority">From 0 (low priority) to 4 (high priority).</param>
        /// <param name="uri">URI of the slave (should contain a valid scheme).</param>
        /// <returns>0 on success, -1 on error.</returns>
        /// <remarks>Must be called before the media is parsed (<see cref="ParseRequest"/>) or played.</remarks>
        public int AddSlave(MediaSlaveType type, uint priority, string uri)
        {
            using var u = new Utf8Buffer(uri);
            return libvlc_media_slaves_add(this, (libvlc_media_slave_type_t)type, priority, u);
        }

        /// <summary>
        /// Clears all slaves previously added by <see cref="AddSlave"/> or internally.
        /// <c>libvlc_media_slaves_clear</c>. Since LibVLC 3.0.0.
        /// </summary>
        public void ClearSlaves() => libvlc_media_slaves_clear(this);

        /// <summary>
        /// The slaves attached to the media (count known + indexable) — those parsed by VLC or added
        /// by <see cref="AddSlave"/>; a typical use is saving them in a database for later.
        /// <c>libvlc_media_slaves_get</c>. Since LibVLC 3.0.0.
        /// </summary>
        public IReadOnlyList<MediaSlave> Slaves
        {
            get
            {
                libvlc_media_slave_t** slaves;
                uint n = libvlc_media_slaves_get(this, &slaves);
                if (n == 0) return Array.Empty<MediaSlave>();
                try
                {
                    var result = new MediaSlave[n];
                    for (uint i = 0; i < n; i++)
                    {
                        var s = slaves[i];
                        result[i] = new MediaSlave(((IntPtr)s->psz_uri).GetUtf8(), (MediaSlaveType)s->i_type, s->i_priority);
                    }
                    return result;
                }
                finally { libvlc_media_slaves_release(slaves, n); }
            }
        }

        /// <summary>
        /// Starts an asynchronous thumbnail generation at a media time; the result arrives via
        /// <see cref="ThumbnailGenerated"/>. <c>libvlc_media_thumbnail_request_by_time</c>. Since LibVLC 4.0.
        /// </summary>
        /// <param name="libVLC">The LibVLC instance to generate the thumbnail with.</param>
        /// <param name="timeMs">The time (ms) at which the thumbnail should be generated.</param>
        /// <param name="width">The thumbnail width.</param>
        /// <param name="height">The thumbnail height.</param>
        /// <param name="speed">The seeking speed.</param>
        /// <param name="crop">Whether to crop the picture to preserve the source aspect ratio.</param>
        /// <param name="pictureType">The thumbnail picture type.</param>
        /// <param name="timeoutMs">A timeout in ms, or 0 to disable the timeout.</param>
        /// <returns>A valid request (release/cancel it via dispose), or null on failure.</returns>
        /// <remarks>
        /// If queued successfully, <see cref="ThumbnailGenerated"/> is guaranteed to fire unless the
        /// request is destroyed early. Provide both <paramref name="width"/> and <paramref name="height"/>
        /// to hardcode the size (stretched, or cropped if <paramref name="crop"/>); set one to 0 to
        /// derive it from the media aspect ratio.
        /// </remarks>
        public ThumbnailRequest? RequestThumbnailByTime(LibVLC libVLC, long timeMs, uint width, uint height,
            ThumbnailerSeekSpeed speed = default, bool crop = false, PictureType pictureType = default, long timeoutMs = 0)
        {
            var req = libvlc_media_thumbnail_request_by_time(libVLC, this, timeMs, (libvlc_thumbnailer_seek_speed_t)speed,
                width, height, crop.ToByte(), (libvlc_picture_type_t)pictureType, timeoutMs);
            return req == null ? null : new ThumbnailRequest((IntPtr)req);
        }

        /// <summary>
        /// Starts an asynchronous thumbnail generation at a position in [0,1]; the result arrives via
        /// <see cref="ThumbnailGenerated"/>. <c>libvlc_media_thumbnail_request_by_pos</c>. Since LibVLC 4.0.
        /// </summary>
        /// <param name="libVLC">The LibVLC instance to generate the thumbnail with.</param>
        /// <param name="position">The position in [0,1] at which the thumbnail should be generated.</param>
        /// <param name="width">The thumbnail width.</param>
        /// <param name="height">The thumbnail height.</param>
        /// <param name="speed">The seeking speed.</param>
        /// <param name="crop">Whether to crop the picture to preserve the source aspect ratio.</param>
        /// <param name="pictureType">The thumbnail picture type.</param>
        /// <param name="timeoutMs">A timeout in ms, or 0 to disable the timeout.</param>
        /// <returns>A valid request (release/cancel it via dispose), or null on failure.</returns>
        /// <remarks>
        /// If queued successfully, <see cref="ThumbnailGenerated"/> is guaranteed to fire unless the
        /// request is destroyed early. Provide both <paramref name="width"/> and <paramref name="height"/>
        /// to hardcode the size (stretched, or cropped if <paramref name="crop"/>); set one to 0 to
        /// derive it from the media aspect ratio.
        /// </remarks>
        public ThumbnailRequest? RequestThumbnailByPosition(LibVLC libVLC, double position, uint width, uint height,
            ThumbnailerSeekSpeed speed = default, bool crop = false, PictureType pictureType = default, long timeoutMs = 0)
        {
            var req = libvlc_media_thumbnail_request_by_pos(libVLC, this, position, (libvlc_thumbnailer_seek_speed_t)speed,
                width, height, crop.ToByte(), (libvlc_picture_type_t)pictureType, timeoutMs);
            return req == null ? null : new ThumbnailRequest((IntPtr)req);
        }

        /// <summary>
        /// A human-readable description of a codec FourCC from a media elementary stream.
        /// <c>libvlc_media_get_codec_description</c>. Since LibVLC 3.0.0.
        /// </summary>
        /// <param name="type">The track type (from a <see cref="MediaTrack"/>).</param>
        /// <param name="codec">The codec or original FourCC (from a <see cref="MediaTrack"/>).</param>
        /// <returns>The codec description.</returns>
        /// <remarks>Call <see cref="ParseRequest"/> or play the media at least once first.</remarks>
        public static string? CodecDescription(TrackType type, uint codec) =>
            ((IntPtr)libvlc_media_get_codec_description((libvlc_track_type_t)type, codec)).GetUtf8();

        // --- events ---

        private EventManager Events =>
            _events ??= new EventManager(libvlc_media_event_manager(this), Dispatch);

        private void Dispatch(libvlc_event_t* e,IntPtr _)
        {
            switch ((libvlc_event_e)e->type)
            {
                case libvlc_event_e.libvlc_MediaMetaChanged:
                    { var h = _meta; if (h != null) h(this, new MetaChangedEventArgs((Meta)e->u.media_meta_changed.meta_type)); break; }
                case libvlc_event_e.libvlc_MediaDurationChanged:
                    { var h = _duration; if (h != null) h(this, new DurationChangedEventArgs(e->u.media_duration_changed.new_duration)); break; }
                case libvlc_event_e.libvlc_MediaParsedChanged:
                    { var h = _parsed; if (h != null) h(this, new ParsedChangedEventArgs((MediaParsedStatus)e->u.media_parsed_changed.new_status)); break; }
                case libvlc_event_e.libvlc_MediaSubItemAdded:
                    { var h = _subItemAdded; if (h != null) h(this, new MediaEventArgs((IntPtr)e->u.media_subitem_added.new_child)); break; }
                case libvlc_event_e.libvlc_MediaSubItemTreeAdded:
                    { var h = _subItemTreeAdded; if (h != null) h(this, new MediaEventArgs((IntPtr)e->u.media_subitemtree_added.item)); break; }
                case libvlc_event_e.libvlc_MediaThumbnailGenerated:
                    { var h = _thumbnailGenerated; if (h != null) h(this, new ThumbnailGeneratedEventArgs((IntPtr)e->u.media_thumbnail_generated.p_thumbnail)); break; }
                case libvlc_event_e.libvlc_MediaAttachedThumbnailsFound:
                    { var h = _attachedThumbnailsFound; if (h != null) h(this, new AttachedThumbnailsFoundEventArgs((IntPtr)e->u.media_attached_thumbnails_found.thumbnails)); break; }
            }
        }

        private EventHandler<MetaChangedEventArgs>? _meta;
        private EventHandler<DurationChangedEventArgs>? _duration;
        private EventHandler<ParsedChangedEventArgs>? _parsed;
        private EventHandler<MediaEventArgs>? _subItemAdded, _subItemTreeAdded;
        private EventHandler<ThumbnailGeneratedEventArgs>? _thumbnailGenerated;
        private EventHandler<AttachedThumbnailsFoundEventArgs>? _attachedThumbnailsFound;

        /// <summary><c>libvlc_MediaMetaChanged</c> — a metadata field changed.</summary>
        public event EventHandler<MetaChangedEventArgs> MetaChanged
        {
            add => Events.Attach(ref _meta, value, libvlc_event_e.libvlc_MediaMetaChanged);
            remove => Events.Detach(ref _meta, value, libvlc_event_e.libvlc_MediaMetaChanged);
        }

        /// <summary><c>libvlc_MediaDurationChanged</c>.</summary>
        public event EventHandler<DurationChangedEventArgs> DurationChanged
        {
            add => Events.Attach(ref _duration, value, libvlc_event_e.libvlc_MediaDurationChanged);
            remove => Events.Detach(ref _duration, value, libvlc_event_e.libvlc_MediaDurationChanged);
        }

        /// <summary><c>libvlc_MediaParsedChanged</c> — the parse status changed.</summary>
        public event EventHandler<ParsedChangedEventArgs> ParsedChanged
        {
            add => Events.Attach(ref _parsed, value, libvlc_event_e.libvlc_MediaParsedChanged);
            remove => Events.Detach(ref _parsed, value, libvlc_event_e.libvlc_MediaParsedChanged);
        }

        /// <summary><c>libvlc_MediaSubItemAdded</c> — a sub-item was added. Call <see cref="MediaEventArgs.GetMedia"/> (default retains).</summary>
        public event EventHandler<MediaEventArgs> SubItemAdded
        {
            add => Events.Attach(ref _subItemAdded, value, libvlc_event_e.libvlc_MediaSubItemAdded);
            remove => Events.Detach(ref _subItemAdded, value, libvlc_event_e.libvlc_MediaSubItemAdded);
        }

        /// <summary><c>libvlc_MediaSubItemTreeAdded</c> — a sub-item tree was added. Call <see cref="MediaEventArgs.GetMedia"/>.</summary>
        public event EventHandler<MediaEventArgs> SubItemTreeAdded
        {
            add => Events.Attach(ref _subItemTreeAdded, value, libvlc_event_e.libvlc_MediaSubItemTreeAdded);
            remove => Events.Detach(ref _subItemTreeAdded, value, libvlc_event_e.libvlc_MediaSubItemTreeAdded);
        }

        /// <summary><c>libvlc_MediaThumbnailGenerated</c> — a requested thumbnail is ready. Call <see cref="ThumbnailGeneratedEventArgs.GetThumbnail"/> (default retains).</summary>
        public event EventHandler<ThumbnailGeneratedEventArgs> ThumbnailGenerated
        {
            add => Events.Attach(ref _thumbnailGenerated, value, libvlc_event_e.libvlc_MediaThumbnailGenerated);
            remove => Events.Detach(ref _thumbnailGenerated, value, libvlc_event_e.libvlc_MediaThumbnailGenerated);
        }

        /// <summary><c>libvlc_MediaAttachedThumbnailsFound</c> — attached thumbnails were found.</summary>
        public event EventHandler<AttachedThumbnailsFoundEventArgs> AttachedThumbnailsFound
        {
            add => Events.Attach(ref _attachedThumbnailsFound, value, libvlc_event_e.libvlc_MediaAttachedThumbnailsFound);
            remove => Events.Detach(ref _attachedThumbnailsFound, value, libvlc_event_e.libvlc_MediaAttachedThumbnailsFound);
        }
    }

    /// <summary>Flags for <see cref="Media.AddOption(string, MediaOptionFlag)"/>. <c>libvlc_media_option_t</c>.</summary>
    [Flags]
    public enum MediaOptionFlag
    {
        /// <summary>Option is trusted (may carry sensitive values).</summary>
        Trusted = libvlc_media_option_trusted,
        /// <summary>Option applies only to this media instance.</summary>
        Unique = libvlc_media_option_unique,
    }

    /// <summary>Selector for <see cref="Media.GetFileStat"/>. <c>libvlc_media_filestat_*</c>.</summary>
    public enum MediaFileStat
    {
        /// <summary>Last modification time (seconds since epoch).</summary>
        MTime = libvlc_media_filestat_mtime,
        /// <summary>File size in bytes.</summary>
        Size = libvlc_media_filestat_size,
    }

    /// <summary>An input slave (extra subtitle/audio track) of a media. <c>libvlc_media_slave_t</c>.</summary>
    public readonly struct MediaSlave
    {
        /// <summary>Slave URI.</summary>
        public readonly string? Uri;
        /// <summary>Slave kind (subtitle/audio).</summary>
        public readonly MediaSlaveType Type;
        /// <summary>Priority (0-4).</summary>
        public readonly uint Priority;
        internal MediaSlave(string? uri, MediaSlaveType type, uint priority) { Uri = uri; Type = type; Priority = priority; }
    }

    /// <summary>Playback statistics snapshot. <c>libvlc_media_stats_t</c>.</summary>
    public readonly struct MediaStats
    {
        internal unsafe MediaStats(libvlc_media_stats_t* s)
        {
            ReadBytes = s->i_read_bytes;
            InputBitrate = s->f_input_bitrate;
            DemuxReadBytes = s->i_demux_read_bytes;
            DemuxBitrate = s->f_demux_bitrate;
            DemuxCorrupted = s->i_demux_corrupted;
            DemuxDiscontinuity = s->i_demux_discontinuity;
            DecodedVideo = s->i_decoded_video;
            DecodedAudio = s->i_decoded_audio;
            DisplayedPictures = s->i_displayed_pictures;
            LatePictures = s->i_late_pictures;
            LostPictures = s->i_lost_pictures;
            PlayedAudioBuffers = s->i_played_abuffers;
            LostAudioBuffers = s->i_lost_abuffers;
        }

        /// <summary>Bytes read from the input.</summary>
        public readonly ulong ReadBytes;
        /// <summary>Input bitrate.</summary>
        public readonly float InputBitrate;
        /// <summary>Bytes read by the demuxer.</summary>
        public readonly ulong DemuxReadBytes;
        /// <summary>Demuxer bitrate.</summary>
        public readonly float DemuxBitrate;
        /// <summary>Corrupted demuxer blocks.</summary>
        public readonly ulong DemuxCorrupted;
        /// <summary>Demuxer discontinuities.</summary>
        public readonly ulong DemuxDiscontinuity;
        /// <summary>Decoded video blocks.</summary>
        public readonly ulong DecodedVideo;
        /// <summary>Decoded audio blocks.</summary>
        public readonly ulong DecodedAudio;
        /// <summary>Displayed pictures.</summary>
        public readonly ulong DisplayedPictures;
        /// <summary>Pictures decoded late.</summary>
        public readonly ulong LatePictures;
        /// <summary>Lost (dropped) pictures.</summary>
        public readonly ulong LostPictures;
        /// <summary>Played audio buffers.</summary>
        public readonly ulong PlayedAudioBuffers;
        /// <summary>Lost audio buffers.</summary>
        public readonly ulong LostAudioBuffers;
    }
    /// <summary>
    /// A pending thumbnail request (<c>libvlc_media_thumbnail_request_t</c>) created by
    /// <see cref="Media.RequestThumbnailByTime"/> / <see cref="Media.RequestThumbnailByPosition"/>.
    /// The generated thumbnail is delivered through <see cref="Media.ThumbnailGenerated"/>; dispose
    /// this handle to cancel/release the request.
    /// </summary>
    public unsafe class ThumbnailRequest : NativeReference
    {
        /// <summary>Wraps an existing native handle.</summary>
        /// <param name="handle">Native <c>libvlc_media_thumbnail_request_t*</c>.</param>
        public ThumbnailRequest(IntPtr handle) : base(handle) { }

        /// <summary>Implicit conversion to the native <c>libvlc_media_thumbnail_request_t*</c> (null for a null request).</summary>
        public static implicit operator libvlc_media_thumbnail_request_t*(ThumbnailRequest? request) =>
            request is null ? null : (libvlc_media_thumbnail_request_t*)request.NativeHandle;

        protected override void Release(IntPtr handle) =>
            libvlc_media_thumbnail_request_destroy((libvlc_media_thumbnail_request_t*)handle);
    }
}
