using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Threading;
using LibVLCSharp.Core;
using LibVLCSharp.Core.Interop;
using LibVLCSharp.WPF.Interop;
using CoreVideoEngine = LibVLCSharp.Core.VideoEngine;

namespace LibVLCSharp.WPF
{
    /// <summary>
    /// Owns the <c>libvlc_video_set_output_callbacks</c> (D3D11 engine) integration — the managed analog
    /// of <c>render_context</c> in VLC's <c>doc/libvlc/d3d11_player.cpp</c>, adapted for D3DImage. Unlike
    /// the sample (which presents a quad to an HWND swap chain), this presents through a
    /// <see cref="D3DImage"/>, which is D3D9-only. So it keeps a host <c>IDirect3D9Ex</c> device whose
    /// shared surface the image composites, and bridges D3D11→D3D9 with a shared texture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Engine ownership is inverted vs the D3D9 path: <b>we</b> create the <c>ID3D11Device</c>/context and
    /// hand the context to libvlc at setup (libvlc renders on it); libvlc does not hand a device back.
    /// HW decode (D3D11VA) requires the device's <c>VIDEO_SUPPORT</c> flag.
    /// </para>
    /// <para>
    /// Sharing direction (<b>Direction B</b>, as in Microsoft's WPFDXInterop <c>D3D11Image</c>): the
    /// D3D11 device creates the render-target texture with the <b>legacy</b> <c>MISC_SHARED</c> flag (NOT
    /// <c>NTHANDLE</c> — D3D9Ex can only open legacy handles) in BGRA; the host D3D9Ex device opens that
    /// shared handle via <c>CreateTexture(pSharedHandle)</c> to get the surface the D3DImage shows. libvlc
    /// is handed the D3D11 render-target view through the <c>select_plane</c> callback.
    /// </para>
    /// <para>
    /// Threading mirrors <see cref="D3D9VideoOutput"/>: <c>_gpuLock</c> serializes the GPU-resource
    /// lifecycle across the UI thread and the libvlc threads; it is NEVER held across
    /// <c>set_output_callbacks</c> and NEVER nested with <c>_sizeLock</c>. Recovery is marshalled to the
    /// UI thread.
    /// </para>
    /// </remarks>
    internal sealed unsafe class D3D11VideoOutput : IVideoOutput
    {
        private readonly Dispatcher _dispatcher;

        // Guards the GPU-resource lifecycle across the UI thread and the libvlc threads. NEVER held
        // across set_output_callbacks; NEVER nested with _sizeLock.
        private readonly object _gpuLock = new object();

        // Host D3D9Ex device (owns the surface WPF composites). Opens the D3D11 shared handle.
        private IntPtr _d3d;          // IDirect3D9Ex*
        private IntPtr _device;       // IDirect3DDevice9Ex*
        private IntPtr _hostTexture;  // IDirect3DTexture9* (opened from the shared handle)
        private IntPtr _hostSurface;  // IDirect3DSurface9* (what the D3DImage displays)

        // Our D3D11 device/context — handed to libvlc at setup; libvlc renders into _d3d11Texture.
        private IntPtr _d3d11Device;  // ID3D11Device*
        private IntPtr _d3d11Context; // ID3D11DeviceContext* (immediate)
        private IntPtr _d3d11Texture; // ID3D11Texture2D* (shared render target libvlc draws into)
        private IntPtr _rtv;          // ID3D11RenderTargetView* over _d3d11Texture (handed to libvlc)
        private IntPtr _sharedHandle; // legacy shared handle aliasing _d3d11Texture and _hostTexture

        private uint _width, _height;

        // The currently-attached managed player (null if none); borrowed — never disposed here.
        // Mutated only on the UI thread. SetOutputCallbacks installs/removes our callbacks on it.
        private MediaPlayer _attachedPlayer;

        // The Core PascalCase alias callback delegates (signature-identical twins of the libvlc_*_cb
        // typedefs). Kept alive for the object's lifetime (GC must not collect them); MediaPlayer also
        // roots them while installed.
        private readonly VideoOutputSetupCallback _setupCb;
        private readonly VideoOutputCleanupCallback _cleanupCb;
        private readonly VideoUpdateOutputCallback _updateOutputCb;
        private readonly VideoSwapCallback _swapCb;
        private readonly VideoMakeCurrentCallback _makeCurrentCb;
        private readonly VideoOutputSetWindowCallback _setWindowCb;
        private readonly VideoOutputSelectPlaneCallback _selectPlaneCb;

