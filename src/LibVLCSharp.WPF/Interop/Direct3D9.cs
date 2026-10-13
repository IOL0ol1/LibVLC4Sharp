using System;
using System.Runtime.InteropServices;

namespace LibVLCSharp.WPF.Interop
{
    /// <summary>
    /// Minimal hand-written Direct3D9Ex interop for the WPF VideoView. COM objects are kept as raw
    /// <see cref="IntPtr"/> and methods are invoked by vtable slot index, so this is fully AnyCPU
    /// (the <c>stdcall</c> COM ABI and IntPtr-sized fields behave identically on x86/x64/arm64).
    /// </summary>
    /// <remarks>
    /// Only the handful of methods the renderer needs are bound. Vtable slot indices follow the
    /// fixed order in <c>d3d9.h</c> (IUnknown occupies slots 0–2). Verified by index in comments.
    /// </remarks>
    internal static unsafe class Direct3D9
    {
        // --- constants (d3d9.h) ---
        public const uint D3D_SDK_VERSION = 32;
        public const uint D3DADAPTER_DEFAULT = 0;
        public const int D3DDEVTYPE_HAL = 1;
        public const uint D3DCREATE_FPU_PRESERVE = 0x2;
        public const uint D3DCREATE_MULTITHREADED = 0x4;
        public const uint D3DCREATE_HARDWARE_VERTEXPROCESSING = 0x40;
        public const uint D3DUSAGE_RENDERTARGET = 0x1;
        public const uint D3DPOOL_DEFAULT = 0;
        public const int D3DSWAPEFFECT_DISCARD = 1;
        public const int D3DFMT_X8R8G8B8 = 22;
        public const int D3DFMT_A8R8G8B8 = 21;      // BGRA; the cross-API share format (↔ DXGI_FORMAT_B8G8R8A8_UNORM)
        public const int D3DFMT_A2R10G10B10 = 35;   // 10-bit RGB render target (HDR/10-bit source)

        // Device-state HRESULTs (d3d9.h, MAKE_D3DHRESULT = 0x88760000 | code). A lost/removed/hung
        // device's resources are invalid and must be recreated.
        public const int D3DERR_DEVICELOST = unchecked((int)0x88760868);
        public const int D3DERR_DEVICEHUNG = unchecked((int)0x88760874);
        public const int D3DERR_DEVICEREMOVED = unchecked((int)0x88760870);

        [StructLayout(LayoutKind.Sequential)]
        public struct D3DPRESENT_PARAMETERS
        {
            public uint BackBufferWidth;
            public uint BackBufferHeight;
            public int BackBufferFormat;          // D3DFORMAT
            public uint BackBufferCount;
            public int MultiSampleType;            // D3DMULTISAMPLE_TYPE
            public uint MultiSampleQuality;
            public int SwapEffect;                 // D3DSWAPEFFECT
            public IntPtr hDeviceWindow;           // HWND
            public int Windowed;                   // BOOL
            public int EnableAutoDepthStencil;     // BOOL
            public int AutoDepthStencilFormat;     // D3DFORMAT
            public uint Flags;
            public uint FullScreen_RefreshRateInHz;
            public uint PresentationInterval;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D3DDISPLAYMODE
        {
            public uint Width;
            public uint Height;
            public uint RefreshRate;
            public int Format;                     // D3DFORMAT
        }

        [DllImport("d3d9.dll", ExactSpelling = true)]
        public static extern int Direct3DCreate9Ex(uint SDKVersion, out IntPtr ppD3D);

        // vtable helper: method pointer at slot `index` of the COM object at `pComObject`.
        private static void* Slot(IntPtr pComObject, int index) => (*(void***)pComObject)[index];

        /// <summary>IUnknown::Release (slot 2).</summary>
        public static uint Release(IntPtr unk)
        {
            if (unk == IntPtr.Zero) return 0;
            return ((delegate* unmanaged[Stdcall]<IntPtr, uint>)Slot(unk, 2))(unk);
        }

