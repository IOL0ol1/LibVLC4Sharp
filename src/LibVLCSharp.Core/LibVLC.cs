using System;
using System.Collections.Generic;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

namespace LibVLCSharp.Core
{
    /// <summary>
    /// A libvlc instance (<c>libvlc_instance_t</c>) — the root object and factory for media,
    /// players and lists. Create one per application.
    /// </summary>
    public unsafe partial class LibVLC : NativeReference
    {
        /// <summary>
        /// Creates and initializes a libvlc instance. <c>libvlc_new</c>.
        /// Arguments are passed as "command line" arguments similar to <c>main()</c> and affect the
        /// LibVLC instance default configuration.
        /// </summary>
        /// <param name="args">
        /// Optional libvlc command-line arguments (e.g. <c>"--no-video-title-show"</c>). Pass an empty
        /// array (the default) when no arguments are needed.
        /// </param>
        /// <remarks>
        /// LibVLC may create threads, so any thread-unsafe process initialization (locale, environment
        /// variables, X11 thread setup, etc.) must be done before calling this constructor.
        /// <b>Warning:</b> there is no guarantee of forward, backward or cross-platform compatibility
        /// for the accepted arguments; avoid them except for debugging.
        /// </remarks>
        public LibVLC(params string[] args) : base(New(args)) { }

        private static IntPtr New(string[] args)
        {
            if (args is null || args.Length == 0)
                return (IntPtr)libvlc_new(0, null);

            var ptrs = new IntPtr[args.Length];
            try
            {
                for (int i = 0; i < args.Length; i++)
                    ptrs[i] = (IntPtr)Utf8Buffer.StringToHGlobalUtf8(args[i]);
                fixed (IntPtr* pp = ptrs)
                    return (IntPtr)libvlc_new(args.Length, (byte**)pp);
            }
            finally
            {
                for (int i = 0; i < ptrs.Length; i++)
                    Utf8Buffer.FreeHGlobal(ptrs[i]);
            }
        }

        /// <summary>Implicit conversion to the native <c>libvlc_instance_t*</c> (null for a null instance).</summary>
        public static implicit operator libvlc_instance_t*(LibVLC? libVLC) =>
            libVLC is null ? null : (libvlc_instance_t*)libVLC.NativeHandle;

        protected override void Dispose(bool disposing)
        {
            // Clear the dialog callbacks (if any) before libvlc_release. Dialog uses the instance
            // pointer it captured at construction, so it works regardless of this wrapper's state.
            if (disposing) _dialog?.Release();
            base.Dispose(disposing);
        }

        protected override void Release(IntPtr handle) =>
            libvlc_release((libvlc_instance_t*)handle);

        private Dialog? _dialog;

        /// <summary>
        /// The interaction-dialog handler for this instance (login/question/progress prompts), created
        /// on first access. libvlc allows a single set of dialog callbacks per instance, so this is the
        /// one shared <see cref="Core.Dialog"/>; subscribe to its events. Disposed with this instance.
        /// </summary>
        public Dialog Dialog => _dialog ??= new Dialog(this);

        /// <summary>
        /// Retrieves the libvlc version string. <c>libvlc_get_version</c>.
        /// Example: <c>"1.1.0-git The Luggage"</c>.
        /// </summary>
        public static string Version => ((IntPtr)libvlc_get_version()).GetUtf8()!;

        /// <summary>
        /// Retrieves the libvlc source changeset. <c>libvlc_get_changeset</c>.
        /// Example: <c>"aa9bce0bc4"</c>.
        /// </summary>
        public static string Changeset => ((IntPtr)libvlc_get_changeset()).GetUtf8()!;

        /// <summary>
        /// Retrieves the compiler version used to build libvlc. <c>libvlc_get_compiler</c>.
        /// Example: <c>"gcc version 4.2.3 (Ubuntu 4.2.3-2ubuntu6)"</c>.
        /// </summary>
        public static string Compiler => ((IntPtr)libvlc_get_compiler()).GetUtf8()!;

        /// <summary>
        /// Returns the ABI version of the libvlc library. <c>libvlc_abi_version</c>.
        /// This is distinct from the VLC package version. The returned value uses the mask
        /// <c>0xFF000000</c> (major VLC), <c>0x00FF0000</c> (major ABI), <c>0x0000FF00</c> (minor ABI),
        /// <c>0x000000FF</c> (micro ABI).
        /// </summary>
        public static int AbiVersion => libvlc_abi_version();