        // Resize reporting (identical to the D3D9 path): libvlc hands us _reportSize via set_window;
        // calling it asks libvlc to render at a given size. Invoked under _sizeLock so a concurrent
        // OnCleanup (which clears it under the same lock) can't make us call a stale pointer.
        private readonly object _sizeLock = new object();
        private VideoOutputResizeCallback _reportSize;
        private IntPtr _reportOpaque;
        private uint _pixelWidth, _pixelHeight;

        // Present hand-off to the UI thread, consumed in PrepareBackBuffer.
        private volatile bool _backBufferChanged;
        private volatile bool _frameReady;

        private int _recovering;      // 0/1, via Interlocked: at most one device recovery in flight
        private bool _disposed;

        public D3D11VideoOutput(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _setupCb = OnSetup;
            _cleanupCb = OnCleanup;
            _updateOutputCb = OnUpdateOutput;
            _swapCb = OnSwap;
            _makeCurrentCb = OnMakeCurrent;
            _setWindowCb = OnSetWindow;
            _selectPlaneCb = OnSelectPlane;
        }

        /// <summary>True when there is a new frame or a back-buffer change to present.</summary>
        public bool HasUpdate => _frameReady || _backBufferChanged;

        // --- attach / detach (UI thread) ------------------------------------------------------

        public void Attach(MediaPlayer mediaPlayer)
        {
            if (_disposed || ReferenceEquals(mediaPlayer, _attachedPlayer)) return;
            Detach();
            _attachedPlayer = mediaPlayer;
            if (mediaPlayer == null) return;
            EnsureDevice();
            if (_device != IntPtr.Zero && _d3d11Device != IntPtr.Zero)
                InstallCallbacks(mediaPlayer);
        }

        public void Detach()
        {
            MediaPlayer mp = _attachedPlayer;
            _attachedPlayer = null;
            if (mp != null)
                DisableCallbacks(mp);   // NOT under any lock — libvlc may re-enter OnCleanup (which locks)
            lock (_sizeLock) { _reportSize = null; _reportOpaque = IntPtr.Zero; }
            lock (_gpuLock)
            {
                _backBufferChanged = true;
                ReleaseSurfacesLocked();
            }
        }

        // --- present hand-off (called by VideoView on the UI thread) --------------------------

        public bool PrepareBackBuffer(D3DImage image)
        {
            _frameReady = false;
            if (_backBufferChanged)
            {
                lock (_gpuLock)
                {
                    _backBufferChanged = false;
                    image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _hostSurface, enableSoftwareFallback: true);
                }
            }
            return image.PixelWidth > 0 && image.PixelHeight > 0;
        }

        public void InvalidateBackBuffer() => _backBufferChanged = true;

        // --- resize reporting (UI thread) -----------------------------------------------------

        public void ReportSize(uint pixelWidth, uint pixelHeight)
        {
            lock (_sizeLock)
            {
                _pixelWidth = pixelWidth;
                _pixelHeight = pixelHeight;
                if (_reportSize != null && pixelWidth != 0 && pixelHeight != 0)
                    _reportSize(_reportOpaque, pixelWidth, pixelHeight);
            }
        }

        // --- callback (un)installation --------------------------------------------------------

        private void InstallCallbacks(MediaPlayer mp) =>
            mp.SetOutputCallbacks(
                CoreVideoEngine.D3d11,
                _setupCb, _cleanupCb, _setWindowCb, _updateOutputCb, _swapCb, _makeCurrentCb,
                null,             // getProcAddress_cb
                null,             // frameMetadata_cb
                _selectPlaneCb);  // select_plane_cb (hands VLC the RTV)

        private void DisableCallbacks(MediaPlayer mp) =>
            mp.SetOutputCallbacks(CoreVideoEngine.Disable,
                null, null, null, null, null, null, null, null, null);

        // --- device creation ------------------------------------------------------------------

