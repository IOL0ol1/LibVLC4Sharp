using System;
using System.Runtime.InteropServices;
using System.Text;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core.Interop
{
    /// <summary>
    /// Converts between the C <c>_Bool</c> (surfaced as <see cref="byte"/> by the generated interop —
    /// 0 = false, non-zero = true) and managed <see cref="bool"/>.
    /// </summary>
    public static class BoolExtensions
    {
        /// <summary>A native <c>_Bool</c> byte as a managed <see cref="bool"/> (non-zero → true).</summary>
        public static bool ToBool(this byte value) => value != 0;

        /// <summary>A managed <see cref="bool"/> as a native <c>_Bool</c> byte (1 or 0).</summary>
        public static byte ToByte(this bool value) => (byte)(value ? 1 : 0);
    }

    /// <summary>
    /// Helpers for passing managed callback delegates to the raw <see cref="IntPtr"/>
    /// function-pointer parameters emitted by the ClangSharp libvlc bindings (the callback typedefs
    /// surface as <c>IntPtr</c>, annotated with the original <c>[NativeTypeName("..._cb")]</c>).
    /// </summary>
    internal static class DelegateExtensions
    {
        /// <summary>
        /// The native callable function pointer for <paramref name="d"/>, or <see cref="IntPtr.Zero"/>
        /// when it is null. The caller must keep <paramref name="d"/> rooted (e.g. in a field) for as
        /// long as native code may invoke the pointer.
        /// </summary>
        public static IntPtr ToFunctionPointer(this Delegate? d) =>
            d is null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(d);
    }

    /// <summary>
    /// Helpers for working with the raw <c>byte*</c> / <see cref="IntPtr"/> UTF-8
    /// strings returned by the libvlc C API.
    /// </summary>
    public static partial class StringExtensions
    {
        /// <summary>Marshals a NUL-terminated UTF-8 C string to a managed string (null pointer → null).</summary>
        public unsafe static string? GetUtf8(this IntPtr pNativeData, bool freeNativeData = false)
        {
            if (pNativeData == IntPtr.Zero) return null;
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            var result = Marshal.PtrToStringUTF8(pNativeData);
#else
            var len = 0;
            while (Marshal.ReadByte(pNativeData, len) != 0) len++;
            var result = Encoding.UTF8.GetString((byte*)pNativeData, len);
#endif
            if (freeNativeData)
                libvlc_free(pNativeData);
            return result;
        }
    }
}
