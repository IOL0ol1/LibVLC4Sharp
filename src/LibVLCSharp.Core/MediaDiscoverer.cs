using System;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>
    /// A media discoverer object (<c>libvlc_media_discoverer_t</c>) that finds available media via
    /// various means — locally (e.g. user media directories), from peripherals (e.g. video capture
    /// devices), on the local network (e.g. SAP), or on the Internet (e.g. Internet radios). Discovered
    /// items are collected into <see cref="MediaList"/>. After construction, attach to media-list events
    /// and call <see cref="Start"/> to begin discovery.
    /// </summary>
    public unsafe class MediaDiscoverer : NativeReference
    {
 
        /// <summary>Wraps an existing native handle.</summary>
        /// <param name="handle">Native <c>libvlc_media_discoverer_t*</c>.</param>
        public MediaDiscoverer(IntPtr handle) : base(handle) { }
 
        /// <summary>Implicit conversion to the native <c>libvlc_media_discoverer_t*</c> (null for a null discoverer).</summary>
        public static implicit operator libvlc_media_discoverer_t*(MediaDiscoverer? mediaDiscoverer) =>
            mediaDiscoverer is null ? null : (libvlc_media_discoverer_t*)mediaDiscoverer.NativeHandle;

        protected override void Release(IntPtr handle) =>
            libvlc_media_discoverer_release((libvlc_media_discoverer_t*)handle);

        /// <summary>
        /// Starts media discovery. <c>libvlc_media_discoverer_start</c>.
        /// To stop it, call <see cref="Stop"/> directly.
        /// </summary>
        /// <returns>-1 in case of error, 0 otherwise.</returns>
        /// <remarks>Since LibVLC 3.0.0.</remarks>
        public int Start() => libvlc_media_discoverer_start(this);

        /// <summary>
        /// Stops media discovery. <c>libvlc_media_discoverer_stop</c>.
        /// </summary>
        /// <remarks>Since LibVLC 3.0.0.</remarks>
        public void Stop() => libvlc_media_discoverer_stop(this);

        /// <summary>
        /// Queries whether the media discoverer is running. <c>libvlc_media_discoverer_is_running</c>.
        /// </summary>
        /// <value><c>true</c> if running; <c>false</c> otherwise.</value>
        public bool IsRunning => libvlc_media_discoverer_is_running(this).ToBool();

        private MediaList? _mediaList;

        /// <summary>
        /// Gets the media list populated by the discoverer. <c>libvlc_media_discoverer_media_list</c>.
        /// Returns a stable cached wrapper; the extra reference the getter takes is released. libvlc
        /// creates the list with the discoverer, so it is never null for a valid discoverer.
        /// </summary>
        /// <returns>The list of discovered media items.</returns>
        public MediaList MediaList
        {
            get => Reconcile(ref _mediaList, (IntPtr)libvlc_media_discoverer_media_list(this), // +1 ref
                static h => libvlc_media_list_release((libvlc_media_list_t*)h),
                static h => new MediaList(h))!;
        }
    }
}