        private void EnsureDevice()
        {
            lock (_gpuLock)
            {
                if (_disposed || (_device != IntPtr.Zero && _d3d11Device != IntPtr.Zero)) return;
                TryCreateDeviceLocked();
            }
        }

        // Best-effort; on failure leaves the devices at IntPtr.Zero (no throw). Caller holds _gpuLock.
        private void TryCreateDeviceLocked()
        {
            // Host D3D9Ex device (consumer side, drives the D3DImage). Identical to the D3D9 path.
            int hr = Direct3D9.Direct3DCreate9Ex(Direct3D9.D3D_SDK_VERSION, out _d3d);
            if (hr < 0) { _d3d = IntPtr.Zero; return; }

            IntPtr focus = GetDesktopWindow();
            var pp = new Direct3D9.D3DPRESENT_PARAMETERS
            {
                Windowed = 1,
                SwapEffect = Direct3D9.D3DSWAPEFFECT_DISCARD,
                hDeviceWindow = focus,
                BackBufferWidth = 1,
                BackBufferHeight = 1,
                BackBufferFormat = Direct3D9.D3DFMT_X8R8G8B8,
            };
            hr = Direct3D9.CreateDeviceEx(
                _d3d, Direct3D9.D3DADAPTER_DEFAULT, Direct3D9.D3DDEVTYPE_HAL, focus,
                Direct3D9.D3DCREATE_HARDWARE_VERTEXPROCESSING | Direct3D9.D3DCREATE_MULTITHREADED | Direct3D9.D3DCREATE_FPU_PRESERVE,
                ref pp, IntPtr.Zero, out _device);
            if (hr < 0) { ReleaseDeviceLocked(); return; }

            // Producer side: our D3D11 device/context (handed to libvlc). VIDEO_SUPPORT enables D3D11VA
            // hardware decoding; BGRA_SUPPORT is required for the shared B8G8R8A8 render target. Retry
            // without VIDEO_SUPPORT so a driver lacking it still composites (software decode).
            if (!TryCreateD3D11Locked())
                ReleaseDeviceLocked();
        }

        // Caller holds _gpuLock.
        private bool TryCreateD3D11Locked()
        {
            uint flags = Direct3D11.D3D11_CREATE_DEVICE_BGRA_SUPPORT | Direct3D11.D3D11_CREATE_DEVICE_VIDEO_SUPPORT;
            int hr = Direct3D11.D3D11CreateDevice(IntPtr.Zero, Direct3D11.D3D_DRIVER_TYPE_HARDWARE, IntPtr.Zero,
                flags, IntPtr.Zero, 0, Direct3D11.D3D11_SDK_VERSION, out _d3d11Device, out _, out _d3d11Context);
            if (hr < 0)
                hr = Direct3D11.D3D11CreateDevice(IntPtr.Zero, Direct3D11.D3D_DRIVER_TYPE_HARDWARE, IntPtr.Zero,
                    Direct3D11.D3D11_CREATE_DEVICE_BGRA_SUPPORT, IntPtr.Zero, 0, Direct3D11.D3D11_SDK_VERSION,
                    out _d3d11Device, out _, out _d3d11Context);
            if (hr < 0)
            {
                _d3d11Device = IntPtr.Zero;
                _d3d11Context = IntPtr.Zero;
                return false;
            }
            // libvlc uses the immediate context from its decode and render threads.
            Direct3D11.EnableMultithreadProtection(_d3d11Context);
            return true;
        }

        // Caller holds _gpuLock (except the finalizer, where the object is unreachable).
        private void ReleaseDeviceLocked()
        {
            ReleaseSurfacesLocked();
            if (_d3d11Context != IntPtr.Zero) { Direct3D11.Release(_d3d11Context); _d3d11Context = IntPtr.Zero; }
            if (_d3d11Device != IntPtr.Zero) { Direct3D11.Release(_d3d11Device); _d3d11Device = IntPtr.Zero; }
            if (_device != IntPtr.Zero) { Direct3D9.Release(_device); _device = IntPtr.Zero; }
            if (_d3d != IntPtr.Zero) { Direct3D9.Release(_d3d); _d3d = IntPtr.Zero; }
        }

