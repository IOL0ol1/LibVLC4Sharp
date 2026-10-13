using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LibVLCSharp.Core.Interop
{
    public unsafe ref struct Utf8Buffer
    {
        private IntPtr _ptr;

        /// <summary>Encodes <paramref name="value"/> to a NUL-terminated UTF-8 buffer (null → null pointer).</summary>
        public Utf8Buffer(string value) => _ptr = StringToHGlobalUtf8(value);

        /// <summary>Passes the buffer as the <c>sbyte*</c> argument of a libvlc call.</summary>
        public static implicit operator byte*(Utf8Buffer buffer) => (byte*)buffer._ptr;

        /// <summary>Frees the unmanaged buffer.</summary>
        public void Dispose()
        {
            FreeHGlobal(_ptr);
            _ptr = IntPtr.Zero;
        }

        /// <summary>
        /// Allocates a NUL-terminated UTF-8 copy in unmanaged memory (null → null pointer); free with
        /// <see cref="FreeHGlobal"/>.
        /// </summary>
        public static IntPtr StringToHGlobalUtf8(string value)
        {
            if (value is null) return IntPtr.Zero;
            var maxBytes = Encoding.UTF8.GetMaxByteCount(value.Length);
            var p = (byte*)Marshal.AllocHGlobal(maxBytes + 1);
            fixed (char* chars = value)
            {
                var actual = Encoding.UTF8.GetBytes(chars, value.Length, p, maxBytes);
                p[actual] = 0;
            }
            return (IntPtr)p;
        }
 
        /// <summary>Frees a buffer returned by <see cref="StringToHGlobalUtf8"/>.</summary>
        public static void FreeHGlobal(IntPtr p)
        {
            Marshal.FreeHGlobal(p);
        }
    }
}