        /// <summary>
        /// Returns the current time as defined by LibVLC, in microseconds. <c>libvlc_clock</c>.
        /// Time increases monotonically (regardless of time-zone changes or RTC adjustments). The origin
        /// is arbitrary but consistent across the whole system (e.g. time since boot).
        /// </summary>
        public static long Clock => libvlc_clock();

        /// <summary>
        /// Returns a human-readable error message for the last LibVLC error on the calling thread, or
        /// null if there was no error. <c>libvlc_errmsg</c>.
        /// The returned string is valid until another error occurs (at least until the next LibVLC call).
        /// </summary>
        public static string? ErrorMsg => ((IntPtr)libvlc_errmsg()).GetUtf8();

        /// <summary>
        /// Clears the LibVLC error status for the current thread. <c>libvlc_clearerr</c>.
        /// This is optional — by default the error status is automatically overridden when a new error
        /// occurs and destroyed when the thread exits.
        /// </summary>
        public static void ClearError() => libvlc_clearerr();

        // --- factories ---

        /// <summary>
        /// Creates an empty <see cref="MediaPlayer"/> bound to this instance. <c>libvlc_media_player_new</c>.
        /// </summary>
        /// <returns>A new media player, or throws on error.</returns>
        public MediaPlayer CreateMediaPlayer() => new MediaPlayer((IntPtr)libvlc_media_player_new(this));

        /// <summary>
        /// Creates a <see cref="MediaPlayer"/> from a <see cref="Media"/> object. <c>libvlc_media_player_new_from_media</c>.
        /// </summary>
        /// <param name="media">The media to load. The media may be safely released after this call.</param>
        /// <returns>A new media player, or throws on error.</returns>
        public MediaPlayer CreateMediaPlayer(Media media) => new MediaPlayer((IntPtr)libvlc_media_player_new_from_media(this, media));

        /// <summary>
        /// Creates a new <see cref="MediaListPlayer"/>. <c>libvlc_media_list_player_new</c>.
        /// </summary>
        /// <returns>A new media list player instance, or throws on error.</returns>
        public MediaListPlayer CreateMediaListPlayer() => new MediaListPlayer((IntPtr)libvlc_media_list_player_new(this));

        /// <summary>
        /// Creates a <see cref="MediaDiscoverer"/> object by service name. <c>libvlc_media_discoverer_new</c>.
        /// After creation, attach to media list events to be notified of newly discovered items, then
        /// call <see cref="MediaDiscoverer.Start"/> to begin discovery.
        /// </summary>
        /// <param name="name">
        /// Service name; use <see cref="MediaDiscoverers"/> to get the discoverer names available in this
        /// instance.
        /// </param>
        /// <returns>A new media discoverer, or throws on error.</returns>
        /// <remarks>Since LibVLC 3.0.0.</remarks>
        public MediaDiscoverer CreateMediaDiscoverer(string name)
        {
            using var u = new Utf8Buffer(name);
            return new MediaDiscoverer((IntPtr)libvlc_media_discoverer_new(this, u));
        }
        /// <summary>
        /// Creates a <see cref="RendererDiscoverer"/> object by service name. <c>libvlc_renderer_discoverer_new</c>.
        /// After creation, attach to events to be notified of discovered renderers, then call
        /// <see cref="RendererDiscoverer.Start"/> to begin discovery.
        /// </summary>
        /// <param name="name">
        /// Service name; use <see cref="RendererDiscoverers"/> to get the discoverer names available in
        /// this instance.
        /// </param>
        /// <returns>A new renderer discoverer, or throws on error.</returns>
        /// <remarks>Since LibVLC 3.0.0.</remarks>
        public RendererDiscoverer CreateRendererDiscoverer(string name)
        {
            using var u = new Utf8Buffer(name);
            return new RendererDiscoverer((IntPtr)libvlc_renderer_discoverer_new(this, u));
        }

        // --- instance configuration ---

        /// <summary>
        /// Sets meta-information about the application. <c>libvlc_set_app_id</c>.
        /// See also <see cref="SetUserAgent"/>.
        /// </summary>
        /// <param name="id">Java-style application identifier, e.g. <c>"com.acme.foobar"</c>.</param>
        /// <param name="version">Application version numbers, e.g. <c>"1.2.3"</c>.</param>
        /// <param name="icon">Application icon name, e.g. <c>"foobar"</c>.</param>
        /// <remarks>Since LibVLC 2.1.0.</remarks>
        public void SetAppId(string id, string version, string icon)
        {
            using var uId = new Utf8Buffer(id);
            using var uVersion = new Utf8Buffer(version);
            using var uIcon = new Utf8Buffer(icon);
            libvlc_set_app_id(this, uId, uVersion, uIcon);
        }