        // Caller holds _gpuLock. Releases in reverse dependency order (view → texture → host surface).
        private void ReleaseSurfacesLocked()
        {
            if (_rtv != IntPtr.Zero) { Direct3D11.Release(_rtv); _rtv = IntPtr.Zero; }
            if (_d3d11Texture != IntPtr.Zero) { Direct3D11.Release(_d3d11Texture); _d3d11Texture = IntPtr.Zero; }
            if (_hostSurface != IntPtr.Zero) { Direct3D9.Release(_hostSurface); _hostSurface = IntPtr.Zero; }
            if (_hostTexture != IntPtr.Zero) { Direct3D9.Release(_hostTexture); _hostTexture = IntPtr.Zero; }
            _sharedHandle = IntPtr.Zero;
        }

        // --- device-loss recovery -------------------------------------------------------------

        private void ScheduleRecover()
        {
            if (_disposed) return;
            if (Interlocked.Exchange(ref _recovering, 1) == 0)
            {
                try { _dispatcher.BeginInvoke((Action)RecoverDevice); }
                catch { Volatile.Write(ref _recovering, 0); }
            }
        }

        private void RecoverDevice()
        {
            try
            {
                if (_disposed) return;
                MediaPlayer mp = _attachedPlayer;

                // 1) Stop libvlc calling us. Must NOT hold _gpuLock: disable can re-enter OnCleanup (which locks).
                if (mp != null) DisableCallbacks(mp);

                // 2) Recreate both devices.
                lock (_gpuLock)
                {
                    if (_disposed) return;
                    ReleaseDeviceLocked();
                    TryCreateDeviceLocked();
                }

                // 3) Re-install so libvlc re-runs OnSetup + OnUpdateOutput against the new devices.
                if (mp != null && _device != IntPtr.Zero && _d3d11Device != IntPtr.Zero)
                    InstallCallbacks(mp);
            }
            catch { /* leave detached; a later GPU error reschedules */ }
            finally { Volatile.Write(ref _recovering, 0); }
        }

        // --- LibVLC output callbacks (called on VLC threads) ----------------------------------

        private bool OnSetup(IntPtr* opaque, libvlc_video_setup_device_cfg_t* cfg, libvlc_video_setup_device_info_t* @out)
        {
            try
            {
                IntPtr ctx;
                lock (_gpuLock) { ctx = _d3d11Context; }
                if (ctx == IntPtr.Zero) return false;
                @out->u.d3d11.device_context = ctx;       // hand libvlc our immediate context (it renders on it)
                @out->u.d3d11.context_mutex = IntPtr.Zero; // we rely on ID3D11Multithread instead of a caller mutex
                Direct3D11.AddRef(ctx);                 // libvlc releases this ref on output teardown
                return true;
            }
            catch { return false; }
        }

        private void OnCleanup(IntPtr opaque)
        {
            lock (_sizeLock) { _reportSize = null; _reportOpaque = IntPtr.Zero; }
            lock (_gpuLock)
            {
                _backBufferChanged = true;
                ReleaseSurfacesLocked();
            }
        }

        private void OnSetWindow(IntPtr opaque, IntPtr reportSize, IntPtr reportMouseMove,
            IntPtr reportMousePress, IntPtr reportMouseRelease, IntPtr reportOpaque)
        {
            lock (_sizeLock)
            {
                _reportSize = reportSize == IntPtr.Zero
                    ? null
                    : Marshal.GetDelegateForFunctionPointer<VideoOutputResizeCallback>(reportSize);
                _reportOpaque = reportOpaque;
                if (_reportSize != null && _pixelWidth != 0 && _pixelHeight != 0)
                    _reportSize(_reportOpaque, _pixelWidth, _pixelHeight);
            }
        }

