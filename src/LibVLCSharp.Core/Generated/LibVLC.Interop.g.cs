using System;
using System.Runtime.InteropServices;

namespace LibVLCSharp.Core.Interop
{
    public partial struct libvlc_instance_t
    {
    }

    public enum libvlc_log_level
    {
        LIBVLC_DEBUG = 0,
        LIBVLC_NOTICE = 2,
        LIBVLC_WARNING = 3,
        LIBVLC_ERROR = 4,
    }

    public partial struct libvlc_log_t
    {
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void libvlc_log_cb([NativeTypeName("void*")] IntPtr data, int level, [NativeTypeName("const libvlc_log_t *")] libvlc_log_t* ctx, [NativeTypeName("const char *")] byte* fmt, [NativeTypeName("va_list")] byte* args);

    public unsafe partial struct libvlc_module_description_t
    {
        [NativeTypeName("char *")]
        public byte* psz_name;

        [NativeTypeName("char *")]
        public byte* psz_shortname;

        [NativeTypeName("char *")]
        public byte* psz_longname;

        [NativeTypeName("char *")]
        public byte* psz_help;

        [NativeTypeName("char *")]
        public byte* psz_help_html;

        [NativeTypeName("struct libvlc_module_description_t *")]
        public libvlc_module_description_t* p_next;
    }

    public partial struct libvlc_renderer_discoverer_t
    {
    }

    public unsafe partial struct libvlc_rd_description_t
    {
        [NativeTypeName("char *")]
        public byte* psz_name;

        [NativeTypeName("char *")]
        public byte* psz_longname;
    }

    public partial struct libvlc_renderer_item_t
    {
    }

    public partial struct libvlc_renderer_discoverer_cbs
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("void (*)(void *, libvlc_renderer_item_t *)")]
        public IntPtr on_item_added;

