using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using MediaPlayer = LibVLCSharp.Core.MediaPlayer;

namespace LibVLCSharp.WPF
{
    /// <summary>The libvlc rendering engine the <see cref="VideoView"/> drives its output through.</summary>
    public enum VideoEngine
    {
        /// <summary>Direct3D9 output (<see cref="D3D9VideoOutput"/>). The default; widest compatibility.</summary>
        D3D9,

        /// <summary>Direct3D11 output (<see cref="D3D11VideoOutput"/>). Enables D3D11VA hardware decode;
        /// bridged to the D3DImage's D3D9 surface via a shared texture.</summary>
        D3D11,
    }

    /// <summary>
    /// Airspace-free WPF video surface for LibVLC 4.x.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The libvlc output-callback integration (the host <c>IDirect3D9Ex</c> device, the shared texture
    /// libvlc renders into, device-loss recovery and resize reporting) lives in an
    /// <see cref="IVideoOutput"/> — <see cref="D3D9VideoOutput"/> (managed analog of <c>render_context</c>
    /// in VLC's <c>doc/libvlc/d3d9_player.c</c>) or <see cref="D3D11VideoOutput"/>, selected by
    /// <see cref="Engine"/>. This control owns only the WPF side: a <see cref="D3DImage"/> that composites
    /// the shared surface (no HWND overlay → no airspace problem), the present cadence (a
    /// <see cref="CompositionTarget.Rendering"/> tick), the bindable <see cref="MediaPlayer"/>, and
    /// load/size/DPI handling.
    /// </para>
    /// <para>
    /// Set <see cref="Engine"/> (default <see cref="VideoEngine.D3D9"/>) BEFORE the control loads / a
    /// player is attached — the output is created once on first use. Bind a managed
    /// <see cref="MediaPlayer"/> before playback. AnyCPU (hand-written D3D9/D3D11 interop, see
    /// <see cref="Interop.Direct3D9"/>/<see cref="Interop.Direct3D11"/>). NEEDS RUNTIME/GPU VALIDATION.
    /// </para>
    /// </remarks>
    public class VideoView : Image, IDisposable
    {
        private readonly D3DImage _image = new D3DImage();

        // Created lazily on first use so Engine (set in XAML/code before load) decides the implementation.
        private IVideoOutput _output;

        // The managed player bound via the MediaPlayer DP — kept referenced so it isn't collected while
        // attached, and used to re-attach on reload. The renderer itself only deals in native handles.
        private MediaPlayer _managedPlayer;

        private bool _renderingHooked;
        private bool _disposed;

        /// <summary>
        /// The managed media player to render. Settable in XAML/bindings; setting it attaches the player
        /// (and detaches any previous one). Set to <c>null</c> to detach.
        /// </summary>
        public static readonly DependencyProperty MediaPlayerProperty = DependencyProperty.Register(
            nameof(MediaPlayer), typeof(MediaPlayer), typeof(VideoView),
            new PropertyMetadata(null, OnMediaPlayerChanged));

        /// <summary>The managed <see cref="MediaPlayer"/> this view renders (a <see cref="DependencyProperty"/>).</summary>
        public MediaPlayer MediaPlayer
        {
            get => (MediaPlayer)GetValue(MediaPlayerProperty);
            set => SetValue(MediaPlayerProperty, value);
        }

        /// <summary>
        /// The libvlc rendering engine to use. Must be set BEFORE the control loads or a player is
        /// attached — the output is created once on first use and not swapped afterwards. Default
        /// <see cref="VideoEngine.D3D9"/>.
        /// </summary>
        public VideoEngine Engine { get; set; } = VideoEngine.D3D9;

        public VideoView()
        {
            Source = _image;
            Stretch = Stretch.Uniform;

            _image.IsFrontBufferAvailableChanged += OnFrontBufferAvailableChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        // Creates the engine-specific output on first use (cheap; the GPU devices are created later, on
        // Attach). Returns null only after disposal.
        private IVideoOutput EnsureOutput()
        {
            if (_output == null && !_disposed)
                _output = Engine == VideoEngine.D3D11
                    ? (IVideoOutput)new D3D11VideoOutput(Dispatcher)
                    : new D3D9VideoOutput(Dispatcher);
            return _output;
        }
 
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateControlSize();
            HookRendering();
            var mp = _managedPlayer;
            if (mp != null) EnsureOutput().Attach(mp);   // (re)attach; no-op if already attached
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _output?.Detach();
            UnhookRendering();
        }

        private static void OnMediaPlayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (VideoView)d;
            view._managedPlayer = (MediaPlayer)e.NewValue;
            view.EnsureOutput().Attach(view._managedPlayer);
        }

        // --- attach / detach ------------------------------------------------------------------

        /// <summary>Attaches a managed <see cref="MediaPlayer"/> (keeps it referenced while attached).</summary>
        public void Attach(MediaPlayer mediaPlayer)
        {
            _managedPlayer = mediaPlayer;
            EnsureOutput().Attach(mediaPlayer);
        }

        /// <summary>Detaches the current player: libvlc stops calling the output callbacks. Keeps the device.</summary>
        public void Detach()
        {
            _managedPlayer = null;
            _output?.Detach();
        }

        // --- size / DPI -----------------------------------------------------------------------

        /// <summary>Reports the control's device-pixel size to libvlc so it renders at that resolution.
        /// Runs on the UI thread (layout/SizeChanged/DPI change).</summary>
        private void UpdateControlSize()
        {
            DpiScale dpi = VisualTreeHelper.GetDpi(this);
            uint w = (uint)Math.Max(0, Math.Round(ActualWidth * dpi.DpiScaleX));
            uint h = (uint)Math.Max(0, Math.Round(ActualHeight * dpi.DpiScaleY));
            EnsureOutput()?.ReportSize(w, h);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateControlSize();
        }

        /// <summary>Re-reports the size when the control moves to a monitor with a different DPI
        /// (DIU size unchanged, so <c>SizeChanged</c> may not fire, but the pixel size changed).</summary>
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            UpdateControlSize();
        }

        // --- presentation (CompositionTarget.Rendering tick) ----------------------------------

        private void HookRendering()
        {
            if (_renderingHooked) return;
            _renderingHooked = true;
            CompositionTarget.Rendering += OnRendering;
        }

        private void UnhookRendering()
        {
            if (!_renderingHooked) return;
            _renderingHooked = false;
            CompositionTarget.Rendering -= OnRendering;
        }

        // Runs once per WPF composition pass on the UI thread (while loaded) — the ONLY place the
        // D3DImage is touched. The renderer rebinds the back buffer if the surface changed; one
        // AddDirtyRect rides this pass to present the latest frame.
        private void OnRendering(object sender, EventArgs e)
        {
            var output = _output;
            if (_disposed || output == null || !_image.IsFrontBufferAvailable || !output.HasUpdate) return;

            _image.Lock();
            try
            {
                if (output.PrepareBackBuffer(_image))
                    _image.AddDirtyRect(new Int32Rect(0, 0, _image.PixelWidth, _image.PixelHeight));
            }
            finally { _image.Unlock(); }
        }

        private void OnFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Rebind on the next render tick when the front buffer comes back (e.g. after lock screen/RDP).
            if (_image.IsFrontBufferAvailable)
                _output?.InvalidateBackBuffer();
        }

        // --- disposal -------------------------------------------------------------------------

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            UnhookRendering();
            _output?.Dispose();
            _managedPlayer = null;
        }
    }
}
