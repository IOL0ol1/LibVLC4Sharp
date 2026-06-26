using System;
using System.Runtime.InteropServices;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>
    /// A media discoverer (<c>libvlc_media_discoverer_t</c>) that finds available media — locally, from
    /// peripherals, on the local network, or on the Internet. After construction, subscribe to
    /// <see cref="MediaAdded"/> / <see cref="MediaRemoved"/>, then call <see cref="Start"/>. libvlc 4.0
    /// registers the callbacks at creation (<c>libvlc_media_discoverer_cbs</c>) and dropped the media-list
    /// accessor. Events fire on libvlc's own threads.
    /// </summary>
    public unsafe class MediaDiscoverer : NativeReference
    {
        private GCHandle _cbs; // opaque handed to libvlc_media_discoverer_new; target wired to this after construction

        /// <summary>Creates a media discoverer by service name, with its events wired. <c>libvlc_media_discoverer_new</c>.</summary>
        internal MediaDiscoverer(LibVLC vlc, string name) : this(Create(vlc, name)) { }

        private MediaDiscoverer(Creation c) : base(c.Handle)
        {
            _cbs = c.Opaque;
            _cbs.Target = this;
        }

        private readonly struct Creation
        {
            public readonly IntPtr Handle;
            public readonly GCHandle Opaque;
            public Creation(IntPtr handle, GCHandle opaque) { Handle = handle; Opaque = opaque; }
        }

        private static Creation Create(LibVLC vlc, string name)
        {
            if (vlc is null) throw new ArgumentNullException(nameof(vlc));
            var gch = GCHandle.Alloc(null);
            var cbs = s_cbs;
            using var u = new Utf8Buffer(name);
            var handle = (IntPtr)libvlc_media_discoverer_new(vlc, u, &cbs, GCHandle.ToIntPtr(gch));
            if (handle == IntPtr.Zero) gch.Free();
            return new Creation(handle, gch);
        }

        /// <summary>Implicit conversion to the native <c>libvlc_media_discoverer_t*</c> (null for a null discoverer).</summary>
        public static implicit operator libvlc_media_discoverer_t*(MediaDiscoverer? mediaDiscoverer) =>
            mediaDiscoverer is null ? null : (libvlc_media_discoverer_t*)mediaDiscoverer.NativeHandle;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_cbs.IsAllocated) _cbs.Free();
        }

        protected override void Release(IntPtr handle) =>
            libvlc_media_discoverer_destroy((libvlc_media_discoverer_t*)handle); // was libvlc_media_discoverer_release (renamed in libvlc 4.0)

        /// <summary>Starts media discovery (-1 on error, 0 otherwise). <c>libvlc_media_discoverer_start</c>.</summary>
        public int Start() => libvlc_media_discoverer_start(this);

        /// <summary>Stops media discovery. <c>libvlc_media_discoverer_stop</c>.</summary>
        public void Stop() => libvlc_media_discoverer_stop(this);

        /// <summary>Whether the discoverer is running. <c>libvlc_media_discoverer_is_running</c>.</summary>
        public bool IsRunning => libvlc_media_discoverer_is_running(this).ToBool();

        /// <summary><c>on_media_added</c> — a media was discovered. Call <see cref="MediaAddedEventArgs.GetMedia"/> (default retains).</summary>
        public event EventHandler<MediaAddedEventArgs>? MediaAdded;
        /// <summary><c>on_media_removed</c> — a previously discovered media was removed.</summary>
        public event EventHandler<MediaEventArgs>? MediaRemoved;

        // Shared callbacks: one cbs struct + delegate set for the whole process (opaque differs per instance).
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DMediaAdded(IntPtr o, libvlc_media_t* media, libvlc_media_t* parent);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DMedia(IntPtr o, libvlc_media_t* media);

        private static MediaDiscoverer? Owner(IntPtr o) => o == IntPtr.Zero ? null : GCHandle.FromIntPtr(o).Target as MediaDiscoverer;

        private static readonly DMediaAdded s_mediaAdded = (o, m, parent) => { var d = Owner(o); d?.MediaAdded?.Invoke(d, new MediaAddedEventArgs((IntPtr)m, (IntPtr)parent)); };
        private static readonly DMedia s_mediaRemoved = (o, m) => { var d = Owner(o); d?.MediaRemoved?.Invoke(d, new MediaEventArgs((IntPtr)m)); };

        private static readonly libvlc_media_discoverer_cbs s_cbs = new libvlc_media_discoverer_cbs
        {
            version = 0,
            on_media_added = Marshal.GetFunctionPointerForDelegate(s_mediaAdded),
            on_media_removed = Marshal.GetFunctionPointerForDelegate(s_mediaRemoved),
        };
    }

    /// <summary>Payload of <see cref="MediaDiscoverer.MediaAdded"/>: the discovered media and its parent node.</summary>
    public readonly struct MediaAddedEventArgs
    {
        private readonly IntPtr media;  // borrowed libvlc_media_t*
        private readonly IntPtr parent; // borrowed libvlc_media_t* (the container/node), may be null
        public MediaAddedEventArgs(IntPtr media, IntPtr parent) { this.media = media; this.parent = parent; }

        /// <summary>Returns the discovered media (see <see cref="MediaEventArgs.GetMedia"/>).</summary>
        public unsafe Media? GetMedia(bool addRef = true) => Wrap(media, addRef);

        /// <summary>Returns the parent (container) media, or null.</summary>
        public unsafe Media? GetParent(bool addRef = true) => Wrap(parent, addRef);

        private static unsafe Media? Wrap(IntPtr handle, bool addRef)
        {
            var p = (libvlc_media_t*)handle;
            if (p == null) return null;
            return addRef ? new Media((IntPtr)libvlc_media_retain(p)) : new Media((IntPtr)p, addRef: false);
        }
    }
}
