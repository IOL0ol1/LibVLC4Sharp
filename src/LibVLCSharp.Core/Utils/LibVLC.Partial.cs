using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
#if NET8_0_OR_GREATER
using System.Reflection;
using System.Threading;
using LibVLCSharp.Core.Interop;
#endif

namespace LibVLCSharp.Core
{
    /// <summary>How the native <c>libvlc</c> library is resolved at runtime.</summary>
    public enum LibVLCLinkMode
    {
        /// <summary>Auto-discover libvlc (per-RID folders, then the OS default search).</summary>
        Default,

        /// <summary>Load from an explicit directory or file path.</summary>
        Path,

        /// <summary>
        /// Resolve symbols from the main program (statically linked / NativeAOT / iOS).
        /// Requires a net7.0+ target.
        /// </summary>
        Static,
    }

    /// <summary>
    /// Configures how the generated <c>[DllImport("libvlc")]</c> bindings bind to a native libvlc
    /// binary. Call <see cref="Initialize"/> / <see cref="Use"/> ONCE at startup, before invoking
    /// any libvlc function — the choice is not hot-swappable (the resolved handle and libvlc's own
    /// process state cannot be changed afterwards).
    /// </summary>
    /// <remarks>
    /// Discovery mirrors LibVLCSharp's <c>LibVLC.Initialize</c>: it probes <c>libvlc/&lt;rid&gt;</c>,
    /// <c>runtimes/&lt;rid&gt;/native</c> (the NuGet convention used by VideoLAN.LibVLC.Windows) and
    /// the app base directory, loading <c>libvlccore</c> before <c>libvlc</c>. On net7.0+ this is
    /// driven lazily by <c>NativeLibrary.SetDllImportResolver</c>; on netstandard the chosen binary
    /// is pre-loaded eagerly so the later <c>DllImport("libvlc")</c> resolves to it.
    /// </remarks>
    public partial class LibVLC
    {
        /// <summary>The DllImport library name emitted by the generated bindings.</summary>
        public const string LibraryName = "libvlc";

        private const string CoreLibraryName = "libvlccore";

        private static LibVLCLinkMode _mode = LibVLCLinkMode.Default;
        private static string? _arg;          // explicit path/dir/file for Path mode
        private static bool _coreLoaded;
#if NET8_0_OR_GREATER
        private static string? _resolvedDir;  // discovered directory, cached for the lazy resolver
#endif

        /// <summary>
        /// LibVLCSharp-style entry point. With no argument, auto-discovers libvlc; with a directory,
        /// loads libvlc from there.
        /// </summary>
        public static void Initialize(string? libvlcDirectory = null)
        {
            if (!string.IsNullOrEmpty(libvlcDirectory))
                Use(LibVLCLinkMode.Path, libvlcDirectory);
            else
                Use(LibVLCLinkMode.Default);
        }

        /// <summary>Configures the native linking strategy. Must run before the first libvlc call.</summary>
        public static void Use(LibVLCLinkMode mode, string? pathOrName = null)
        {
            _mode = mode;
            _arg = pathOrName;
            _coreLoaded = false;
#if NET8_0_OR_GREATER
            _resolvedDir = null;
            EnsureResolverRegistered();
            // Nothing loads yet on the resolver path — it fires on the first P/Invoke.
#else
            ConfigureNetStandard();
#endif
        }

        /// <summary>Loads libvlc from an explicit directory or file path.</summary>
        public static void UsePath(string path) => Use(LibVLCLinkMode.Path, path);

        /// <summary>Resolves libvlc symbols from the main program (static / AOT). net7.0+ only.</summary>
        public static void UseStatic() => Use(LibVLCLinkMode.Static);

        // --- Discovery (shared across target frameworks) ------------------------------------

        private static string? DiscoverDirectory()
        {
            foreach (var dir in CandidateDirectories())
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                    continue;
                foreach (var name in NativeFileNames(LibraryName))
                {
                    if (File.Exists(Path.Combine(dir, name)))
                        return dir;
                }
            }
            return null;
        }

        private static IEnumerable<string> CandidateDirectories()
        {
            var baseDir = AppContext.BaseDirectory;
            var rid = RuntimeFolder();
            yield return Path.Combine(baseDir, "libvlc", rid);             // LibVLCSharp convention
            yield return Path.Combine(baseDir, "runtimes", rid, "native"); // NuGet runtimes convention
            yield return baseDir;                                          // alongside the app
        }

