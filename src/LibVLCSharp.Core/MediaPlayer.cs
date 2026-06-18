using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>A media player (<c>libvlc_media_player_t</c>): plays a <see cref="Media"/>.</summary>
    public unsafe class MediaPlayer : NativeReference
    {
        private EventManager? _events;

        /// <summary>Wraps an existing native handle.</summary>
        /// <param name="handle">Native <c>libvlc_media_player_t*</c>.</param>
        public MediaPlayer(IntPtr handle) : base(handle) { }

        /// <summary>Implicit conversion to the native <c>libvlc_media_player_t*</c> (null for a null player).</summary>
        public static implicit operator libvlc_media_player_t*(MediaPlayer? mediaPlayer) =>
            mediaPlayer is null ? null : (libvlc_media_player_t*)mediaPlayer.NativeHandle;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timeWatch?.Dispose();   // unwatch_time before release (needs the player alive)
                _events?.Dispose();
            }
            base.Dispose(disposing);
        }

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
            set => libvlc_media_player_set_title(this, value);
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

        // --- time watch ---

        private TimeWatch? _timeWatch;

        /// <summary>
        /// Subscribes to high-precision playback-time updates. <c>libvlc_media_player_watch_time</c>.
        /// Release the returned handle (or the player) to unsubscribe. Only one watch is active at a
        /// time. Callbacks fire on libvlc threads.
        /// </summary>
        /// <param name="minPeriodUs">Minimum interval between <paramref name="onUpdate"/> calls, in microseconds.</param>
        /// <param name="onUpdate">Periodic time update (required).</param>
        /// <param name="onPaused">Invoked on pause with the system date (microseconds), or null.</param>
        /// <param name="onSeek">Invoked on seek with the new time point, or null.</param>
        public IDisposable WatchTime(long minPeriodUs, Action<MediaPlayer, MediaTimePoint> onUpdate,
            Action<MediaPlayer, long>? onPaused = null, Action<MediaPlayer, MediaTimePoint>? onSeek = null)
        {
            if (onUpdate is null) throw new ArgumentNullException(nameof(onUpdate));
            return _timeWatch = new TimeWatch(this, minPeriodUs, onUpdate, onPaused, onSeek);
        }

        private unsafe class TimeWatch : IDisposable
        {
            private readonly MediaPlayer _mp;
            private readonly Action<MediaPlayer, MediaTimePoint> _onUpdate;
            private readonly Action<MediaPlayer, long>? _onPaused;
            private readonly Action<MediaPlayer, MediaTimePoint>? _onSeek;
            private readonly libvlc_media_player_watch_time_on_update _u;
            private readonly libvlc_media_player_watch_time_on_paused? _p;
            private readonly libvlc_media_player_watch_time_on_seek? _s;
            private bool _disposed;

            public TimeWatch(MediaPlayer mp, long minPeriodUs, Action<MediaPlayer, MediaTimePoint> onUpdate,
                Action<MediaPlayer, long>? onPaused, Action<MediaPlayer, MediaTimePoint>? onSeek)
            {
                _mp = mp; _onUpdate = onUpdate; _onPaused = onPaused; _onSeek = onSeek;
                _u = OnUpdate;
                _p = onPaused is null ? null : OnPaused;
                _s = onSeek is null ? null : OnSeek;

                int rc = libvlc_media_player_watch_time(mp, minPeriodUs,
                    Marshal.GetFunctionPointerForDelegate(_u),
                    _p is null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(_p),
                    _s is null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(_s),
                    IntPtr.Zero);
                if (rc != 0)
                    throw new InvalidOperationException("libvlc_media_player_watch_time failed (already watching?).");
            }

            private void OnUpdate(libvlc_media_player_time_point_t* value, IntPtr data) => _onUpdate(_mp, new MediaTimePoint(value));
            private void OnPaused(long systemDateUs, IntPtr data) => _onPaused?.Invoke(_mp, systemDateUs);
            private void OnSeek(libvlc_media_player_time_point_t* value, IntPtr data) => _onSeek?.Invoke(_mp, new MediaTimePoint(value));

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                libvlc_media_player_unwatch_time(_mp);   // guarantees no callback is in flight on return
                if (ReferenceEquals(_mp._timeWatch, this)) _mp._timeWatch = null;
            }
        }

        // --- events ---

        private EventManager Events =>
            _events ??= new EventManager(libvlc_media_player_event_manager(this), Dispatch);

        private void Dispatch(libvlc_event_t* e, IntPtr _)
        {
            switch ((libvlc_event_e)e->type)
            {
                // no payload
                case libvlc_event_e.libvlc_MediaPlayerNothingSpecial: { var h = _nothingSpecial; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaPlayerOpening: { var h = _opening; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaPlayerPlaying: { var h = _playing; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaPlayerPaused: { var h = _paused; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaPlayerStopped: { var h = _stopped; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaPlayerForward: { var h = _forward; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaPlayerBackward: { var h = _backward; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaPlayerStopping: { var h = _stopping; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaPlayerEncounteredError: { var h = _error; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaPlayerCorked: { var h = _corked; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaPlayerUncorked: { var h = _uncorked; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaPlayerMuted: { var h = _muted; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaPlayerUnmuted: { var h = _unmuted; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaPlayerTitleListChanged: { var h = _titleListChanged; if (h != null) h(this, EventArgs.Empty); break; }

                // payload
                case libvlc_event_e.libvlc_MediaPlayerMediaChanged: { var h = _mediaChanged; if (h != null) h(this, new MediaEventArgs(NativeWrap.Media(e->u.media_player_media_changed.new_media))); break; }
                case libvlc_event_e.libvlc_MediaPlayerMediaStopping: { var h = _mediaStopping; if (h != null) h(this, new MediaEventArgs(NativeWrap.Media(e->u.media_player_media_stopping.media))); break; }
                case libvlc_event_e.libvlc_MediaPlayerBuffering: { var h = _buffering; if (h != null) h(this, new BufferingEventArgs(e->u.media_player_buffering.new_cache)); break; }
                case libvlc_event_e.libvlc_MediaPlayerTimeChanged: { var h = _timeChanged; if (h != null) h(this, new TimeChangedEventArgs(e->u.media_player_time_changed.new_time)); break; }
                case libvlc_event_e.libvlc_MediaPlayerPositionChanged: { var h = _positionChanged; if (h != null) h(this, new PositionChangedEventArgs(e->u.media_player_position_changed.new_position)); break; }
                case libvlc_event_e.libvlc_MediaPlayerSeekableChanged: { var h = _seekableChanged; if (h != null) h(this, new SeekableChangedEventArgs(e->u.media_player_seekable_changed.new_seekable != 0)); break; }
                case libvlc_event_e.libvlc_MediaPlayerPausableChanged: { var h = _pausableChanged; if (h != null) h(this, new PausableChangedEventArgs(e->u.media_player_pausable_changed.new_pausable != 0)); break; }
                case libvlc_event_e.libvlc_MediaPlayerSnapshotTaken: { var h = _snapshotTaken; if (h != null) h(this, new SnapshotTakenEventArgs(((IntPtr)e->u.media_player_snapshot_taken.psz_filename).GetUtf8())); break; }
                case libvlc_event_e.libvlc_MediaPlayerLengthChanged: { var h = _lengthChanged; if (h != null) h(this, new LengthChangedEventArgs(e->u.media_player_length_changed.new_length)); break; }
                case libvlc_event_e.libvlc_MediaPlayerVout: { var h = _vout; if (h != null) h(this, new VoutEventArgs(e->u.media_player_vout.new_count)); break; }
                case libvlc_event_e.libvlc_MediaPlayerESAdded: { var h = _esAdded; if (h != null) h(this, EsChanged(e)); break; }
                case libvlc_event_e.libvlc_MediaPlayerESDeleted: { var h = _esDeleted; if (h != null) h(this, EsChanged(e)); break; }
                case libvlc_event_e.libvlc_MediaPlayerESUpdated: { var h = _esUpdated; if (h != null) h(this, EsChanged(e)); break; }
                case libvlc_event_e.libvlc_MediaPlayerESSelected:
                    {
                        var h = _esSelected;
                        if (h != null)
                            h(this, new EsSelectionChangedEventArgs(
                                (TrackType)e->u.media_player_es_selection_changed.i_type,
                                ((IntPtr)e->u.media_player_es_selection_changed.psz_unselected_id).GetUtf8(),
                                ((IntPtr)e->u.media_player_es_selection_changed.psz_selected_id).GetUtf8()));
                        break;
                    }
                case libvlc_event_e.libvlc_MediaPlayerAudioVolume: { var h = _audioVolume; if (h != null) h(this, new AudioVolumeEventArgs(e->u.media_player_audio_volume.volume)); break; }
                case libvlc_event_e.libvlc_MediaPlayerAudioDevice: { var h = _audioDevice; if (h != null) h(this, new AudioDeviceEventArgs(((IntPtr)e->u.media_player_audio_device.device).GetUtf8())); break; }
                case libvlc_event_e.libvlc_MediaPlayerProgramAdded: { var h = _programAdded; if (h != null) h(this, new ProgramEventArgs(e->u.media_player_program_changed.i_id)); break; }
                case libvlc_event_e.libvlc_MediaPlayerProgramDeleted: { var h = _programDeleted; if (h != null) h(this, new ProgramEventArgs(e->u.media_player_program_changed.i_id)); break; }
                case libvlc_event_e.libvlc_MediaPlayerProgramUpdated: { var h = _programUpdated; if (h != null) h(this, new ProgramEventArgs(e->u.media_player_program_changed.i_id)); break; }
                case libvlc_event_e.libvlc_MediaPlayerProgramSelected: { var h = _programSelected; if (h != null) h(this, new ProgramSelectionChangedEventArgs(e->u.media_player_program_selection_changed.i_unselected_id, e->u.media_player_program_selection_changed.i_selected_id)); break; }
                case libvlc_event_e.libvlc_MediaPlayerTitleSelectionChanged: { var h = _titleSelectionChanged; if (h != null) h(this, new TitleSelectionChangedEventArgs((IntPtr)e->u.media_player_title_selection_changed.title, e->u.media_player_title_selection_changed.index)); break; }
                case libvlc_event_e.libvlc_MediaPlayerChapterChanged: { var h = _chapterChanged; if (h != null) h(this, new ChapterChangedEventArgs(e->u.media_player_chapter_changed.new_chapter)); break; }
                case libvlc_event_e.libvlc_MediaPlayerRecordChanged: { var h = _recordChanged; if (h != null) h(this, new RecordChangedEventArgs(e->u.media_player_record_changed.recording.ToBool(), ((IntPtr)e->u.media_player_record_changed.recorded_file_path).GetUtf8())); break; }
            }
        }

        private static EsChangedEventArgs EsChanged(libvlc_event_t* e) => new EsChangedEventArgs(
            (TrackType)e->u.media_player_es_changed.i_type,
            e->u.media_player_es_changed.i_id,
            ((IntPtr)e->u.media_player_es_changed.psz_id).GetUtf8());

        private EventHandler? _nothingSpecial, _opening, _playing, _paused, _stopped, _forward, _backward,
            _stopping, _error, _corked, _uncorked, _muted, _unmuted, _titleListChanged;
        private EventHandler<MediaEventArgs>? _mediaChanged, _mediaStopping;
        private EventHandler<BufferingEventArgs>? _buffering;
        private EventHandler<TimeChangedEventArgs>? _timeChanged;
        private EventHandler<PositionChangedEventArgs>? _positionChanged;
        private EventHandler<SeekableChangedEventArgs>? _seekableChanged;
        private EventHandler<PausableChangedEventArgs>? _pausableChanged;
        private EventHandler<SnapshotTakenEventArgs>? _snapshotTaken;
        private EventHandler<LengthChangedEventArgs>? _lengthChanged;
        private EventHandler<VoutEventArgs>? _vout;
        private EventHandler<EsChangedEventArgs>? _esAdded, _esDeleted, _esUpdated;
        private EventHandler<EsSelectionChangedEventArgs>? _esSelected;
        private EventHandler<AudioVolumeEventArgs>? _audioVolume;
        private EventHandler<AudioDeviceEventArgs>? _audioDevice;
        private EventHandler<ProgramEventArgs>? _programAdded, _programDeleted, _programUpdated;
        private EventHandler<ProgramSelectionChangedEventArgs>? _programSelected;
        private EventHandler<TitleSelectionChangedEventArgs>? _titleSelectionChanged;
        private EventHandler<ChapterChangedEventArgs>? _chapterChanged;
        private EventHandler<RecordChangedEventArgs>? _recordChanged;

        /// <summary><c>libvlc_MediaPlayerNothingSpecial</c>.</summary>
        public event EventHandler NothingSpecial { add => Events.Attach(ref _nothingSpecial, value, libvlc_event_e.libvlc_MediaPlayerNothingSpecial); remove => Events.Detach(ref _nothingSpecial, value, libvlc_event_e.libvlc_MediaPlayerNothingSpecial); }
        /// <summary><c>libvlc_MediaPlayerOpening</c>.</summary>
        public event EventHandler Opening { add => Events.Attach(ref _opening, value, libvlc_event_e.libvlc_MediaPlayerOpening); remove => Events.Detach(ref _opening, value, libvlc_event_e.libvlc_MediaPlayerOpening); }
        /// <summary><c>libvlc_MediaPlayerPlaying</c>.</summary>
        public event EventHandler Playing { add => Events.Attach(ref _playing, value, libvlc_event_e.libvlc_MediaPlayerPlaying); remove => Events.Detach(ref _playing, value, libvlc_event_e.libvlc_MediaPlayerPlaying); }
        /// <summary><c>libvlc_MediaPlayerPaused</c>.</summary>
        public event EventHandler Paused { add => Events.Attach(ref _paused, value, libvlc_event_e.libvlc_MediaPlayerPaused); remove => Events.Detach(ref _paused, value, libvlc_event_e.libvlc_MediaPlayerPaused); }
        /// <summary><c>libvlc_MediaPlayerStopped</c>.</summary>
        public event EventHandler Stopped { add => Events.Attach(ref _stopped, value, libvlc_event_e.libvlc_MediaPlayerStopped); remove => Events.Detach(ref _stopped, value, libvlc_event_e.libvlc_MediaPlayerStopped); }
        /// <summary><c>libvlc_MediaPlayerForward</c>.</summary>
        public event EventHandler Forward { add => Events.Attach(ref _forward, value, libvlc_event_e.libvlc_MediaPlayerForward); remove => Events.Detach(ref _forward, value, libvlc_event_e.libvlc_MediaPlayerForward); }
        /// <summary><c>libvlc_MediaPlayerBackward</c>.</summary>
        public event EventHandler Backward { add => Events.Attach(ref _backward, value, libvlc_event_e.libvlc_MediaPlayerBackward); remove => Events.Detach(ref _backward, value, libvlc_event_e.libvlc_MediaPlayerBackward); }
        /// <summary><c>libvlc_MediaPlayerStopping</c> (the 4.x replacement for EndReached).</summary>
        public event EventHandler Stopping { add => Events.Attach(ref _stopping, value, libvlc_event_e.libvlc_MediaPlayerStopping); remove => Events.Detach(ref _stopping, value, libvlc_event_e.libvlc_MediaPlayerStopping); }
        /// <summary><c>libvlc_MediaPlayerEncounteredError</c>.</summary>
        public event EventHandler EncounteredError { add => Events.Attach(ref _error, value, libvlc_event_e.libvlc_MediaPlayerEncounteredError); remove => Events.Detach(ref _error, value, libvlc_event_e.libvlc_MediaPlayerEncounteredError); }
        /// <summary><c>libvlc_MediaPlayerCorked</c>.</summary>
        public event EventHandler Corked { add => Events.Attach(ref _corked, value, libvlc_event_e.libvlc_MediaPlayerCorked); remove => Events.Detach(ref _corked, value, libvlc_event_e.libvlc_MediaPlayerCorked); }
        /// <summary><c>libvlc_MediaPlayerUncorked</c>.</summary>
        public event EventHandler Uncorked { add => Events.Attach(ref _uncorked, value, libvlc_event_e.libvlc_MediaPlayerUncorked); remove => Events.Detach(ref _uncorked, value, libvlc_event_e.libvlc_MediaPlayerUncorked); }
        /// <summary><c>libvlc_MediaPlayerMuted</c>.</summary>
        public event EventHandler Muted { add => Events.Attach(ref _muted, value, libvlc_event_e.libvlc_MediaPlayerMuted); remove => Events.Detach(ref _muted, value, libvlc_event_e.libvlc_MediaPlayerMuted); }
        /// <summary><c>libvlc_MediaPlayerUnmuted</c>.</summary>
        public event EventHandler Unmuted { add => Events.Attach(ref _unmuted, value, libvlc_event_e.libvlc_MediaPlayerUnmuted); remove => Events.Detach(ref _unmuted, value, libvlc_event_e.libvlc_MediaPlayerUnmuted); }
        /// <summary><c>libvlc_MediaPlayerTitleListChanged</c>.</summary>
        public event EventHandler TitleListChanged { add => Events.Attach(ref _titleListChanged, value, libvlc_event_e.libvlc_MediaPlayerTitleListChanged); remove => Events.Detach(ref _titleListChanged, value, libvlc_event_e.libvlc_MediaPlayerTitleListChanged); }

        /// <summary><c>libvlc_MediaPlayerMediaChanged</c> (owning <see cref="Core.Media"/>; dispose it).</summary>
        public event EventHandler<MediaEventArgs> MediaChanged { add => Events.Attach(ref _mediaChanged, value, libvlc_event_e.libvlc_MediaPlayerMediaChanged); remove => Events.Detach(ref _mediaChanged, value, libvlc_event_e.libvlc_MediaPlayerMediaChanged); }
        /// <summary><c>libvlc_MediaPlayerMediaStopping</c> (owning <see cref="Core.Media"/>; dispose it).</summary>
        public event EventHandler<MediaEventArgs> MediaStopping { add => Events.Attach(ref _mediaStopping, value, libvlc_event_e.libvlc_MediaPlayerMediaStopping); remove => Events.Detach(ref _mediaStopping, value, libvlc_event_e.libvlc_MediaPlayerMediaStopping); }
        /// <summary><c>libvlc_MediaPlayerBuffering</c>.</summary>
        public event EventHandler<BufferingEventArgs> Buffering { add => Events.Attach(ref _buffering, value, libvlc_event_e.libvlc_MediaPlayerBuffering); remove => Events.Detach(ref _buffering, value, libvlc_event_e.libvlc_MediaPlayerBuffering); }
        /// <summary><c>libvlc_MediaPlayerTimeChanged</c>.</summary>
        public event EventHandler<TimeChangedEventArgs> TimeChanged { add => Events.Attach(ref _timeChanged, value, libvlc_event_e.libvlc_MediaPlayerTimeChanged); remove => Events.Detach(ref _timeChanged, value, libvlc_event_e.libvlc_MediaPlayerTimeChanged); }
        /// <summary><c>libvlc_MediaPlayerPositionChanged</c>.</summary>
        public event EventHandler<PositionChangedEventArgs> PositionChanged { add => Events.Attach(ref _positionChanged, value, libvlc_event_e.libvlc_MediaPlayerPositionChanged); remove => Events.Detach(ref _positionChanged, value, libvlc_event_e.libvlc_MediaPlayerPositionChanged); }
        /// <summary><c>libvlc_MediaPlayerSeekableChanged</c>.</summary>
        public event EventHandler<SeekableChangedEventArgs> SeekableChanged { add => Events.Attach(ref _seekableChanged, value, libvlc_event_e.libvlc_MediaPlayerSeekableChanged); remove => Events.Detach(ref _seekableChanged, value, libvlc_event_e.libvlc_MediaPlayerSeekableChanged); }
        /// <summary><c>libvlc_MediaPlayerPausableChanged</c>.</summary>
        public event EventHandler<PausableChangedEventArgs> PausableChanged { add => Events.Attach(ref _pausableChanged, value, libvlc_event_e.libvlc_MediaPlayerPausableChanged); remove => Events.Detach(ref _pausableChanged, value, libvlc_event_e.libvlc_MediaPlayerPausableChanged); }
        /// <summary><c>libvlc_MediaPlayerSnapshotTaken</c>.</summary>
        public event EventHandler<SnapshotTakenEventArgs> SnapshotTaken { add => Events.Attach(ref _snapshotTaken, value, libvlc_event_e.libvlc_MediaPlayerSnapshotTaken); remove => Events.Detach(ref _snapshotTaken, value, libvlc_event_e.libvlc_MediaPlayerSnapshotTaken); }
        /// <summary><c>libvlc_MediaPlayerLengthChanged</c>.</summary>
        public event EventHandler<LengthChangedEventArgs> LengthChanged { add => Events.Attach(ref _lengthChanged, value, libvlc_event_e.libvlc_MediaPlayerLengthChanged); remove => Events.Detach(ref _lengthChanged, value, libvlc_event_e.libvlc_MediaPlayerLengthChanged); }
        /// <summary><c>libvlc_MediaPlayerVout</c>.</summary>
        public event EventHandler<VoutEventArgs> Vout { add => Events.Attach(ref _vout, value, libvlc_event_e.libvlc_MediaPlayerVout); remove => Events.Detach(ref _vout, value, libvlc_event_e.libvlc_MediaPlayerVout); }
        /// <summary><c>libvlc_MediaPlayerESAdded</c>.</summary>
        public event EventHandler<EsChangedEventArgs> ESAdded { add => Events.Attach(ref _esAdded, value, libvlc_event_e.libvlc_MediaPlayerESAdded); remove => Events.Detach(ref _esAdded, value, libvlc_event_e.libvlc_MediaPlayerESAdded); }
        /// <summary><c>libvlc_MediaPlayerESDeleted</c>.</summary>
        public event EventHandler<EsChangedEventArgs> ESDeleted { add => Events.Attach(ref _esDeleted, value, libvlc_event_e.libvlc_MediaPlayerESDeleted); remove => Events.Detach(ref _esDeleted, value, libvlc_event_e.libvlc_MediaPlayerESDeleted); }
        /// <summary><c>libvlc_MediaPlayerESUpdated</c>.</summary>
        public event EventHandler<EsChangedEventArgs> ESUpdated { add => Events.Attach(ref _esUpdated, value, libvlc_event_e.libvlc_MediaPlayerESUpdated); remove => Events.Detach(ref _esUpdated, value, libvlc_event_e.libvlc_MediaPlayerESUpdated); }
        /// <summary><c>libvlc_MediaPlayerESSelected</c>.</summary>
        public event EventHandler<EsSelectionChangedEventArgs> ESSelected { add => Events.Attach(ref _esSelected, value, libvlc_event_e.libvlc_MediaPlayerESSelected); remove => Events.Detach(ref _esSelected, value, libvlc_event_e.libvlc_MediaPlayerESSelected); }
        /// <summary><c>libvlc_MediaPlayerAudioVolume</c>.</summary>
        public event EventHandler<AudioVolumeEventArgs> AudioVolume { add => Events.Attach(ref _audioVolume, value, libvlc_event_e.libvlc_MediaPlayerAudioVolume); remove => Events.Detach(ref _audioVolume, value, libvlc_event_e.libvlc_MediaPlayerAudioVolume); }
        /// <summary><c>libvlc_MediaPlayerAudioDevice</c>.</summary>
        public event EventHandler<AudioDeviceEventArgs> AudioDevice { add => Events.Attach(ref _audioDevice, value, libvlc_event_e.libvlc_MediaPlayerAudioDevice); remove => Events.Detach(ref _audioDevice, value, libvlc_event_e.libvlc_MediaPlayerAudioDevice); }
        /// <summary><c>libvlc_MediaPlayerProgramAdded</c>.</summary>
        public event EventHandler<ProgramEventArgs> ProgramAdded { add => Events.Attach(ref _programAdded, value, libvlc_event_e.libvlc_MediaPlayerProgramAdded); remove => Events.Detach(ref _programAdded, value, libvlc_event_e.libvlc_MediaPlayerProgramAdded); }
        /// <summary><c>libvlc_MediaPlayerProgramDeleted</c>.</summary>
        public event EventHandler<ProgramEventArgs> ProgramDeleted { add => Events.Attach(ref _programDeleted, value, libvlc_event_e.libvlc_MediaPlayerProgramDeleted); remove => Events.Detach(ref _programDeleted, value, libvlc_event_e.libvlc_MediaPlayerProgramDeleted); }
        /// <summary><c>libvlc_MediaPlayerProgramUpdated</c>.</summary>
        public event EventHandler<ProgramEventArgs> ProgramUpdated { add => Events.Attach(ref _programUpdated, value, libvlc_event_e.libvlc_MediaPlayerProgramUpdated); remove => Events.Detach(ref _programUpdated, value, libvlc_event_e.libvlc_MediaPlayerProgramUpdated); }
        /// <summary><c>libvlc_MediaPlayerProgramSelected</c>.</summary>
        public event EventHandler<ProgramSelectionChangedEventArgs> ProgramSelected { add => Events.Attach(ref _programSelected, value, libvlc_event_e.libvlc_MediaPlayerProgramSelected); remove => Events.Detach(ref _programSelected, value, libvlc_event_e.libvlc_MediaPlayerProgramSelected); }
        /// <summary><c>libvlc_MediaPlayerTitleSelectionChanged</c>.</summary>
        public event EventHandler<TitleSelectionChangedEventArgs> TitleSelectionChanged { add => Events.Attach(ref _titleSelectionChanged, value, libvlc_event_e.libvlc_MediaPlayerTitleSelectionChanged); remove => Events.Detach(ref _titleSelectionChanged, value, libvlc_event_e.libvlc_MediaPlayerTitleSelectionChanged); }
        /// <summary><c>libvlc_MediaPlayerChapterChanged</c>.</summary>
        public event EventHandler<ChapterChangedEventArgs> ChapterChanged { add => Events.Attach(ref _chapterChanged, value, libvlc_event_e.libvlc_MediaPlayerChapterChanged); remove => Events.Detach(ref _chapterChanged, value, libvlc_event_e.libvlc_MediaPlayerChapterChanged); }
        /// <summary><c>libvlc_MediaPlayerRecordChanged</c>.</summary>
        public event EventHandler<RecordChangedEventArgs> RecordChanged { add => Events.Attach(ref _recordChanged, value, libvlc_event_e.libvlc_MediaPlayerRecordChanged); remove => Events.Detach(ref _recordChanged, value, libvlc_event_e.libvlc_MediaPlayerRecordChanged); }
    }

    /// <summary>A playback time observation delivered to <see cref="MediaPlayer.WatchTime"/>. <c>libvlc_media_player_time_point_t</c>.</summary>
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
}
