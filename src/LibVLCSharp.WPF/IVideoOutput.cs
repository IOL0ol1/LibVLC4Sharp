using System;
using System.Windows.Interop;
using MediaPlayer = LibVLCSharp.Core.MediaPlayer;

namespace LibVLCSharp.WPF
{
    /// <summary>
    /// The libvlc video-output integration that <see cref="VideoView"/> drives, independent of the
    /// rendering engine (D3D9 or D3D11). An output owns the host <c>IDirect3D9Ex</c> device and the
    /// shared surface a <see cref="D3DImage"/> composites; <see cref="VideoView"/> owns the WPF side
    /// (the image, the present cadence, size/DPI). Implementations:
    /// <see cref="D3D9VideoOutput"/> and <see cref="D3D11VideoOutput"/>.
    /// </summary>
    /// <remarks>
    /// Threading mirrors the implementations: <see cref="Attach"/>/<see cref="Detach"/>/
    /// <see cref="ReportSize"/>/<see cref="PrepareBackBuffer"/>/<see cref="InvalidateBackBuffer"/>/
    /// <see cref="IDisposable.Dispose"/> are called on the UI thread; the libvlc callbacks fire on
    /// libvlc threads. <see cref="PrepareBackBuffer"/> must be called inside the image's Lock/Unlock.
    /// </remarks>
    internal interface IVideoOutput : IDisposable
    {
        /// <summary>True when there is a new frame or a back-buffer change to present.</summary>
        bool HasUpdate { get; }

        /// <summary>Installs the output callbacks on <paramref name="mediaPlayer"/> via the Core
        /// <see cref="MediaPlayer.SetOutputCallbacks"/> wrapper (detaching any previously attached player
        /// first); creates the host device on first use. Pass <c>null</c> to detach.</summary>
        void Attach(MediaPlayer mediaPlayer);

        /// <summary>Disables libvlc's output and releases the per-output surfaces; keeps the host device.</summary>
        void Detach();

        /// <summary>Rebinds <paramref name="image"/>'s back buffer to the current host surface if it
        /// changed since the last present. Returns whether there is content to mark dirty.</summary>
        bool PrepareBackBuffer(D3DImage image);

        /// <summary>Forces a back-buffer rebind on the next present (e.g. when the front buffer returns).</summary>
        void InvalidateBackBuffer();

        /// <summary>Reports the target output size (device pixels) to libvlc so it renders at that resolution.</summary>
        void ReportSize(uint pixelWidth, uint pixelHeight);
    }
}
