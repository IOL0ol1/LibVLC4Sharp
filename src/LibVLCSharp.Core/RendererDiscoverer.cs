using System;
using System.Runtime.InteropServices;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>
    /// Discovers renderer targets (<c>libvlc_renderer_discoverer_t</c>) available on the local network,
    /// such as Chromecasts. After construction, subscribe to <see cref="ItemAdded"/> /
    /// <see cref="ItemRemoved"/>, then call <see cref="Start"/> to begin discovery. Assign a discovered
    /// <see cref="RendererItem"/> to <see cref="MediaPlayer.Renderer"/> to render on it. Events fire on
    /// libvlc's own threads. libvlc 4.0 registers the callbacks at creation (<c>libvlc_renderer_discoverer_cbs</c>).
    /// </summary>
    public unsafe class RendererDiscoverer : NativeReference
    {
        private GCHandle _cbs; // opaque handed to libvlc_renderer_discoverer_new; target wired to this after construction

        /// <summary>Creates a renderer discoverer by service name, with its item events wired. <c>libvlc_renderer_discoverer_new</c>.</summary>
        internal RendererDiscoverer(LibVLC vlc, string name) : this(Create(vlc, name)) { }

        private RendererDiscoverer(Creation c) : base(c.Handle)
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
            var handle = (IntPtr)libvlc_renderer_discoverer_new(vlc, u, &cbs, GCHandle.ToIntPtr(gch));
            if (handle == IntPtr.Zero) gch.Free();
            return new Creation(handle, gch);
        }

        /// <summary>Implicit conversion to the native <c>libvlc_renderer_discoverer_t*</c> (null for a null discoverer).</summary>
        public static implicit operator libvlc_renderer_discoverer_t*(RendererDiscoverer? rendererDiscoverer) =>
            rendererDiscoverer is null ? null : (libvlc_renderer_discoverer_t*)rendererDiscoverer.NativeHandle;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_cbs.IsAllocated) _cbs.Free();
        }

        protected override void Release(IntPtr handle) =>
            libvlc_renderer_discoverer_destroy((libvlc_renderer_discoverer_t*)handle); // was libvlc_renderer_discoverer_release (renamed in libvlc 4.0)

        /// <summary>Starts renderer discovery (-1 on error, 0 otherwise). <c>libvlc_renderer_discoverer_start</c>.</summary>
        public int Start() => libvlc_renderer_discoverer_start(this);

        /// <summary>Stops renderer discovery. <c>libvlc_renderer_discoverer_stop</c>.</summary>
        public void Stop() => libvlc_renderer_discoverer_stop(this);

        /// <summary><c>on_item_added</c> — a new renderer item was discovered. Call <see cref="RendererItemEventArgs.GetItem"/> (default retains).</summary>
        public event EventHandler<RendererItemEventArgs>? ItemAdded;
        /// <summary><c>on_item_removed</c> — a previously discovered renderer item was removed.</summary>
        public event EventHandler<RendererItemEventArgs>? ItemRemoved;

        // Shared callbacks: one cbs struct + delegate set for the whole process (opaque differs per instance).
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void DItem(IntPtr o, libvlc_renderer_item_t* item);

        private static RendererDiscoverer? Owner(IntPtr o) => o == IntPtr.Zero ? null : GCHandle.FromIntPtr(o).Target as RendererDiscoverer;

        private static readonly DItem s_itemAdded = (o, it) => { var d = Owner(o); d?.ItemAdded?.Invoke(d, new RendererItemEventArgs((IntPtr)it)); };
        private static readonly DItem s_itemRemoved = (o, it) => { var d = Owner(o); d?.ItemRemoved?.Invoke(d, new RendererItemEventArgs((IntPtr)it)); };

        private static readonly libvlc_renderer_discoverer_cbs s_cbs = new libvlc_renderer_discoverer_cbs
        {
            version = 0,
            on_item_added = Marshal.GetFunctionPointerForDelegate(s_itemAdded),
            on_item_removed = Marshal.GetFunctionPointerForDelegate(s_itemRemoved),
        };
    }

    /// <summary>Payload of <see cref="RendererDiscoverer.ItemAdded"/> / <see cref="RendererDiscoverer.ItemRemoved"/>.</summary>
    public readonly struct RendererItemEventArgs
    {
        private readonly IntPtr item; // borrowed libvlc_renderer_item_t*
        public RendererItemEventArgs(IntPtr item) => this.item = item;

        /// <summary>Returns the renderer item. <paramref name="addRef"/> true (default) retains a copy you must dispose; false is a borrowed view valid only inside the handler.</summary>
        public unsafe RendererItem? GetItem(bool addRef = true)
        {
            var p = (libvlc_renderer_item_t*)item;
            if (p == null) return null;
            return addRef
                ? new RendererItem((IntPtr)libvlc_renderer_item_retain(p)) // was libvlc_renderer_item_hold (renamed in libvlc 4.0)
                : new RendererItem((IntPtr)p, addRef: false);
        }
    }

    /// <summary>
    /// A renderer item (<c>libvlc_renderer_item_t</c>), e.g. a Chromecast, delivered by
    /// <see cref="RendererDiscoverer.ItemAdded"/> and assigned to <see cref="MediaPlayer.Renderer"/> to
    /// render on that target. An item is valid until <see cref="RendererDiscoverer.ItemRemoved"/> fires for it.
    /// </summary>
    public unsafe class RendererItem : NativeReference
    {
        /// <summary>Wraps an existing native handle (does not add a reference).</summary>
        /// <param name="handle">Native <c>libvlc_renderer_item_t*</c>.</param>
        public RendererItem(IntPtr handle) : base(handle) { }

        /// <summary>
        /// Wraps a native handle. When <paramref name="addRef"/> is <c>false</c> the item is a borrowed
        /// view that is never released and is valid only for as long as its owner keeps it alive
        /// (e.g. a renderer item obtained from <see cref="RendererItemEventArgs.GetItem"/> with
        /// <c>addRef: false</c>, valid only inside the event handler). Do not dispose a borrowed item.
        /// </summary>
        /// <param name="handle">Native <c>libvlc_renderer_item_t*</c>.</param>
        /// <param name="addRef"><c>true</c> to retain ownership and release on dispose; <c>false</c> for a borrowed view.</param>
        public RendererItem(IntPtr handle, bool addRef) : base(handle, addRef) { }

        /// <summary>Implicit conversion to the native <c>libvlc_renderer_item_t*</c> (null for a null item).</summary>
        public static implicit operator libvlc_renderer_item_t*(RendererItem? rendererItem) =>
            rendererItem is null ? null : (libvlc_renderer_item_t*)rendererItem.NativeHandle;

        protected override void Release(IntPtr handle) => libvlc_renderer_item_release((libvlc_renderer_item_t*)handle);

        /// <summary>Human-readable name of the renderer item. <c>libvlc_renderer_item_name</c>.</summary>
        public string Name => ((IntPtr)libvlc_renderer_item_name(this)).GetUtf8()!;

        /// <summary>Non-translated type, e.g. <c>"chromecast"</c>. <c>libvlc_renderer_item_type</c>.</summary>
        public string Type => ((IntPtr)libvlc_renderer_item_type(this)).GetUtf8()!;

        /// <summary>Icon URI, or <see langword="null"/> if none. <c>libvlc_renderer_item_icon_uri</c>.</summary>
        public string? IconUri => ((IntPtr)libvlc_renderer_item_icon_uri(this)).GetUtf8();

        /// <summary>
        /// Capability flags. <c>libvlc_renderer_item_flags</c>. Bit 0x0001 (<c>LIBVLC_RENDERER_CAN_AUDIO</c>) —
        /// can render audio; bit 0x0002 (<c>LIBVLC_RENDERER_CAN_VIDEO</c>) — can render video.
        /// </summary>
        public int Flags => libvlc_renderer_item_flags(this);
    }
}
