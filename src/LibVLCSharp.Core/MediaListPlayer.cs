using System;
using System.Runtime.InteropServices;
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
        // cbs opaque (a GCHandle) handed to libvlc_media_list_player_new; libvlc registers the shared
        // MediaPlayer callbacks on the list player's internal media player with it. The MediaPlayer getter
        // points this GCHandle's target at the wrapped player so its events fire (uniform with CreateMediaPlayer).
        private GCHandle _playerCbs;

        /// <summary>Creates a media list player bound to <paramref name="vlc"/>, with its internal player's events wired. <c>libvlc_media_list_player_new</c>.</summary>
        internal MediaListPlayer(LibVLC vlc) : this(Create(vlc)) { }

        private MediaListPlayer(Creation c) : base(c.Handle) => _playerCbs = c.Opaque;

        private readonly struct Creation
        {
            public readonly IntPtr Handle;
            public readonly GCHandle Opaque;
            public Creation(IntPtr handle, GCHandle opaque) { Handle = handle; Opaque = opaque; }
        }

        private static Creation Create(LibVLC vlc)
        {
            if (vlc is null) throw new ArgumentNullException(nameof(vlc));
            var gch = GCHandle.Alloc(null);            // target set to the wrapped player in the MediaPlayer getter
            var cbs = MediaPlayer.SharedCbs;            // local copy; libvlc reads it during the call
            var handle = (IntPtr)libvlc_media_list_player_new(vlc, &cbs, GCHandle.ToIntPtr(gch));
            if (handle == IntPtr.Zero) gch.Free();      // base ctor will throw on the null handle
            return new Creation(handle, gch);
        }

        /// <summary>Implicit conversion to the native <c>libvlc_media_list_player_t*</c> (null for a null player).</summary>
        public static implicit operator libvlc_media_list_player_t*(MediaListPlayer? mediaListPlayer) =>
            mediaListPlayer is null ? null : (libvlc_media_list_player_t*)mediaListPlayer.NativeHandle;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);                    // release the list player; after this libvlc won't call back
            if (_playerCbs.IsAllocated) _playerCbs.Free();
        }

        protected override void Release(IntPtr handle) =>
            libvlc_media_list_player_release((libvlc_media_list_player_t*)handle);

        private MediaPlayer? _mediaPlayer;
        private MediaList? _mediaList;

        /// <summary>
        /// The internal media player used by this media list player.
        /// <c>libvlc_media_list_player_get_media_player</c> — returns a cached owning wrapper; the extra
        /// reference the getter takes is released. Its events are wired: the shared player callbacks were
        /// registered on this player at <see cref="LibVLC.CreateMediaListPlayer"/>, and accessing this
        /// getter routes them to the returned <see cref="Core.MediaPlayer"/>.
        /// </summary>
        /// <remarks>libvlc 4.0 removed <c>libvlc_media_list_player_set_media_player</c>; the player is bound at construction.</remarks>
        public MediaPlayer? MediaPlayer
        {
            get
            {
                var mp = Reconcile(ref _mediaPlayer, (IntPtr)libvlc_media_list_player_get_media_player(this), // +1 ref, or null
                    static h => libvlc_media_player_release((libvlc_media_player_t*)h),
                    static h => new MediaPlayer(h));
                if (mp != null && _playerCbs.IsAllocated) _playerCbs.Target = mp; // route player callbacks to the wrapper so its events fire
                return mp;
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

    }
}
