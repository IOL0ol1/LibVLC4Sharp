using System;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>
    /// Discovers renderer targets (<c>libvlc_renderer_discoverer_t</c>) available on the local network,
    /// such as Chromecasts. After construction, subscribe to <see cref="ItemAdded"/> and
    /// <see cref="ItemDeleted"/> events, then call <see cref="Start"/> to begin discovery. Assign a
    /// discovered <see cref="RendererItem"/> to <see cref="MediaPlayer.Renderer"/> to render on it.
    /// </summary>
    public unsafe class RendererDiscoverer : NativeReference
    {
        private EventManager? _events;
 
        /// <summary>Wraps an existing native handle.</summary>
        /// <param name="handle">Native <c>libvlc_renderer_discoverer_t*</c>.</param>
        public RendererDiscoverer(IntPtr handle) : base(handle) { }

        /// <summary>Implicit conversion to the native <c>libvlc_renderer_discoverer_t*</c> (null for a null discoverer).</summary>
        public static implicit operator libvlc_renderer_discoverer_t*(RendererDiscoverer? rendererDiscoverer) =>
            rendererDiscoverer is null ? null : (libvlc_renderer_discoverer_t*)rendererDiscoverer.NativeHandle;

        protected override void Dispose(bool disposing)
        {
            if (disposing) _events?.Dispose();
            base.Dispose(disposing);
        }

        protected override void Release(IntPtr handle) =>
            libvlc_renderer_discoverer_release((libvlc_renderer_discoverer_t*)handle);

        /// <summary>
        /// Starts renderer discovery. <c>libvlc_renderer_discoverer_start</c>.
        /// To stop it, call <see cref="Stop"/> directly.
        /// </summary>
        /// <returns>-1 in case of error, 0 otherwise.</returns>
        /// <remarks>Since LibVLC 3.0.0.</remarks>
        public int Start() => libvlc_renderer_discoverer_start(this);

        /// <summary>
        /// Stops renderer discovery. <c>libvlc_renderer_discoverer_stop</c>.
        /// </summary>
        /// <remarks>Since LibVLC 3.0.0.</remarks>
        public void Stop() => libvlc_renderer_discoverer_stop(this);

        private EventManager Events =>
            _events ??= new EventManager(libvlc_renderer_discoverer_event_manager(this), Dispatch);

        private void Dispatch(libvlc_event_t* e,IntPtr _)
        {
            switch ((libvlc_event_e)e->type)
            {
                case libvlc_event_e.libvlc_RendererDiscovererItemAdded:
                    { var h = _itemAdded; if (h != null) h(this, new RendererItemEventArgs((IntPtr)e->u.renderer_discoverer_item_added.item)); break; }
                case libvlc_event_e.libvlc_RendererDiscovererItemDeleted:
                    { var h = _itemDeleted; if (h != null) h(this, new RendererItemEventArgs((IntPtr)e->u.renderer_discoverer_item_deleted.item)); break; }
            }
        }

        private EventHandler<RendererItemEventArgs>? _itemAdded, _itemDeleted;

        /// <summary>
        /// Raised when a new renderer item is discovered. <c>libvlc_RendererDiscovererItemAdded</c>.
        /// Call <see cref="RendererItemEventArgs.GetItem"/> (default holds).
        /// </summary>
        public event EventHandler<RendererItemEventArgs> ItemAdded
        {
            add => Events.Attach(ref _itemAdded, value, libvlc_event_e.libvlc_RendererDiscovererItemAdded);
            remove => Events.Detach(ref _itemAdded, value, libvlc_event_e.libvlc_RendererDiscovererItemAdded);
        }

        /// <summary>
        /// Raised when a previously discovered renderer item is removed. <c>libvlc_RendererDiscovererItemDeleted</c>.
        /// Call <see cref="RendererItemEventArgs.GetItem"/> (default holds).
        /// </summary>
        public event EventHandler<RendererItemEventArgs> ItemDeleted
        {
            add => Events.Attach(ref _itemDeleted, value, libvlc_event_e.libvlc_RendererDiscovererItemDeleted);
            remove => Events.Detach(ref _itemDeleted, value, libvlc_event_e.libvlc_RendererDiscovererItemDeleted);
        }
    }

    /// <summary>
    /// A renderer item (<c>libvlc_renderer_item_t</c>), e.g. a Chromecast, passed via a
    /// <see cref="RendererDiscoverer.ItemAdded"/> event and assigned to
    /// <see cref="MediaPlayer.Renderer"/> to render on that target. An item is valid until the
    /// corresponding <see cref="RendererDiscoverer.ItemDeleted"/> event fires with the same pointer.
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
        /// <param name="addRef"><c>true</c> to hold ownership and release on dispose; <c>false</c> for a borrowed view.</param>
        public RendererItem(IntPtr handle, bool addRef) : base(handle, addRef) { }

        /// <summary>Implicit conversion to the native <c>libvlc_renderer_item_t*</c> (null for a null item).</summary>
        public static implicit operator libvlc_renderer_item_t*(RendererItem? rendererItem) =>
            rendererItem is null ? null : (libvlc_renderer_item_t*)rendererItem.NativeHandle;

        protected override void Release(IntPtr handle) => libvlc_renderer_item_release((libvlc_renderer_item_t*)handle);

        /// <summary>
        /// Gets the human-readable name of the renderer item. <c>libvlc_renderer_item_name</c>.
        /// </summary>
        /// <remarks>Since LibVLC 3.0.0.</remarks>
        public string Name => ((IntPtr)libvlc_renderer_item_name(this)).GetUtf8()!;

        /// <summary>
        /// Gets the (non-translated) type of the renderer item, e.g. <c>"chromecast"</c>.
        /// <c>libvlc_renderer_item_type</c>.
        /// </summary>
        /// <remarks>Since LibVLC 3.0.0.</remarks>
        public string Type => ((IntPtr)libvlc_renderer_item_type(this)).GetUtf8()!;

        /// <summary>
        /// Gets the icon URI of the renderer item, or <see langword="null"/> if none.
        /// <c>libvlc_renderer_item_icon_uri</c>.
        /// </summary>
        /// <remarks>Since LibVLC 3.0.0.</remarks>
        public string? IconUri => ((IntPtr)libvlc_renderer_item_icon_uri(this)).GetUtf8();

        /// <summary>
        /// Gets the capability flags of the renderer item. <c>libvlc_renderer_item_flags</c>.
        /// Bit 0x0001 (<c>LIBVLC_RENDERER_CAN_AUDIO</c>) — renderer can render audio.
        /// Bit 0x0002 (<c>LIBVLC_RENDERER_CAN_VIDEO</c>) — renderer can render video.
        /// </summary>
        /// <remarks>Since LibVLC 3.0.0.</remarks>
        public int Flags => libvlc_renderer_item_flags(this);
    }
}
