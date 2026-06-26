using System;
using System.Collections;
using System.Collections.Generic;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>
    /// An owned, read-only list of <see cref="MediaTrack"/> (<c>libvlc_media_tracklist_t</c>). Release
    /// it when done. Items are value snapshots, so they remain valid after the list is disposed.
    /// </summary>
    public class MediaTrackList : NativeReference, IReadOnlyList<MediaTrack>
    {
        /// <summary>Wraps an existing native handle.</summary>
        /// <param name="handle">Native <c>libvlc_media_tracklist_t*</c>.</param>
        public MediaTrackList(IntPtr handle) : base(handle) { }

        /// <summary>Implicit conversion to the native <c>libvlc_media_tracklist_t*</c> (null for a null list).</summary>
        public static unsafe implicit operator libvlc_media_tracklist_t*(MediaTrackList list) =>
            list is null ? null : (libvlc_media_tracklist_t*)list.NativeHandle;

        protected override unsafe void Release(IntPtr handle) =>
            libvlc_media_tracklist_delete((libvlc_media_tracklist_t*)handle);

        /// <summary>
        /// Returns the number of tracks in the list. <c>libvlc_media_tracklist_count</c>.
        /// </summary>
        /// <returns>Number of tracks, or 0 if the list is empty.</returns>
        /// <remarks>Since LibVLC 4.0.0.</remarks>
        public unsafe int Count => (int)libvlc_media_tracklist_count(this).ToUInt32();

        /// <summary>
        /// Returns the track at the given index (a value snapshot). <c>libvlc_media_tracklist_at</c>.
        /// </summary>
        /// <param name="index">Valid index in the range [0, <see cref="Count"/>).</param>
        /// <returns>
        /// A valid <see cref="MediaTrack"/> (never null if <see cref="Count"/> returned a valid count).
        /// </returns>
        /// <remarks>
        /// Since LibVLC 4.0.0.
        /// <b>Warning:</b> behaviour is undefined if the index is out of range.
        /// </remarks>
        public unsafe MediaTrack this[int index]
        {
            get
            {
                if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
                // Borrowed pointer (owned by the list); copied into a value MediaTrack on the spot.
                return new MediaTrack(libvlc_media_tracklist_at(this, new UIntPtr((uint)index)));
            }
        }

        public IEnumerator<MediaTrack> GetEnumerator()
        {
            int count = Count;
            for (int i = 0; i < count; i++) yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    /// <summary>Audio-specific track details (present when <see cref="MediaTrack.Type"/> is <see cref="TrackType.Audio"/>).</summary>
    public readonly struct AudioTrackInfo
    {
        internal unsafe AudioTrackInfo(libvlc_audio_track_t* a)
        {
            Channels = a->i_channels;
            Rate = a->i_rate;
        }

        /// <summary>Number of audio channels. <c>libvlc_audio_track_t.i_channels</c>.</summary>
        public uint Channels { get; }
        /// <summary>Audio sample rate in Hz. <c>libvlc_audio_track_t.i_rate</c>.</summary>
        public uint Rate { get; }
    }

    /// <summary>Video-specific track details (present when <see cref="MediaTrack.Type"/> is <see cref="TrackType.Video"/>).</summary>
    public readonly struct VideoTrackInfo
    {
        internal unsafe VideoTrackInfo(libvlc_video_track_t* v)
        {
            Width = v->i_width;
            Height = v->i_height;
            SarNum = v->i_sar_num;
            SarDen = v->i_sar_den;
            FrameRateNum = v->i_frame_rate_num;
            FrameRateDen = v->i_frame_rate_den;
            Orientation = (VideoOrient)v->i_orientation;
            Projection = (VideoProjection)v->i_projection;
            Multiview = (VideoMultiview)v->i_multiview;
        }

        /// <summary>Video frame width in pixels. <c>libvlc_video_track_t.i_width</c>.</summary>
        public uint Width { get; }
        /// <summary>Video frame height in pixels. <c>libvlc_video_track_t.i_height</c>.</summary>
        public uint Height { get; }
        /// <summary>
        /// Sample aspect ratio numerator. <c>libvlc_video_track_t.i_sar_num</c>.
        /// </summary>
        /// <remarks>The display aspect ratio is Width/Height * SarNum/SarDen.</remarks>
        public uint SarNum { get; }
        /// <summary>
        /// Sample aspect ratio denominator. <c>libvlc_video_track_t.i_sar_den</c>.
        /// </summary>
        public uint SarDen { get; }
        /// <summary>Frame rate numerator. <c>libvlc_video_track_t.i_frame_rate_num</c>.</summary>
        public uint FrameRateNum { get; }
        /// <summary>Frame rate denominator. <c>libvlc_video_track_t.i_frame_rate_den</c>.</summary>
        public uint FrameRateDen { get; }
        /// <summary>
        /// Display orientation. <c>libvlc_video_track_t.i_orientation</c>.
        /// </summary>
        public VideoOrient Orientation { get; }
        /// <summary>
        /// Video projection type (e.g. rectangular, equirectangular for 360° spherical).
        /// <c>libvlc_video_track_t.i_projection</c>.
        /// </summary>
        public VideoProjection Projection { get; }
        /// <summary>
        /// Stereoscopic (multiview) layout. <c>libvlc_video_track_t.i_multiview</c>.
        /// </summary>
        public VideoMultiview Multiview { get; }
    }

    /// <summary>
    /// An immutable snapshot of a media track (<c>libvlc_media_track_t</c>). Field values are copied
    /// out of the native struct on construction, so the instance never references native memory and
    /// has no lifetime concerns. Obtained from <see cref="MediaTrackList"/>,
    /// <see cref="MediaPlayer.GetSelectedTrack"/> or <see cref="MediaPlayer.GetTrack"/>. Select it with
    /// <see cref="MediaPlayer.SelectTrack"/>, which uses the stable <see cref="Id"/>.
    /// </summary>
    public readonly struct MediaTrack
    {
        internal unsafe MediaTrack(libvlc_media_track_t* t)
        {
            Type = (TrackType)t->i_type;
            Id = ((IntPtr)t->psz_id).GetUtf8()!; // psz_id (const char*) is always set by libvlc
            IsIdStable = t->id_stable.ToBool();
            Name = ((IntPtr)t->psz_name).GetUtf8();
            Language = ((IntPtr)t->psz_language).GetUtf8();
            Description = ((IntPtr)t->psz_description).GetUtf8();
            Codec = t->i_codec;
            OriginalFourCC = t->i_original_fourcc;
            Profile = t->i_profile;
            Level = t->i_level;
            Bitrate = t->i_bitrate;
            IsSelected = t->selected.ToBool();
            InternalId = t->i_id;
            // libvlc 4.0 nightly 202606260430 renamed the anonymous union field from Anonymous to u (union libvlc_media_track_data).
            Audio = Type == TrackType.Audio && t->u.audio != null ? new AudioTrackInfo(t->u.audio) : (AudioTrackInfo?)null;
            Video = Type == TrackType.Video && t->u.video != null ? new VideoTrackInfo(t->u.video) : (VideoTrackInfo?)null;
            SubtitleEncoding = Type == TrackType.Text && t->u.subtitle != null ? ((IntPtr)t->u.subtitle->psz_encoding).GetUtf8() : null;
        }

        /// <summary>Track kind (audio/video/text). <c>i_type</c>.</summary>
        public TrackType Type { get; }
        /// <summary>
        /// String identifier of the track. <c>psz_id</c>. Can be saved and passed to
        /// <see cref="MediaPlayer.SelectTracksByIds"/> to restore the track selection across
        /// playback instances.
        /// </summary>
        public string Id { get; }
        /// <summary>
        /// Whether <see cref="Id"/> is stable. <c>id_stable</c>. A stable identifier is
        /// certified to be the same across different playback instances for the same track.
        /// </summary>
        public bool IsIdStable { get; }
        /// <summary>
        /// Human-readable name of the track, or null. <c>psz_name</c>.
        /// </summary>
        /// <remarks>Only valid when the track is fetched from a media player.</remarks>
        public string? Name { get; }
        /// <summary>ISO language code of the track, or null. <c>psz_language</c>.</summary>
        public string? Language { get; }
        /// <summary>Human-readable description of the track, or null. <c>psz_description</c>.</summary>
        public string? Description { get; }
        /// <summary>Codec FourCC identifier. <c>i_codec</c>.</summary>
        public uint Codec { get; }
        /// <summary>Original codec FourCC before any remapping. <c>i_original_fourcc</c>.</summary>
        public uint OriginalFourCC { get; }
        /// <summary>Codec-specific profile, or -1 if not applicable. <c>i_profile</c>.</summary>
        public int Profile { get; }
        /// <summary>Codec-specific level, or -1 if not applicable. <c>i_level</c>.</summary>
        public int Level { get; }
        /// <summary>Track bitrate in bits per second. <c>i_bitrate</c>.</summary>
        public uint Bitrate { get; }
        /// <summary>
        /// Whether the track is currently selected. <c>selected</c>.
        /// </summary>
        /// <remarks>Only valid when the track is fetched from a media player.</remarks>
        public bool IsSelected { get; }
        /// <summary>
        /// Legacy numeric track identifier. <c>i_id</c>.
        /// </summary>
        /// <remarks>Deprecated by libvlc; prefer <see cref="Id"/>.</remarks>
        public int InternalId { get; }
        /// <summary>
        /// Audio-specific details when <see cref="Type"/> is <see cref="TrackType.Audio"/>; otherwise null.
        /// </summary>
        public AudioTrackInfo? Audio { get; }
        /// <summary>
        /// Video-specific details when <see cref="Type"/> is <see cref="TrackType.Video"/>; otherwise null.
        /// </summary>
        public VideoTrackInfo? Video { get; }
        /// <summary>
        /// Subtitle text encoding (e.g. <c>"UTF-8"</c>) when <see cref="Type"/> is
        /// <see cref="TrackType.Text"/>; otherwise null. <c>libvlc_subtitle_track_t.psz_encoding</c>.
        /// </summary>
        public string? SubtitleEncoding { get; }
    }

}
