using System;
using System.Runtime.InteropServices;

namespace LibVLCSharp.WPF.Interop
{
    /// <summary>
    /// Minimal hand-written Direct3D11 + DXGI interop for the WPF VideoView's D3D11 output path. COM
    /// objects are kept as raw <see cref="IntPtr"/> and methods are invoked by vtable slot index, so
    /// this is fully AnyCPU (the <c>stdcall</c> COM ABI behaves identically on x86/x64/arm64).
    /// </summary>
    /// <remarks>
    /// Only what the renderer needs is bound. Vtable slot indices follow the fixed order in
    /// <c>d3d11.h</c>/<c>dxgi.h</c> (IUnknown occupies slots 0–2); each is noted in the method comment.
    /// </remarks>
    internal static unsafe class Direct3D11
    {
        // --- D3D11CreateDevice ---
        public const int D3D_DRIVER_TYPE_HARDWARE = 1;
        public const uint D3D11_SDK_VERSION = 7;
        public const uint D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x20;
        public const uint D3D11_CREATE_DEVICE_VIDEO_SUPPORT = 0x800;

        // DXGI / D3D11 enums
        public const int DXGI_FORMAT_B8G8R8A8_UNORM = 87; // ↔ D3DFMT_A8R8G8B8 (the cross-API share format)
        public const int D3D11_USAGE_DEFAULT = 0;
        public const uint D3D11_BIND_SHADER_RESOURCE = 0x8;
        public const uint D3D11_BIND_RENDER_TARGET = 0x20;
        public const uint D3D11_RESOURCE_MISC_SHARED = 0x2; // legacy (NON-NT) shared handle — the only kind D3D9Ex can open

        // DXGI_ERROR_DEVICE_REMOVED / _RESET / _HUNG — the device-lost class for D3D11.
        public const int DXGI_ERROR_DEVICE_REMOVED = unchecked((int)0x887A0005);
        public const int DXGI_ERROR_DEVICE_HUNG = unchecked((int)0x887A0006);
        public const int DXGI_ERROR_DEVICE_RESET = unchecked((int)0x887A0007);

        [StructLayout(LayoutKind.Sequential)]
        public struct DXGI_SAMPLE_DESC
        {
            public uint Count;
            public uint Quality;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D3D11_TEXTURE2D_DESC
        {
            public uint Width;
            public uint Height;
            public uint MipLevels;
            public uint ArraySize;
            public int Format;                 // DXGI_FORMAT
            public DXGI_SAMPLE_DESC SampleDesc;
            public int Usage;                  // D3D11_USAGE
            public uint BindFlags;
            public uint CPUAccessFlags;
            public uint MiscFlags;
        }

        [DllImport("d3d11.dll", ExactSpelling = true)]
        public static extern int D3D11CreateDevice(
            IntPtr pAdapter, int driverType, IntPtr software, uint flags,
            IntPtr pFeatureLevels, uint featureLevels, uint sdkVersion,
            out IntPtr ppDevice, out int pFeatureLevel, out IntPtr ppImmediateContext);

        // vtable helper: method pointer at slot `index` of the COM object at `pComObject`.
        private static void* Slot(IntPtr pComObject, int index) => (*(void***)pComObject)[index];

        // --- IUnknown (slots 0–2) ---

        public static int QueryInterface(IntPtr unk, ref Guid iid, out IntPtr ppv)
        {
            IntPtr p;
            int hr;
            fixed (Guid* pIid = &iid)
                hr = ((delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)Slot(unk, 0))(unk, pIid, &p);
            ppv = p;
            return hr;
        }

        public static uint AddRef(IntPtr unk) =>
            ((delegate* unmanaged[Stdcall]<IntPtr, uint>)Slot(unk, 1))(unk);

        public static uint Release(IntPtr unk)
        {
            if (unk == IntPtr.Zero) return 0;
            return ((delegate* unmanaged[Stdcall]<IntPtr, uint>)Slot(unk, 2))(unk);
        }

        // --- ID3D11Device ---

