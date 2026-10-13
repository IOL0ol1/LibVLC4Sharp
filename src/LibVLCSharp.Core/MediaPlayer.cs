using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>A media player (<c>libvlc_media_player_t</c>): plays a <see cref="Media"/>.</summary>
    /// <remarks>
    /// Events (libvlc 4.0 <c>libvlc_media_player_cbs</c>) are wired only for players created through a
    /// constructor / <see cref="LibVLC.CreateMediaPlayer()"/>; see the <c>MediaPlayer.Events.cs</c> partial.
    /// </remarks>
    public unsafe class MediaPlayer : NativeReference
    {
        /// <summary>
        /// Wraps an existing native handle the library already owns (e.g. the player returned by
        /// <c>MediaListPlayer.MediaPlayer</c>). <c>internal</c> because libvlc 4.0 only accepts
        /// <c>libvlc_media_player_cbs</c> at creation: a wrapped instance has no callbacks registered, so
        /// its events never fire. Public players (a constructor / <see cref="LibVLC.CreateMediaPlayer()"/>)
        /// always have events wired.
        /// </summary>
        /// <param name="handle">Native <c>libvlc_media_player_t*</c>.</param>
        internal MediaPlayer(IntPtr handle) : base(handle) { }

        /// <summary>Implicit conversion to the native <c>libvlc_media_player_t*</c> (null for a null player).</summary>
        public static implicit operator libvlc_media_player_t*(MediaPlayer? mediaPlayer) =>
            mediaPlayer is null ? null : (libvlc_media_player_t*)mediaPlayer.NativeHandle;

        // Dispose (in the MediaPlayer.Events.cs partial) unwatches any active time watch and frees the
        // event-callback GCHandle around this release.
        protected override void Release(IntPtr handle) =>
            libvlc_media_player_release((libvlc_media_player_t*)handle);

        private Media? _media;

        /// <summary>
        /// The media to play.
        /// Setter: <c>libvlc_media_player_set_media</c>.
        /// Getter: <c>libvlc_media_player_get_media</c> — returns the assigned instance (the extra
        /// reference the getter takes is released); a new owning wrapper if changed externally.
        /// </summary>
        public Media? Media
        {
            get => Reconcile(ref _media, (IntPtr)libvlc_media_player_get_media(this), // +1 ref, or null
                static h => libvlc_media_release((libvlc_media_t*)h),
                static h => new Media(h));
            set
            {
                _media = value;
                libvlc_media_player_set_media(this, value);
            }
        }

        private Media? _nextMedia;

        /// <summary>
        /// The media to play next once the current one ends (gapless). Since libvlc 4.0.
        /// Setter: <c>libvlc_media_player_set_next_media</c>. Getter: <c>libvlc_media_player_get_next_media</c>
        /// — returns the assigned instance (the extra reference the getter takes is released); a new owning
        /// wrapper if changed externally.
        /// </summary>
        public Media? NextMedia
        {
            get => Reconcile(ref _nextMedia, (IntPtr)libvlc_media_player_get_next_media(this), // +1 ref, or null
                static h => libvlc_media_release((libvlc_media_t*)h),
                static h => new Media(h));
            set
            {
                _nextMedia = value;
                libvlc_media_player_set_next_media(this, value);
            }
        }

        // --- time watch (high-precision playback time; libvlc_media_player_watch_time_cbs) ---

        private TimeWatch? _timeWatch;

        /// <summary>
        /// Subscribes to high-precision playback-time updates. <c>libvlc_media_player_watch_time</c>.
        /// Dispose the returned handle (or the player) to unsubscribe. Only one watch is active at a time.
        /// Callbacks fire on libvlc's own threads.
        /// </summary>
        /// <param name="minPeriodUs">Minimum interval between <paramref name="onUpdate"/> calls, in microseconds.</param>
        /// <param name="onUpdate">Periodic time update (required).</param>
        /// <param name="onPaused">Invoked on pause with the system date (microseconds), or null.</param>
        /// <param name="onSeek">Invoked on seek with the new time point, or null.</param>
        /// <returns>A handle; dispose it to stop watching.</returns>
        public IDisposable WatchTime(long minPeriodUs, Action<MediaPlayer, MediaTimePoint> onUpdate,
            Action<MediaPlayer, long>? onPaused = null, Action<MediaPlayer, MediaTimePoint>? onSeek = null)
        {
            if (onUpdate is null) throw new ArgumentNullException(nameof(onUpdate));
            return _timeWatch = new TimeWatch(this, minPeriodUs, onUpdate, onPaused, onSeek);
        }

        private sealed unsafe class TimeWatch : IDisposable
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void OnPoint(IntPtr opaque, libvlc_media_player_time_point_t* value);
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void OnPaused(IntPtr opaque, long systemDateUs);

            private readonly MediaPlayer _mp;
            private readonly Action<MediaPlayer, MediaTimePoint> _onUpdate;
            private readonly Action<MediaPlayer, long>? _onPaused;
            private readonly Action<MediaPlayer, MediaTimePoint>? _onSeek;
            private readonly OnPoint _u;   // rooted for the watch's lifetime (libvlc holds the function pointers)
            private readonly OnPaused? _p;
            private readonly OnPoint? _s;
            private bool _disposed;

            public TimeWatch(MediaPlayer mp, long minPeriodUs, Action<MediaPlayer, MediaTimePoint> onUpdate,
                Action<MediaPlayer, long>? onPaused, Action<MediaPlayer, MediaTimePoint>? onSeek)
            {
                _mp = mp; _onUpdate = onUpdate; _onPaused = onPaused; _onSeek = onSeek;
                _u = Update;
                _p = onPaused is null ? null : Paused;
                _s = onSeek is null ? null : Seek;
                var cbs = new libvlc_media_player_watch_time_cbs
                {
                    version = 0,
                    on_update = Marshal.GetFunctionPointerForDelegate(_u),
                    on_paused = _p is null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(_p),
                    on_seek = _s is null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(_s),
                };
                if (libvlc_media_player_watch_time(mp, minPeriodUs, &cbs, IntPtr.Zero) != 0)
                    throw new InvalidOperationException("libvlc_media_player_watch_time failed (already watching?).");
            }

            private void Update(IntPtr _, libvlc_media_player_time_point_t* value) => _onUpdate(_mp, new MediaTimePoint(value));
            private void Paused(IntPtr _, long systemDateUs) => _onPaused?.Invoke(_mp, systemDateUs);
            private void Seek(IntPtr _, libvlc_media_player_time_point_t* value) => _onSeek?.Invoke(_mp, new MediaTimePoint(value));

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                libvlc_media_player_unwatch_time(_mp);   // guarantees no callback is in flight on return
                if (ReferenceEquals(_mp._timeWatch, this)) _mp._timeWatch = null;
                GC.KeepAlive(_u); GC.KeepAlive(_p); GC.KeepAlive(_s);
            }
        }

        /// <summary>Whether playback is currently active. <c>libvlc_media_player_is_playing</c>.</summary>
        public bool IsPlaying => libvlc_media_player_is_playing(this).ToBool();

        /// <summary>Current player state. <c>libvlc_media_player_get_state</c>.</summary>
        public State State => (State)libvlc_media_player_get_state(this);

        /// <summary>Media length in ms (-1 if unknown). <c>libvlc_media_player_get_length</c>.</summary>
        public long Length => libvlc_media_player_get_length(this);

        /// <summary>Playback time in ms. <c>libvlc_media_player_get_time</c> / <c>set_time</c> (fast=false).</summary>
        public long Time
        {
            get => libvlc_media_player_get_time(this);
            set => libvlc_media_player_set_time(this, value, 0);
        }

        /// <summary>Playback position in [0,1]. <c>libvlc_media_player_get_position</c> / <c>set_position</c>.</summary>
        public double Position
        {
            get => libvlc_media_player_get_position(this);
            set => libvlc_media_player_set_position(this, value, 0);
        }

        /// <summary>Audio volume in percent. <c>libvlc_audio_get_volume</c> / <c>set_volume</c>.</summary>
        public int Volume
        {
            get => libvlc_audio_get_volume(this);
            set => libvlc_audio_set_volume(this, value);
        }

        /// <summary>Audio mute state. <c>libvlc_audio_get_mute</c> / <c>set_mute</c>.</summary>
        public bool Mute
        {
            get => libvlc_audio_get_mute(this) > 0;
            set => libvlc_audio_set_mute(this, value ? 1 : 0);
        }

        private Equalizer? _equalizer;

        /// <summary>The audio equalizer (set-backed; libvlc exposes no getter). <c>libvlc_media_player_set_equalizer</c>.</summary>
        public Equalizer? Equalizer
        {
            get => _equalizer;
            set
            {
                _equalizer = value;
                libvlc_media_player_set_equalizer(this, value);
            }
        }

        private RendererItem? _renderer;

        /// <summary>The renderer target, e.g. a Chromecast (set-backed; libvlc exposes no getter).
        /// Must be set before playback. <c>libvlc_media_player_set_renderer</c>.</summary>
        public RendererItem? Renderer
        {
            get => _renderer;
            set
            {
                _renderer = value;
                libvlc_media_player_set_renderer(this, value);
            }
        }

        /// <summary>Starts/resumes playback. <c>libvlc_media_player_play</c>.</summary>
        public int Play() => libvlc_media_player_play(this);

        /// <summary>Toggles pause. <c>libvlc_media_player_pause</c>.</summary>
        public void Pause() => libvlc_media_player_pause(this);

        /// <summary>Sets pause state explicitly. <c>libvlc_media_player_set_pause</c>.</summary>
        public void Pause(bool pause) => libvlc_media_player_set_pause(this, pause ? 1 : 0);

        /// <summary>Requests asynchronous stop. <c>libvlc_media_player_stop_async</c>.</summary>
        public int Stop() => libvlc_media_player_stop_async(this);

        /// <summary>Steps to the next video frame (best effort). <c>libvlc_media_player_next_frame</c>.</summary>
        public void NextFrame() => libvlc_media_player_next_frame(this);

        /// <summary>Jumps by a relative time in ms (0 on success). <c>libvlc_media_player_jump_time</c>.</summary>
        public int JumpTime(long deltaMs) => libvlc_media_player_jump_time(this, deltaMs);

        /// <summary>Playback rate (1.0 = normal). <c>libvlc_media_player_get_rate</c> / <c>set_rate</c>.</summary>
        public float Rate
        {
            get => libvlc_media_player_get_rate(this);
            set => libvlc_media_player_set_rate(this, value);
        }

        /// <summary>Whether the input is seekable. <c>libvlc_media_player_is_seekable</c>.</summary>
        public bool IsSeekable => libvlc_media_player_is_seekable(this).ToBool();

        /// <summary>Whether pause is supported. <c>libvlc_media_player_can_pause</c>.</summary>
        public bool CanPause => libvlc_media_player_can_pause(this).ToBool();

        /// <summary>Whether the selected program is scrambled. <c>libvlc_media_player_program_scrambled</c>.</summary>
        public bool ProgramScrambled => libvlc_media_player_program_scrambled(this).ToBool();

        /// <summary>Number of active video outputs. <c>libvlc_media_player_has_vout</c>.</summary>
        public uint VideoOutputCount => libvlc_media_player_has_vout(this);

        /// <summary>Sends a navigation action (DVD/BD menus). <c>libvlc_media_player_navigate</c>.</summary>
        public void Navigate(NavigateMode mode) => libvlc_media_player_navigate(this, (uint)mode);

        /// <summary>Shows the title briefly at a screen position. <c>libvlc_media_player_set_video_title_display</c>.</summary>
        public void SetVideoTitleDisplay(Position position, uint timeoutMs) =>
            libvlc_media_player_set_video_title_display(this, (libvlc_position_t)position, timeoutMs);

        /// <summary>Enables/disables recording to <paramref name="dirPath"/>. <c>libvlc_media_player_record</c>.</summary>
        public void Record(bool enable, string dirPath)
        {
            using var u = new Utf8Buffer(dirPath);
            libvlc_media_player_record(this, enable.ToByte(), u);
        }

        // --- A-B loop ---

        /// <summary>Sets an A→B loop by time in ms (0 on success). <c>libvlc_media_player_set_abloop_time</c>.</summary>
        public int SetABLoopTime(long aTimeMs, long bTimeMs) => libvlc_media_player_set_abloop_time(this, aTimeMs, bTimeMs);

        /// <summary>Sets an A→B loop by position in [0,1] (0 on success). <c>libvlc_media_player_set_abloop_position</c>.</summary>
        public int SetABLoopPosition(double aPos, double bPos) => libvlc_media_player_set_abloop_position(this, aPos, bPos);

        /// <summary>Clears the A→B loop. <c>libvlc_media_player_reset_abloop</c>.</summary>
        public int ResetABLoop() => libvlc_media_player_reset_abloop(this);

        /// <summary>
        /// Reads the current A→B loop state and its A/B markers (time in ms and position in [0,1]).
        /// <c>libvlc_media_player_get_abloop</c>.
        /// </summary>
        public ABLoop GetABLoop(out long aTimeMs, out double aPos, out long bTimeMs, out double bPos)
        {
            long at, bt; double ap, bp;
            var state = libvlc_media_player_get_abloop(this, &at, &ap, &bt, &bp);
            aTimeMs = at; aPos = ap; bTimeMs = bt; bPos = bp;
            return (ABLoop)state;
        }

        // --- title & chapter ---

        /// <summary>Current title index. <c>libvlc_media_player_get_title</c> / <c>set_title</c>.</summary>
        public int Title
        {
            get => libvlc_media_player_get_title(this);
            set => libvlc_media_player_set_title(this, (uint)value); // libvlc 4.0 nightly 202606260430 changed the parameter to unsigned
        }

        /// <summary>Number of titles. <c>libvlc_media_player_get_title_count</c>.</summary>
        public int TitleCount => libvlc_media_player_get_title_count(this);

        /// <summary>Current chapter index. <c>libvlc_media_player_get_chapter</c> / <c>set_chapter</c>.</summary>
        public int Chapter
        {
            get => libvlc_media_player_get_chapter(this);
            set => libvlc_media_player_set_chapter(this, value);
        }

        /// <summary>Number of chapters in the current title. <c>libvlc_media_player_get_chapter_count</c>.</summary>
        public int ChapterCount => libvlc_media_player_get_chapter_count(this);

        /// <summary>Number of chapters in a given title. <c>libvlc_media_player_get_chapter_count_for_title</c>.</summary>
        public int GetChapterCountForTitle(int title) => libvlc_media_player_get_chapter_count_for_title(this, title);

        /// <summary>Seeks to the next chapter. <c>libvlc_media_player_next_chapter</c>.</summary>
        public void NextChapter() => libvlc_media_player_next_chapter(this);

        /// <summary>Seeks to the previous chapter. <c>libvlc_media_player_previous_chapter</c>.</summary>
        public void PreviousChapter() => libvlc_media_player_previous_chapter(this);

        // --- tracks & program ---

        /// <summary>Selects tracks of a type by comma-separated id string. <c>libvlc_media_player_select_tracks_by_ids</c>.</summary>
        public void SelectTracksByIds(TrackType type, string ids)
        {
            using var u = new Utf8Buffer(ids);
            libvlc_media_player_select_tracks_by_ids(this, (libvlc_track_type_t)type, u);
        }

        /// <summary>
        /// Selects a single track (unselecting the current track of that type). Routes through the
        /// stable <see cref="MediaTrack.Id"/> via <c>libvlc_media_player_select_tracks_by_ids</c>, so it
        /// works for tracks from any source (<see cref="GetTracks"/>, <see cref="GetSelectedTrack"/> or
        /// <see cref="GetTrack"/>).
        /// </summary>
        public void SelectTrack(MediaTrack track) => SelectTracksByIds(track.Type, track.Id);

        /// <summary>
        /// Selects multiple tracks of a type (pass none to clear that type's selection). Routes through
        /// the stable <see cref="MediaTrack.Id"/>s via <c>libvlc_media_player_select_tracks_by_ids</c>.
        /// Note: libvlc currently does not support selecting multiple audio tracks.
        /// </summary>
        public void SelectTracks(TrackType type, params MediaTrack[] tracks)
        {
            // "" unselects all tracks of that category (matches "pass none to clear").
            var ids = tracks is null || tracks.Length == 0
                ? string.Empty
                : string.Join(",", Array.ConvertAll(tracks, t => t.Id));
            SelectTracksByIds(type, ids);
        }

        /// <summary>Unselects all tracks of a type. <c>libvlc_media_player_unselect_track_type</c>.</summary>
        public void UnselectTrackType(TrackType type) =>
            libvlc_media_player_unselect_track_type(this, (libvlc_track_type_t)type);

        /// <summary>Selects a program by its group id. <c>libvlc_media_player_select_program_id</c>.</summary>
        public void SelectProgram(int groupId) => libvlc_media_player_select_program_id(this, groupId);

        /// <summary>
        /// The tracks of a given type as a new owned <see cref="MediaTrackList"/> (dispose it), or null
        /// if unavailable. <c>libvlc_media_player_get_tracklist</c>.
        /// </summary>
        /// <param name="type">Track kind to list.</param>
        /// <param name="selectedOnly">When true, only currently-selected tracks are returned.</param>
        public MediaTrackList? GetTracks(TrackType type, bool selectedOnly = false)
        {
            var list = libvlc_media_player_get_tracklist(this, (libvlc_track_type_t)type, selectedOnly.ToByte());
            return list == null ? null : new MediaTrackList((IntPtr)list);
        }

        /// <summary>The currently selected track of a type, or null. <c>libvlc_media_player_get_selected_track</c>.</summary>
        public MediaTrack? GetSelectedTrack(TrackType type)
        {
            var t = libvlc_media_player_get_selected_track(this, (libvlc_track_type_t)type);
            if (t == null) return null;
            // The pointer is released here; MediaTrack is a value snapshot that copies out all fields.
            try { return new MediaTrack(t); }
            finally { libvlc_media_track_release(t); }
        }

        /// <summary>The track with the given stable id (see <see cref="MediaTrack.Id"/>), or null. <c>libvlc_media_player_get_track_from_id</c>.</summary>
        public MediaTrack? GetTrack(string id)
        {
            using var u = new Utf8Buffer(id);
            var t = libvlc_media_player_get_track_from_id(this, u);
            if (t == null) return null;
            // The pointer is released here; MediaTrack is a value snapshot that copies out all fields.
            try { return new MediaTrack(t); }
            finally { libvlc_media_track_release(t); }
        }

        /// <summary>The program list as a new owned <see cref="ProgramList"/> (dispose it), or null. <c>libvlc_media_player_get_programlist</c>.</summary>
        public ProgramList? GetPrograms()
        {
            var list = libvlc_media_player_get_programlist(this);
            return list == null ? null : new ProgramList((IntPtr)list);
        }

        /// <summary>The currently selected program, or null. <c>libvlc_media_player_get_selected_program</c>.</summary>
        public Program? GetSelectedProgram()
        {
            var p = libvlc_media_player_get_selected_program(this);
            if (p == null) return null;
            try { return new Program(p); }
            finally { libvlc_player_program_delete(p); }
        }

        /// <summary>The program with the given group id, or null. <c>libvlc_media_player_get_program_from_id</c>.</summary>
        public Program? GetProgram(int groupId)
        {
            var p = libvlc_media_player_get_program_from_id(this, groupId);
            if (p == null) return null;
            try { return new Program(p); }
            finally { libvlc_player_program_delete(p); }
        }

        /// <summary>Adds an input slave (subtitle/audio) by URI (0 on success). <c>libvlc_media_player_add_slave</c>.</summary>
        public int AddSlave(MediaSlaveType type, string uri, bool select)
        {
            using var u = new Utf8Buffer(uri);
            return libvlc_media_player_add_slave(this, (libvlc_media_slave_type_t)type, u, select.ToByte());
        }

        // --- audio ---

        /// <summary>Toggles mute. <c>libvlc_audio_toggle_mute</c>.</summary>
        public void ToggleMute() => libvlc_audio_toggle_mute(this);

        /// <summary>Audio delay in microseconds. <c>libvlc_audio_get_delay</c> / <c>set_delay</c>.</summary>
        public long AudioDelay
        {
            get => libvlc_audio_get_delay(this);
            set => libvlc_audio_set_delay(this, value);
        }

        /// <summary>Audio stereo mode. <c>libvlc_audio_get_stereomode</c> / <c>set_stereomode</c>.</summary>
        public AudioOutputStereomode AudioStereoMode
        {
            get => (AudioOutputStereomode)libvlc_audio_get_stereomode(this);
            set => libvlc_audio_set_stereomode(this, (libvlc_audio_output_stereomode_t)value);
        }

        /// <summary>Audio mix mode. <c>libvlc_audio_get_mixmode</c> / <c>set_mixmode</c>.</summary>
        public AudioOutputMixmode AudioMixMode
        {
            get => (AudioOutputMixmode)libvlc_audio_get_mixmode(this);
            set => libvlc_audio_set_mixmode(this, (libvlc_audio_output_mixmode_t)value);
        }

        /// <summary>Selects the audio output module by name (0 on success). <c>libvlc_audio_output_set</c>.</summary>
        public int SetAudioOutput(string name)
        {
            using var u = new Utf8Buffer(name);
            return libvlc_audio_output_set(this, u);
        }

        /// <summary>Selects the audio output device by id (0 on success). <c>libvlc_audio_output_device_set</c>.</summary>
        public int SetAudioOutputDevice(string deviceId)
        {
            using var u = new Utf8Buffer(deviceId);
            return libvlc_audio_output_device_set(this, u);
        }

        /// <summary>Media role hint for the audio policy. <c>libvlc_media_player_get_role</c> / <c>set_role</c>.</summary>
        public MediaPlayerRole Role
        {
            get => (MediaPlayerRole)libvlc_media_player_get_role(this);
            set => libvlc_media_player_set_role(this, (uint)value);
        }

        // --- video ---

        /// <summary>Video scale factor (0 = fit window). <c>libvlc_video_get_scale</c> / <c>set_scale</c>.</summary>
        public float Scale
        {
            get => libvlc_video_get_scale(this);
            set => libvlc_video_set_scale(this, value);
        }

        /// <summary>Sets the aspect ratio (e.g. "16:9"; null to reset). <c>libvlc_video_set_aspect_ratio</c>.</summary>
        public void SetAspectRatio(string aspect)
        {
            using var u = new Utf8Buffer(aspect);
            libvlc_video_set_aspect_ratio(this, u);
        }

        /// <summary>How the video fits the display. <c>libvlc_video_get_display_fit</c> / <c>set_display_fit</c>.</summary>
        public VideoFitMode DisplayFit
        {
            get => (VideoFitMode)libvlc_video_get_display_fit(this);
            set => libvlc_video_set_display_fit(this, (libvlc_video_fit_mode_t)value);
        }

        /// <summary>Video stereoscopic mode. <c>libvlc_video_get_video_stereo_mode</c> / <c>set_video_stereo_mode</c>.</summary>
        public VideoStereoMode VideoStereoMode
        {
            get => (VideoStereoMode)libvlc_video_get_video_stereo_mode(this);
            set => libvlc_video_set_video_stereo_mode(this, (libvlc_video_stereo_mode_t)value);
        }

        /// <summary>Enables/disables keyboard input handling on the video. <c>libvlc_video_set_key_input</c>.</summary>
        public void SetKeyInput(bool on) => libvlc_video_set_key_input(this, on ? 1u : 0u);

        /// <summary>Enables/disables mouse input handling on the video. <c>libvlc_video_set_mouse_input</c>.</summary>
        public void SetMouseInput(bool on) => libvlc_video_set_mouse_input(this, on ? 1u : 0u);

        /// <summary>Gets the pixel size of video output <paramref name="num"/> (true on success). <c>libvlc_video_get_size</c>.</summary>
        public bool GetVideoSize(uint num, out uint width, out uint height)
        {
            uint w, h;
            int ret = libvlc_video_get_size(this, num, &w, &h);
            width = w;
            height = h;
            return ret == 0;
        }

        /// <summary>Gets the mouse cursor position over video output <paramref name="num"/> (true on success). <c>libvlc_video_get_cursor</c>.</summary>
        public bool GetCursor(uint num, out int x, out int y)
        {
            int px, py;
            int ret = libvlc_video_get_cursor(this, num, &px, &py);
            x = px;
            y = py;
            return ret == 0;
        }

        /// <summary>Subtitle (SPU) delay in microseconds. <c>libvlc_video_get_spu_delay</c> / <c>set_spu_delay</c>.</summary>
        public long SpuDelay
        {
            get => libvlc_video_get_spu_delay(this);
            set => libvlc_video_set_spu_delay(this, value);
        }

        /// <summary>Subtitle text scale factor (1.0 = normal). <c>libvlc_video_get_spu_text_scale</c> / <c>set_spu_text_scale</c>.</summary>
        public float SpuTextScale
        {
            get => libvlc_video_get_spu_text_scale(this);
            set => libvlc_video_set_spu_text_scale(this, value);
        }

        /// <summary>Teletext page (0 = off). <c>libvlc_video_get_teletext</c> / <c>set_teletext</c>.</summary>
        public int Teletext
        {
            get => libvlc_video_get_teletext(this);
            set => libvlc_video_set_teletext(this, value);
        }

        /// <summary>Teletext background transparency. <c>libvlc_video_get_teletext_transparency</c> / <c>set_teletext_transparency</c>.</summary>
        public bool TeletextTransparency
        {
            get => libvlc_video_get_teletext_transparency(this).ToBool();
            set => libvlc_video_set_teletext_transparency(this, value.ToByte());
        }

        /// <summary>Saves a snapshot of video output <paramref name="num"/> to a file (0/auto for width/height keeps aspect; 0 on success). <c>libvlc_video_take_snapshot</c>.</summary>
        public int TakeSnapshot(uint num, string filePath, uint width, uint height)
        {
            using var u = new Utf8Buffer(filePath);
            return libvlc_video_take_snapshot(this, num, u, width, height);
        }

        /// <summary>Sets deinterlacing (state: -1 auto, 0 off, 1 on; mode e.g. "blend"; 0 on success). <c>libvlc_video_set_deinterlace</c>.</summary>
        public int SetDeinterlace(int state, string mode)
        {
            using var u = new Utf8Buffer(mode);
            return libvlc_video_set_deinterlace(this, state, u);
        }

        /// <summary>Crops to a num:den ratio (0/0 to reset). <c>libvlc_video_set_crop_ratio</c>.</summary>
        public void SetCropRatio(uint num, uint den) => libvlc_video_set_crop_ratio(this, num, den);

        /// <summary>Crops to a pixel window. <c>libvlc_video_set_crop_window</c>.</summary>
        public void SetCropWindow(uint x, uint y, uint width, uint height) =>
            libvlc_video_set_crop_window(this, x, y, width, height);

        /// <summary>Crops by border thickness in pixels. <c>libvlc_video_set_crop_border</c>.</summary>
        public void SetCropBorder(uint left, uint right, uint top, uint bottom) =>
            libvlc_video_set_crop_border(this, left, right, top, bottom);

        /// <summary>Sets the 360°/spherical projection mode. <c>libvlc_video_set_projection_mode</c>.</summary>
        public void SetProjectionMode(VideoProjection mode) =>
            libvlc_video_set_projection_mode(this, (libvlc_video_projection_t)mode);

        /// <summary>Resets the projection mode to the media default. <c>libvlc_video_unset_projection_mode</c>.</summary>
        public void UnsetProjectionMode() => libvlc_video_unset_projection_mode(this);

        // --- drawable / window ---

        /// <summary>Native window handle for video output (Windows HWND). <c>libvlc_media_player_get_hwnd</c> / <c>set_hwnd</c>.</summary>
        public IntPtr Hwnd
        {
            get => libvlc_media_player_get_hwnd(this);
            set => libvlc_media_player_set_hwnd(this, value);
        }

        /// <summary>macOS NSView/CALayer handle. <c>libvlc_media_player_get_nsobject</c> / <c>set_nsobject</c>.</summary>
        public IntPtr NSObject
        {
            get => libvlc_media_player_get_nsobject(this);
            set => libvlc_media_player_set_nsobject(this, value);
        }

        /// <summary>X11 window id (0 = none). <c>libvlc_media_player_get_xwindow</c> / <c>set_xwindow</c>.</summary>
        public uint XWindow
        {
            get => libvlc_media_player_get_xwindow(this);
            set => libvlc_media_player_set_xwindow(this, value);
        }

        /// <summary>Sets the Android <c>AWindowHandler</c>. <c>libvlc_media_player_set_android_context</c>.</summary>
        public void SetAndroidContext(IntPtr aWindowHandler) => libvlc_media_player_set_android_context(this, aWindowHandler);

        // --- lifetime / threading (software-rendering callbacks) ---

        /// <summary>Locks the player for direct rendering callbacks. <c>libvlc_media_player_lock</c>.</summary>
        public void Lock() => libvlc_media_player_lock(this);

        /// <summary>Unlocks the player. <c>libvlc_media_player_unlock</c>.</summary>
        public void Unlock() => libvlc_media_player_unlock(this);

        /// <summary>Signals the player's condition variable. <c>libvlc_media_player_signal</c>.</summary>
        public void Signal() => libvlc_media_player_signal(this);

        /// <summary>Waits on the player's condition variable (between <see cref="Lock"/>/<see cref="Unlock"/>). <c>libvlc_media_player_wait</c>.</summary>
        public void Wait() => libvlc_media_player_wait(this);

        // --- audio output device / format ---

        /// <summary>Current audio output device id, or null. <c>libvlc_audio_output_device_get</c>.</summary>
        public string? AudioOutputDevice => ((IntPtr)libvlc_audio_output_device_get(this)).GetUtf8(true);

        /// <summary>Sets the PCM format for the audio callbacks output. <c>libvlc_audio_set_format</c>.</summary>
        public void SetAudioFormat(string fourcc, uint rate, uint channels)
        {
            using var u = new Utf8Buffer(fourcc);
            libvlc_audio_set_format(this, u, rate, channels);
        }

        /// <summary>
        /// Lazily enumerates the devices of the current audio output module.
        /// <c>libvlc_audio_output_device_enum</c>. The native list is fetched when enumeration begins
        /// and released when it completes or the enumerator is disposed — enumerate fully (e.g.
        /// <c>foreach</c> / <c>ToList()</c>) so the list is freed.
        /// </summary>
        public IEnumerable<AudioOutputDevice> EnumAudioOutputDevices()
        {
            IntPtr head;
            unsafe { head = (IntPtr)libvlc_audio_output_device_enum(this); }
            if (head == IntPtr.Zero) yield break;
            try
            {
                for (var cur = head; cur != IntPtr.Zero;)
                {
                    AudioOutputDevice device;
                    IntPtr next;
                    unsafe
                    {
                        var d = (libvlc_audio_output_device_t*)cur;
                        device = new AudioOutputDevice(((IntPtr)d->psz_device).GetUtf8(), ((IntPtr)d->psz_description).GetUtf8());
                        next = (IntPtr)d->p_next;
                    }
                    yield return device;
                    cur = next;
                }
            }
            finally
            {
                unsafe { libvlc_audio_output_device_list_release((libvlc_audio_output_device_t*)head); }
            }
        }


        // --- video adjustments / overlays ---

        /// <summary>Reads a float video-adjust value. <c>libvlc_video_get_adjust_float</c>.</summary>
        public float GetVideoAdjustFloat(VideoAdjustOption option) => libvlc_video_get_adjust_float(this, (uint)option);

        /// <summary>Sets a float video-adjust value (enable adjustments first). <c>libvlc_video_set_adjust_float</c>.</summary>
        public void SetVideoAdjustFloat(VideoAdjustOption option, float value) => libvlc_video_set_adjust_float(this, (uint)option, value);

        /// <summary>Reads an int video-adjust value (e.g. Enable). <c>libvlc_video_get_adjust_int</c>.</summary>
        public int GetVideoAdjustInt(VideoAdjustOption option) => libvlc_video_get_adjust_int(this, (uint)option);

        /// <summary>Sets an int video-adjust value (e.g. Enable). <c>libvlc_video_set_adjust_int</c>.</summary>
        public void SetVideoAdjustInt(VideoAdjustOption option, int value) => libvlc_video_set_adjust_int(this, (uint)option, value);

        /// <summary>Current aspect ratio (e.g. "16:9"), or null. <c>libvlc_video_get_aspect_ratio</c>.</summary>
        public string? AspectRatio => ((IntPtr)libvlc_video_get_aspect_ratio(this)).GetUtf8(true);

        /// <summary>Current deinterlace mode, or null. <c>libvlc_video_get_deinterlace</c>.</summary>
        public string? GetDeinterlace()
        {
            byte* mode = null;
            libvlc_video_get_deinterlace(this, &mode);
            return ((IntPtr)mode).GetUtf8(true);
        }

        /// <summary>Reads a logo option. <c>libvlc_video_get_logo_int</c>.</summary>
        public int GetLogo(VideoLogoOption option) => libvlc_video_get_logo_int(this, (uint)option);

        /// <summary>Sets an integer logo option. <c>libvlc_video_set_logo_int</c>.</summary>
        public void SetLogo(VideoLogoOption option, int value) => libvlc_video_set_logo_int(this, (uint)option, value);

        /// <summary>Sets a string logo option (e.g. the logo file path). <c>libvlc_video_set_logo_string</c>.</summary>
        public void SetLogo(VideoLogoOption option, string value)
        {
            using var u = new Utf8Buffer(value);
            libvlc_video_set_logo_string(this, (uint)option, u);
        }

        /// <summary>Reads a marquee option. <c>libvlc_video_get_marquee_int</c>.</summary>
        public int GetMarquee(VideoMarqueeOption option) => libvlc_video_get_marquee_int(this, (uint)option);

        /// <summary>Sets an integer marquee option. <c>libvlc_video_set_marquee_int</c>.</summary>
        public void SetMarquee(VideoMarqueeOption option, int value) => libvlc_video_set_marquee_int(this, (uint)option, value);

        /// <summary>Sets a string marquee option (e.g. the displayed text). <c>libvlc_video_set_marquee_string</c>.</summary>
        public void SetMarquee(VideoMarqueeOption option, string value)
        {
            using var u = new Utf8Buffer(value);
            libvlc_video_set_marquee_string(this, (uint)option, u);
        }

        /// <summary>Sets the software-rendering (vmem) pixel format. <c>libvlc_video_set_format</c>.</summary>
        public void SetVideoFormat(string chroma, uint width, uint height, uint pitch)
        {
            using var u = new Utf8Buffer(chroma);
            libvlc_video_set_format(this, u, width, height, pitch);
        }

        /// <summary>Updates the 360°/spherical viewpoint (0 on success). <c>libvlc_video_update_viewpoint</c>.</summary>
        /// <param name="viewpoint">The new viewpoint.</param>
        /// <param name="absolute">True to set the absolute viewpoint, false to apply it relative to the current one.</param>
        public int UpdateViewpoint(in Viewpoint viewpoint, bool absolute = true)
        {
            var vp = new libvlc_video_viewpoint_t
            {
                f_yaw = viewpoint.Yaw,
                f_pitch = viewpoint.Pitch,
                f_roll = viewpoint.Roll,
                f_field_of_view = viewpoint.FieldOfView,
            };
            return libvlc_video_update_viewpoint(this, &vp, absolute.ToByte());
        }

        // --- fullscreen ---

        /// <summary>Fullscreen state (output-dependent; no-op under callback rendering). <c>libvlc_get_fullscreen</c> / <c>set_fullscreen</c>.</summary>
        public bool Fullscreen
        {
            get => libvlc_get_fullscreen(this).ToBool();
            set => libvlc_set_fullscreen(this, value.ToByte());
        }

        /// <summary>Toggles fullscreen. <c>libvlc_toggle_fullscreen</c>.</summary>
        public void ToggleFullscreen() => libvlc_toggle_fullscreen(this);

        // --- title / chapter descriptions ---

        /// <summary>Full title descriptions for the current media (count known + indexable). <c>libvlc_media_player_get_full_title_descriptions</c>.</summary>
        public IReadOnlyList<TitleDescription> GetTitleDescriptions()
        {
            libvlc_title_description_t** titles;
            int n = libvlc_media_player_get_full_title_descriptions(this, &titles);
            if (n <= 0) return Array.Empty<TitleDescription>();
            try
            {
                var result = new TitleDescription[n];
                for (int i = 0; i < n; i++)
                {
                    var t = titles[i];
                    result[i] = new TitleDescription(((IntPtr)t->psz_name).GetUtf8(), t->i_duration, t->i_flags);
                }
                return result;
            }
            finally { libvlc_title_descriptions_release(titles, (uint)n); }
        }

        /// <summary>Full chapter descriptions for a title (-1 = current title; count known + indexable). <c>libvlc_media_player_get_full_chapter_descriptions</c>.</summary>
        public IReadOnlyList<ChapterDescription> GetChapterDescriptions(int titleIndex = -1)
        {
            libvlc_chapter_description_t** chapters;
            int n = libvlc_media_player_get_full_chapter_descriptions(this, titleIndex, &chapters);
            if (n <= 0) return Array.Empty<ChapterDescription>();
            try
            {
                var result = new ChapterDescription[n];
                for (int i = 0; i < n; i++)
                {
                    var c = chapters[i];
                    result[i] = new ChapterDescription(((IntPtr)c->psz_name).GetUtf8(), c->i_time_offset, c->i_duration);
                }
                return result;
            }
            finally { libvlc_chapter_descriptions_release(chapters, (uint)n); }
        }

        // --- rendering / output callbacks ---
        // The delegates are rooted in fields for as long as they are installed; libvlc holds their
        // function pointers and calls them on its own threads. Install before playback.

        // The caller's alias delegates are rooted directly in these fields for as long as they are
        // installed. The call sites marshal each to a native function pointer via ToFunctionPointer();
        // the alias delegates carry [UnmanagedFunctionPointer(Cdecl)], so no interop-twin conversion
        // is needed.
        private AudioPlayCallback? _audioPlay;
        private AudioPauseCallback? _audioPause;
        private AudioResumeCallback? _audioResume;
        private AudioFlushCallback? _audioFlush;
        private AudioDrainCallback? _audioDrain;
        private AudioSetVolumeCallback? _audioSetVolume;
        private AudioSetupCallback? _audioSetup;
        private AudioCleanupCallback? _audioCleanup;
        private VideoLockCallback? _videoLock;
        private VideoUnlockCallback? _videoUnlock;
        private VideoDisplayCallback? _videoDisplay;
        private VideoFormatCallback? _videoFormatSetup;
        private VideoCleanupCallback? _videoFormatCleanup;
        private VideoOutputSetupCallback? _outSetup;
        private VideoOutputCleanupCallback? _outCleanup;
        private VideoOutputSetWindowCallback? _outWindow;
        private VideoUpdateOutputCallback? _outUpdate;
        private VideoSwapCallback? _outSwap;
        private VideoMakeCurrentCallback? _outMakeCurrent;
        private VideoGetProcAddressCallback? _outGetProc;
        private VideoFrameMetadataCallback? _outMetadata;
        private VideoOutputSelectPlaneCallback? _outSelectPlane;

        /// <summary>Installs PCM audio output callbacks. <c>libvlc_audio_set_callbacks</c>.</summary>
        public void SetAudioCallbacks(AudioPlayCallback play, AudioPauseCallback? pause = null,
            AudioResumeCallback? resume = null, AudioFlushCallback? flush = null, AudioDrainCallback? drain = null,
            IntPtr opaque = default)
        {
            _audioPlay = play;
            _audioPause = pause;
            _audioResume = resume;
            _audioFlush = flush;
            _audioDrain = drain;
            libvlc_audio_set_callbacks(this, _audioPlay.ToFunctionPointer(), _audioPause.ToFunctionPointer(),
                _audioResume.ToFunctionPointer(), _audioFlush.ToFunctionPointer(), _audioDrain.ToFunctionPointer(), opaque);
        }

        /// <summary>Installs the audio volume/mute callback. <c>libvlc_audio_set_volume_callback</c>.</summary>
        public void SetVolumeCallback(AudioSetVolumeCallback setVolume)
        {
            _audioSetVolume = setVolume;
            libvlc_audio_set_volume_callback(this, _audioSetVolume.ToFunctionPointer());
        }

        /// <summary>Installs the audio format setup/cleanup callbacks. <c>libvlc_audio_set_format_callbacks</c>.</summary>
        public void SetAudioFormatCallbacks(AudioSetupCallback? setup, AudioCleanupCallback? cleanup)
        {
            _audioSetup = setup;
            _audioCleanup = cleanup;
            libvlc_audio_set_format_callbacks(this, _audioSetup.ToFunctionPointer(), _audioCleanup.ToFunctionPointer());
        }

        /// <summary>Installs software-rendering (vmem) video callbacks. <c>libvlc_video_set_callbacks</c>.</summary>
        public void SetVideoCallbacks(VideoLockCallback @lock, VideoUnlockCallback? unlock = null,
            VideoDisplayCallback? display = null, IntPtr opaque = default)
        {
            _videoLock = @lock;
            _videoUnlock = unlock;
            _videoDisplay = display;
            libvlc_video_set_callbacks(this, _videoLock.ToFunctionPointer(), _videoUnlock.ToFunctionPointer(),
                _videoDisplay.ToFunctionPointer(), opaque);
        }

        /// <summary>Installs the vmem video format setup/cleanup callbacks. <c>libvlc_video_set_format_callbacks</c>.</summary>
        public void SetVideoFormatCallbacks(VideoFormatCallback? setup, VideoCleanupCallback? cleanup)
        {
            _videoFormatSetup = setup;
            _videoFormatCleanup = cleanup;
            libvlc_video_set_format_callbacks(this, _videoFormatSetup.ToFunctionPointer(), _videoFormatCleanup.ToFunctionPointer());
        }

        /// <summary>
        /// Installs accelerated video output callbacks for a rendering engine (D3D11/D3D9/OpenGL/...).
        /// Returns true on success. Any callback may be null. <c>libvlc_video_set_output_callbacks</c>.
        /// </summary>
        public bool SetOutputCallbacks(VideoEngine engine,
            VideoOutputSetupCallback? setup, VideoOutputCleanupCallback? cleanup,
            VideoOutputSetWindowCallback? window, VideoUpdateOutputCallback? updateOutput,
            VideoSwapCallback? swap, VideoMakeCurrentCallback? makeCurrent,
            VideoGetProcAddressCallback? getProcAddress, VideoFrameMetadataCallback? metadata,
            VideoOutputSelectPlaneCallback? selectPlane, IntPtr opaque = default)
        {
            _outSetup = setup;
            _outCleanup = cleanup;
            _outWindow = window;
            _outUpdate = updateOutput;
            _outSwap = swap;
            _outMakeCurrent = makeCurrent;
            _outGetProc = getProcAddress;
            _outMetadata = metadata;
            _outSelectPlane = selectPlane;
            return libvlc_video_set_output_callbacks(this, (libvlc_video_engine_t)engine,
                _outSetup.ToFunctionPointer(), _outCleanup.ToFunctionPointer(), _outWindow.ToFunctionPointer(),
                _outUpdate.ToFunctionPointer(), _outSwap.ToFunctionPointer(), _outMakeCurrent.ToFunctionPointer(),
                _outGetProc.ToFunctionPointer(), _outMetadata.ToFunctionPointer(), _outSelectPlane.ToFunctionPointer(), opaque).ToBool();
        }
        // GCHandle handed to libvlc as the cbs opaque; its target is wired to `this` after construction.
        // default (not allocated) for instances that merely wrap an existing handle — they have no
        // callbacks (libvlc 4.0 cannot register them after creation) so subscribing throws.
        private GCHandle _cbsHandle;

        /// <summary>Creates a media player bound to <paramref name="vlc"/>, with events wired. <c>libvlc_media_player_new</c>.</summary>
        public MediaPlayer(LibVLC vlc) : this(vlc, (MediaPlayerCallbacks?)null) { }

        /// <summary>Creates a media player, pre-subscribing the handlers in <paramref name="callbacks"/> (a batch over <c>+=</c>). <c>libvlc_media_player_new</c>.</summary>
        public MediaPlayer(LibVLC vlc, MediaPlayerCallbacks? callbacks) : this(Create(vlc, null)) => Subscribe(callbacks);

        /// <summary>Creates a media player from <paramref name="media"/>, with events wired. <c>libvlc_media_player_new_from_media</c>.</summary>
        public MediaPlayer(LibVLC vlc, Media media, MediaPlayerCallbacks? callbacks = null) : this(Create(vlc, media)) => Subscribe(callbacks);

        private MediaPlayer(Creation c) : base(c.Handle)
        {
            _cbsHandle = c.Opaque;
            _cbsHandle.Target = this; // route the opaque to this instance now that it exists
        }

        private readonly struct Creation
        {
            public readonly IntPtr Handle;
            public readonly GCHandle Opaque;
            public Creation(IntPtr handle, GCHandle opaque) { Handle = handle; Opaque = opaque; }
        }

        private static Creation Create(LibVLC vlc, Media? media)
        {
            if (vlc is null) throw new ArgumentNullException(nameof(vlc));
            var gch = GCHandle.Alloc(null); // target set in the ctor once the player exists
            var cbs = s_cbs;                 // local copy; libvlc reads it during the call
            IntPtr opaque = GCHandle.ToIntPtr(gch);
            IntPtr handle = (IntPtr)(media is null
                ? libvlc_media_player_new(vlc, &cbs, opaque)
                : libvlc_media_player_new_from_media(vlc, media, &cbs, opaque));
            if (handle == IntPtr.Zero) gch.Free(); // base ctor will throw on the null handle
            return new Creation(handle, gch);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timeWatch?.Dispose();          // unwatch_time before release (needs the player alive)
            base.Dispose(disposing);                       // release the player; after this libvlc won't call back
            if (_cbsHandle.IsAllocated) _cbsHandle.Free(); // then free the opaque GCHandle
        }

        private void Subscribe(MediaPlayerCallbacks? c)
        {
            if (c is null) return;
            MediaChanged += c.MediaChanged; MediaStopping += c.MediaStopping; StateChanged += c.StateChanged;
            BufferingChanged += c.BufferingChanged; CapabilitiesChanged += c.CapabilitiesChanged;
            PositionChanged += c.PositionChanged; LengthChanged += c.LengthChanged;
            TrackListChanged += c.TrackListChanged; TrackSelectionChanged += c.TrackSelectionChanged;
            ProgramListChanged += c.ProgramListChanged; ProgramSelectionChanged += c.ProgramSelectionChanged;
            TitlesChanged += c.TitlesChanged; TitleSelectionChanged += c.TitleSelectionChanged;
            ChapterSelectionChanged += c.ChapterSelectionChanged; RecordingChanged += c.RecordingChanged;
            ScreenshotTaken += c.ScreenshotTaken; MediaParsed += c.MediaParsed; MediaMetaChanged += c.MediaMetaChanged;
            MediaSubitemsChanged += c.MediaSubitemsChanged; MediaAttachmentsAdded += c.MediaAttachmentsAdded;
            VoutChanged += c.VoutChanged; CorkChanged += c.CorkChanged; AudioVolumeChanged += c.AudioVolumeChanged;
            AudioMuteChanged += c.AudioMuteChanged; AudioDeviceChanged += c.AudioDeviceChanged;
        }

        // --- events (field-like; raised directly from the static dispatch methods; payload structs => no boxing) ---
        // Every publicly-creatable MediaPlayer (constructor / LibVLC.CreateMediaPlayer()) has callbacks wired,
        // so these always fire. The only instances without callbacks come from the internal MediaPlayer(IntPtr)
        // wrapper (libvlc 4.0 cannot register callbacks on an existing handle); their events stay silent until
        // the MediaListPlayer phase wires them via libvlc_media_list_player_new(cbs).

        /// <summary><c>on_media_changed</c> — the played media changed.</summary>
        public event EventHandler<MediaEventArgs>? MediaChanged;
        /// <summary><c>on_media_stopping</c> — the current media is stopping (carries the reason).</summary>
        public event EventHandler<MediaStoppingEventArgs>? MediaStopping;
        /// <summary><c>on_state_changed</c> — the player state changed.</summary>
        public event EventHandler<StateChangedEventArgs>? StateChanged;
        /// <summary><c>on_buffering_changed</c> — buffering progress in [0, 100].</summary>
        public event EventHandler<BufferingEventArgs>? BufferingChanged;
        /// <summary><c>on_capabilities_changed</c> — seek/pause/rate/rewind capabilities changed.</summary>
        public event EventHandler<CapabilitiesChangedEventArgs>? CapabilitiesChanged;
        /// <summary><c>on_position_changed</c> — playback time (ms) and position [0,1] changed (high frequency).</summary>
        public event EventHandler<PositionChangedEventArgs>? PositionChanged;
        /// <summary><c>on_length_changed</c> — media length (ms) changed.</summary>
        public event EventHandler<LengthChangedEventArgs>? LengthChanged;
        /// <summary><c>on_track_list_changed</c> — a track was added/removed/updated.</summary>
        public event EventHandler<TrackListChangedEventArgs>? TrackListChanged;
        /// <summary><c>on_track_selection_changed</c> — the selected track of a type changed.</summary>
        public event EventHandler<TrackSelectionChangedEventArgs>? TrackSelectionChanged;
        /// <summary><c>on_program_list_changed</c> — a program was added/removed/updated.</summary>
        public event EventHandler<ProgramListChangedEventArgs>? ProgramListChanged;
        /// <summary><c>on_program_selection_changed</c> — the selected program changed.</summary>
        public event EventHandler<ProgramSelectionChangedEventArgs>? ProgramSelectionChanged;
        /// <summary><c>on_titles_changed</c> — the title list changed.</summary>
        public event EventHandler? TitlesChanged;
        /// <summary><c>on_title_selection_changed</c> — the selected title changed.</summary>
        public event EventHandler<TitleSelectionChangedEventArgs>? TitleSelectionChanged;
        /// <summary><c>on_chapter_selection_changed</c> — the selected chapter changed.</summary>
        public event EventHandler<ChapterSelectionChangedEventArgs>? ChapterSelectionChanged;
        /// <summary><c>on_recording_changed</c> — recording started/stopped.</summary>
        public event EventHandler<RecordingChangedEventArgs>? RecordingChanged;
        /// <summary><c>on_screenshot_taken</c> — a screenshot was written.</summary>
        public event EventHandler<ScreenshotTakenEventArgs>? ScreenshotTaken;
        /// <summary><c>on_media_parsed</c> — the media finished parsing.</summary>
        public event EventHandler<MediaEventArgs>? MediaParsed;
        /// <summary><c>on_media_meta_changed</c> — a metadata field of the media changed.</summary>
        public event EventHandler<MediaEventArgs>? MediaMetaChanged;
        /// <summary><c>on_media_subitems_changed</c> — the media's subitems changed.</summary>
        public event EventHandler<MediaEventArgs>? MediaSubitemsChanged;
        /// <summary><c>on_media_attachments_added</c> — attachment pictures were found.</summary>
        public event EventHandler<MediaAttachmentsAddedEventArgs>? MediaAttachmentsAdded;
        /// <summary><c>on_vout_changed</c> — the number of active video outputs changed.</summary>
        public event EventHandler<VoutChangedEventArgs>? VoutChanged;
        /// <summary><c>on_cork_changed</c> — playback was corked/uncorked (e.g. by another app).</summary>
        public event EventHandler<CorkChangedEventArgs>? CorkChanged;
        /// <summary><c>on_audio_volume_changed</c> — the audio volume changed.</summary>
        public event EventHandler<AudioVolumeChangedEventArgs>? AudioVolumeChanged;
        /// <summary><c>on_audio_mute_changed</c> — the audio mute state changed.</summary>
        public event EventHandler<AudioMuteChangedEventArgs>? AudioMuteChanged;
        /// <summary><c>on_audio_device_changed</c> — the audio output device changed.</summary>
        public event EventHandler<AudioDeviceChangedEventArgs>? AudioDeviceChanged;

        // --- shared callback registration: one cbs struct + one set of delegates for the whole process ---

        private static MediaPlayer? Owner(IntPtr opaque) =>
            opaque == IntPtr.Zero ? null : GCHandle.FromIntPtr(opaque).Target as MediaPlayer;

        // cdecl delegate types matching the libvlc_media_player_cbs function-pointer fields.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DVoid(IntPtr o);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DMedia(IntPtr o, libvlc_media_t* m);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DMediaStopping(IntPtr o, libvlc_media_t* m, libvlc_stopping_reason_t r);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DState(IntPtr o, libvlc_state_t s);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DFloat(IntPtr o, float v);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DCaps(IntPtr o, libvlc_capability_t a, libvlc_capability_t b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DPosition(IntPtr o, long t, double p);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DTime(IntPtr o, long t);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DTrackList(IntPtr o, libvlc_list_action_t a, libvlc_track_type_t t, byte* id);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DTrackSel(IntPtr o, libvlc_track_type_t t, byte* u, byte* s);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DProgramList(IntPtr o, libvlc_list_action_t a, int id);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DIntInt(IntPtr o, int a, int b);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DTitleSel(IntPtr o, libvlc_title_description_t* t, uint i);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DChapterSel(IntPtr o, libvlc_title_description_t* t, uint ti, libvlc_chapter_description_t* c, uint ci);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DRecording(IntPtr o, byte rec, byte* path);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DString(IntPtr o, byte* s);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DAttachments(IntPtr o, libvlc_media_t* m, libvlc_picture_list_t* l);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DUInt(IntPtr o, uint v);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DBool(IntPtr o, byte v);

        // Delegate instances rooted for the process lifetime (libvlc holds their function pointers).
        private static readonly DMedia s_mediaChanged = (o, m) => { var p = Owner(o); p?.MediaChanged?.Invoke(p, new MediaEventArgs((IntPtr)m)); };
        private static readonly DMediaStopping s_mediaStopping = (o, m, r) => { var p = Owner(o); p?.MediaStopping?.Invoke(p, new MediaStoppingEventArgs((IntPtr)m, (StoppingReason)r)); };
        private static readonly DState s_stateChanged = (o, s) => { var p = Owner(o); p?.StateChanged?.Invoke(p, new StateChangedEventArgs((State)s)); };
        private static readonly DFloat s_bufferingChanged = (o, v) => { var p = Owner(o); p?.BufferingChanged?.Invoke(p, new BufferingEventArgs(v)); };
        private static readonly DCaps s_capabilitiesChanged = (o, a, b) => { var p = Owner(o); p?.CapabilitiesChanged?.Invoke(p, new CapabilitiesChangedEventArgs((Capability)a, (Capability)b)); };
        private static readonly DPosition s_positionChanged = (o, t, pos) => { var p = Owner(o); p?.PositionChanged?.Invoke(p, new PositionChangedEventArgs(t, pos)); };
        private static readonly DTime s_lengthChanged = (o, t) => { var p = Owner(o); p?.LengthChanged?.Invoke(p, new LengthChangedEventArgs(t)); };
        private static readonly DTrackList s_trackListChanged = (o, a, t, id) => { var p = Owner(o); p?.TrackListChanged?.Invoke(p, new TrackListChangedEventArgs((ListAction)a, (TrackType)t, ((IntPtr)id).GetUtf8())); };
        private static readonly DTrackSel s_trackSelectionChanged = (o, t, u, s) => { var p = Owner(o); p?.TrackSelectionChanged?.Invoke(p, new TrackSelectionChangedEventArgs((TrackType)t, ((IntPtr)u).GetUtf8(), ((IntPtr)s).GetUtf8())); };
        private static readonly DProgramList s_programListChanged = (o, a, id) => { var p = Owner(o); p?.ProgramListChanged?.Invoke(p, new ProgramListChangedEventArgs((ListAction)a, id)); };
        private static readonly DIntInt s_programSelectionChanged = (o, u, s) => { var p = Owner(o); p?.ProgramSelectionChanged?.Invoke(p, new ProgramSelectionChangedEventArgs(u, s)); };
        private static readonly DVoid s_titlesChanged = (o) => { var p = Owner(o); p?.TitlesChanged?.Invoke(p, EventArgs.Empty); };
        private static readonly DTitleSel s_titleSelectionChanged = (o, t, i) => { var p = Owner(o); p?.TitleSelectionChanged?.Invoke(p, new TitleSelectionChangedEventArgs((IntPtr)t, (int)i)); };
        private static readonly DChapterSel s_chapterSelectionChanged = (o, t, ti, c, ci) => { var p = Owner(o); p?.ChapterSelectionChanged?.Invoke(p, new ChapterSelectionChangedEventArgs((IntPtr)t, (int)ti, (IntPtr)c, (int)ci)); };
        private static readonly DRecording s_recordingChanged = (o, rec, path) => { var p = Owner(o); p?.RecordingChanged?.Invoke(p, new RecordingChangedEventArgs(rec.ToBool(), ((IntPtr)path).GetUtf8())); };
        private static readonly DString s_screenshotTaken = (o, s) => { var p = Owner(o); p?.ScreenshotTaken?.Invoke(p, new ScreenshotTakenEventArgs(((IntPtr)s).GetUtf8())); };
        private static readonly DMedia s_mediaParsed = (o, m) => { var p = Owner(o); p?.MediaParsed?.Invoke(p, new MediaEventArgs((IntPtr)m)); };
        private static readonly DMedia s_mediaMetaChanged = (o, m) => { var p = Owner(o); p?.MediaMetaChanged?.Invoke(p, new MediaEventArgs((IntPtr)m)); };
        private static readonly DMedia s_mediaSubitemsChanged = (o, m) => { var p = Owner(o); p?.MediaSubitemsChanged?.Invoke(p, new MediaEventArgs((IntPtr)m)); };
        private static readonly DAttachments s_mediaAttachmentsAdded = (o, m, l) => { var p = Owner(o); p?.MediaAttachmentsAdded?.Invoke(p, new MediaAttachmentsAddedEventArgs((IntPtr)m, (IntPtr)l)); };
        private static readonly DUInt s_voutChanged = (o, n) => { var p = Owner(o); p?.VoutChanged?.Invoke(p, new VoutChangedEventArgs((int)n)); };
        private static readonly DBool s_corkChanged = (o, b) => { var p = Owner(o); p?.CorkChanged?.Invoke(p, new CorkChangedEventArgs(b.ToBool())); };
        private static readonly DFloat s_audioVolumeChanged = (o, v) => { var p = Owner(o); p?.AudioVolumeChanged?.Invoke(p, new AudioVolumeChangedEventArgs(v)); };
        private static readonly DBool s_audioMuteChanged = (o, b) => { var p = Owner(o); p?.AudioMuteChanged?.Invoke(p, new AudioMuteChangedEventArgs(b.ToBool())); };
        private static readonly DString s_audioDeviceChanged = (o, s) => { var p = Owner(o); p?.AudioDeviceChanged?.Invoke(p, new AudioDeviceChangedEventArgs(((IntPtr)s).GetUtf8())); };

        // The shared cbs struct, built once. TODO(libvlc4-cbs): the binding does not export
        // LIBVLC_MEDIA_PLAYER_CBS_VER — verify version 0 against the VLC header (if libvlc_media_player_new
        // returns NULL the value is wrong).
        private static readonly libvlc_media_player_cbs s_cbs = new libvlc_media_player_cbs
        {
            version = 0,
            on_media_changed = Marshal.GetFunctionPointerForDelegate(s_mediaChanged),
            on_media_stopping = Marshal.GetFunctionPointerForDelegate(s_mediaStopping),
            on_state_changed = Marshal.GetFunctionPointerForDelegate(s_stateChanged),
            on_buffering_changed = Marshal.GetFunctionPointerForDelegate(s_bufferingChanged),
            on_capabilities_changed = Marshal.GetFunctionPointerForDelegate(s_capabilitiesChanged),
            on_position_changed = Marshal.GetFunctionPointerForDelegate(s_positionChanged),
            on_length_changed = Marshal.GetFunctionPointerForDelegate(s_lengthChanged),
            on_track_list_changed = Marshal.GetFunctionPointerForDelegate(s_trackListChanged),
            on_track_selection_changed = Marshal.GetFunctionPointerForDelegate(s_trackSelectionChanged),
            on_program_list_changed = Marshal.GetFunctionPointerForDelegate(s_programListChanged),
            on_program_selection_changed = Marshal.GetFunctionPointerForDelegate(s_programSelectionChanged),
            on_titles_changed = Marshal.GetFunctionPointerForDelegate(s_titlesChanged),
            on_title_selection_changed = Marshal.GetFunctionPointerForDelegate(s_titleSelectionChanged),
            on_chapter_selection_changed = Marshal.GetFunctionPointerForDelegate(s_chapterSelectionChanged),
            on_recording_changed = Marshal.GetFunctionPointerForDelegate(s_recordingChanged),
            on_screenshot_taken = Marshal.GetFunctionPointerForDelegate(s_screenshotTaken),
            on_media_parsed = Marshal.GetFunctionPointerForDelegate(s_mediaParsed),
            on_media_meta_changed = Marshal.GetFunctionPointerForDelegate(s_mediaMetaChanged),
            on_media_subitems_changed = Marshal.GetFunctionPointerForDelegate(s_mediaSubitemsChanged),
            on_media_attachments_added = Marshal.GetFunctionPointerForDelegate(s_mediaAttachmentsAdded),
            on_vout_changed = Marshal.GetFunctionPointerForDelegate(s_voutChanged),
            on_cork_changed = Marshal.GetFunctionPointerForDelegate(s_corkChanged),
            on_audio_volume_changed = Marshal.GetFunctionPointerForDelegate(s_audioVolumeChanged),
            on_audio_mute_changed = Marshal.GetFunctionPointerForDelegate(s_audioMuteChanged),
            on_audio_device_changed = Marshal.GetFunctionPointerForDelegate(s_audioDeviceChanged),
        };

        // The shared player cbs, exposed so MediaListPlayer can register the same callbacks on its internal
        // media player via libvlc_media_list_player_new(cbs); it then routes the cbs opaque (a GCHandle) to
        // the wrapped MediaPlayer so that player's events fire too — keeping every surfaced player uniform.
        internal static libvlc_media_player_cbs SharedCbs => s_cbs;
    }

    /// <summary><c>libvlc_media_player_time_point_t</c>.</summary>
    public readonly struct MediaTimePoint
    {
        internal unsafe MediaTimePoint(libvlc_media_player_time_point_t* p)
        {
            Position = p->position;
            Rate = p->rate;
            TimestampUs = p->ts_us;
            LengthUs = p->length_us;
            SystemDateUs = p->system_date_us;
        }

        /// <summary>Position in [0, 1].</summary>
        public readonly double Position;
        /// <summary>Playback rate.</summary>
        public readonly double Rate;
        /// <summary>Media timestamp in microseconds.</summary>
        public readonly long TimestampUs;
        /// <summary>Media length in microseconds (may be 0 if unknown).</summary>
        public readonly long LengthUs;
        /// <summary>System date of this point in microseconds (libvlc clock).</summary>
        public readonly long SystemDateUs;

        public static implicit operator libvlc_media_player_time_point_t(MediaTimePoint p) => new libvlc_media_player_time_point_t
        {
            position = p.Position,
            rate = p.Rate,
            ts_us = p.TimestampUs,
            length_us = p.LengthUs,
            system_date_us = p.SystemDateUs,
        };

        /// <summary>
        /// Interpolates the media timestamp and position for <paramref name="systemNowUs"/> (a libvlc
        /// clock value). Returns false if the player is paused/stopped. <c>libvlc_media_player_time_point_interpolate</c>.
        /// </summary>
        public unsafe bool Interpolate(long systemNowUs, out long timestampUs, out double position)
        {
            var p = (libvlc_media_player_time_point_t)this;
            long ts;
            double pos;
            int rc = libvlc_media_player_time_point_interpolate(&p, systemNowUs, &ts, &pos);
            timestampUs = ts;
            position = pos;
            return rc == 0;
        }

        /// <summary>
        /// The system date (libvlc clock, microseconds) at which the interpolated timestamp will next
        /// be a multiple of <paramref name="nextIntervalUs"/>. <c>libvlc_media_player_time_point_get_next_date</c>.
        /// </summary>
        public unsafe long GetNextDate(long systemNowUs, long interpolatedTimestampUs, long nextIntervalUs)
        {
            var p = (libvlc_media_player_time_point_t)this;
            return libvlc_media_player_time_point_get_next_date(&p, systemNowUs, interpolatedTimestampUs, nextIntervalUs);
        }
    }

    /// <summary>A 360°/spherical video viewpoint. <c>libvlc_video_viewpoint_t</c>.</summary>
    public readonly struct Viewpoint
    {
        /// <summary>Horizontal angle in degrees.</summary>
        public readonly float Yaw;
        /// <summary>Vertical angle in degrees.</summary>
        public readonly float Pitch;
        /// <summary>Roll angle in degrees.</summary>
        public readonly float Roll;
        /// <summary>Field of view in degrees (0 = unchanged).</summary>
        public readonly float FieldOfView;

        public Viewpoint(float yaw, float pitch, float roll, float fieldOfView)
        {
            Yaw = yaw; Pitch = pitch; Roll = roll; FieldOfView = fieldOfView;
        }
    }

    /// <summary>An audio output device of the current output module. <c>libvlc_audio_output_device_t</c>.</summary>
    public readonly struct AudioOutputDevice
    {
        /// <summary>Device id to pass to <see cref="MediaPlayer.SetAudioOutputDevice"/>.</summary>
        public readonly string? DeviceId;
        /// <summary>Human-readable device description, or null.</summary>
        public readonly string? Description;

        internal AudioOutputDevice(string? deviceId, string? description)
        {
            DeviceId = deviceId; Description = description;
        }
    }

    /// <summary>A title (e.g. a DVD/BD title) description. <c>libvlc_title_description_t</c>.</summary>
    public readonly struct TitleDescription
    {
        /// <summary>Title name, or null.</summary>
        public readonly string? Name;
        /// <summary>Duration in milliseconds.</summary>
        public readonly long DurationMs;
        /// <summary>
        /// Bitfield indicating whether the title was recognized as a menu, interactive, or plain
        /// content by the demuxer. <c>i_flags</c>.
        /// </summary>
        public readonly uint Flags;

        internal TitleDescription(string? name, long durationMs, uint flags)
        {
            Name = name; DurationMs = durationMs; Flags = flags;
        }
    }

    /// <summary>A chapter description. <c>libvlc_chapter_description_t</c>.</summary>
    public readonly struct ChapterDescription
    {
        /// <summary>Chapter name, or null.</summary>
        public readonly string? Name;
        /// <summary>Start offset in milliseconds.</summary>
        public readonly long TimeOffsetMs;
        /// <summary>Duration in milliseconds.</summary>
        public readonly long DurationMs;

        internal ChapterDescription(string? name, long timeOffsetMs, long durationMs)
        {
            Name = name; TimeOffsetMs = timeOffsetMs; DurationMs = durationMs;
        }
    }

    /// <summary>
    /// Optional batch of handlers passed to a <see cref="MediaPlayer"/> constructor; each non-null handler
    /// is subscribed to the matching event. Equivalent to subscribing with <c>+=</c> after construction.
    /// </summary>
    public sealed class MediaPlayerCallbacks
    {
        public EventHandler<MediaEventArgs>? MediaChanged;
        public EventHandler<MediaStoppingEventArgs>? MediaStopping;
        public EventHandler<StateChangedEventArgs>? StateChanged;
        public EventHandler<BufferingEventArgs>? BufferingChanged;
        public EventHandler<CapabilitiesChangedEventArgs>? CapabilitiesChanged;
        public EventHandler<PositionChangedEventArgs>? PositionChanged;
        public EventHandler<LengthChangedEventArgs>? LengthChanged;
        public EventHandler<TrackListChangedEventArgs>? TrackListChanged;
        public EventHandler<TrackSelectionChangedEventArgs>? TrackSelectionChanged;
        public EventHandler<ProgramListChangedEventArgs>? ProgramListChanged;
        public EventHandler<ProgramSelectionChangedEventArgs>? ProgramSelectionChanged;
        public EventHandler? TitlesChanged;
        public EventHandler<TitleSelectionChangedEventArgs>? TitleSelectionChanged;
        public EventHandler<ChapterSelectionChangedEventArgs>? ChapterSelectionChanged;
        public EventHandler<RecordingChangedEventArgs>? RecordingChanged;
        public EventHandler<ScreenshotTakenEventArgs>? ScreenshotTaken;
        public EventHandler<MediaEventArgs>? MediaParsed;
        public EventHandler<MediaEventArgs>? MediaMetaChanged;
        public EventHandler<MediaEventArgs>? MediaSubitemsChanged;
        public EventHandler<MediaAttachmentsAddedEventArgs>? MediaAttachmentsAdded;
        public EventHandler<VoutChangedEventArgs>? VoutChanged;
        public EventHandler<CorkChangedEventArgs>? CorkChanged;
        public EventHandler<AudioVolumeChangedEventArgs>? AudioVolumeChanged;
        public EventHandler<AudioMuteChangedEventArgs>? AudioMuteChanged;
        public EventHandler<AudioDeviceChangedEventArgs>? AudioDeviceChanged;
    }

    // ----------------------------------------------------------------------------------------------
    // Event payloads for MediaPlayer (libvlc_media_player_cbs). All are readonly structs delivered via
    // EventHandler<T> (no boxing). Payloads wrapping a borrowed native pointer are valid only for the
    // duration of the handler — call their GetXxx method inside the handler (default retains a copy).
    // ----------------------------------------------------------------------------------------------

    /// <summary>Carries a borrowed <c>libvlc_media_t*</c> (media changed / stopping / parsed / meta / subitems).</summary>
    public readonly struct MediaEventArgs
    {
        private readonly IntPtr media;
        public MediaEventArgs(IntPtr media) => this.media = media;

        /// <summary>Returns the media. <paramref name="addRef"/> true (default) retains a copy you must dispose; false is a borrowed view valid only inside the handler.</summary>
        public unsafe Media? GetMedia(bool addRef = true)
        {
            var p = (libvlc_media_t*)media;
            if (p == null) return null;
            return addRef ? new Media((IntPtr)libvlc_media_retain(p)) : new Media((IntPtr)p, addRef: false);
        }
    }

    /// <summary>Payload of <see cref="MediaPlayer.MediaStopping"/>.</summary>
    public readonly struct MediaStoppingEventArgs
    {
        private readonly IntPtr media;
        /// <summary>Why playback is stopping.</summary>
        public readonly StoppingReason Reason;
        public MediaStoppingEventArgs(IntPtr media, StoppingReason reason) { this.media = media; Reason = reason; }

        /// <summary>Returns the media being stopped (see <see cref="MediaEventArgs.GetMedia"/>).</summary>
        public unsafe Media? GetMedia(bool addRef = true)
        {
            var p = (libvlc_media_t*)media;
            if (p == null) return null;
            return addRef ? new Media((IntPtr)libvlc_media_retain(p)) : new Media((IntPtr)p, addRef: false);
        }
    }

    /// <summary>Payload of <see cref="MediaPlayer.StateChanged"/>.</summary>
    public readonly struct StateChangedEventArgs
    {
        /// <summary>The new player state.</summary>
        public readonly State State;
        public StateChangedEventArgs(State state) => State = state;
    }

    /// <summary>Payload of <see cref="MediaPlayer.BufferingChanged"/>.</summary>
    public readonly struct BufferingEventArgs
    {
        /// <summary>Buffering progress in [0, 100].</summary>
        public readonly float Cache;
        public BufferingEventArgs(float cache) => Cache = cache;
    }

    /// <summary>Payload of <see cref="MediaPlayer.CapabilitiesChanged"/>.</summary>
    public readonly struct CapabilitiesChangedEventArgs
    {
        /// <summary>Capabilities before the change (bitmask).</summary>
        public readonly Capability Old;
        /// <summary>Capabilities after the change (bitmask).</summary>
        public readonly Capability New;
        public CapabilitiesChangedEventArgs(Capability old, Capability @new) { Old = old; New = @new; }
    }

    /// <summary>Payload of <see cref="MediaPlayer.PositionChanged"/> (libvlc delivers time and position together).</summary>
    public readonly struct PositionChangedEventArgs
    {
        /// <summary>New playback time in milliseconds.</summary>
        public readonly long TimeMs;
        /// <summary>New position in [0, 1].</summary>
        public readonly double Position;
        public PositionChangedEventArgs(long timeMs, double position) { TimeMs = timeMs; Position = position; }
    }

    /// <summary>Payload of <see cref="MediaPlayer.LengthChanged"/>.</summary>
    public readonly struct LengthChangedEventArgs
    {
        /// <summary>New length in milliseconds.</summary>
        public readonly long LengthMs;
        public LengthChangedEventArgs(long lengthMs) => LengthMs = lengthMs;
    }

    /// <summary>Payload of <see cref="MediaPlayer.TrackListChanged"/>.</summary>
    public readonly struct TrackListChangedEventArgs
    {
        /// <summary>Whether a track was added, removed, or updated.</summary>
        public readonly ListAction Action;
        /// <summary>The affected track type.</summary>
        public readonly TrackType TrackType;
        /// <summary>String id of the affected track (<c>psz_id</c>).</summary>
        public readonly string? Id;
        public TrackListChangedEventArgs(ListAction action, TrackType trackType, string? id) { Action = action; TrackType = trackType; Id = id; }
    }

    /// <summary>Payload of <see cref="MediaPlayer.TrackSelectionChanged"/>.</summary>
    public readonly struct TrackSelectionChangedEventArgs
    {
        /// <summary>The track type whose selection changed.</summary>
        public readonly TrackType TrackType;
        /// <summary>Previously selected track id, or null.</summary>
        public readonly string? UnselectedId;
        /// <summary>Newly selected track id, or null.</summary>
        public readonly string? SelectedId;
        public TrackSelectionChangedEventArgs(TrackType trackType, string? unselectedId, string? selectedId) { TrackType = trackType; UnselectedId = unselectedId; SelectedId = selectedId; }
    }

    /// <summary>Payload of <see cref="MediaPlayer.ProgramListChanged"/>.</summary>
    public readonly struct ProgramListChangedEventArgs
    {
        /// <summary>Whether a program was added, removed, or updated.</summary>
        public readonly ListAction Action;
        /// <summary>Program (group) id.</summary>
        public readonly int Id;
        public ProgramListChangedEventArgs(ListAction action, int id) { Action = action; Id = id; }
    }

    /// <summary>Payload of <see cref="MediaPlayer.ProgramSelectionChanged"/>.</summary>
    public readonly struct ProgramSelectionChangedEventArgs
    {
        /// <summary>Previously selected program id, or -1.</summary>
        public readonly int UnselectedId;
        /// <summary>Newly selected program id, or -1.</summary>
        public readonly int SelectedId;
        public ProgramSelectionChangedEventArgs(int unselectedId, int selectedId) { UnselectedId = unselectedId; SelectedId = selectedId; }
    }

    /// <summary>Payload of <see cref="MediaPlayer.TitleSelectionChanged"/>.</summary>
    public readonly struct TitleSelectionChangedEventArgs
    {
        private readonly IntPtr title; // borrowed const libvlc_title_description_t*
        /// <summary>Selected title index.</summary>
        public readonly int Index;
        public TitleSelectionChangedEventArgs(IntPtr title, int index) { this.title = title; Index = index; }

        /// <summary>Reads the selected title description (call inside the handler; pointer is borrowed).</summary>
        public unsafe TitleDescription? GetTitleDescription()
        {
            var td = (libvlc_title_description_t*)title;
            if (td == null) return null;
            return new TitleDescription(((IntPtr)td->psz_name).GetUtf8(), td->i_duration, td->i_flags);
        }
    }

    /// <summary>Payload of <see cref="MediaPlayer.ChapterSelectionChanged"/>.</summary>
    public readonly struct ChapterSelectionChangedEventArgs
    {
        private readonly IntPtr title;   // borrowed const libvlc_title_description_t*
        private readonly IntPtr chapter; // borrowed const libvlc_chapter_description_t*
        /// <summary>Index of the title the chapter belongs to.</summary>
        public readonly int TitleIndex;
        /// <summary>Selected chapter index.</summary>
        public readonly int ChapterIndex;
        public ChapterSelectionChangedEventArgs(IntPtr title, int titleIndex, IntPtr chapter, int chapterIndex)
        { this.title = title; TitleIndex = titleIndex; this.chapter = chapter; ChapterIndex = chapterIndex; }

        /// <summary>Reads the title description (call inside the handler; pointer is borrowed).</summary>
        public unsafe TitleDescription? GetTitleDescription()
        {
            var td = (libvlc_title_description_t*)title;
            if (td == null) return null;
            return new TitleDescription(((IntPtr)td->psz_name).GetUtf8(), td->i_duration, td->i_flags);
        }

        /// <summary>Reads the chapter description (call inside the handler; pointer is borrowed).</summary>
        public unsafe ChapterDescription? GetChapterDescription()
        {
            var cd = (libvlc_chapter_description_t*)chapter;
            if (cd == null) return null;
            return new ChapterDescription(((IntPtr)cd->psz_name).GetUtf8(), cd->i_time_offset, cd->i_duration);
        }
    }

    /// <summary>Payload of <see cref="MediaPlayer.RecordingChanged"/>.</summary>
    public readonly struct RecordingChangedEventArgs
    {
        /// <summary>True if recording started, false if it stopped.</summary>
        public readonly bool Recording;
        /// <summary>Path of the recorded file when recording stops, otherwise null.</summary>
        public readonly string? FilePath;
        public RecordingChangedEventArgs(bool recording, string? filePath) { Recording = recording; FilePath = filePath; }
    }

    /// <summary>Payload of <see cref="MediaPlayer.ScreenshotTaken"/>.</summary>
    public readonly struct ScreenshotTakenEventArgs
    {
        /// <summary>Path the screenshot was written to.</summary>
        public readonly string? FilePath;
        public ScreenshotTakenEventArgs(string? filePath) => FilePath = filePath;
    }

    /// <summary>Payload of <see cref="MediaPlayer.MediaAttachmentsAdded"/>.</summary>
    public readonly struct MediaAttachmentsAddedEventArgs
    {
        private readonly IntPtr media;        // borrowed libvlc_media_t*
        private readonly IntPtr attachments;  // borrowed libvlc_picture_list_t*
        public MediaAttachmentsAddedEventArgs(IntPtr media, IntPtr attachments) { this.media = media; this.attachments = attachments; }

        /// <summary>Returns the media the attachments belong to (see <see cref="MediaEventArgs.GetMedia"/>).</summary>
        public unsafe Media? GetMedia(bool addRef = true)
        {
            var p = (libvlc_media_t*)media;
            if (p == null) return null;
            return addRef ? new Media((IntPtr)libvlc_media_retain(p)) : new Media((IntPtr)p, addRef: false);
        }

        /// <summary>Returns the attachment pictures. Must be called inside the handler (the list is borrowed). <paramref name="owner"/> true retains each picture (dispose them); false gives borrowed views.</summary>
        public unsafe IReadOnlyList<Picture> GetAttachments(bool owner = true)
        {
            var list = (libvlc_picture_list_t*)attachments;
            if (list == null) return Array.Empty<Picture>();
            int count = (int)libvlc_picture_list_count(list).ToUInt32();
            var pictures = new Picture[count];
            for (int i = 0; i < count; i++)
            {
                var pic = libvlc_picture_list_at(list, new UIntPtr((uint)i)); // borrowed
                pictures[i] = owner ? new Picture((IntPtr)libvlc_picture_retain(pic)) : new Picture((IntPtr)pic, addRef: false);
            }
            return pictures;
        }
    }

    /// <summary>Payload of <see cref="MediaPlayer.VoutChanged"/>.</summary>
    public readonly struct VoutChangedEventArgs
    {
        /// <summary>Number of active video outputs.</summary>
        public readonly int Count;
        public VoutChangedEventArgs(int count) => Count = count;
    }

    /// <summary>Payload of <see cref="MediaPlayer.CorkChanged"/>.</summary>
    public readonly struct CorkChangedEventArgs
    {
        /// <summary>True if playback is now corked (should pause), false if uncorked.</summary>
        public readonly bool Corked;
        public CorkChangedEventArgs(bool corked) => Corked = corked;
    }

    /// <summary>Payload of <see cref="MediaPlayer.AudioVolumeChanged"/>.</summary>
    public readonly struct AudioVolumeChangedEventArgs
    {
        /// <summary>New volume (1.0 = 100%).</summary>
        public readonly float Volume;
        public AudioVolumeChangedEventArgs(float volume) => Volume = volume;
    }

    /// <summary>Payload of <see cref="MediaPlayer.AudioMuteChanged"/>.</summary>
    public readonly struct AudioMuteChangedEventArgs
    {
        /// <summary>True if audio is now muted.</summary>
        public readonly bool Muted;
        public AudioMuteChangedEventArgs(bool muted) => Muted = muted;
    }

    /// <summary>Payload of <see cref="MediaPlayer.AudioDeviceChanged"/>.</summary>
    public readonly struct AudioDeviceChangedEventArgs
    {
        /// <summary>The new audio output device id.</summary>
        public readonly string? Device;
        public AudioDeviceChangedEventArgs(string? device) => Device = device;
    }
}
