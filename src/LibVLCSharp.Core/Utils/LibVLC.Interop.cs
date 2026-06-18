using System;
using System.Runtime.InteropServices;

namespace LibVLCSharp.Core.Interop
{
    public static unsafe partial class libvlc
    { 
        public static byte libvlc_video_set_anw_callbacks(libvlc_media_player_t* mp, [NativeTypeName("libvlc_video_output_setup_cb")] IntPtr setup_cb, [NativeTypeName("libvlc_video_output_cleanup_cb")] IntPtr cleanup_cb, [NativeTypeName("libvlc_video_update_output_cb")] IntPtr update_output_cb, [NativeTypeName("void*")] IntPtr opaque)
        {
            return libvlc_video_set_output_callbacks(mp, libvlc_video_engine_t.libvlc_video_engine_anw, setup_cb, cleanup_cb, IntPtr.Zero, update_output_cb, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, opaque);
        }

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* libvlc_printerr([NativeTypeName("const char *")] byte* fmt, byte* args);
    }
}