        private static string RuntimeFolder()
        {
            string os =
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "linux";
            string arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.Arm => "arm",
                Architecture.Arm64 => "arm64",
                _ => "x64",
            };
            return os + "-" + arch;
        }

        private static string[] NativeFileNames(string stem)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new[] { stem + ".dll" };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new[] { "lib" + stem + ".dylib", stem + ".dylib" };
            return new[] { "lib" + stem + ".so", "lib" + stem + ".so.5", stem + ".so" };
        }

        /// <summary>Loads libvlccore (best-effort) then libvlc from <paramref name="dir"/>.</summary>
        private static IntPtr LoadFromDirectory(string dir)
        {
            PreloadCore(dir);
            foreach (var name in NativeFileNames(LibraryName))
            {
                var path = Path.Combine(dir, name);
                if (!File.Exists(path)) continue;
                var handle = LoadNative(path);
                if (handle != IntPtr.Zero) return handle;
            }
            return IntPtr.Zero;
        }

        private static void PreloadCore(string? dir)
        {
            if (_coreLoaded || string.IsNullOrEmpty(dir)) return;
            foreach (var name in NativeFileNames(CoreLibraryName))
            {
                var path = Path.Combine(dir, name);
                if (File.Exists(path) && LoadNative(path) != IntPtr.Zero)
                {
                    _coreLoaded = true;
                    return;
                }
            }
        }

#if NET8_0_OR_GREATER
        private static int _registered;

        private static void EnsureResolverRegistered()
        {
            // SetDllImportResolver may only be set once per assembly.
            if (Interlocked.Exchange(ref _registered, 1) == 0)
                NativeLibrary.SetDllImportResolver(typeof(libvlc).Assembly, Resolve);
        }

        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
                return IntPtr.Zero; // not ours — let the default resolver handle it.

            switch (_mode)
            {
                case LibVLCLinkMode.Static:
                    return NativeLibrary.GetMainProgramHandle();

                case LibVLCLinkMode.Path:
                    return ResolveExplicit();

                default:
                    var dir = _resolvedDir ??= DiscoverDirectory();
                    if (dir != null)
                    {
                        var handle = LoadFromDirectory(dir);
                        if (handle != IntPtr.Zero) return handle;
                    }
                    return NativeLibrary.TryLoad(LibraryName, assembly, searchPath, out var fallback)
                        ? fallback
                        : IntPtr.Zero;
            }
        }

        private static IntPtr ResolveExplicit()
        {
            if (Directory.Exists(_arg))
                return LoadFromDirectory(_arg);
            if (File.Exists(_arg))
            {
                PreloadCore(Path.GetDirectoryName(_arg));
                return LoadNative(_arg);
            }
            // Treat as a bare library name and defer to the default search.
            return NativeLibrary.TryLoad(_arg ?? LibraryName, out var handle) ? handle : IntPtr.Zero;
        }

        private static IntPtr LoadNative(string path) =>
            NativeLibrary.TryLoad(path, out var handle) ? handle : IntPtr.Zero;
#else
        private static void ConfigureNetStandard()
        {
            switch (_mode)
            {
                case LibVLCLinkMode.Static:
                    throw new PlatformNotSupportedException(
                        "LibVLCLinkMode.Static requires a net7.0+ target (NativeLibrary.GetMainProgramHandle).");

                case LibVLCLinkMode.Path:
                    if (string.IsNullOrEmpty(_arg))
                        throw new ArgumentNullException(nameof(_arg));
                    var arg = _arg!;
                    IntPtr handle;
                    if (Directory.Exists(arg))
                    {
                        handle = LoadFromDirectory(arg);
                    }
                    else
                    {
                        PreloadCore(Path.GetDirectoryName(arg));
                        handle = LoadNative(arg);
                    }
                    if (handle == IntPtr.Zero)
                        throw new DllNotFoundException($"Failed to load libvlc from '{arg}'.");
                    break;

                default:
                    // Pre-load the discovered binary so the later DllImport("libvlc") resolves to it
                    // (the OS loader matches the already-loaded module by base name). If nothing is
                    // discovered, fall through to the OS search at the first P/Invoke.
                    var dir = DiscoverDirectory();
                    if (dir != null)
                        LoadFromDirectory(dir);
                    break;
            }
        }

        private static IntPtr LoadNative(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return NativeMethods.LoadLibrary(path);
            return NativeMethods.dlopen(path, NativeMethods.RTLD_NOW | NativeMethods.RTLD_GLOBAL);
        }

        private static class NativeMethods
        {
            public const int RTLD_NOW = 0x002;
            public const int RTLD_GLOBAL = 0x100;

            [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern IntPtr LoadLibrary(string lpFileName);

            [DllImport("libdl", EntryPoint = "dlopen")]
            public static extern IntPtr dlopen(string fileName, int flags);
        }
#endif
    }
}