        /// <summary>
        /// Sets the application name. <c>libvlc_set_user_agent</c>.
        /// LibVLC passes this as the user-agent string when a protocol requires it.
        /// </summary>
        /// <param name="name">Human-readable application name, e.g. <c>"FooBar player 1.2.3"</c>.</param>
        /// <param name="http">HTTP User-Agent string, e.g. <c>"FooBar/1.2.3 Python/2.6.0"</c>.</param>
        /// <remarks>Since LibVLC 1.1.1.</remarks>
        public void SetUserAgent(string name, string http)
        {
            using var uName = new Utf8Buffer(name);
            using var uHttp = new Utf8Buffer(http);
            libvlc_set_user_agent(this, uName, uHttp);
        }

        /// <summary>
        /// Saves previously set metadata of <paramref name="media"/> to disk. <c>libvlc_media_save_meta</c>.
        /// </summary>
        /// <param name="media">The media descriptor whose metadata should be written.</param>
        /// <returns><c>true</c> if the write operation was successful.</returns>
        public bool SaveMeta(Media media) => libvlc_media_save_meta(this, media) != 0;

        private libvlc_log_cb? _logCb;            // rooted while a log handler is installed
        private Action<LogMessage>? _logHandler;

        /// <summary>
        /// Sets (or clears) the logging callback for this instance. <c>libvlc_log_set</c> /
        /// <c>libvlc_log_unset</c>. Pass <c>null</c> to deregister and stop logging.
        /// </summary>
        /// <param name="handler">
        /// Callback invoked for each log message, or <c>null</c> to unset. The callback runs on libvlc
        /// threads and must be thread-safe.
        /// </param>
        /// <remarks>
        /// This method is thread-safe: it waits for any pending callback invocation to complete.
        /// Some debug messages emitted during LibVLC initialization cannot be captured.
        /// <b>Warning:</b> a deadlock may occur if this method is called from within the callback.
        /// When unsetting (<c>libvlc_log_unset</c>), the call waits for pending invocations — calling
        /// from within the callback causes a deadlock. Since LibVLC 2.1.0.
        /// </remarks>
        public void SetLog(Action<LogMessage> handler)
        {
            if (handler is null)
            {
                _logHandler = null;
                libvlc_log_unset(this);
                _logCb = null;
                return;
            }
            _logHandler = handler;
            _logCb = OnLog;
            libvlc_log_set(this, _logCb.ToFunctionPointer(), IntPtr.Zero);
        }

        /// <summary>
        /// Sets up logging to a file. <c>libvlc_log_set_file</c>.
        /// </summary>
        /// <param name="fd">
        /// A <c>FILE*</c> opened for writing. The pointer must remain valid until logging is unset via
        /// <see cref="SetLog"/> with <c>null</c>.
        /// </param>
        /// <remarks>Since LibVLC 2.1.0.</remarks>
        public void SetLogFile(IntPtr fd) => libvlc_log_set_file(this, fd);

        private void OnLog(IntPtr data, int level, libvlc_log_t* ctx, byte* fmt, byte* args)
        {
            var handler = _logHandler;
            if (handler is null) return;

            byte* module = null, file = null;
            uint line = 0;
            libvlc_log_get_context(ctx, &module, &file, &line);

            byte* name = null, header = null;
            UIntPtr id = default;
            libvlc_log_get_object(ctx, &name, &header, &id);

            handler(new LogMessage((LogLevel)level,
                ((IntPtr)module).GetUtf8(), ((IntPtr)file).GetUtf8(), line,
                ((IntPtr)name).GetUtf8(), ((IntPtr)header).GetUtf8(), id,
                ((IntPtr)libvlc_printerr(fmt, args)).GetUtf8()));
        }

        // --- enumerations ---

        /// <summary>
        /// Lazily enumerates the available audio output modules. <c>libvlc_audio_output_list_get</c>.
        /// Count is not known up front (a native linked list), hence <see cref="IEnumerable{T}"/>. The
        /// list is fetched when enumeration begins and released when it completes or the enumerator is
        /// disposed — enumerate fully (e.g. <c>foreach</c> / <c>ToList()</c>).
        /// </summary>
        /// <returns>
        /// A sequence of <see cref="AudioOutput"/> values, or an empty sequence on error.
        /// </returns>
        public IEnumerable<AudioOutput> AudioOutputs()
        {
            IntPtr head;
            unsafe { head = (IntPtr)libvlc_audio_output_list_get(this); }
            if (head == IntPtr.Zero) yield break;
            try
            {
                for (var cur = head; cur != IntPtr.Zero;)
                {
                    AudioOutput item;
                    IntPtr next;
                    unsafe
                    {
                        var o = (libvlc_audio_output_t*)cur;
                        item = new AudioOutput(((IntPtr)o->psz_name).GetUtf8(), ((IntPtr)o->psz_description).GetUtf8());
                        next = (IntPtr)o->p_next;
                    }
                    yield return item;
                    cur = next;
                }
            }
            finally { unsafe { libvlc_audio_output_list_release((libvlc_audio_output_t*)head); } }
        }