        /// <summary>IUnknown::AddRef (slot 1).</summary>
        public static uint AddRef(IntPtr unk) =>
            ((delegate* unmanaged[Stdcall]<IntPtr, uint>)Slot(unk, 1))(unk);

        // --- IDirect3D9Ex (IDirect3D9 methods 3..16, Ex methods 17..) ---

        /// <summary>IDirect3D9::GetAdapterDisplayMode (slot 8).</summary>
        public static int GetAdapterDisplayMode(IntPtr d3d9, uint adapter, out D3DDISPLAYMODE mode)
        {
            fixed (D3DDISPLAYMODE* p = &mode)
                return ((delegate* unmanaged[Stdcall]<IntPtr, uint, D3DDISPLAYMODE*, int>)Slot(d3d9, 8))(d3d9, adapter, p);
        }

        /// <summary>IDirect3D9Ex::CreateDeviceEx (slot 20).</summary>
        public static int CreateDeviceEx(IntPtr d3d9ex, uint adapter, int devType, IntPtr focusWindow,
            uint behaviorFlags, ref D3DPRESENT_PARAMETERS pp, IntPtr fullscreenMode, out IntPtr device)
        {
            IntPtr dev;
            int hr;
            fixed (D3DPRESENT_PARAMETERS* ppp = &pp)
                hr = ((delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, uint, D3DPRESENT_PARAMETERS*, IntPtr, IntPtr*, int>)
                    Slot(d3d9ex, 20))(d3d9ex, adapter, devType, focusWindow, behaviorFlags, ppp, fullscreenMode, &dev);
            device = dev;
            return hr;
        }

        // --- IDirect3DDevice9 (base methods; valid on an IDirect3DDevice9Ex pointer too) ---

        /// <summary>IDirect3DDevice9::Present (slot 17).</summary>
        public static int Present(IntPtr device) =>
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int>)Slot(device, 17))
                (device, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        /// <summary>IDirect3DDevice9::CreateTexture (slot 23).</summary>
        public static int CreateTexture(IntPtr device, uint width, uint height, uint levels, uint usage,
            int format, uint pool, out IntPtr texture, ref IntPtr sharedHandle)
        {
            IntPtr tex;
            int hr;
            fixed (IntPtr* pShared = &sharedHandle)
                hr = ((delegate* unmanaged[Stdcall]<IntPtr, uint, uint, uint, uint, int, uint, IntPtr*, IntPtr*, int>)
                    Slot(device, 23))(device, width, height, levels, usage, format, pool, &tex, pShared);
            texture = tex;
            return hr;
        }

        /// <summary>IDirect3DDevice9::SetRenderTarget (slot 37).</summary>
        public static int SetRenderTarget(IntPtr device, uint index, IntPtr surface) =>
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, int>)Slot(device, 37))(device, index, surface);

        /// <summary>IDirect3DDevice9Ex::CheckDeviceState (slot 128) — reports whether the device is still usable.</summary>
        public static int CheckDeviceState(IntPtr deviceEx, IntPtr hwnd) =>
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)Slot(deviceEx, 128))(deviceEx, hwnd);

        /// <summary>True for the device-lost class of HRESULTs that require recreating the device.</summary>
        public static bool IsDeviceLost(int hr) =>
            hr == D3DERR_DEVICELOST || hr == D3DERR_DEVICEHUNG || hr == D3DERR_DEVICEREMOVED;

        // --- IDirect3DTexture9 (IDirect3DResource9 3..10, BaseTexture 11..16, Texture 17..) ---

        /// <summary>IDirect3DTexture9::GetSurfaceLevel (slot 18).</summary>
        public static int GetSurfaceLevel(IntPtr texture, uint level, out IntPtr surface)
        {
            IntPtr surf;
            int hr = ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>)Slot(texture, 18))(texture, level, &surf);
            surface = surf;
            return hr;
        }

        public static void Check(int hr, string what)
        {
            if (hr < 0)
                throw new InvalidOperationException($"{what} failed (HRESULT 0x{hr:X8}).");
        }
    }
}
