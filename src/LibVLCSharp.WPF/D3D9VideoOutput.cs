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
    /// Owns the <c>libvlc_video_set_output_callbacks</c> (D3D9 engine) integration — the managed analog
    /// of the <c>render_context</c> struct in VLC's <c>doc/libvlc/d3d9_player.c</c>. It holds the host
    /// <c>IDirect3D9Ex</c> device and the shared D3D9 texture that libvlc renders into, implements the
    /// six output callbacks, installs/disables them on a player, recovers from device loss, and reports
    /// the output size back to libvlc. It knows nothing about the WPF control beyond a
    /// <see cref="D3DImage"/> it binds the host surface to (in <see cref="PrepareBackBuffer"/>);
    /// <see cref="VideoView"/> drives the present cadence and marks the image dirty.
    /// </summary>
    /// <remarks>
    /// Threading: <see cref="Attach"/>/<see cref="Detach"/>/<see cref="ReportSize"/>/<see cref="Dispose()"/>
    /// are called on the UI thread; the callbacks fire on libvlc threads. <c>_gpuLock</c> serializes the
    /// GPU-resource lifecycle across both; it is NEVER held while calling <c>set_output_callbacks</c>
    /// (which re-enters our callbacks) and NEVER nested with <c>_sizeLock</c>. Device recovery is
    /// marshalled to the UI thread via the dispatcher so it stays serialized with Attach/Detach.
    /// </remarks>
    internal sealed unsafe class D3D9VideoOutput : IVideoOutput
    {
        private readonly Dispatcher _dispatcher;

        // Guards the GPU-resource lifecycle (devices, textures, surfaces, shared handle) across the UI
        // thread (attach/detach/recovery/dispose) and the VLC threads (OnUpdateOutput/OnCleanup/
        // OnMakeCurrent). NEVER held across set_output_callbacks; NEVER nested with _sizeLock.
        private readonly object _gpuLock = new object();

        // Host D3D9Ex device (owns the surface WPF composites).
        private IntPtr _d3d;          // IDirect3D9Ex*
        private IntPtr _device;       // IDirect3DDevice9Ex*

        // Host-side shared texture/surface (what the D3DImage displays).
        private IntPtr _hostTexture;  // IDirect3DTexture9*
        private IntPtr _hostSurface;  // IDirect3DSurface9*

        // LibVLC-side texture/surface (VLC's render target), aliasing the same VRAM via the handle.
        private IntPtr _vlcDevice;    // IDirect3DDevice9*
        private IntPtr _vlcTexture;   // IDirect3DTexture9*
        private IntPtr _vlcSurface;   // IDirect3DSurface9*
        private IntPtr _sharedHandle;

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

        // Resize reporting. libvlc hands us _reportSize (+ _reportOpaque) via the set_window callback;
        // calling it asks libvlc to render at a given pixel size. Invoked from the UI thread (ReportSize)
        // and the VLC thread (OnSetWindow, initial size) under _sizeLock — the report fn is invoked
        // *under* the lock so a concurrent OnCleanup (which clears it under the same lock) can't make us
        // call a stale pointer. _pixelWidth/_pixelHeight cache the last size measured on the UI thread.
        private readonly object _sizeLock = new object();
        private VideoOutputResizeCallback _reportSize;
        private IntPtr _reportOpaque;
        private uint _pixelWidth, _pixelHeight;

        // Present hand-off to the UI thread. Volatile flags published by the VLC callbacks; consumed in
        // PrepareBackBuffer on the UI thread. _hostSurface is the current back buffer (read under _gpuLock).
        private volatile bool _backBufferChanged;
        private volatile bool _frameReady;

        private int _recovering;      // 0/1, via Interlocked: at most one device recovery in flight
        private bool _disposed;

        public D3D9VideoOutput(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _setupCb = OnSetup;
            _cleanupCb = OnCleanup;
            _updateOutputCb = OnUpdateOutput;
            _swapCb = OnSwap;
            _makeCurrentCb = OnMakeCurrent;
            _setWindowCb = OnSetWindow;
        }

        /// <summary>True when there is a new frame or a back-buffer change to present.</summary>
        public bool HasUpdate => _frameReady || _backBufferChanged;

        // --- attach / detach (UI thread) ------------------------------------------------------

        /// <summary>Installs the D3D9 output callbacks on <paramref name="mediaPlayer"/> via the Core
        /// <see cref="MediaPlayer.SetOutputCallbacks"/> wrapper (detaching any previously attached player
        /// first). Creates the host device on first use.</summary>
        public void Attach(MediaPlayer mediaPlayer)
        {
            if (_disposed || ReferenceEquals(mediaPlayer, _attachedPlayer)) return;
            Detach();
            _attachedPlayer = mediaPlayer;
            if (mediaPlayer == null) return;
            EnsureDevice();
            if (_device != IntPtr.Zero)
                InstallCallbacks(mediaPlayer);
        }

        /// <summary>Disables libvlc's output (so no further callbacks fire into us) and releases the
        /// per-output surfaces. Keeps the host device for fast re-attach.</summary>
        public void Detach()
        {
            MediaPlayer mp = _attachedPlayer;
            _attachedPlayer = null;
            if (mp != null)
                DisableCallbacks(mp);   // NOT under any lock — libvlc may re-enter OnCleanup (which locks)
            lock (_sizeLock) { _reportSize = null; _reportOpaque = IntPtr.Zero; }
            lock (_gpuLock)
            {
                _backBufferChanged = true;   // the next present rebinds the image to the (released) surface
                ReleaseSurfacesLocked();
            }
        }

        // --- present hand-off (called by VideoView on the UI thread) --------------------------

        /// <summary>
        /// Rebinds <paramref name="image"/>'s back buffer to the current host surface if it changed since
        /// the last present (under <c>_gpuLock</c> so the surface can't be released mid-bind). Must be
        /// called inside the image's <c>Lock()</c>/<c>Unlock()</c>. Returns whether there is content to
        /// mark dirty. Steady-state frames take no GPU lock — only a surface change does.
        /// </summary>
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

        /// <summary>Forces a back-buffer rebind on the next present (e.g. when the D3DImage front buffer returns).</summary>
        public void InvalidateBackBuffer() => _backBufferChanged = true;

        // --- resize reporting (UI thread) -----------------------------------------------------

        /// <summary>Reports the target output size (device pixels) to libvlc so it renders at that resolution.</summary>
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
                CoreVideoEngine.D3d9,
                _setupCb, _cleanupCb,
                _setWindowCb,        // set_window_cb (resize reporting)
                _updateOutputCb, _swapCb, _makeCurrentCb,
                null,                // getProcAddress_cb
                null,                // frameMetadata_cb
                null);               // select_plane_cb

        private void DisableCallbacks(MediaPlayer mp) =>
            mp.SetOutputCallbacks(CoreVideoEngine.Disable,
                null, null, null, null, null, null, null, null, null);

        // --- D3D9 host device -----------------------------------------------------------------

        private void EnsureDevice()
        {
            lock (_gpuLock)
            {
                if (_disposed || _device != IntPtr.Zero) return;
                TryCreateDeviceLocked();
            }
        }

        // Best-effort device creation; on failure leaves _device == IntPtr.Zero (no throw). Caller holds _gpuLock.
        private void TryCreateDeviceLocked()
        {
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
                _d3d,
                Direct3D9.D3DADAPTER_DEFAULT,
                Direct3D9.D3DDEVTYPE_HAL,
                focus,
                Direct3D9.D3DCREATE_HARDWARE_VERTEXPROCESSING | Direct3D9.D3DCREATE_MULTITHREADED | Direct3D9.D3DCREATE_FPU_PRESERVE,
                ref pp,
                IntPtr.Zero,
                out _device);
            if (hr < 0)
                ReleaseDeviceLocked();   // drops _d3d too; _device stays 0
        }

        // Caller holds _gpuLock (except the finalizer, where the object is unreachable).
        private void ReleaseDeviceLocked()
        {
            ReleaseSurfacesLocked();
            if (_device != IntPtr.Zero) { Direct3D9.Release(_device); _device = IntPtr.Zero; }
            if (_d3d != IntPtr.Zero) { Direct3D9.Release(_d3d); _d3d = IntPtr.Zero; }
        }

        // Caller holds _gpuLock.
        private void ReleaseSurfacesLocked()
        {
            if (_hostSurface != IntPtr.Zero) { Direct3D9.Release(_hostSurface); _hostSurface = IntPtr.Zero; }
            if (_hostTexture != IntPtr.Zero) { Direct3D9.Release(_hostTexture); _hostTexture = IntPtr.Zero; }
            if (_vlcSurface != IntPtr.Zero) { Direct3D9.Release(_vlcSurface); _vlcSurface = IntPtr.Zero; }
            if (_vlcTexture != IntPtr.Zero) { Direct3D9.Release(_vlcTexture); _vlcTexture = IntPtr.Zero; }
            if (_vlcDevice != IntPtr.Zero) { Direct3D9.Release(_vlcDevice); _vlcDevice = IntPtr.Zero; }
            _sharedHandle = IntPtr.Zero;
        }

        // --- device-loss recovery -------------------------------------------------------------

        // Called from a VLC thread when a GPU op reports a lost/removed/hung device. Deduped so a storm
        // of failing frames triggers exactly one recovery, marshalled to the UI thread.
        private void ScheduleRecover()
        {
            if (_disposed) return;
            if (Interlocked.Exchange(ref _recovering, 1) == 0)
            {
                // Must not let an exception escape into native code (BeginInvoke throws once the
                // dispatcher has shut down).
                try { _dispatcher.BeginInvoke((Action)RecoverDevice); }
                catch { Volatile.Write(ref _recovering, 0); }
            }
        }

        // Rebuilds the host device and re-establishes libvlc's output. Runs on the UI thread.
        private void RecoverDevice()
        {
            try
            {
                if (_disposed) return;
                MediaPlayer mp = _attachedPlayer;

                // 1) Stop libvlc calling us. Must NOT hold _gpuLock: disable can re-enter OnCleanup, which locks.
                if (mp != null) DisableCallbacks(mp);

                // 2) Recreate the IDirect3D9Ex + device (covers DEVICEREMOVED, not just a resettable loss).
                lock (_gpuLock)
                {
                    if (_disposed) return;
                    ReleaseDeviceLocked();
                    TryCreateDeviceLocked();
                }

                // 3) Re-install so libvlc re-runs OnSetup (new device) + OnUpdateOutput (new shared textures).
                if (mp != null && _device != IntPtr.Zero)
                    InstallCallbacks(mp);
            }
            catch { /* leave detached; if it keeps failing, a later GPU error reschedules */ }
            finally { Volatile.Write(ref _recovering, 0); }
        }

        // --- LibVLC output callbacks (called on VLC threads) ----------------------------------

        private bool OnSetup(IntPtr* opaque, libvlc_video_setup_device_cfg_t* cfg, libvlc_video_setup_device_info_t* @out)
        {
            try
            {
                IntPtr d3d;
                lock (_gpuLock) { d3d = _d3d; }
                if (d3d == IntPtr.Zero) return false;
                @out->d3d9.device = d3d;   // hand VLC our IDirect3D9Ex
                @out->d3d9.adapter = (int)Direct3D9.D3DADAPTER_DEFAULT;
                return true;
            }
            catch { return false; }
        }

        private void OnCleanup(IntPtr opaque)
        {
            // The report function is invalid once the output is torn down — drop it so a late
            // ReportSize on the UI thread can't call into a stale native pointer.
            lock (_sizeLock) { _reportSize = null; _reportOpaque = IntPtr.Zero; }
            lock (_gpuLock)
            {
                _backBufferChanged = true;   // the render tick rebinds the D3DImage to null
                ReleaseSurfacesLocked();
            }
        }

        // Called by libvlc to hand us the function we call to report a new output size (and, unused here,
        // the mouse-report functions). Called with a null report function on teardown.
        private void OnSetWindow(IntPtr opaque, IntPtr reportSize, IntPtr reportMouseMove,
            IntPtr reportMousePress, IntPtr reportMouseRelease, IntPtr reportOpaque)
        {
            // Reporting is done under the lock (as the VLC sample does under its critical section): a
            // concurrent OnCleanup clears _reportSize under the same lock, so we never call a stale pointer.
            lock (_sizeLock)
            {
                _reportSize = reportSize == IntPtr.Zero
                    ? null
                    : Marshal.GetDelegateForFunctionPointer<VideoOutputResizeCallback>(reportSize);
                _reportOpaque = reportOpaque;
                // Report the control's current size as the initial output size (mirrors the VLC sample).
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
                    if (_disposed || _device == IntPtr.Zero) return false;
                    ReleaseSurfacesLocked();

                    _width = cfg->width;
                    _height = cfg->height;
                    _vlcDevice = cfg->device;
                    Direct3D9.AddRef(_vlcDevice);

                    int hr = Direct3D9.GetAdapterDisplayMode(_d3d, Direct3D9.D3DADAPTER_DEFAULT, out var mode);

                    // 10-bit surface for >8-bit sources (less banding). Falls back to the 8-bit display
                    // format if the 10-bit render target isn't supported. NB: classic WPF composites SDR
                    // (8-bit sRGB), so this only improves libvlc's internal precision before WPF's
                    // downsample — it is NOT true HDR output.
                    int format = hr < 0 ? 0 : (cfg->bitdepth > 8 ? Direct3D9.D3DFMT_A2R10G10B10 : mode.Format);

                    _sharedHandle = IntPtr.Zero;
                    if (hr >= 0)
                    {
                        hr = Direct3D9.CreateTexture(_device, _width, _height, 1, Direct3D9.D3DUSAGE_RENDERTARGET,
                            format, Direct3D9.D3DPOOL_DEFAULT, out _hostTexture, ref _sharedHandle);
                        if (hr < 0 && format != mode.Format && !Direct3D9.IsDeviceLost(hr))
                        {
                            // 10-bit render target unsupported — retry at the 8-bit display format.
                            format = mode.Format;
                            _sharedHandle = IntPtr.Zero;
                            hr = Direct3D9.CreateTexture(_device, _width, _height, 1, Direct3D9.D3DUSAGE_RENDERTARGET,
                                format, Direct3D9.D3DPOOL_DEFAULT, out _hostTexture, ref _sharedHandle);
                        }
                    }
                    if (hr >= 0)
                        hr = Direct3D9.CreateTexture(_vlcDevice, _width, _height, 1, Direct3D9.D3DUSAGE_RENDERTARGET,
                            format, Direct3D9.D3DPOOL_DEFAULT, out _vlcTexture, ref _sharedHandle);
                    if (hr >= 0) hr = Direct3D9.GetSurfaceLevel(_vlcTexture, 0, out _vlcSurface);
                    if (hr >= 0) hr = Direct3D9.SetRenderTarget(_vlcDevice, 0, _vlcSurface);
                    if (hr >= 0) hr = Direct3D9.GetSurfaceLevel(_hostTexture, 0, out _hostSurface);

                    if (hr < 0)
                    {
                        // Setup failed: detach any partial surface and decide whether the device is lost.
                        _backBufferChanged = true;
                        ReleaseSurfacesLocked();
                        lost = Direct3D9.IsDeviceLost(hr)
                            || Direct3D9.IsDeviceLost(Direct3D9.CheckDeviceState(_device, GetDesktopWindow()));
                    }
                    else
                    {
                        output->d3d9_format = (uint)format;
                        output->full_range = 1;
                        output->colorspace = libvlc_video_color_space_t.libvlc_video_colorspace_BT709;
                        output->primaries = libvlc_video_color_primaries_t.libvlc_video_primaries_BT709;
                        output->transfer = libvlc_video_transfer_func_t.libvlc_video_transfer_func_SRGB;
                        output->orientation = libvlc_video_orient_t.libvlc_video_orient_top_left;
                        _backBufferChanged = true;   // PrepareBackBuffer rebinds to the new _hostSurface
                        ok = true;
                    }
                }
            }
            catch { return false; }

            if (lost) ScheduleRecover();
            return ok;
        }

        // Per frame, on a VLC thread: flag that a new frame is in the shared surface. No cross-thread
        // post, no allocation — VideoView's render tick picks it up via HasUpdate/PrepareBackBuffer.
        private void OnSwap(IntPtr opaque) => _frameReady = true;

        private bool OnMakeCurrent(IntPtr opaque, bool enter)
        {
            if (enter) return true;   // VLC has already set its render target; nothing to do on enter.
            bool lost = false;
            try
            {
                lock (_gpuLock)
                {
                    if (_vlcDevice != IntPtr.Zero)
                    {
                        int hr = Direct3D9.Present(_vlcDevice); // flush VLC's rendering before swap
                        if (hr < 0) lost = true;
                    }
                }
            }
            catch { return false; }
            if (lost) ScheduleRecover();
            return true;
        }

        // --- disposal -------------------------------------------------------------------------

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~D3D9VideoOutput() => Dispose(false);

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
                // Finalizer backstop: free GPU objects only (no locks, no libvlc). A still-attached,
                // finalized output has dangling native callbacks regardless — Detach/Release before drop.
                ReleaseDeviceLocked();
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
    }
}