        /// <summary>
        /// Lazily enumerates the available audio filter modules. <c>libvlc_audio_filter_list_get</c>.
        /// </summary>
        /// <returns>
        /// A sequence of <see cref="ModuleDescription"/> values, or an empty sequence on error.
        /// </returns>
        public IEnumerable<ModuleDescription> AudioFilters()
        {
            IntPtr head;
            unsafe { head = (IntPtr)libvlc_audio_filter_list_get(this); }
            foreach (var m in WalkModules(head)) yield return m;
        }

        /// <summary>
        /// Lazily enumerates the available video filter modules. <c>libvlc_video_filter_list_get</c>.
        /// </summary>
        /// <returns>
        /// A sequence of <see cref="ModuleDescription"/> values, or an empty sequence on error.
        /// </returns>
        public IEnumerable<ModuleDescription> VideoFilters()
        {
            IntPtr head;
            unsafe { head = (IntPtr)libvlc_video_filter_list_get(this); }
            foreach (var m in WalkModules(head)) yield return m;
        }

        // Walks a libvlc_module_description_t* linked list, releasing it when enumeration ends.
        private static IEnumerable<ModuleDescription> WalkModules(IntPtr head)
        {
            if (head == IntPtr.Zero) yield break;
            try
            {
                for (var cur = head; cur != IntPtr.Zero;)
                {
                    ModuleDescription item;
                    IntPtr next;
                    unsafe
                    {
                        var m = (libvlc_module_description_t*)cur;
                        item = new ModuleDescription(
                            ((IntPtr)m->psz_name).GetUtf8(), ((IntPtr)m->psz_shortname).GetUtf8(),
                            ((IntPtr)m->psz_longname).GetUtf8(), ((IntPtr)m->psz_help).GetUtf8());
                        next = (IntPtr)m->p_next;
                    }
                    yield return item;
                    cur = next;
                }
            }
            finally { unsafe { libvlc_module_description_list_release((libvlc_module_description_t*)head); } }
        }

        /// <summary>
        /// Returns the media discoverer services for a given category. <c>libvlc_media_discoverer_list_get</c>.
        /// </summary>
        /// <param name="category">Category of services to fetch.</param>
        /// <returns>
        /// A read-only list of <see cref="MediaDiscovererDescription"/> values (empty on error). Pass a
        /// <see cref="MediaDiscovererDescription.Name"/> to <see cref="CreateMediaDiscoverer"/>.
        /// </returns>
        /// <remarks>Since LibVLC 3.0.0.</remarks>
        public IReadOnlyList<MediaDiscovererDescription> MediaDiscoverers(MediaDiscovererCategory category)
        {
            libvlc_media_discoverer_description_t** services;
            UIntPtr n = libvlc_media_discoverer_list_get(this, (libvlc_media_discoverer_category_t)category, &services);
            int count = (int)n.ToUInt32();
            if (count == 0) return Array.Empty<MediaDiscovererDescription>();
            try
            {
                var result = new MediaDiscovererDescription[count];
                for (int i = 0; i < count; i++)
                {
                    var d = services[i];
                    result[i] = new MediaDiscovererDescription(
                        ((IntPtr)d->psz_name).GetUtf8(), ((IntPtr)d->psz_longname).GetUtf8(),
                        (MediaDiscovererCategory)d->i_cat);
                }
                return result;
            }
            finally { libvlc_media_discoverer_list_release(services, n); }
        }

