using System;
using System.Collections.Generic;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
 
    /// <summary>
    /// Bridges a single <c>libvlc_event_manager_t*</c> to managed events. One native callback is
    /// rooted for the manager's lifetime; each event type is attached on its first subscriber and
    /// detached when its last subscriber leaves.
    /// </summary>
    /// <remarks>
    /// Attach/unsubscribe are serialized with a lock; the delegate field is read lock-free when
    /// dispatching. Callbacks fire on libvlc's own threads — marshal to your UI thread as needed.
    /// <c>libvlc_event_detach</c> guarantees no callback is in flight once it returns.
    /// </remarks>
    internal unsafe class EventManager : IDisposable
    {
        private readonly libvlc_event_manager_t* _manager; 
        private readonly Callback _callback; // kept alive for the manager's lifetime
        private readonly IntPtr _callbackPtr; // its native function pointer (rooted via _callback)
        private readonly HashSet<int> _attached = new HashSet<int>();

        public EventManager(libvlc_event_manager_t* manager, Callback dispatch)
        {
            _manager = manager;
            _callback = dispatch;
            _callbackPtr = _callback.ToFunctionPointer();
        }
 
        /// <summary>Adds <paramref name="handler"/> and attaches the native event on the first subscriber.</summary>
        public void Attach<T>(ref T? field, T handler, libvlc_event_e type) where T : Delegate
        {
            if (handler is null) return;
            lock (_attached)
            {
                bool wasEmpty = field is null;
                field = (T?)Delegate.Combine(field, handler);
                if (wasEmpty && _attached.Add((int)type))
                    libvlc_event_attach(_manager, (int)type, _callbackPtr, IntPtr.Zero);
            }
        }

        /// <summary>Removes <paramref name="handler"/> and detaches the native event when the last subscriber leaves.</summary>
        public void Detach<T>(ref T? field, T handler, libvlc_event_e type) where T : Delegate
        {
            if (handler is null) return;
            lock (_attached)
            {
                if (field is null) return;
                field = (T?)Delegate.Remove(field, handler);
                if (field is null && _attached.Remove((int)type))
                    libvlc_event_detach(_manager, (int)type, _callbackPtr, IntPtr.Zero);
            }
        }

        public void Dispose()
        {
            lock (_attached)
            {
                foreach (var type in _attached)
                    libvlc_event_detach(_manager, type, _callbackPtr, IntPtr.Zero);
                _attached.Clear();
            }
        }
    }

    // ----------------------------------------------------------------------------------------------
    // Event payloads. All are readonly structs delivered through EventHandler<T> (no heap allocation:
    // T is a generic type argument, so the struct is not boxed). Payload-less events use the plain
    // EventHandler with EventArgs.Empty.
    // ----------------------------------------------------------------------------------------------

    // --- Media ---

    /// <summary>Payload of <see cref="Media.MetaChanged"/>.</summary>
    public readonly struct MetaChangedEventArgs
    {
        /// <summary>The metadata field that changed.</summary>
        public readonly Meta Meta;
        public MetaChangedEventArgs(Meta meta) => Meta = meta;
    }

    /// <summary>Payload of <see cref="Media.DurationChanged"/>.</summary>
    public readonly struct DurationChangedEventArgs
    {
        /// <summary>New duration in milliseconds.</summary>
        public readonly long DurationMs;
        public DurationChangedEventArgs(long durationMs) => DurationMs = durationMs;
    }

    /// <summary>Payload of <see cref="Media.ParsedChanged"/>.</summary>
    public readonly struct ParsedChangedEventArgs
    {
        /// <summary>New parse status.</summary>
        public readonly MediaParsedStatus Status;
        public ParsedChangedEventArgs(MediaParsedStatus status) => Status = status;
    }

    /// <summary>
    /// Payload of <see cref="Media.ThumbnailGenerated"/>. Wraps the borrowed
    /// <c>libvlc_picture_t*</c> that is valid only for the duration of the event
    /// handler — call <see cref="GetThumbnail"/> inside the handler.
    /// </summary>
    public readonly struct ThumbnailGeneratedEventArgs
    {
        /// <summary>Borrowed native <c>libvlc_picture_t*</c>; valid only for the duration of the event.</summary>
        private readonly IntPtr thumbnail;
        public ThumbnailGeneratedEventArgs(IntPtr thumbnail) => this.thumbnail = thumbnail;

        /// <summary>
        /// Returns the generated thumbnail.
        /// <para>
        /// When <paramref name="addRef"/> is <c>true</c> (default) the <see cref="Picture"/> is retained
        /// (+1) and may be kept after the handler returns; the caller must dispose it. When
        /// <c>false</c> the <see cref="Picture"/> is a non-owning view valid only inside the handler:
        /// do not dispose it and do not keep a reference past the handler.
        /// </para>
        /// </summary>
        public unsafe Picture? GetThumbnail(bool addRef = true)
        {
            var p = (libvlc_picture_t*)thumbnail;
            if (p == null) return null;
            return addRef
                ? new Picture((IntPtr)libvlc_picture_retain(p))
                : new Picture((IntPtr)p, addRef: false);
        }
    }

    /// <summary>
    /// Payload of <see cref="Media.AttachedThumbnailsFound"/>. Wraps the borrowed
    /// <c>libvlc_picture_list_t*</c> that libvlc addRef and frees once the event handler returns, so this
    /// payload is only meaningful inside the handler — call <see cref="GetThumbnails"/> there.
    /// </summary>
    public readonly struct AttachedThumbnailsFoundEventArgs
    {
        /// <summary>Borrowed native <c>libvlc_picture_list_t*</c>; valid only for the duration of the event.</summary>
        private readonly IntPtr thumbnails;
        public AttachedThumbnailsFoundEventArgs(IntPtr thumbnails) => this.thumbnails = thumbnails;

        /// <summary>
        /// Returns the attached thumbnails. Must be called inside the event handler (the underlying list
        /// is freed by libvlc when the handler returns).
        /// <para>
        /// When <paramref name="owner"/> is <c>true</c> (default) each <see cref="Picture"/> is retained
        /// (+1) and may be kept after the handler returns; the caller must dispose each one. When
        /// <c>false</c> each <see cref="Picture"/> is a non-owning view valid only inside the handler:
        /// do not dispose it and do not keep a reference past the handler.
        /// </para>
        /// </summary>
        public unsafe IReadOnlyList<Picture> GetThumbnails(bool owner = true)
        {
            var list = (libvlc_picture_list_t*)thumbnails;
            if (list == null) return Array.Empty<Picture>();
            int count = (int)libvlc_picture_list_count(list).ToUInt32();
            var pictures = new Picture[count];
            for (int i = 0; i < count; i++)
            {
                var pic = libvlc_picture_list_at(list, new UIntPtr((uint)i)); // borrowed
                pictures[i] = owner
                    ? new Picture((IntPtr)libvlc_picture_retain(pic))         // own a +1 ref
                    : new Picture((IntPtr)pic, addRef: false);                  // borrowed view
            }
            return pictures;
        }
    }

    /// <summary>
    /// Payload of events that carry a single <see cref="Core.Media"/> (sub-items, media changes).
    /// Wraps the borrowed <c>libvlc_media_t*</c> that is valid only for the duration of the event
    /// handler — call <see cref="GetMedia"/> inside the handler.
    /// </summary>
    public readonly struct MediaEventArgs
    {
        /// <summary>Borrowed native <c>libvlc_media_t*</c>; valid only for the duration of the event.</summary>
        private readonly IntPtr media;
        public MediaEventArgs(IntPtr media) => this.media = media;

        /// <summary>
        /// Returns the media.
        /// <para>
        /// When <paramref name="addRef"/> is <c>true</c> (default) the <see cref="Core.Media"/> is retained
        /// (+1) and may be kept after the handler returns; the caller must dispose it. When
        /// <c>false</c> the <see cref="Core.Media"/> is a non-owning view valid only inside the handler:
        /// do not dispose it and do not keep a reference past the handler.
        /// </para>
        /// </summary>
        public unsafe Media? GetMedia(bool addRef = true)
        {
            var p = (libvlc_media_t*)media;
            if (p == null) return null;
            return addRef
                ? new Media((IntPtr)libvlc_media_retain(p))
                : new Media((IntPtr)p, addRef: false);
        }
    }

    // --- MediaPlayer ---

    /// <summary>Payload of <see cref="MediaPlayer.Buffering"/>.</summary>
    public readonly struct BufferingEventArgs
    {
        /// <summary>Buffering progress in [0, 100].</summary>
        public readonly float Cache;
        public BufferingEventArgs(float cache) => Cache = cache;
    }

    /// <summary>Payload of <see cref="MediaPlayer.TimeChanged"/>.</summary>
    public readonly struct TimeChangedEventArgs
    {
        /// <summary>New playback time in milliseconds.</summary>
        public readonly long TimeMs;
        public TimeChangedEventArgs(long timeMs) => TimeMs = timeMs;
    }

    /// <summary>Payload of <see cref="MediaPlayer.PositionChanged"/>.</summary>
    public readonly struct PositionChangedEventArgs
    {
        /// <summary>New position in [0, 1].</summary>
        public readonly double Position;
        public PositionChangedEventArgs(double position) => Position = position;
    }

    /// <summary>Payload of <see cref="MediaPlayer.LengthChanged"/>.</summary>
    public readonly struct LengthChangedEventArgs
    {
        /// <summary>New length in milliseconds.</summary>
        public readonly long LengthMs;
        public LengthChangedEventArgs(long lengthMs) => LengthMs = lengthMs;
    }

    /// <summary>Payload of <see cref="MediaPlayer.SeekableChanged"/>.</summary>
    public readonly struct SeekableChangedEventArgs
    {
        /// <summary>Whether the input is now seekable.</summary>
        public readonly bool Seekable;
        public SeekableChangedEventArgs(bool seekable) => Seekable = seekable;
    }

    /// <summary>Payload of <see cref="MediaPlayer.PausableChanged"/>.</summary>
    public readonly struct PausableChangedEventArgs
    {
        /// <summary>Whether the input can now be paused.</summary>
        public readonly bool Pausable;
        public PausableChangedEventArgs(bool pausable) => Pausable = pausable;
    }

    /// <summary>Payload of <see cref="MediaPlayer.Vout"/>.</summary>
    public readonly struct VoutEventArgs
    {
        /// <summary>Number of active video outputs.</summary>
        public readonly int Count;
        public VoutEventArgs(int count) => Count = count;
    }

    /// <summary>Payload of <see cref="MediaPlayer.SnapshotTaken"/>.</summary>
    public readonly struct SnapshotTakenEventArgs
    {
        /// <summary>Path the snapshot was written to.</summary>
        public readonly string? FilePath;
        public SnapshotTakenEventArgs(string? filePath) => FilePath = filePath;
    }

    /// <summary>Payload of <see cref="MediaPlayer.ChapterChanged"/>.</summary>
    public readonly struct ChapterChangedEventArgs
    {
        /// <summary>New chapter index.</summary>
        public readonly int Chapter;
        public ChapterChangedEventArgs(int chapter) => Chapter = chapter;
    }

    /// <summary>
    /// Payload of <see cref="MediaPlayer.TitleSelectionChanged"/>. Wraps the borrowed
    /// <c>const libvlc_title_description_t*</c> that is valid only for the duration of the event
    /// handler — call <see cref="GetTitleDescription"/> inside the handler.
    /// </summary>
    public readonly struct TitleSelectionChangedEventArgs
    {
        /// <summary>Borrowed native <c>const libvlc_title_description_t*</c>; valid only for the duration of the event.</summary>
        private readonly IntPtr title;
        /// <summary>Selected title index.</summary>
        public readonly int Index;
        public TitleSelectionChangedEventArgs(IntPtr title, int index) { this.title = title; Index = index; }

        /// <summary>
        /// Returns a managed <see cref="TitleDescription"/> by reading the borrowed native pointer.
        /// Must be called inside the event handler — the underlying pointer is only valid for the
        /// duration of the callback.
        /// </summary>
        /// <returns>A <see cref="TitleDescription"/> with the title's name, duration, and raw flags,
        /// or <see langword="null"/> if the native pointer is null.</returns>
        public unsafe TitleDescription? GetTitleDescription()
        {
            var td = (libvlc_title_description_t*)title;
            if (td == null) return null;
            return new TitleDescription(
                ((IntPtr)td->psz_name).GetUtf8(),
                td->i_duration,
                td->i_flags);
        }
    }

    /// <summary>Payload of <see cref="MediaPlayer.AudioVolume"/>.</summary>
    public readonly struct AudioVolumeEventArgs
    {
        /// <summary>New volume in [0, 1] (1.0 = 100%).</summary>
        public readonly float Volume;
        public AudioVolumeEventArgs(float volume) => Volume = volume;
    }

    /// <summary>Payload of <see cref="MediaPlayer.AudioDevice"/>.</summary>
    public readonly struct AudioDeviceEventArgs
    {
        /// <summary>The new audio output device id.</summary>
        public readonly string? Device;
        public AudioDeviceEventArgs(string? device) => Device = device;
    }

    /// <summary>Payload of <see cref="MediaPlayer.ProgramAdded"/> / <see cref="MediaPlayer.ProgramDeleted"/> / <see cref="MediaPlayer.ProgramUpdated"/>.</summary>
    public readonly struct ProgramEventArgs
    {
        /// <summary>Program (group) id.</summary>
        public readonly int Id;
        public ProgramEventArgs(int id) => Id = id;
    }

    /// <summary>Payload of <see cref="MediaPlayer.ProgramSelected"/>.</summary>
    public readonly struct ProgramSelectionChangedEventArgs
    {
        /// <summary>Previously selected program id, or -1.</summary>
        public readonly int UnselectedId;
        /// <summary>Newly selected program id, or -1.</summary>
        public readonly int SelectedId;
        public ProgramSelectionChangedEventArgs(int unselectedId, int selectedId) { UnselectedId = unselectedId; SelectedId = selectedId; }
    }

    /// <summary>Payload of <see cref="MediaPlayer.ESAdded"/> / <see cref="MediaPlayer.ESDeleted"/> / <see cref="MediaPlayer.ESUpdated"/>.</summary>
    public readonly struct EsChangedEventArgs
    {
        /// <summary>Track type (audio/video/text).</summary>
        public readonly TrackType TrackType;
        /// <summary>Numeric ES id. Deprecated by libvlc — prefer <see cref="EsId"/>.</summary>
        public readonly int Id;
        /// <summary>String ES id (<c>psz_id</c>). Pass to <c>libvlc_media_player_get_track_from_id</c> to obtain the full track description.</summary>
        public readonly string? EsId;
        public EsChangedEventArgs(TrackType trackType, int id, string? esId) { TrackType = trackType; Id = id; EsId = esId; }
    }

    /// <summary>Payload of <see cref="MediaPlayer.ESSelected"/>.</summary>
    public readonly struct EsSelectionChangedEventArgs
    {
        /// <summary>Track type whose selection changed.</summary>
        public readonly TrackType TrackType;
        /// <summary>Previously selected ES id, or null.</summary>
        public readonly string? UnselectedId;
        /// <summary>Newly selected ES id, or null.</summary>
        public readonly string? SelectedId;
        public EsSelectionChangedEventArgs(TrackType trackType, string? unselectedId, string? selectedId)
        { TrackType = trackType; UnselectedId = unselectedId; SelectedId = selectedId; }
    }

    /// <summary>Payload of <see cref="MediaPlayer.RecordChanged"/>.</summary>
    public readonly struct RecordChangedEventArgs
    {
        /// <summary>True if recording started, false if it stopped.</summary>
        public readonly bool Recording;
        /// <summary>Path of the recorded file when recording stops, otherwise null.</summary>
        public readonly string? FilePath;
        public RecordChangedEventArgs(bool recording, string? filePath) { Recording = recording; FilePath = filePath; }
    }

    // --- MediaList ---

    /// <summary>
    /// Payload of <see cref="MediaList.ItemAdded"/> / <see cref="MediaList.ItemDeleted"/>.
    /// Wraps the borrowed <c>libvlc_media_t*</c> that is valid only for the duration of the event
    /// handler — call <see cref="GetItem"/> inside the handler.
    /// </summary>
    public readonly struct MediaListItemEventArgs
    {
        /// <summary>Borrowed native <c>libvlc_media_t*</c>; valid only for the duration of the event.</summary>
        private readonly IntPtr item;
        /// <summary>Index of the item in the list.</summary>
        public readonly int Index;
        public MediaListItemEventArgs(IntPtr item, int index) { this.item = item; Index = index; }

        /// <summary>
        /// Returns the media at <see cref="Index"/>.
        /// <para>
        /// When <paramref name="addRef"/> is <c>true</c> (default) the <see cref="Core.Media"/> is retained
        /// (+1) and may be kept after the handler returns; the caller must dispose it. When
        /// <c>false</c> the <see cref="Core.Media"/> is a non-owning view valid only inside the handler:
        /// do not dispose it and do not keep a reference past the handler.
        /// </para>
        /// </summary>
        public unsafe Media? GetItem(bool addRef = true)
        {
            var p = (libvlc_media_t*)item;
            if (p == null) return null;
            return addRef
                ? new Media((IntPtr)libvlc_media_retain(p))
                : new Media((IntPtr)p, addRef: false);
        }
    }

    // --- RendererDiscoverer ---

    /// <summary>
    /// Payload of <see cref="RendererDiscoverer.ItemAdded"/> / <see cref="RendererDiscoverer.ItemDeleted"/>.
    /// Wraps the borrowed <c>libvlc_renderer_item_t*</c> that is valid only for the duration of the event
    /// handler — call <see cref="GetItem"/> inside the handler.
    /// </summary>
    public readonly struct RendererItemEventArgs
    {
        /// <summary>Borrowed native <c>libvlc_renderer_item_t*</c>; valid only for the duration of the event.</summary>
        private readonly IntPtr item;
        public RendererItemEventArgs(IntPtr item) => this.item = item;

        /// <summary>
        /// Returns the renderer item.
        /// <para>
        /// When <paramref name="addRef"/> is <c>true</c> (default) the <see cref="RendererItem"/> is held
        /// (+1) and may be kept after the handler returns; the caller must dispose it. When
        /// <c>false</c> the <see cref="RendererItem"/> is a non-owning view valid only inside the handler:
        /// do not dispose it and do not keep a reference past the handler.
        /// </para>
        /// </summary>
        public unsafe RendererItem? GetItem(bool addRef = true)
        {
            var p = (libvlc_renderer_item_t*)item;
            if (p == null) return null;
            return addRef
                ? new RendererItem((IntPtr)libvlc_renderer_item_hold(p))
                : new RendererItem((IntPtr)p, addRef: false);
        }
    }
}
