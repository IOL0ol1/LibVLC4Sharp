using System;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>
    /// A media list player (<c>libvlc_media_list_player_t</c>): plays through a
    /// <see cref="MediaList"/> in a certain order. Required to support playlist files; the
    /// normal <see cref="MediaPlayer"/> can only play a single media and does not handle
    /// playlist files properly.
    /// </summary>
    public unsafe class MediaListPlayer : NativeReference
    {
        private EventManager? _events;

        /// <summary>Wraps an existing native handle.</summary>
        /// <param name="handle">Native <c>libvlc_media_list_player_t*</c>.</param>
        public MediaListPlayer(IntPtr handle) : base(handle) { }

        /// <summary>Implicit conversion to the native <c>libvlc_media_list_player_t*</c> (null for a null player).</summary>
        public static implicit operator libvlc_media_list_player_t*(MediaListPlayer? mediaListPlayer) =>
            mediaListPlayer is null ? null : (libvlc_media_list_player_t*)mediaListPlayer.NativeHandle;

        protected override void Dispose(bool disposing)
        {
            if (disposing) _events?.Dispose();
            base.Dispose(disposing);
        }

        protected override void Release(IntPtr handle) =>
            libvlc_media_list_player_release((libvlc_media_list_player_t*)handle);

        private MediaPlayer? _mediaPlayer;
        private MediaList? _mediaList;

        /// <summary>
        /// The underlying media player used by this media list player.
        /// Setter: <c>libvlc_media_list_player_set_media_player</c> — replaces the current media
        /// player instance.
        /// Getter: <c>libvlc_media_list_player_get_media_player</c> — returns the same instance you
        /// assigned (the extra reference the getter takes is released). If the player was changed
        /// outside this wrapper, a new owning wrapper is returned (dispose it yourself).
        /// </summary>
        public MediaPlayer? MediaPlayer
        {
            get => Reconcile(ref _mediaPlayer, (IntPtr)libvlc_media_list_player_get_media_player(this), // +1 ref, or null
                static h => libvlc_media_player_release((libvlc_media_player_t*)h),
                static h => new MediaPlayer(h));
            set
            {
                _mediaPlayer = value;
                libvlc_media_list_player_set_media_player(this, value);
            }
        }

        /// <summary>
        /// The media list associated with the player (set-backed; libvlc exposes no getter).
        /// <c>libvlc_media_list_player_set_media_list</c>.
        /// </summary>
        /// <remarks>Setting this associates the given <see cref="MediaList"/> with the player.</remarks>
        public MediaList? MediaList
        {
            get => _mediaList;
            set
            {
                _mediaList = value;
                libvlc_media_list_player_set_media_list(this, value);
            }
        }

        /// <summary>
        /// Whether the media list is currently playing. <c>libvlc_media_list_player_is_playing</c>.
        /// </summary>
        /// <returns><c>true</c> if playing, <c>false</c> if not playing.</returns>
        public bool IsPlaying => libvlc_media_list_player_is_playing(this).ToBool();

        /// <summary>Plays the media list from the current position. <c>libvlc_media_list_player_play</c>.</summary>
        public void Play() => libvlc_media_list_player_play(this);

        /// <summary>Toggles pause (or resumes) the media list. <c>libvlc_media_list_player_pause</c>.</summary>
        public void Pause() => libvlc_media_list_player_pause(this);

        /// <summary>Stops playing the media list asynchronously. <c>libvlc_media_list_player_stop_async</c>.</summary>
        public void Stop() => libvlc_media_list_player_stop_async(this);

        /// <summary>Plays the next item from the media list. <c>libvlc_media_list_player_next</c>.</summary>
        /// <returns>0 on success, -1 if there is no next item.</returns>
        public int Next() => libvlc_media_list_player_next(this);

        /// <summary>Plays the previous item from the media list. <c>libvlc_media_list_player_previous</c>.</summary>
        /// <returns>0 on success, -1 if there is no previous item.</returns>
        public int Previous() => libvlc_media_list_player_previous(this);

        /// <summary>
        /// Plays the media list item at the given position.
        /// <c>libvlc_media_list_player_play_item_at_index</c>.
        /// </summary>
        /// <param name="index">Index in the media list to play.</param>
        /// <returns>0 on success, -1 if the item was not found.</returns>
        public int Play(int index) => libvlc_media_list_player_play_item_at_index(this, index);

        /// <summary>
        /// Plays the given media item from the list. <c>libvlc_media_list_player_play_item</c>.
        /// </summary>
        /// <param name="media">The media instance to play.</param>
        /// <returns>0 on success, -1 if the media is not part of the media list.</returns>
        public int Play(Media media) => libvlc_media_list_player_play_item(this, media);

        /// <summary>Current <see cref="LibVLCSharp.Core.State"/> of the media list player. <c>libvlc_media_list_player_get_state</c>.</summary>
        public State State => (State)libvlc_media_list_player_get_state(this);

        /// <summary>
        /// Pauses or resumes the media list. <c>libvlc_media_list_player_set_pause</c>.
        /// Since LibVLC 3.0.0.
        /// </summary>
        /// <param name="pause"><c>true</c> to pause; <c>false</c> to play/resume.</param>
        public void Pause(bool pause) => libvlc_media_list_player_set_pause(this, pause ? 1 : 0);

        /// <summary>
        /// Sets the playback mode for the playlist (write-only; libvlc exposes no getter).
        /// <c>libvlc_media_list_player_set_playback_mode</c>.
        /// </summary>
        /// <param name="mode">The playback mode specification.</param>
        public void PlaybackMode(PlaybackMode mode) => libvlc_media_list_player_set_playback_mode(this, (libvlc_playback_mode_t)mode);

        private EventManager Events =>
            _events ??= new EventManager(libvlc_media_list_player_event_manager(this), Dispatch);

        private void Dispatch(libvlc_event_t* e,IntPtr _)
        {
            switch ((libvlc_event_e)e->type)
            {
                case libvlc_event_e.libvlc_MediaListPlayerPlayed: { var h = _played; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaListPlayerStopped: { var h = _stopped; if (h != null) h(this, EventArgs.Empty); break; }
                case libvlc_event_e.libvlc_MediaListPlayerNextItemSet:
                    { var h = _nextItemSet; if (h != null) h(this, new MediaEventArgs((IntPtr)e->u.media_list_player_next_item_set.item)); break; }
            }
        }

        private EventHandler? _played, _stopped;
        private EventHandler<MediaEventArgs>? _nextItemSet;

        /// <summary>Raised when the media list has been played through. <c>libvlc_MediaListPlayerPlayed</c>.</summary>
        public event EventHandler Played { add => Events.Attach(ref _played, value, libvlc_event_e.libvlc_MediaListPlayerPlayed); remove => Events.Detach(ref _played, value, libvlc_event_e.libvlc_MediaListPlayerPlayed); }
        /// <summary>Raised when the media list player has stopped. <c>libvlc_MediaListPlayerStopped</c>.</summary>
        public event EventHandler Stopped { add => Events.Attach(ref _stopped, value, libvlc_event_e.libvlc_MediaListPlayerStopped); remove => Events.Detach(ref _stopped, value, libvlc_event_e.libvlc_MediaListPlayerStopped); }
        /// <summary>
        /// Raised when the next item in the media list has been set.
        /// <c>libvlc_MediaListPlayerNextItemSet</c>. Call <see cref="MediaEventArgs.GetMedia"/> (default retains).
        /// </summary>
        public event EventHandler<MediaEventArgs> NextItemSet { add => Events.Attach(ref _nextItemSet, value, libvlc_event_e.libvlc_MediaListPlayerNextItemSet); remove => Events.Detach(ref _nextItemSet, value, libvlc_event_e.libvlc_MediaListPlayerNextItemSet); }
    }
}