        /// <summary>
        /// Returns all available renderer discoverer services. <c>libvlc_renderer_discoverer_list_get</c>.
        /// </summary>
        /// <returns>
        /// A read-only list of <see cref="RendererDiscovererDescription"/> values (empty on error). Pass
        /// a <see cref="RendererDiscovererDescription.Name"/> to <see cref="CreateRendererDiscoverer"/>.
        /// </returns>
        /// <remarks>Since LibVLC 3.0.0.</remarks>
        public IReadOnlyList<RendererDiscovererDescription> RendererDiscoverers()
        {
            libvlc_rd_description_t** services;
            UIntPtr n = libvlc_renderer_discoverer_list_get(this, &services);
            int count = (int)n.ToUInt32();
            if (count == 0) return Array.Empty<RendererDiscovererDescription>();
            try
            {
                var result = new RendererDiscovererDescription[count];
                for (int i = 0; i < count; i++)
                {
                    var d = services[i];
                    result[i] = new RendererDiscovererDescription(
                        ((IntPtr)d->psz_name).GetUtf8(), ((IntPtr)d->psz_longname).GetUtf8());
                }
                return result;
            }
            finally { libvlc_renderer_discoverer_list_release(services, n); }
        }
    }
    /// <summary>
    /// A structured libvlc log record delivered to <see cref="LibVLC.SetLog"/>. 
    /// </summary>
    public readonly struct LogMessage
    {
        internal LogMessage(LogLevel level, string? module, string? file, uint line,
            string? objectName, string? header, UIntPtr objectId, string? message)
        {
            Level = level; Module = module; File = file; Line = line;
            ObjectName = objectName; Header = header; ObjectId = objectId; Message = message;
        }

        /// <summary>Severity.</summary>
        public readonly LogLevel Level;
        /// <summary>
        /// VLC module emitting the message, or null if unknown. <c>libvlc_log_get_context</c>.
        /// Only valid until the logging callback returns.
        /// </summary>
        public readonly string? Module;
        /// <summary>
        /// Source code file name, or null if unknown. <c>libvlc_log_get_context</c>.
        /// Only valid until the logging callback returns.
        /// </summary>
        public readonly string? File;
        /// <summary>Source code line number, or 0 if unknown. <c>libvlc_log_get_context</c>.</summary>
        public readonly uint Line;
        /// <summary>
        /// VLC object type name emitting the message, or null. May be <c>"generic"</c> if the type is
        /// unknown. <c>libvlc_log_get_object</c>. Only valid until the logging callback returns.
        /// </summary>
        public readonly string? ObjectName;
        /// <summary>
        /// Object header, or null if unset. Used to distinguish VLM inputs in current versions.
        /// <c>libvlc_log_get_object</c>.
        /// </summary>
        public readonly string? Header;
        /// <summary>
        /// Temporarily-unique object identifier (<c>uintptr_t id</c>). Zero if the message is not
        /// associated with any VLC object. <c>libvlc_log_get_object</c>.
        /// </summary>
        public readonly UIntPtr ObjectId;
        /// <summary>The fully formatted log message.</summary>
        public readonly string? Message;
    }

    /// <summary>An audio output module. <c>libvlc_audio_output_t</c>.</summary>
    public readonly struct AudioOutput
    {
        /// <summary>Module name (pass to <see cref="MediaPlayer.SetAudioOutput"/>).</summary>
        public readonly string? Name;
        /// <summary>Human-readable description.</summary>
        public readonly string? Description;
        internal AudioOutput(string? name, string? description) { Name = name; Description = description; }
    }

    /// <summary>A libvlc module (filter) description. <c>libvlc_module_description_t</c>.</summary>
    public readonly struct ModuleDescription
    {
        /// <summary>Module name.</summary>
        public readonly string? Name;
        /// <summary>Short name, or null.</summary>
        public readonly string? ShortName;
        /// <summary>Long name, or null.</summary>
        public readonly string? LongName;
        /// <summary>Help text, or null.</summary>
        public readonly string? Help;
        internal ModuleDescription(string? name, string? shortName, string? longName, string? help)
        { Name = name; ShortName = shortName; LongName = longName; Help = help; }
    }

    /// <summary>A media discoverer service description. <c>libvlc_media_discoverer_description_t</c>.</summary>
    public readonly struct MediaDiscovererDescription
    {
        /// <summary>Service name (pass to <see cref="LibVLC.CreateMediaDiscoverer"/>).</summary>
        public readonly string? Name;
        /// <summary>Human-readable long name.</summary>
        public readonly string? LongName;
        /// <summary>Service category.</summary>
        public readonly MediaDiscovererCategory Category;
        internal MediaDiscovererDescription(string? name, string? longName, MediaDiscovererCategory category)
        { Name = name; LongName = longName; Category = category; }
    }

    /// <summary>A renderer discoverer service description. <c>libvlc_rd_description_t</c>.</summary>
    public readonly struct RendererDiscovererDescription
    {
        /// <summary>Service name (pass to <see cref="LibVLC.CreateRendererDiscoverer"/>).</summary>
        public readonly string? Name;
        /// <summary>Human-readable long name.</summary>
        public readonly string? LongName;
        internal RendererDiscovererDescription(string? name, string? longName) { Name = name; LongName = longName; }
    }
}