        /// <summary>ID3D11Device::CreateTexture2D (slot 5). <paramref name="desc"/> by ref; no initial data.</summary>
        public static int CreateTexture2D(IntPtr device, ref D3D11_TEXTURE2D_DESC desc, out IntPtr texture)
        {
            IntPtr tex;
            int hr;
            fixed (D3D11_TEXTURE2D_DESC* pDesc = &desc)
                hr = ((delegate* unmanaged[Stdcall]<IntPtr, D3D11_TEXTURE2D_DESC*, IntPtr, IntPtr*, int>)Slot(device, 5))
                    (device, pDesc, IntPtr.Zero, &tex);
            texture = tex;
            return hr;
        }

        /// <summary>ID3D11Device::CreateRenderTargetView (slot 9). Null view desc → inherit the resource's format.</summary>
        public static int CreateRenderTargetView(IntPtr device, IntPtr resource, out IntPtr rtv)
        {
            IntPtr v;
            int hr = ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, IntPtr*, int>)Slot(device, 9))
                (device, resource, IntPtr.Zero, &v);
            rtv = v;
            return hr;
        }

        /// <summary>ID3D11Device::GetDeviceRemovedReason (slot 39) — S_OK while the device is alive.</summary>
        public static int GetDeviceRemovedReason(IntPtr device) =>
            ((delegate* unmanaged[Stdcall]<IntPtr, int>)Slot(device, 39))(device);

        // --- ID3D11DeviceContext ---

        /// <summary>ID3D11DeviceContext::Flush (slot 111) — submit queued GPU commands so the shared
        /// surface is coherent before the D3D9 side composites it.</summary>
        public static void Flush(IntPtr context) =>
            ((delegate* unmanaged[Stdcall]<IntPtr, void>)Slot(context, 111))(context);

        // --- IDXGIResource (IDXGIObject 3..6, IDXGIDeviceSubObject 7, Resource 8..) ---

        // IID_IDXGIResource = {035f3ab4-482e-4e50-b41f-8a7f8bd8960b}
        private static Guid IID_IDXGIResource =
            new Guid(0x035f3ab4, 0x482e, 0x4e50, 0xb4, 0x1f, 0x8a, 0x7f, 0x8b, 0xd8, 0x96, 0x0b);

        /// <summary>Gets the legacy (non-NT) shared handle of a <c>MISC_SHARED</c> resource via
        /// IDXGIResource::GetSharedHandle (slot 8). This handle is what <c>IDirect3DDevice9Ex::CreateTexture</c>
        /// opens on the host D3D9 device.</summary>
        public static int GetSharedHandle(IntPtr resource, out IntPtr sharedHandle)
        {
            sharedHandle = IntPtr.Zero;
            int hr = QueryInterface(resource, ref IID_IDXGIResource, out IntPtr dxgiRes);
            if (hr < 0) return hr;
            try
            {
                IntPtr h;
                hr = ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Slot(dxgiRes, 8))(dxgiRes, &h);
                if (hr >= 0) sharedHandle = h;
            }
            finally { Release(dxgiRes); }
            return hr;
        }

        // --- ID3D11Multithread (IUnknown 0..2, Enter 3, Leave 4, SetMultithreadProtected 5, Get 6) ---

        // IID_ID3D11Multithread = {9B7E4E00-342C-4106-A19F-4F2704F689F0}
        private static Guid IID_ID3D11Multithread =
            new Guid(0x9B7E4E00, 0x342C, 0x4106, 0xA1, 0x9F, 0x4F, 0x27, 0x04, 0xF6, 0x89, 0xF0);

        /// <summary>Best-effort: enables the immediate context's internal lock so libvlc can safely use it
        /// from its decode and render threads. The context implements ID3D11Multithread.</summary>
        public static void EnableMultithreadProtection(IntPtr context)
        {
            if (QueryInterface(context, ref IID_ID3D11Multithread, out IntPtr mt) < 0 || mt == IntPtr.Zero)
                return;
            try { ((delegate* unmanaged[Stdcall]<IntPtr, int, int>)Slot(mt, 5))(mt, 1); }
            finally { Release(mt); }
        }

        /// <summary>True for the device-removed class of HRESULTs that require recreating the device.</summary>
        public static bool IsDeviceLost(int hr) =>
            hr == DXGI_ERROR_DEVICE_REMOVED || hr == DXGI_ERROR_DEVICE_HUNG || hr == DXGI_ERROR_DEVICE_RESET;

        public static void Check(int hr, string what)
        {
            if (hr < 0)
                throw new InvalidOperationException($"{what} failed (HRESULT 0x{hr:X8}).");
        }
    }
}