        [NativeTypeName("void (*)(void *, libvlc_renderer_item_t *)")]
        public IntPtr on_item_removed;
    }

    public partial struct libvlc_picture_t
    {
    }

    public partial struct libvlc_picture_list_t
    {
    }

    public enum libvlc_picture_type_t
    {
        libvlc_picture_Argb,
        libvlc_picture_Png,
        libvlc_picture_Jpg,
        libvlc_picture_WebP,
        libvlc_picture_Rgba,
    }

    public enum libvlc_video_orient_t
    {
        libvlc_video_orient_top_left,
        libvlc_video_orient_top_right,
        libvlc_video_orient_bottom_left,
        libvlc_video_orient_bottom_right,
        libvlc_video_orient_left_top,
        libvlc_video_orient_left_bottom,
        libvlc_video_orient_right_top,
        libvlc_video_orient_right_bottom,
    }

    public enum libvlc_video_projection_t
    {
        libvlc_video_projection_rectangular,
        libvlc_video_projection_equirectangular,
        libvlc_video_projection_cubemap_layout_standard = 0x100,
    }

    public enum libvlc_video_multiview_t
    {
        libvlc_video_multiview_2d,
        libvlc_video_multiview_stereo_sbs,
        libvlc_video_multiview_stereo_tb,
        libvlc_video_multiview_stereo_row,
        libvlc_video_multiview_stereo_col,
        libvlc_video_multiview_stereo_frame,
        libvlc_video_multiview_stereo_checkerboard,
    }

    public enum libvlc_track_type_t
    {
        libvlc_track_unknown = -1,
        libvlc_track_audio = 0,
        libvlc_track_video = 1,
        libvlc_track_text = 2,
    }

    public partial struct libvlc_audio_track_t
    {
        [NativeTypeName("unsigned int")]
        public uint i_channels;

        [NativeTypeName("unsigned int")]
        public uint i_rate;
    }

    public partial struct libvlc_video_viewpoint_t
    {
        public float f_yaw;

        public float f_pitch;

        public float f_roll;

        public float f_field_of_view;
    }

    public partial struct libvlc_video_track_t
    {
        [NativeTypeName("unsigned int")]
        public uint i_height;

        [NativeTypeName("unsigned int")]
        public uint i_width;

        [NativeTypeName("unsigned int")]
        public uint i_sar_num;

        [NativeTypeName("unsigned int")]
        public uint i_sar_den;

        [NativeTypeName("unsigned int")]
        public uint i_frame_rate_num;

        [NativeTypeName("unsigned int")]
        public uint i_frame_rate_den;

        public libvlc_video_orient_t i_orientation;

        public libvlc_video_projection_t i_projection;

        public libvlc_video_viewpoint_t pose;

        public libvlc_video_multiview_t i_multiview;
    }

    public unsafe partial struct libvlc_subtitle_track_t
    {
        [NativeTypeName("char *")]
        public byte* psz_encoding;
    }

    public unsafe partial struct libvlc_media_track_t
    {
        [NativeTypeName("uint32_t")]
        public uint i_codec;

        [NativeTypeName("uint32_t")]
        public uint i_original_fourcc;

        public int i_id;

        public libvlc_track_type_t i_type;

        public int i_profile;

        public int i_level;

        [NativeTypeName("union libvlc_media_track_data")]
        public libvlc_media_track_data u;

        [NativeTypeName("unsigned int")]
        public uint i_bitrate;

        [NativeTypeName("char *")]
        public byte* psz_language;

        [NativeTypeName("char *")]
        public byte* psz_description;

        [NativeTypeName("const char *")]
        public byte* psz_id;

        [NativeTypeName("_Bool")]
        public byte id_stable;

        [NativeTypeName("char *")]
        public byte* psz_name;

        [NativeTypeName("_Bool")]
        public byte selected;

        [StructLayout(LayoutKind.Explicit)]
        public unsafe partial struct libvlc_media_track_data
        {
            [FieldOffset(0)]
            public libvlc_audio_track_t* audio;

            [FieldOffset(0)]
            public libvlc_video_track_t* video;

            [FieldOffset(0)]
            public libvlc_subtitle_track_t* subtitle;
        }
    }

    public partial struct libvlc_media_tracklist_t
    {
    }

    public partial struct libvlc_media_t
    {
    }

    public enum libvlc_meta_t
    {
        libvlc_meta_Title,
        libvlc_meta_Artist,
        libvlc_meta_Genre,
        libvlc_meta_Copyright,
        libvlc_meta_Album,
        libvlc_meta_TrackNumber,
        libvlc_meta_Description,
        libvlc_meta_Rating,
        libvlc_meta_Date,
        libvlc_meta_Setting,
        libvlc_meta_URL,
        libvlc_meta_Language,
        libvlc_meta_NowPlaying,
        libvlc_meta_Publisher,
        libvlc_meta_EncodedBy,
        libvlc_meta_ArtworkURL,
        libvlc_meta_TrackID,
        libvlc_meta_TrackTotal,
        libvlc_meta_Director,
        libvlc_meta_Season,
        libvlc_meta_Episode,
        libvlc_meta_ShowName,
        libvlc_meta_Actors,
        libvlc_meta_AlbumArtist,
        libvlc_meta_DiscNumber,
        libvlc_meta_DiscTotal,
    }

    public enum libvlc_state_t
    {
        libvlc_NothingSpecial = 0,
        libvlc_Opening,
        libvlc_Playing,
        libvlc_Paused,
        libvlc_Stopped,
        libvlc_Stopping,
        libvlc_Error,
    }

    public partial struct libvlc_media_stats_t
    {
        [NativeTypeName("uint64_t")]
        public ulong i_read_bytes;

        public float f_input_bitrate;

        [NativeTypeName("uint64_t")]
        public ulong i_demux_read_bytes;

        public float f_demux_bitrate;

        [NativeTypeName("uint64_t")]
        public ulong i_demux_corrupted;

        [NativeTypeName("uint64_t")]
        public ulong i_demux_discontinuity;

        [NativeTypeName("uint64_t")]
        public ulong i_decoded_video;

        [NativeTypeName("uint64_t")]
        public ulong i_decoded_audio;

        [NativeTypeName("uint64_t")]
        public ulong i_displayed_pictures;

        [NativeTypeName("uint64_t")]
        public ulong i_late_pictures;

        [NativeTypeName("uint64_t")]
        public ulong i_lost_pictures;

        [NativeTypeName("uint64_t")]
        public ulong i_played_abuffers;

        [NativeTypeName("uint64_t")]
        public ulong i_lost_abuffers;
    }

    public enum libvlc_media_type_t
    {
        libvlc_media_type_unknown,
        libvlc_media_type_file,
        libvlc_media_type_directory,
        libvlc_media_type_disc,
        libvlc_media_type_stream,
        libvlc_media_type_playlist,
    }

    public enum libvlc_media_slave_type_t
    {
        libvlc_media_slave_type_subtitle,
        libvlc_media_slave_type_generic,
        libvlc_media_slave_type_audio = libvlc_media_slave_type_generic,
    }

    public unsafe partial struct libvlc_media_slave_t
    {
        [NativeTypeName("char *")]
        public byte* psz_uri;

        public libvlc_media_slave_type_t i_type;

        [NativeTypeName("unsigned int")]
        public uint i_priority;
    }

    public partial struct libvlc_media_open_cbs
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("int (*)(void *, void **, uint64_t *)")]
        public IntPtr open;

        [NativeTypeName("ptrdiff_t (*)(void *, unsigned char *, size_t)")]
        public IntPtr read;

        [NativeTypeName("int (*)(void *, uint64_t)")]
        public IntPtr seek;

        [NativeTypeName("void (*)(void *)")]
        public IntPtr close;
    }

    public partial struct libvlc_media_list_t
    {
    }

    public partial struct libvlc_media_player_t
    {
    }

    public unsafe partial struct libvlc_title_description_t
    {
        [NativeTypeName("libvlc_time_t")]
        public long i_duration;

        [NativeTypeName("char *")]
        public byte* psz_name;

        [NativeTypeName("unsigned int")]
        public uint i_flags;
    }

    public unsafe partial struct libvlc_chapter_description_t
    {
        [NativeTypeName("libvlc_time_t")]
        public long i_time_offset;

        [NativeTypeName("libvlc_time_t")]
        public long i_duration;

        [NativeTypeName("char *")]
        public byte* psz_name;
    }

    public unsafe partial struct libvlc_audio_output_t
    {
        [NativeTypeName("char *")]
        public byte* psz_name;

        [NativeTypeName("char *")]
        public byte* psz_description;

        [NativeTypeName("struct libvlc_audio_output_t *")]
        public libvlc_audio_output_t* p_next;
    }

    public unsafe partial struct libvlc_audio_output_device_t
    {
        [NativeTypeName("struct libvlc_audio_output_device_t *")]
        public libvlc_audio_output_device_t* p_next;

        [NativeTypeName("char *")]
        public byte* psz_device;

        [NativeTypeName("char *")]
        public byte* psz_description;
    }

    public enum libvlc_video_marquee_option_t
    {
        libvlc_marquee_Enable = 0,
        libvlc_marquee_Text,
        libvlc_marquee_Color,
        libvlc_marquee_Opacity,
        libvlc_marquee_Position,
        libvlc_marquee_Refresh,
        libvlc_marquee_Size,
        libvlc_marquee_Timeout,
        libvlc_marquee_X,
        libvlc_marquee_Y,
    }

    public enum libvlc_navigate_mode_t
    {
        libvlc_navigate_activate = 0,
        libvlc_navigate_up,
        libvlc_navigate_down,
        libvlc_navigate_left,
        libvlc_navigate_right,
        libvlc_navigate_popup,
    }

    public enum libvlc_position_t
    {
        libvlc_position_disable = -1,
        libvlc_position_center,
        libvlc_position_left,
        libvlc_position_right,
        libvlc_position_top,
        libvlc_position_top_left,
        libvlc_position_top_right,
        libvlc_position_bottom,
        libvlc_position_bottom_left,
        libvlc_position_bottom_right,
    }

    public enum libvlc_video_fit_mode_t
    {
        libvlc_video_fit_none = 0,
        libvlc_video_fit_smaller,
        libvlc_video_fit_larger,
        libvlc_video_fit_width,
        libvlc_video_fit_height,
    }

    public enum libvlc_teletext_key_t
    {
        libvlc_teletext_key_red = (sbyte)('r') << 16,
        libvlc_teletext_key_green = (sbyte)('g') << 16,
        libvlc_teletext_key_yellow = (sbyte)('y') << 16,
        libvlc_teletext_key_blue = (sbyte)('b') << 16,
        libvlc_teletext_key_index = (sbyte)('i') << 16,
    }

    public enum libvlc_abloop_t
    {
        libvlc_abloop_none,
        libvlc_abloop_a,
        libvlc_abloop_b,
    }

    public enum libvlc_capability_t
    {
        libvlc_capability_seek = 0x01,
        libvlc_capability_pause = 0x02,
        libvlc_capability_change_rate = 0x04,
        libvlc_capability_rewind = 0x08,
    }

    public enum libvlc_list_action_t
    {
        libvlc_list_action_added,
        libvlc_list_action_removed,
        libvlc_list_action_updated,
    }

    public enum libvlc_stopping_reason_t
    {
        libvlc_stopping_reason_error,
        libvlc_stopping_reason_eos,
        libvlc_stopping_reason_user,
    }

    public partial struct libvlc_equalizer_t
    {
    }

    public partial struct libvlc_media_player_cbs
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("void (*)(void *, libvlc_media_t *)")]
        public IntPtr on_media_changed;

        [NativeTypeName("void (*)(void *, libvlc_media_t *, libvlc_stopping_reason_t)")]
        public IntPtr on_media_stopping;

        [NativeTypeName("void (*)(void *, libvlc_state_t)")]
        public IntPtr on_state_changed;

        [NativeTypeName("void (*)(void *, float)")]
        public IntPtr on_buffering_changed;

        [NativeTypeName("void (*)(void *, libvlc_capability_t, libvlc_capability_t)")]
        public IntPtr on_capabilities_changed;

        [NativeTypeName("void (*)(void *, libvlc_time_t, double)")]
        public IntPtr on_position_changed;

        [NativeTypeName("void (*)(void *, libvlc_time_t)")]
        public IntPtr on_length_changed;

        [NativeTypeName("void (*)(void *, libvlc_list_action_t, libvlc_track_type_t, const char *)")]
        public IntPtr on_track_list_changed;

        [NativeTypeName("void (*)(void *, libvlc_track_type_t, const char *, const char *)")]
        public IntPtr on_track_selection_changed;

        [NativeTypeName("void (*)(void *, libvlc_list_action_t, int)")]
        public IntPtr on_program_list_changed;

        [NativeTypeName("void (*)(void *, int, int)")]
        public IntPtr on_program_selection_changed;

        [NativeTypeName("void (*)(void *)")]
        public IntPtr on_titles_changed;

        [NativeTypeName("void (*)(void *, const libvlc_title_description_t *, unsigned int)")]
        public IntPtr on_title_selection_changed;

        [NativeTypeName("void (*)(void *, const libvlc_title_description_t *, unsigned int, const libvlc_chapter_description_t *, unsigned int)")]
        public IntPtr on_chapter_selection_changed;

        [NativeTypeName("void (*)(void *, _Bool, const char *)")]
        public IntPtr on_recording_changed;

        [NativeTypeName("void (*)(void *, const char *)")]
        public IntPtr on_screenshot_taken;

        [NativeTypeName("void (*)(void *, libvlc_media_t *)")]
        public IntPtr on_media_parsed;

        [NativeTypeName("void (*)(void *, libvlc_media_t *)")]
        public IntPtr on_media_meta_changed;

        [NativeTypeName("void (*)(void *, libvlc_media_t *)")]
        public IntPtr on_media_subitems_changed;

        [NativeTypeName("void (*)(void *, libvlc_media_t *, libvlc_picture_list_t *)")]
        public IntPtr on_media_attachments_added;

        [NativeTypeName("void (*)(void *, unsigned int)")]
        public IntPtr on_vout_changed;

        [NativeTypeName("void (*)(void *, _Bool)")]
        public IntPtr on_cork_changed;

        [NativeTypeName("void (*)(void *, float)")]
        public IntPtr on_audio_volume_changed;

        [NativeTypeName("void (*)(void *, _Bool)")]
        public IntPtr on_audio_mute_changed;

        [NativeTypeName("void (*)(void *, const char *)")]
        public IntPtr on_audio_device_changed;
    }

    public enum libvlc_video_color_primaries_t
    {
        libvlc_video_primaries_BT601_525 = 1,
        libvlc_video_primaries_BT601_625 = 2,
        libvlc_video_primaries_BT709 = 3,
        libvlc_video_primaries_BT2020 = 4,
        libvlc_video_primaries_DCI_P3 = 5,
        libvlc_video_primaries_BT470_M = 6,
    }

    public enum libvlc_video_color_space_t
    {
        libvlc_video_colorspace_BT601 = 1,
        libvlc_video_colorspace_BT709 = 2,
        libvlc_video_colorspace_BT2020 = 3,
    }

    public enum libvlc_video_transfer_func_t
    {
        libvlc_video_transfer_func_LINEAR = 1,
        libvlc_video_transfer_func_SRGB = 2,
        libvlc_video_transfer_func_BT470_BG = 3,
        libvlc_video_transfer_func_BT470_M = 4,
        libvlc_video_transfer_func_BT709 = 5,
        libvlc_video_transfer_func_PQ = 6,
        libvlc_video_transfer_func_SMPTE_240 = 7,
        libvlc_video_transfer_func_HLG = 8,
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("void*")]
    public unsafe delegate IntPtr libvlc_video_lock_cb([NativeTypeName("void*")] IntPtr opaque, [NativeTypeName("void **")] IntPtr* planes);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void libvlc_video_unlock_cb([NativeTypeName("void*")] IntPtr opaque, [NativeTypeName("void*")] IntPtr picture, [NativeTypeName("void *const *")] IntPtr* planes);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_video_display_cb([NativeTypeName("void*")] IntPtr opaque, [NativeTypeName("void*")] IntPtr picture);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("unsigned int")]
    public unsafe delegate uint libvlc_video_format_cb([NativeTypeName("void **")] IntPtr* opaque, [NativeTypeName("char *")] byte* chroma, [NativeTypeName("unsigned int *")] uint* width, [NativeTypeName("unsigned int *")] uint* height, [NativeTypeName("unsigned int *")] uint* pitches, [NativeTypeName("unsigned int *")] uint* lines);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_video_cleanup_cb([NativeTypeName("void*")] IntPtr opaque);

    public partial struct libvlc_video_setup_device_cfg_t
    {
        [NativeTypeName("_Bool")]
        public byte hardware_decoding;
    }

    public partial struct libvlc_video_setup_device_info_t
    {
        [NativeTypeName("union libvlc_video_setup_device_data")]
        public libvlc_video_setup_device_data u;

        [StructLayout(LayoutKind.Explicit)]
        public partial struct libvlc_video_setup_device_data
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_libvlc_media_player_L979_C9")]
            public _d3d11_e__Struct d3d11;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_libvlc_media_player_L983_C9")]
            public _d3d9_e__Struct d3d9;

            public partial struct _d3d11_e__Struct
            {
                [NativeTypeName("void*")]
                public IntPtr device_context;

                [NativeTypeName("void*")]
                public IntPtr context_mutex;
            }

            public partial struct _d3d9_e__Struct
            {
                [NativeTypeName("void*")]
                public IntPtr device;

                public int adapter;
            }
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("_Bool")]
    public unsafe delegate bool libvlc_video_output_setup_cb([NativeTypeName("void **")] IntPtr* opaque, [NativeTypeName("const libvlc_video_setup_device_cfg_t *")] libvlc_video_setup_device_cfg_t* cfg, libvlc_video_setup_device_info_t* @out);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_video_output_cleanup_cb([NativeTypeName("void*")] IntPtr opaque);

    public partial struct libvlc_video_render_cfg_t
    {
        [NativeTypeName("unsigned int")]
        public uint width;

        [NativeTypeName("unsigned int")]
        public uint height;

        [NativeTypeName("unsigned int")]
        public uint bitdepth;

        [NativeTypeName("_Bool")]
        public byte full_range;

        public libvlc_video_color_space_t colorspace;

        public libvlc_video_color_primaries_t primaries;

        public libvlc_video_transfer_func_t transfer;

        [NativeTypeName("void*")]
        public IntPtr device;
    }

    public partial struct libvlc_video_output_cfg_t
    {
        [NativeTypeName("union libvlc_video_output_format")]
        public libvlc_video_output_format u;

        [NativeTypeName("_Bool")]
        public byte full_range;

        public libvlc_video_color_space_t colorspace;

        public libvlc_video_color_primaries_t primaries;

        public libvlc_video_transfer_func_t transfer;

        public libvlc_video_orient_t orientation;

        [StructLayout(LayoutKind.Explicit)]
        public partial struct libvlc_video_output_format
        {
            [FieldOffset(0)]
            public int dxgi_format;

            [FieldOffset(0)]
            [NativeTypeName("uint32_t")]
            public uint d3d9_format;

            [FieldOffset(0)]
            public int opengl_format;

            [FieldOffset(0)]
            [NativeTypeName("void*")]
            public IntPtr p_surface;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_libvlc_media_player_L1061_C9")]
            public _anw_e__Struct anw;

            public partial struct _anw_e__Struct
            {
                [NativeTypeName("void*")]
                public IntPtr video;

                [NativeTypeName("void*")]
                public IntPtr subtitle;
            }
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("_Bool")]
    public unsafe delegate bool libvlc_video_update_output_cb([NativeTypeName("void*")] IntPtr opaque, [NativeTypeName("const libvlc_video_render_cfg_t *")] libvlc_video_render_cfg_t* cfg, libvlc_video_output_cfg_t* output);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_video_swap_cb([NativeTypeName("void*")] IntPtr opaque);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("_Bool")]
    public delegate bool libvlc_video_makeCurrent_cb([NativeTypeName("void*")] IntPtr opaque, [NativeTypeName("_Bool")] bool enter);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("void*")]
    public unsafe delegate IntPtr libvlc_video_getProcAddress_cb([NativeTypeName("void*")] IntPtr opaque, [NativeTypeName("const char *")] byte* fct_name);

    public unsafe partial struct libvlc_video_frame_hdr10_metadata_t
    {
        [NativeTypeName("uint16_t[2]")]
        public fixed ushort RedPrimary[2];

        [NativeTypeName("uint16_t[2]")]
        public fixed ushort GreenPrimary[2];

        [NativeTypeName("uint16_t[2]")]
        public fixed ushort BluePrimary[2];

        [NativeTypeName("uint16_t[2]")]
        public fixed ushort WhitePoint[2];

        [NativeTypeName("unsigned int")]
        public uint MaxMasteringLuminance;

        [NativeTypeName("unsigned int")]
        public uint MinMasteringLuminance;

        [NativeTypeName("uint16_t")]
        public ushort MaxContentLightLevel;

        [NativeTypeName("uint16_t")]
        public ushort MaxFrameAverageLightLevel;
    }

    public enum libvlc_video_metadata_type_t
    {
        libvlc_video_metadata_frame_hdr10,
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_video_frameMetadata_cb([NativeTypeName("void*")] IntPtr opaque, libvlc_video_metadata_type_t type, [NativeTypeName("const void *")] IntPtr metadata);

    public enum libvlc_video_engine_t
    {
        libvlc_video_engine_disable,
        libvlc_video_engine_opengl,
        libvlc_video_engine_gles2,
        libvlc_video_engine_d3d11,
        libvlc_video_engine_d3d9,
        libvlc_video_engine_anw,
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_video_output_resize_cb([NativeTypeName("void*")] IntPtr report_opaque, [NativeTypeName("unsigned int")] uint width, [NativeTypeName("unsigned int")] uint height);

    public enum libvlc_video_output_mouse_button_t
    {
        libvlc_video_output_mouse_button_left = 0,
        libvlc_video_output_mouse_button_middle = 1,
        libvlc_video_output_mouse_button_right = 2,
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_video_output_mouse_move_cb([NativeTypeName("void*")] IntPtr opaque, int x, int y);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_video_output_mouse_press_cb([NativeTypeName("void*")] IntPtr opaque, libvlc_video_output_mouse_button_t button);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_video_output_mouse_release_cb([NativeTypeName("void*")] IntPtr opaque, libvlc_video_output_mouse_button_t button);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_video_output_set_window_cb([NativeTypeName("void*")] IntPtr opaque, [NativeTypeName("libvlc_video_output_resize_cb")] IntPtr report_size_change, [NativeTypeName("libvlc_video_output_mouse_move_cb")] IntPtr report_mouse_move, [NativeTypeName("libvlc_video_output_mouse_press_cb")] IntPtr report_mouse_pressed, [NativeTypeName("libvlc_video_output_mouse_release_cb")] IntPtr report_mouse_released, [NativeTypeName("void*")] IntPtr report_opaque);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: NativeTypeName("_Bool")]
    public delegate bool libvlc_video_output_select_plane_cb([NativeTypeName("void*")] IntPtr opaque, [NativeTypeName("size_t")] UIntPtr plane, [NativeTypeName("void*")] IntPtr output);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_audio_play_cb([NativeTypeName("void*")] IntPtr data, [NativeTypeName("const void *")] IntPtr samples, [NativeTypeName("unsigned int")] uint count, [NativeTypeName("int64_t")] long pts);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_audio_pause_cb([NativeTypeName("void*")] IntPtr data, [NativeTypeName("int64_t")] long pts);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_audio_resume_cb([NativeTypeName("void*")] IntPtr data, [NativeTypeName("int64_t")] long pts);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_audio_flush_cb([NativeTypeName("void*")] IntPtr data, [NativeTypeName("int64_t")] long pts);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_audio_drain_cb([NativeTypeName("void*")] IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_audio_set_volume_cb([NativeTypeName("void*")] IntPtr data, float volume, [NativeTypeName("_Bool")] bool mute);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int libvlc_audio_setup_cb([NativeTypeName("void **")] IntPtr* opaque, [NativeTypeName("char *")] byte* format, [NativeTypeName("unsigned int *")] uint* rate, [NativeTypeName("unsigned int *")] uint* channels);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void libvlc_audio_cleanup_cb([NativeTypeName("void*")] IntPtr opaque);

    public unsafe partial struct libvlc_player_program_t
    {
        public int i_group_id;

        [NativeTypeName("char *")]
        public byte* psz_name;

        [NativeTypeName("_Bool")]
        public byte b_selected;

        [NativeTypeName("_Bool")]
        public byte b_scrambled;
    }

    public partial struct libvlc_player_programlist_t
    {
    }

    public enum libvlc_video_stereo_mode_t
    {
        libvlc_VideoStereoAuto = 0,
        libvlc_VideoStereoStereo,
        libvlc_VideoStereoLeftEye,
        libvlc_VideoStereoRightEye,
        libvlc_VideoStereoSideBySide,
    }

    public enum libvlc_video_logo_option_t
    {
        libvlc_logo_enable,
        libvlc_logo_file,
        libvlc_logo_x,
        libvlc_logo_y,
        libvlc_logo_delay,
        libvlc_logo_repeat,
        libvlc_logo_opacity,
        libvlc_logo_position,
    }

    public enum libvlc_video_adjust_option_t
    {
        libvlc_adjust_Enable = 0,
        libvlc_adjust_Contrast,
        libvlc_adjust_Brightness,
        libvlc_adjust_Hue,
        libvlc_adjust_Saturation,
        libvlc_adjust_Gamma,
    }

    public enum libvlc_audio_output_stereomode_t
    {
        libvlc_AudioStereoMode_Unset = 0,
        libvlc_AudioStereoMode_Stereo = 1,
        libvlc_AudioStereoMode_RStereo = 2,
        libvlc_AudioStereoMode_Left = 3,
        libvlc_AudioStereoMode_Right = 4,
        libvlc_AudioStereoMode_Dolbys = 5,
        libvlc_AudioStereoMode_Mono = 7,
    }

    public enum libvlc_audio_output_mixmode_t
    {
        libvlc_AudioMixMode_Unset = 0,
        libvlc_AudioMixMode_Stereo = 1,
        libvlc_AudioMixMode_Binaural = 2,
        libvlc_AudioMixMode_4_0 = 3,
        libvlc_AudioMixMode_5_1 = 4,
        libvlc_AudioMixMode_7_1 = 5,
    }

    public enum libvlc_media_player_role_t
    {
        libvlc_role_None = 0,
        libvlc_role_Music,
        libvlc_role_Video,
        libvlc_role_Communication,
        libvlc_role_Game,
        libvlc_role_Notification,
        libvlc_role_Animation,
        libvlc_role_Production,
        libvlc_role_Accessibility,
        libvlc_role_Test,
    }

    public partial struct libvlc_media_player_time_point_t
    {
        public double position;

        public double rate;

        [NativeTypeName("libvlc_time_t")]
        public long ts_us;

        [NativeTypeName("libvlc_time_t")]
        public long length_us;

        [NativeTypeName("libvlc_time_t")]
        public long system_date_us;
    }

    public partial struct libvlc_media_player_watch_time_cbs
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("void (*)(void *, const libvlc_media_player_time_point_t *)")]
        public IntPtr on_update;

        [NativeTypeName("void (*)(void *, libvlc_time_t)")]
        public IntPtr on_paused;

        [NativeTypeName("void (*)(void *, const libvlc_media_player_time_point_t *)")]
        public IntPtr on_seek;
    }

    public partial struct libvlc_media_list_player_t
    {
    }

    public enum libvlc_playback_mode_t
    {
        libvlc_playback_mode_default,
        libvlc_playback_mode_loop,
        libvlc_playback_mode_repeat,
    }

    public enum libvlc_media_discoverer_category_t
    {
        libvlc_media_discoverer_devices,
        libvlc_media_discoverer_lan,
        libvlc_media_discoverer_podcasts,
        libvlc_media_discoverer_localdirs,
    }

    public unsafe partial struct libvlc_media_discoverer_description_t
    {
        [NativeTypeName("char *")]
        public byte* psz_name;

        [NativeTypeName("char *")]
        public byte* psz_longname;

        public libvlc_media_discoverer_category_t i_cat;
    }

    public partial struct libvlc_media_discoverer_t
    {
    }

    public partial struct libvlc_media_discoverer_cbs
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("void (*)(void *, libvlc_media_t *, libvlc_media_t *)")]
        public IntPtr on_media_added;

        [NativeTypeName("void (*)(void *, libvlc_media_t *)")]
        public IntPtr on_media_removed;
    }

    public partial struct libvlc_dialog_id
    {
    }

    public enum libvlc_dialog_question_type
    {
        LIBVLC_DIALOG_QUESTION_NORMAL,
        LIBVLC_DIALOG_QUESTION_WARNING,
        LIBVLC_DIALOG_QUESTION_CRITICAL,
    }

    public partial struct libvlc_dialog_cbs
    {
        [NativeTypeName("uint32_t")]
        public uint version;

        [NativeTypeName("void (*)(void *, libvlc_dialog_id *, const char *, const char *, const char *, _Bool)")]
        public IntPtr pf_display_login;

        [NativeTypeName("void (*)(void *, libvlc_dialog_id *, const char *, const char *, libvlc_dialog_question_type, const char *, const char *, const char *)")]
        public IntPtr pf_display_question;

        [NativeTypeName("void (*)(void *, libvlc_dialog_id *, const char *, const char *, _Bool, float, const char *)")]
        public IntPtr pf_display_progress;

        [NativeTypeName("void (*)(void *, libvlc_dialog_id *)")]
        public IntPtr pf_cancel;

        [NativeTypeName("void (*)(void *, libvlc_dialog_id *, float, const char *)")]
        public IntPtr pf_update_progress;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void libvlc_dialog_error_cbs([NativeTypeName("void*")] IntPtr p_data, [NativeTypeName("const char *")] byte* psz_title, [NativeTypeName("const char *")] byte* psz_text);

    public static unsafe partial class libvlc
    {
        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* libvlc_errmsg();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_clearerr();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_instance_t* libvlc_new(int argc, [NativeTypeName("const char *const *")] byte** argv);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_release(libvlc_instance_t* p_instance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_instance_t* libvlc_retain(libvlc_instance_t* p_instance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_abi_version();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_set_user_agent(libvlc_instance_t* p_instance, [NativeTypeName("const char *")] byte* name, [NativeTypeName("const char *")] byte* http);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_set_app_id(libvlc_instance_t* p_instance, [NativeTypeName("const char *")] byte* id, [NativeTypeName("const char *")] byte* version, [NativeTypeName("const char *")] byte* icon);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* libvlc_get_version();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* libvlc_get_compiler();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* libvlc_get_changeset();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_free([NativeTypeName("void*")] IntPtr ptr);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_log_get_context([NativeTypeName("const libvlc_log_t *")] libvlc_log_t* ctx, [NativeTypeName("const char **")] byte** module, [NativeTypeName("const char **")] byte** file, [NativeTypeName("unsigned int *")] uint* line);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_log_get_object([NativeTypeName("const libvlc_log_t *")] libvlc_log_t* ctx, [NativeTypeName("const char **")] byte** name, [NativeTypeName("const char **")] byte** header, [NativeTypeName("uintptr_t *")] UIntPtr* id);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_log_unset(libvlc_instance_t* p_instance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_log_set(libvlc_instance_t* p_instance, [NativeTypeName("libvlc_log_cb")] IntPtr cb, [NativeTypeName("void*")] IntPtr data);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_log_set_file(libvlc_instance_t* p_instance, [NativeTypeName("FILE *")] IntPtr stream);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_module_description_list_release(libvlc_module_description_t* p_list);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_module_description_t* libvlc_audio_filter_list_get(libvlc_instance_t* p_instance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_module_description_t* libvlc_video_filter_list_get(libvlc_instance_t* p_instance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("libvlc_time_t")]
        public static extern long libvlc_clock();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_renderer_item_t* libvlc_renderer_item_retain(libvlc_renderer_item_t* p_item);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_renderer_item_release(libvlc_renderer_item_t* p_item);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* libvlc_renderer_item_name([NativeTypeName("const libvlc_renderer_item_t *")] libvlc_renderer_item_t* p_item);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* libvlc_renderer_item_type([NativeTypeName("const libvlc_renderer_item_t *")] libvlc_renderer_item_t* p_item);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* libvlc_renderer_item_icon_uri([NativeTypeName("const libvlc_renderer_item_t *")] libvlc_renderer_item_t* p_item);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_renderer_item_flags([NativeTypeName("const libvlc_renderer_item_t *")] libvlc_renderer_item_t* p_item);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_renderer_discoverer_t* libvlc_renderer_discoverer_new(libvlc_instance_t* p_inst, [NativeTypeName("const char *")] byte* psz_name, [NativeTypeName("const struct libvlc_renderer_discoverer_cbs *")] libvlc_renderer_discoverer_cbs* cbs, [NativeTypeName("void*")] IntPtr cbs_opaque);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_renderer_discoverer_destroy(libvlc_renderer_discoverer_t* p_rd);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_renderer_discoverer_start(libvlc_renderer_discoverer_t* p_rd);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_renderer_discoverer_stop(libvlc_renderer_discoverer_t* p_rd);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern UIntPtr libvlc_renderer_discoverer_list_get(libvlc_instance_t* p_inst, libvlc_rd_description_t*** ppp_services);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_renderer_discoverer_list_release(libvlc_rd_description_t** pp_services, [NativeTypeName("size_t")] UIntPtr i_count);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_picture_t* libvlc_picture_retain(libvlc_picture_t* pic);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_picture_release(libvlc_picture_t* pic);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_picture_save([NativeTypeName("const libvlc_picture_t *")] libvlc_picture_t* pic, [NativeTypeName("const char *")] byte* path);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const unsigned char *")]
        public static extern byte* libvlc_picture_get_buffer([NativeTypeName("const libvlc_picture_t *")] libvlc_picture_t* pic, [NativeTypeName("size_t *")] UIntPtr* size);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_picture_type_t libvlc_picture_type([NativeTypeName("const libvlc_picture_t *")] libvlc_picture_t* pic);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint libvlc_picture_get_stride([NativeTypeName("const libvlc_picture_t *")] libvlc_picture_t* pic);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint libvlc_picture_get_width([NativeTypeName("const libvlc_picture_t *")] libvlc_picture_t* pic);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint libvlc_picture_get_height([NativeTypeName("const libvlc_picture_t *")] libvlc_picture_t* pic);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("libvlc_time_t")]
        public static extern long libvlc_picture_get_time([NativeTypeName("const libvlc_picture_t *")] libvlc_picture_t* pic);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern UIntPtr libvlc_picture_list_count([NativeTypeName("const libvlc_picture_list_t *")] libvlc_picture_list_t* list);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_picture_t* libvlc_picture_list_at([NativeTypeName("const libvlc_picture_list_t *")] libvlc_picture_list_t* list, [NativeTypeName("size_t")] UIntPtr index);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_picture_list_destroy(libvlc_picture_list_t* list);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern UIntPtr libvlc_media_tracklist_count([NativeTypeName("const libvlc_media_tracklist_t *")] libvlc_media_tracklist_t* list);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_track_t* libvlc_media_tracklist_at(libvlc_media_tracklist_t* list, [NativeTypeName("size_t")] UIntPtr index);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_tracklist_delete(libvlc_media_tracklist_t* list);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_track_t* libvlc_media_track_retain(libvlc_media_track_t* track);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_track_release(libvlc_media_track_t* track);

        public const int libvlc_media_option_trusted = 0x2;
        public const int libvlc_media_option_unique = 0x100;

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_t* libvlc_media_new_location([NativeTypeName("const char *")] byte* psz_mrl);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_t* libvlc_media_new_path([NativeTypeName("const char *")] byte* path);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_t* libvlc_media_new_fd(int fd);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_t* libvlc_media_new_callbacks([NativeTypeName("const struct libvlc_media_open_cbs *")] libvlc_media_open_cbs* cbs, [NativeTypeName("void*")] IntPtr cbs_opaque);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_t* libvlc_media_new_as_node([NativeTypeName("const char *")] byte* psz_name);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_add_option(libvlc_media_t* p_md, [NativeTypeName("const char *")] byte* psz_options);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_add_option_flag(libvlc_media_t* p_md, [NativeTypeName("const char *")] byte* psz_options, [NativeTypeName("unsigned int")] uint i_flags);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_t* libvlc_media_retain(libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_release(libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* libvlc_media_get_mrl(libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_t* libvlc_media_duplicate(libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* libvlc_media_get_meta(libvlc_media_t* p_md, libvlc_meta_t e_meta);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_set_meta(libvlc_media_t* p_md, libvlc_meta_t e_meta, [NativeTypeName("const char *")] byte* psz_value);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* libvlc_media_get_meta_extra(libvlc_media_t* p_md, [NativeTypeName("const char *")] byte* psz_name);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_set_meta_extra(libvlc_media_t* p_md, [NativeTypeName("const char *")] byte* psz_name, [NativeTypeName("const char *")] byte* psz_value);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint libvlc_media_get_meta_extra_names(libvlc_media_t* p_md, [NativeTypeName("char ***")] byte*** pppsz_names);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_meta_extra_names_release([NativeTypeName("char **")] byte** ppsz_names, [NativeTypeName("unsigned int")] uint i_count);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_save_meta(libvlc_instance_t* inst, libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte libvlc_media_get_stats(libvlc_media_t* p_md, libvlc_media_stats_t* p_stats);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("struct libvlc_media_list_t *")]
        public static extern libvlc_media_list_t* libvlc_media_subitems(libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("libvlc_time_t")]
        public static extern long libvlc_media_get_duration(libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_get_filestat(libvlc_media_t* p_md, [NativeTypeName("unsigned int")] uint type, [NativeTypeName("uint64_t *")] ulong* @out);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte libvlc_media_is_parsed(libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_set_user_data(libvlc_media_t* p_md, [NativeTypeName("void*")] IntPtr p_new_user_data);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("void*")]
        public static extern IntPtr libvlc_media_get_user_data(libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_tracklist_t* libvlc_media_get_tracklist(libvlc_media_t* p_md, libvlc_track_type_t type);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* libvlc_media_get_codec_description(libvlc_track_type_t i_type, [NativeTypeName("uint32_t")] uint i_codec);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_type_t libvlc_media_get_type(libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_slaves_add(libvlc_media_t* p_md, libvlc_media_slave_type_t i_type, [NativeTypeName("unsigned int")] uint i_priority, [NativeTypeName("const char *")] byte* psz_uri);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_slaves_clear(libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint libvlc_media_slaves_get(libvlc_media_t* p_md, libvlc_media_slave_t*** ppp_slaves);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_slaves_release(libvlc_media_slave_t** pp_slaves, [NativeTypeName("unsigned int")] uint i_count);

        public const int libvlc_title_menu = 0x01;
        public const int libvlc_title_interactive = 0x02;

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_player_t* libvlc_media_player_new(libvlc_instance_t* p_libvlc_instance, [NativeTypeName("const struct libvlc_media_player_cbs *")] libvlc_media_player_cbs* cbs, [NativeTypeName("void*")] IntPtr cbs_opaque);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_player_t* libvlc_media_player_new_from_media(libvlc_instance_t* inst, libvlc_media_t* p_md, [NativeTypeName("const struct libvlc_media_player_cbs *")] libvlc_media_player_cbs* cbs, [NativeTypeName("void*")] IntPtr cbs_opaque);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_release(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_player_t* libvlc_media_player_retain(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_set_media(libvlc_media_player_t* p_mi, libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_t* libvlc_media_player_get_media(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_set_next_media(libvlc_media_player_t* p_mi, libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_t* libvlc_media_player_get_next_media(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte libvlc_media_player_is_playing(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_play(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_set_pause(libvlc_media_player_t* mp, int do_pause);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_pause(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_stop_async(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_set_renderer(libvlc_media_player_t* p_mi, libvlc_renderer_item_t* p_item);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_callbacks(libvlc_media_player_t* mp, [NativeTypeName("libvlc_video_lock_cb")] IntPtr @lock, [NativeTypeName("libvlc_video_unlock_cb")] IntPtr unlock, [NativeTypeName("libvlc_video_display_cb")] IntPtr display, [NativeTypeName("void*")] IntPtr opaque);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_format(libvlc_media_player_t* mp, [NativeTypeName("const char *")] byte* chroma, [NativeTypeName("unsigned int")] uint width, [NativeTypeName("unsigned int")] uint height, [NativeTypeName("unsigned int")] uint pitch);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_format_callbacks(libvlc_media_player_t* mp, [NativeTypeName("libvlc_video_format_cb")] IntPtr setup, [NativeTypeName("libvlc_video_cleanup_cb")] IntPtr cleanup);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte libvlc_video_set_output_callbacks(libvlc_media_player_t* mp, libvlc_video_engine_t engine, [NativeTypeName("libvlc_video_output_setup_cb")] IntPtr setup_cb, [NativeTypeName("libvlc_video_output_cleanup_cb")] IntPtr cleanup_cb, [NativeTypeName("libvlc_video_output_set_window_cb")] IntPtr window_cb, [NativeTypeName("libvlc_video_update_output_cb")] IntPtr update_output_cb, [NativeTypeName("libvlc_video_swap_cb")] IntPtr swap_cb, [NativeTypeName("libvlc_video_makeCurrent_cb")] IntPtr makeCurrent_cb, [NativeTypeName("libvlc_video_getProcAddress_cb")] IntPtr getProcAddress_cb, [NativeTypeName("libvlc_video_frameMetadata_cb")] IntPtr metadata_cb, [NativeTypeName("libvlc_video_output_select_plane_cb")] IntPtr select_plane_cb, [NativeTypeName("void*")] IntPtr opaque);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_set_nsobject(libvlc_media_player_t* p_mi, [NativeTypeName("void*")] IntPtr drawable);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("void*")]
        public static extern IntPtr libvlc_media_player_get_nsobject(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_set_xwindow(libvlc_media_player_t* p_mi, [NativeTypeName("uint32_t")] uint drawable);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint libvlc_media_player_get_xwindow(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_set_hwnd(libvlc_media_player_t* p_mi, [NativeTypeName("void*")] IntPtr drawable);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("void*")]
        public static extern IntPtr libvlc_media_player_get_hwnd(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_set_android_context(libvlc_media_player_t* p_mi, [NativeTypeName("void*")] IntPtr p_awindow_handler);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_audio_set_callbacks(libvlc_media_player_t* mp, [NativeTypeName("libvlc_audio_play_cb")] IntPtr play, [NativeTypeName("libvlc_audio_pause_cb")] IntPtr pause, [NativeTypeName("libvlc_audio_resume_cb")] IntPtr resume, [NativeTypeName("libvlc_audio_flush_cb")] IntPtr flush, [NativeTypeName("libvlc_audio_drain_cb")] IntPtr drain, [NativeTypeName("void*")] IntPtr opaque);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_audio_set_volume_callback(libvlc_media_player_t* mp, [NativeTypeName("libvlc_audio_set_volume_cb")] IntPtr set_volume);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_audio_set_format_callbacks(libvlc_media_player_t* mp, [NativeTypeName("libvlc_audio_setup_cb")] IntPtr setup, [NativeTypeName("libvlc_audio_cleanup_cb")] IntPtr cleanup);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_audio_set_format(libvlc_media_player_t* mp, [NativeTypeName("const char *")] byte* format, [NativeTypeName("unsigned int")] uint rate, [NativeTypeName("unsigned int")] uint channels);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("libvlc_time_t")]
        public static extern long libvlc_media_player_get_length(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("libvlc_time_t")]
        public static extern long libvlc_media_player_get_time(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_set_time(libvlc_media_player_t* p_mi, [NativeTypeName("libvlc_time_t")] long i_time, [NativeTypeName("_Bool")] byte b_fast);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_jump_time(libvlc_media_player_t* p_mi, [NativeTypeName("libvlc_time_t")] long i_time);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern double libvlc_media_player_get_position(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_set_position(libvlc_media_player_t* p_mi, double f_pos, [NativeTypeName("_Bool")] byte b_fast);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_set_abloop_time(libvlc_media_player_t* p_mi, [NativeTypeName("libvlc_time_t")] long a_time, [NativeTypeName("libvlc_time_t")] long b_time);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_set_abloop_position(libvlc_media_player_t* p_mi, double a_pos, double b_pos);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_reset_abloop(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_abloop_t libvlc_media_player_get_abloop(libvlc_media_player_t* p_mi, [NativeTypeName("libvlc_time_t *")] long* a_time, double* a_pos, [NativeTypeName("libvlc_time_t *")] long* b_time, double* b_pos);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_set_chapter(libvlc_media_player_t* p_mi, int i_chapter);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_get_chapter(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_get_chapter_count(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_get_chapter_count_for_title(libvlc_media_player_t* p_mi, int i_title);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_set_title(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint i_title);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_get_title(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_get_title_count(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_previous_chapter(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_next_chapter(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float libvlc_media_player_get_rate(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_set_rate(libvlc_media_player_t* p_mi, float rate);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_state_t libvlc_media_player_get_state(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint libvlc_media_player_has_vout(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte libvlc_media_player_is_seekable(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte libvlc_media_player_can_pause(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte libvlc_media_player_program_scrambled(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_next_frame(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_navigate(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint navigate);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_set_video_title_display(libvlc_media_player_t* p_mi, libvlc_position_t position, [NativeTypeName("unsigned int")] uint timeout);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_tracklist_t* libvlc_media_player_get_tracklist(libvlc_media_player_t* p_mi, libvlc_track_type_t type, [NativeTypeName("_Bool")] byte selected);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_track_t* libvlc_media_player_get_selected_track(libvlc_media_player_t* p_mi, libvlc_track_type_t type);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_track_t* libvlc_media_player_get_track_from_id(libvlc_media_player_t* p_mi, [NativeTypeName("const char *")] byte* psz_id);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_select_track(libvlc_media_player_t* p_mi, [NativeTypeName("const libvlc_media_track_t *")] libvlc_media_track_t* track);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_unselect_track_type(libvlc_media_player_t* p_mi, libvlc_track_type_t type);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_select_tracks(libvlc_media_player_t* p_mi, libvlc_track_type_t type, [NativeTypeName("const libvlc_media_track_t **")] libvlc_media_track_t** tracks, [NativeTypeName("size_t")] UIntPtr track_count);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_select_tracks_by_ids(libvlc_media_player_t* p_mi, libvlc_track_type_t type, [NativeTypeName("const char *")] byte* psz_ids);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_add_slave(libvlc_media_player_t* p_mi, libvlc_media_slave_type_t i_type, [NativeTypeName("const char *")] byte* psz_uri, [NativeTypeName("_Bool")] byte b_select);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_player_program_delete(libvlc_player_program_t* program);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern UIntPtr libvlc_player_programlist_count([NativeTypeName("const libvlc_player_programlist_t *")] libvlc_player_programlist_t* list);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_player_program_t* libvlc_player_programlist_at(libvlc_player_programlist_t* list, [NativeTypeName("size_t")] UIntPtr index);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_player_programlist_delete(libvlc_player_programlist_t* list);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_select_program_id(libvlc_media_player_t* p_mi, int i_group_id);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_player_program_t* libvlc_media_player_get_selected_program(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_player_program_t* libvlc_media_player_get_program_from_id(libvlc_media_player_t* p_mi, int i_group_id);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_player_programlist_t* libvlc_media_player_get_programlist(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_toggle_fullscreen(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_set_fullscreen(libvlc_media_player_t* p_mi, [NativeTypeName("_Bool")] byte b_fullscreen);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte libvlc_get_fullscreen(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_key_input(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint on);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_mouse_input(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint on);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_video_get_size(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint num, [NativeTypeName("unsigned int *")] uint* px, [NativeTypeName("unsigned int *")] uint* py);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_video_get_cursor(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint num, int* px, int* py);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float libvlc_video_get_scale(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_scale(libvlc_media_player_t* p_mi, float f_factor);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* libvlc_video_get_aspect_ratio(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_aspect_ratio(libvlc_media_player_t* p_mi, [NativeTypeName("const char *")] byte* psz_aspect);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_video_fit_mode_t libvlc_video_get_display_fit(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_display_fit(libvlc_media_player_t* p_mi, libvlc_video_fit_mode_t fit);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_video_viewpoint_t* libvlc_video_new_viewpoint();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_video_update_viewpoint(libvlc_media_player_t* p_mi, [NativeTypeName("const libvlc_video_viewpoint_t *")] libvlc_video_viewpoint_t* p_viewpoint, [NativeTypeName("_Bool")] byte b_absolute);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_video_stereo_mode_t libvlc_video_get_video_stereo_mode(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_video_stereo_mode(libvlc_media_player_t* p_mi, [NativeTypeName("const libvlc_video_stereo_mode_t")] libvlc_video_stereo_mode_t i_mode);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("libvlc_time_t")]
        public static extern long libvlc_video_get_spu_delay(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float libvlc_video_get_spu_text_scale(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_spu_text_scale(libvlc_media_player_t* p_mi, float f_scale);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_video_set_spu_delay(libvlc_media_player_t* p_mi, [NativeTypeName("libvlc_time_t")] long i_delay);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_get_full_title_descriptions(libvlc_media_player_t* p_mi, libvlc_title_description_t*** titles);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_title_descriptions_release(libvlc_title_description_t** p_titles, [NativeTypeName("unsigned int")] uint i_count);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_get_full_chapter_descriptions(libvlc_media_player_t* p_mi, int i_chapters_of_title, libvlc_chapter_description_t*** pp_chapters);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_chapter_descriptions_release(libvlc_chapter_description_t** p_chapters, [NativeTypeName("unsigned int")] uint i_count);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_crop_ratio(libvlc_media_player_t* mp, [NativeTypeName("unsigned int")] uint num, [NativeTypeName("unsigned int")] uint den);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_crop_window(libvlc_media_player_t* mp, [NativeTypeName("unsigned int")] uint x, [NativeTypeName("unsigned int")] uint y, [NativeTypeName("unsigned int")] uint width, [NativeTypeName("unsigned int")] uint height);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_crop_border(libvlc_media_player_t* mp, [NativeTypeName("unsigned int")] uint left, [NativeTypeName("unsigned int")] uint right, [NativeTypeName("unsigned int")] uint top, [NativeTypeName("unsigned int")] uint bottom);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_video_get_teletext(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_teletext(libvlc_media_player_t* p_mi, int i_page);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_teletext_transparency(libvlc_media_player_t* p_mi, [NativeTypeName("_Bool")] byte transparent);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte libvlc_video_get_teletext_transparency(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_video_take_snapshot(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint num, [NativeTypeName("const char *")] byte* psz_filepath, [NativeTypeName("unsigned int")] uint i_width, [NativeTypeName("unsigned int")] uint i_height);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_video_get_deinterlace(libvlc_media_player_t* mp, [NativeTypeName("char **")] byte** modep);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_video_set_deinterlace(libvlc_media_player_t* p_mi, int deinterlace, [NativeTypeName("const char *")] byte* psz_mode);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_video_get_marquee_int(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint option);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_marquee_int(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint option, int i_val);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_marquee_string(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint option, [NativeTypeName("const char *")] byte* psz_text);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_video_get_logo_int(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint option);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_logo_int(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint option, int value);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_logo_string(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint option, [NativeTypeName("const char *")] byte* psz_value);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_video_get_adjust_int(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint option);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_adjust_int(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint option, int value);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float libvlc_video_get_adjust_float(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint option);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_adjust_float(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint option, float value);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_set_projection_mode(libvlc_media_player_t* player, libvlc_video_projection_t projection_mode);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_video_unset_projection_mode(libvlc_media_player_t* player);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_audio_output_t* libvlc_audio_output_list_get(libvlc_instance_t* p_instance);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_audio_output_list_release(libvlc_audio_output_t* p_list);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_audio_output_set(libvlc_media_player_t* p_mi, [NativeTypeName("const char *")] byte* psz_name);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_audio_output_device_t* libvlc_audio_output_device_enum(libvlc_media_player_t* mp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_audio_output_device_list_release(libvlc_audio_output_device_t* p_list);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_audio_output_device_set(libvlc_media_player_t* mp, [NativeTypeName("const char *")] byte* device_id);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* libvlc_audio_output_device_get(libvlc_media_player_t* mp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_audio_toggle_mute(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_audio_get_mute(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_audio_set_mute(libvlc_media_player_t* p_mi, int status);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_audio_get_volume(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_audio_set_volume(libvlc_media_player_t* p_mi, int i_volume);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_audio_output_stereomode_t libvlc_audio_get_stereomode(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_audio_set_stereomode(libvlc_media_player_t* p_mi, libvlc_audio_output_stereomode_t mode);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_audio_output_mixmode_t libvlc_audio_get_mixmode(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_audio_set_mixmode(libvlc_media_player_t* p_mi, libvlc_audio_output_mixmode_t mode);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("libvlc_time_t")]
        public static extern long libvlc_audio_get_delay(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_audio_set_delay(libvlc_media_player_t* p_mi, [NativeTypeName("libvlc_time_t")] long i_delay);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint libvlc_audio_equalizer_get_preset_count();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* libvlc_audio_equalizer_get_preset_name([NativeTypeName("unsigned int")] uint u_index);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("unsigned int")]
        public static extern uint libvlc_audio_equalizer_get_band_count();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float libvlc_audio_equalizer_get_band_frequency([NativeTypeName("unsigned int")] uint u_index);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_equalizer_t* libvlc_audio_equalizer_new();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_equalizer_t* libvlc_audio_equalizer_new_from_preset([NativeTypeName("unsigned int")] uint u_index);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_audio_equalizer_release(libvlc_equalizer_t* p_equalizer);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_audio_equalizer_set_preamp(libvlc_equalizer_t* p_equalizer, float f_preamp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float libvlc_audio_equalizer_get_preamp(libvlc_equalizer_t* p_equalizer);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_audio_equalizer_set_amp_at_index(libvlc_equalizer_t* p_equalizer, float f_amp, [NativeTypeName("unsigned int")] uint u_band);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float libvlc_audio_equalizer_get_amp_at_index(libvlc_equalizer_t* p_equalizer, [NativeTypeName("unsigned int")] uint u_band);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_set_equalizer(libvlc_media_player_t* p_mi, libvlc_equalizer_t* p_equalizer);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_get_role(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_set_role(libvlc_media_player_t* p_mi, [NativeTypeName("unsigned int")] uint role);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_record(libvlc_media_player_t* p_mi, [NativeTypeName("_Bool")] byte enable, [NativeTypeName("const char *")] byte* dir_path);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_watch_time(libvlc_media_player_t* p_mi, [NativeTypeName("libvlc_time_t")] long min_period_us, [NativeTypeName("const struct libvlc_media_player_watch_time_cbs *")] libvlc_media_player_watch_time_cbs* cbs, [NativeTypeName("void*")] IntPtr cbs_opaque);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_unwatch_time(libvlc_media_player_t* p_mi);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_player_time_point_interpolate([NativeTypeName("const libvlc_media_player_time_point_t *")] libvlc_media_player_time_point_t* point, [NativeTypeName("libvlc_time_t")] long system_now_us, [NativeTypeName("libvlc_time_t *")] long* out_ts_us, double* out_pos);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("libvlc_time_t")]
        public static extern long libvlc_media_player_time_point_get_next_date([NativeTypeName("const libvlc_media_player_time_point_t *")] libvlc_media_player_time_point_t* point, [NativeTypeName("libvlc_time_t")] long system_now_us, [NativeTypeName("libvlc_time_t")] long interpolated_ts_us, [NativeTypeName("libvlc_time_t")] long next_interval_us);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_lock(libvlc_media_player_t* mp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_unlock(libvlc_media_player_t* mp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_wait(libvlc_media_player_t* mp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_player_signal(libvlc_media_player_t* mp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_list_t* libvlc_media_list_new();

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_list_release(libvlc_media_list_t* p_ml);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_list_t* libvlc_media_list_retain(libvlc_media_list_t* p_ml);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_list_set_media(libvlc_media_list_t* p_ml, libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_t* libvlc_media_list_media(libvlc_media_list_t* p_ml);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_list_add_media(libvlc_media_list_t* p_ml, libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_list_insert_media(libvlc_media_list_t* p_ml, libvlc_media_t* p_md, int i_pos);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_list_remove_index(libvlc_media_list_t* p_ml, int i_pos);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_list_count(libvlc_media_list_t* p_ml);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_t* libvlc_media_list_item_at_index(libvlc_media_list_t* p_ml, int i_pos);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_list_index_of_item(libvlc_media_list_t* p_ml, libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte libvlc_media_list_is_readonly(libvlc_media_list_t* p_ml);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_list_lock(libvlc_media_list_t* p_ml);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_list_unlock(libvlc_media_list_t* p_ml);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_list_player_t* libvlc_media_list_player_new(libvlc_instance_t* p_instance, [NativeTypeName("const struct libvlc_media_player_cbs *")] libvlc_media_player_cbs* cbs, [NativeTypeName("void*")] IntPtr cbs_opaque);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_list_player_release(libvlc_media_list_player_t* p_mlp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_list_player_t* libvlc_media_list_player_retain(libvlc_media_list_player_t* p_mlp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_player_t* libvlc_media_list_player_get_media_player(libvlc_media_list_player_t* p_mlp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_list_player_set_media_list(libvlc_media_list_player_t* p_mlp, libvlc_media_list_t* p_mlist);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_list_player_play(libvlc_media_list_player_t* p_mlp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_list_player_pause(libvlc_media_list_player_t* p_mlp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_list_player_set_pause(libvlc_media_list_player_t* p_mlp, int do_pause);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte libvlc_media_list_player_is_playing(libvlc_media_list_player_t* p_mlp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_state_t libvlc_media_list_player_get_state(libvlc_media_list_player_t* p_mlp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_list_player_play_item_at_index(libvlc_media_list_player_t* p_mlp, int i_index);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_list_player_play_item(libvlc_media_list_player_t* p_mlp, libvlc_media_t* p_md);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_list_player_stop_async(libvlc_media_list_player_t* p_mlp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_list_player_next(libvlc_media_list_player_t* p_mlp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_list_player_previous(libvlc_media_list_player_t* p_mlp);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_list_player_set_playback_mode(libvlc_media_list_player_t* p_mlp, libvlc_playback_mode_t e_mode);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern libvlc_media_discoverer_t* libvlc_media_discoverer_new(libvlc_instance_t* p_inst, [NativeTypeName("const char *")] byte* psz_name, [NativeTypeName("const struct libvlc_media_discoverer_cbs *")] libvlc_media_discoverer_cbs* cbs, [NativeTypeName("void*")] IntPtr cbs_opaque);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_media_discoverer_start(libvlc_media_discoverer_t* p_mdis);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_discoverer_stop(libvlc_media_discoverer_t* p_mdis);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_discoverer_destroy(libvlc_media_discoverer_t* p_mdis);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("_Bool")]
        public static extern byte libvlc_media_discoverer_is_running(libvlc_media_discoverer_t* p_mdis);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern UIntPtr libvlc_media_discoverer_list_get(libvlc_instance_t* p_inst, libvlc_media_discoverer_category_t i_cat, libvlc_media_discoverer_description_t*** ppp_services);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_media_discoverer_list_release(libvlc_media_discoverer_description_t** pp_services, [NativeTypeName("size_t")] UIntPtr i_count);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_dialog_set_callbacks(libvlc_instance_t* p_instance, [NativeTypeName("const libvlc_dialog_cbs *")] libvlc_dialog_cbs* p_cbs, [NativeTypeName("void*")] IntPtr p_data);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_dialog_set_error_callback(libvlc_instance_t* p_instance, [NativeTypeName("libvlc_dialog_error_cbs")] IntPtr p_cbs, [NativeTypeName("void*")] IntPtr p_data);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void libvlc_dialog_set_context(libvlc_dialog_id* p_id, [NativeTypeName("void*")] IntPtr p_context);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("void*")]
        public static extern IntPtr libvlc_dialog_get_context(libvlc_dialog_id* p_id);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_dialog_post_login(libvlc_dialog_id* p_id, [NativeTypeName("const char *")] byte* psz_username, [NativeTypeName("const char *")] byte* psz_password, [NativeTypeName("_Bool")] byte b_store);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_dialog_post_action(libvlc_dialog_id* p_id, int i_action);

        [DllImport("libvlc", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int libvlc_dialog_dismiss(libvlc_dialog_id* p_id);

        [NativeTypeName("#define VLC_LIBVLC_H 1")]
        public const int VLC_LIBVLC_H = 1;

        [NativeTypeName("#define VLC_LIBVLC_RENDERER_DISCOVERER_H 1")]
        public const int VLC_LIBVLC_RENDERER_DISCOVERER_H = 1;

        [NativeTypeName("#define LIBVLC_RENDERER_CAN_AUDIO 0x0001")]
        public const int LIBVLC_RENDERER_CAN_AUDIO = 0x0001;

        [NativeTypeName("#define LIBVLC_RENDERER_CAN_VIDEO 0x0002")]
        public const int LIBVLC_RENDERER_CAN_VIDEO = 0x0002;

        [NativeTypeName("#define VLC_LIBVLC_PICTURE_H 1")]
        public const int VLC_LIBVLC_PICTURE_H = 1;

        [NativeTypeName("#define VLC_LIBVLC_MEDIA_H 1")]
        public const int VLC_LIBVLC_MEDIA_H = 1;

        [NativeTypeName("#define VLC_LIBVLC_MEDIA_TRACK_H 1")]
        public const int VLC_LIBVLC_MEDIA_TRACK_H = 1;

        [NativeTypeName("#define VLC_LIBVLC_VIDEO_H 1")]
        public const int VLC_LIBVLC_VIDEO_H = 1;

        [NativeTypeName("#define libvlc_media_filestat_mtime 0")]
        public const int libvlc_media_filestat_mtime = 0;

        [NativeTypeName("#define libvlc_media_filestat_size 1")]
        public const int libvlc_media_filestat_size = 1;

        [NativeTypeName("#define VLC_LIBVLC_MEDIA_PLAYER_H 1")]
        public const int VLC_LIBVLC_MEDIA_PLAYER_H = 1;

        [NativeTypeName("#define LIBVLC_MEDIA_LIST_H 1")]
        public const int LIBVLC_MEDIA_LIST_H = 1;

        [NativeTypeName("#define LIBVLC_MEDIA_LIST_PLAYER_H 1")]
        public const int LIBVLC_MEDIA_LIST_PLAYER_H = 1;

        [NativeTypeName("#define VLC_LIBVLC_MEDIA_DISCOVERER_H 1")]
        public const int VLC_LIBVLC_MEDIA_DISCOVERER_H = 1;

        [NativeTypeName("#define LIBVLC_DIALOG_H 1")]
        public const int LIBVLC_DIALOG_H = 1;

        [NativeTypeName("#define LIBVLC_VERSION_H 1")]
        public const int LIBVLC_VERSION_H = 1;

        [NativeTypeName("#define LIBVLC_VERSION_MAJOR (4)")]
        public const int LIBVLC_VERSION_MAJOR = (4);

        [NativeTypeName("#define LIBVLC_VERSION_MINOR (0)")]
        public const int LIBVLC_VERSION_MINOR = (0);

        [NativeTypeName("#define LIBVLC_VERSION_REVISION (0)")]
        public const int LIBVLC_VERSION_REVISION = (0);

        [NativeTypeName("#define LIBVLC_VERSION_EXTRA (0)")]
        public const int LIBVLC_VERSION_EXTRA = (0);

        [NativeTypeName("#define LIBVLC_VERSION_INT LIBVLC_VERSION(LIBVLC_VERSION_MAJOR, LIBVLC_VERSION_MINOR, \\\r\n                        LIBVLC_VERSION_REVISION, LIBVLC_VERSION_EXTRA)")]
        public const int LIBVLC_VERSION_INT = (((4) << 24) | ((0) << 16) | ((0) << 8) | ((0)));

        [NativeTypeName("#define LIBVLC_ABI_VERSION_MAJOR (12)")]
        public const int LIBVLC_ABI_VERSION_MAJOR = (12);

        [NativeTypeName("#define LIBVLC_ABI_VERSION_MINOR (0)")]
        public const int LIBVLC_ABI_VERSION_MINOR = (0);

        [NativeTypeName("#define LIBVLC_ABI_VERSION_MICRO (0)")]
        public const int LIBVLC_ABI_VERSION_MICRO = (0);

        [NativeTypeName("#define LIBVLC_ABI_VERSION_INT LIBVLC_VERSION(LIBVLC_VERSION_MAJOR, LIBVLC_ABI_VERSION_MAJOR, \\\r\n                        LIBVLC_ABI_VERSION_MINOR, LIBVLC_ABI_VERSION_MICRO )")]
        public const int LIBVLC_ABI_VERSION_INT = (((4) << 24) | ((12) << 16) | ((0) << 8) | ((0)));
    }
}