        private bool OnUpdateOutput(IntPtr opaque, libvlc_video_render_cfg_t* cfg, libvlc_video_output_cfg_t* output)
        {
            bool ok = false, lost = false;
            try
            {
                lock (_gpuLock)
                {
                    if (_disposed || _device == IntPtr.Zero || _d3d11Device == IntPtr.Zero) return false;
                    ReleaseSurfacesLocked();

                    _width = cfg->width;
                    _height = cfg->height;

                    // 1) D3D11 shared render-target texture (BGRA, legacy MISC_SHARED). libvlc draws here.
                    var desc = new Direct3D11.D3D11_TEXTURE2D_DESC
                    {
                        Width = _width,
                        Height = _height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = Direct3D11.DXGI_FORMAT_B8G8R8A8_UNORM,
                        SampleDesc = new Direct3D11.DXGI_SAMPLE_DESC { Count = 1, Quality = 0 },
                        Usage = Direct3D11.D3D11_USAGE_DEFAULT,
                        BindFlags = Direct3D11.D3D11_BIND_RENDER_TARGET | Direct3D11.D3D11_BIND_SHADER_RESOURCE,
                        CPUAccessFlags = 0,
                        MiscFlags = Direct3D11.D3D11_RESOURCE_MISC_SHARED,
                    };
                    int hr = Direct3D11.CreateTexture2D(_d3d11Device, ref desc, out _d3d11Texture);
                    if (hr >= 0) hr = Direct3D11.GetSharedHandle(_d3d11Texture, out _sharedHandle);
                    if (hr >= 0) hr = Direct3D11.CreateRenderTargetView(_d3d11Device, _d3d11Texture, out _rtv);

                    // 2) Host D3D9 surface aliasing the SAME VRAM (opens the shared handle). D3DImage shows this.
                    if (hr >= 0)
                        hr = Direct3D9.CreateTexture(_device, _width, _height, 1, Direct3D9.D3DUSAGE_RENDERTARGET,
                            Direct3D9.D3DFMT_A8R8G8B8, Direct3D9.D3DPOOL_DEFAULT, out _hostTexture, ref _sharedHandle);
                    if (hr >= 0) hr = Direct3D9.GetSurfaceLevel(_hostTexture, 0, out _hostSurface);

                    if (hr < 0)
                    {
                        _backBufferChanged = true;
                        ReleaseSurfacesLocked();
                        lost = Direct3D11.IsDeviceLost(hr) || Direct3D9.IsDeviceLost(hr)
                            || Direct3D11.GetDeviceRemovedReason(_d3d11Device) < 0;
                    }
                    else
                    {
                        output->u.dxgi_format = Direct3D11.DXGI_FORMAT_B8G8R8A8_UNORM;
                        output->full_range = 1;
                        output->colorspace = libvlc_video_color_space_t.libvlc_video_colorspace_BT709;
                        output->primaries = libvlc_video_color_primaries_t.libvlc_video_primaries_BT709;
                        output->transfer = libvlc_video_transfer_func_t.libvlc_video_transfer_func_SRGB;
                        output->orientation = libvlc_video_orient_t.libvlc_video_orient_top_left;
                        _backBufferChanged = true;
                        ok = true;
                    }
                }
            }
            catch { return false; }

            if (lost) ScheduleRecover();
            return ok;
        }

        // libvlc asks for the render target of each plane before it renders. Our output is a single BGRA
        // target, so we return the one RTV. `output` is an ID3D11RenderTargetView** to fill in.
        private bool OnSelectPlane(IntPtr opaque, UIntPtr plane, IntPtr output)
        {
            if (output == IntPtr.Zero) return false;
            IntPtr rtv;
            lock (_gpuLock) { rtv = _rtv; }
            if (rtv == IntPtr.Zero) return false;
            *(IntPtr*)output = rtv;
            return true;
        }

        private void OnSwap(IntPtr opaque) => _frameReady = true;

        private bool OnMakeCurrent(IntPtr opaque, bool enter)
        {
            if (enter) return true;   // nothing to do on enter; libvlc has the RTV from select_plane.
            try
            {
                // Submit libvlc's queued D3D11 commands so the shared surface is coherent before the
                // host D3D9 device (WPF's compositor) reads it — there is no keyed mutex across the APIs.
                lock (_gpuLock)
                {
                    if (_d3d11Context != IntPtr.Zero)
                        Direct3D11.Flush(_d3d11Context);
                }
            }
            catch { return false; }
            return true;
        }

        // --- disposal -------------------------------------------------------------------------

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~D3D11VideoOutput() => Dispose(false);

        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;
            if (disposing)
            {
                Detach();
                lock (_gpuLock) { ReleaseDeviceLocked(); }
            }
            else
            {
                ReleaseDeviceLocked();
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
    }
}
