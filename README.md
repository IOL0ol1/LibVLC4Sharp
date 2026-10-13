# LibVLCSharp.Core (LibVLC4Sharp)

[![NuGet Version](https://img.shields.io/nuget/v/LibVLC4Sharp.Core.svg)](https://www.nuget.org/packages/LibVLC4Sharp.Core/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/LibVLC4Sharp.Core.svg)](https://www.nuget.org/packages/LibVLC4Sharp.Core/)

 **!!!!! This is NOT an official libVLC project !!!!!**

# Why this project?
This project started because I needed `libvlc_video_set_output_callbacks`, the new video rendering
API from LibVLC 4.0.0. I believe it provides a cleaner solution to airspace issues in WPF and other
UI frameworks. While [LibVLCSharp](https://github.com/videolan/libvlcsharp) is very well-designed,
the v4 prerelease is not yet officially stable, and its interop layer is not fully automated. So this
project uses GitHub Actions + ClangSharpPInvokeGenerator to keep up-to-date P/Invoke bindings for VLC.
I leveraged AI for coding assistance. Most of the glue code and scripts were AI-generated, and I have     
manually reviewed every single piece of code with fine-tuning on encapsulated implementation details.    

Note: LibVLC 4.0 has not been officially released by the VLC team, so all pre-4.0 APIs are unstable.       
You will need to pair them with VLC nightly builds.      

# Introduction
A .NET binding for **libVLC 4.x** with two layers in one package:

1. **Raw interop** (`LibVLCSharp.Core.Interop`) — P/Invoke bindings auto-generated from the libvlc
   4.x headers with `ClangSharpPInvokeGenerator`, following the C `snake_case` names so the C
   documentation maps directly onto the API (similar to `FFmpeg.AutoGen`).
2. **Managed API** (`LibVLCSharp.Core`) — a hand-written object-oriented layer over the interop:
   `LibVLC`, `Media`, `MediaPlayer`, `MediaList`, `MediaListPlayer`, `Equalizer`, `Picture`,
   `RendererItem`/`RendererDiscoverer`, `MediaDiscoverer`, `Dialog`, … with `IDisposable` lifetime,
   managed `string`/`enum` parameters, and standard .NET events.

# Quick start

```cmd
dotnet add package LibVLC4Sharp.Core --prerelease
```

> Packages are published as **prerelease nightlies** (`4.0.0-nightly.<date>`) that track the libvlc
> 4.x headers, so `--prerelease` (or "Include prerelease" in your IDE) is required to install them.

## Managed API (recommended)

```csharp
using System;
using LibVLCSharp.Core; 

// Resolve a native libvlc 4.x once, before any libvlc call (see "Native linking"):
LibVLC.Initialize(); 
// LibVLC.Use(LibVLCLinkMode.Path, @"C:\path\to\vlc-4.x\");  // or an explicit folder

using var vlc    = new LibVLC("--no-video-title-show");
using var media  = Media.FromPath(@"C:\video.mp4");
using var player = vlc.CreateMediaPlayer(media);

player.Playing      += (s, e) => Console.WriteLine("playing");
player.TimeChanged  += (s, e) => Console.WriteLine($"t = {e.TimeMs} ms");   // readonly-struct payload
player.EncounteredError += (s, e) => Console.WriteLine("error");

player.Play();
Console.ReadLine();
```

## Raw interop (snake_case, matches the C header)

```csharp
using System;
using LibVLCSharp.Core.Interop;
using static LibVLCSharp.Core.Interop.libvlc;

LibVLC.Initialize();
string version = ((IntPtr)libvlc_get_version()).GetUtf8();   // byte* UTF-8 -> managed string
Console.WriteLine(version);                                  // or: LibVLCSharp.Core.LibVLC.Version
```

# Package

A single package, `LibVLC4Sharp.Core`, multi-targeting `netstandard2.0;netstandard2.1;net8.0`.

> The NuGet package id uses the `LibVLC4Sharp` prefix (4 = LibVLC 4.0) to avoid the reserved
> `LibVLCSharp.*` prefix on nuget.org. The C# namespace remains `LibVLCSharp.Core`.

# Native linking

All three classic linking strategies are covered from the one assembly via the static members on
`LibVLCSharp.Core.LibVLC` — call them **once at startup, before any libvlc call** (the choice is not
hot-swappable). A UI layer (e.g. WPF) configures this in its `App` startup.

```csharp
LibVLC.Initialize();                                  // auto-discover (default)
LibVLC.Use(LibVLCLinkMode.Default);                   // OS / per-RID search
LibVLC.Use(LibVLCLinkMode.Path, @"C:\VLC\");          // explicit folder or file (= LibVLC.UsePath)
LibVLC.Use(LibVLCLinkMode.Static);                    // symbols in the main program (= LibVLC.UseStatic)
```

| Mode | net7.0+ (resolver) | netstandard2.0 / 2.1 |
| --- | --- | --- |
| `Default` | `NativeLibrary` default search | OS loader resolves `libvlc` |
| `Path` | `NativeLibrary.Load(path)` | pre-loads the binary (`LoadLibrary`/`dlopen`) |
| `Static` | `NativeLibrary.GetMainProgramHandle()` | not supported (throws) |

On net7.0+ this uses `NativeLibrary.SetDllImportResolver`; on netstandard targets `Static` requires a
net7.0+ TFM. `LibVLC.Initialize()` auto-discovers libvlc in `libvlc/<rid>` and
`runtimes/<rid>/native` (loading `libvlccore` before `libvlc`); pass a directory to
`Initialize(dir)` to override.

### Native libvlc binaries (user-provided)

This repo does **not** ship libvlc. Provide a **LibVLC 4.x** build yourself (the output-callbacks
API requires 4.x — the `VideoLAN.LibVLC.Windows` NuGet only carries 3.x). Drop `libvlc.dll`,
`libvlccore.dll` and the `plugins/` folder into `runtimes/win-x64/native/` (or any folder) and:

```csharp
LibVLC.UsePath(@"C:\path\to\vlc-4.x\");   // directory containing libvlc.dll
```

# Managed API at a glance

- **Lifetime** — every wrapper derives from `NativeReference` (`IDisposable` + finalizer, releases
  the native handle once). Each exposes a `public static implicit operator libvlc_*_t*` so it passes
  straight into raw interop.
- **Strings** — `char *` parameters are managed `string` (UTF-8 marshaled by `Utf8Marshaler`);
  `char *` return values stay `byte*`/`sbyte*` and are read with the `IntPtr.GetUtf8()` helper.
- **Enums** — public PascalCase enums (no `Vlc` prefix), e.g. `State`, `Meta`, `TrackType`,
  generated at compile time from the interop enums (never drift).
- **Events** — standard `EventHandler` (no payload) / `EventHandler<TArgs>` where `TArgs` is a
  zero-allocation `readonly struct` (e.g. `TimeChangedEventArgs.TimeMs`, `MetaChangedEventArgs.Meta`).
  Events live on the owning object (`media.MetaChanged`, `player.TimeChanged`, …) and attach the
  native callback on the first subscriber / detach on the last.
- **Callbacks** — software/accelerated rendering and audio callbacks (`SetVideoCallbacks`,
  `SetOutputCallbacks`, `SetAudioCallbacks`, `WatchTime`, …) take delegates that the wrapper keeps
  alive; `Media.FromStream`/`FromCallbacks` wrap a managed `Stream`/callbacks as input.
- **Dialogs** — `Dialog` routes libvlc login/question/progress/error interactions to events.

The managed surface covers essentially the whole libvlc public API; the few intentionally
**not** wrapped (`*_retain`/`*_hold`, `*_user_data`, `video_new_viewpoint`) are handled internally
or replaced by idiomatic .NET equivalents.

# UI — VideoView (airspace-free)

| Package | Framework | Status |
| --- | --- | --- |
| `LibVLC4Sharp.WPF` | `net8.0-windows` (AnyCPU) | D3D9 **and** D3D11 output callbacks → `D3DImage` |

(An Avalonia `VideoView` is planned — see TODO.) The WPF `VideoView` (namespace `LibVLCSharp.WPF`)
references `LibVLCSharp.Core` and uses `libvlc_video_set_output_callbacks`. WPF can only composite a
Direct3D9 surface (via `D3DImage`), so both engines end up presenting through a host `IDirect3D9Ex`
device's shared surface — no HWND overlay, so the airspace problem is gone (you can freely overlay
WPF controls, use opacity, transforms, etc.). The engine is selected by the `Engine` property
(`VideoEngine`, default `D3D9`) and the output is created once on first use:

- **`D3D9`** (default, widest compatibility) — libvlc renders into a Direct3D9 texture shared with the
  host D3D9Ex device. Based on VLC's `doc/libvlc/d3d9_player.c`.
- **`D3D11`** — *we* create the `ID3D11Device` and hand its context to libvlc (enabling D3D11VA
  hardware decode); libvlc renders into a D3D11 texture that is bridged to the D3D9Ex surface via a
  shared handle. Based on VLC's `doc/libvlc/d3d11_player.cpp`.

The D3D9/D3D11 interop is hand-written (`Interop/Direct3D9.cs`, `Interop/Direct3D11.cs`, vtable-index
calls), so the package is **AnyCPU** with no extra dependencies.

```csharp
// XAML: xmlns:vlc="clr-namespace:LibVLCSharp.WPF;assembly=LibVLCSharp.WPF"
//       <vlc:VideoView x:Name="Video" Engine="D3D11" />
var view = new LibVLCSharp.WPF.VideoView { Engine = VideoEngine.D3D11 };  // set Engine before load
// ... add to your visual tree ...
view.Attach(player);        // a managed MediaPlayer — call before play (or bind the MediaPlayer property)
```

> WPF rendering is implemented and compiles against the generated bindings, but still needs
> validation on a machine with a GPU + libvlc 4.x. The Avalonia renderer (D3D11 → Avalonia 11
> `CompositionDrawingSurface`) is the next step.

# Notes

- no `libvlc_printerr` — C# has poor compatibility with C variadic arguments.
- no `libvlc_role_Last` — equivalent to `libvlc_role_Test`.
- libvlc logging (`LibVLC.SetLog`) exposes structured fields (level/module/file/line/object); the
  printf message is **not** formatted (portable `va_list` formatting is unavailable).

# How it works

The interop is generated **offline** and committed to the repo — no source generator runs the
ClangSharp step at consumer build time (the enum/delegate alias generators below DO run at build).

**Generation (`tools/`):**
1. `tools/fetch-headers.ps1` — shallow + sparse `git` checkout of `videolan/vlc`'s `include/vlc`
   headers into `tools/.vlc/`.
2. `tools/generate.ps1` — runs `fetch-headers.ps1` (unless `-SkipFetch`), restores the
   `ClangSharpPInvokeGenerator` dotnet tool, and runs it with the `generate.rsp` response file to
   emit `src/LibVLCSharp.Core/Generated/LibVLC.Interop.g.cs`. The `.rsp` carries the libvlc-specific
   customizations: `const char *` parameters → `string` + UTF-8 marshaling, callback-typedef params
   → the generated delegate type (not `IntPtr`), and `[MarshalAs(U1)]` on `_Bool`.
3. `tools/fetch-libvlc.ps1` — downloads an official libvlc 4.x Windows nightly (`libvlc.dll` +
   `libvlccore.dll` + `plugins/`) so the sample can run; not part of binding generation.

```powershell
# Regenerate locally (works on both pwsh 7+ and Windows PowerShell 5.1)
pwsh tools/generate.ps1
# or
powershell -ExecutionPolicy Bypass -File tools/generate.ps1
```

**Build:** `LibVLCSharp.Core` compiles the generated interop, the hand-written managed wrappers, and
a few helpers under `src/LibVLCSharp.Core/Utils/` (`NativeTypeNameAttribute`, the `GetUtf8` string
helper, the `Utf8Buffer` UTF-8 marshaling, and the `LibVLC` partial that holds the native-linking
logic). Two Roslyn incremental source generators in `LibVLCSharp.Core.Generator` (referenced as
analyzers) emit, at compile time, PascalCase **enum** aliases and PascalCase **delegate** twins over
the interop types.

**CI (`.github/workflows`):**
- `check-vlc-update.yml` runs daily: regenerates the bindings from the latest libvlc headers and, if
  the generated interop actually changed, derives the version from the header macros
  (`libvlc_version.h`) as `MAJOR.MINOR.REVISION-nightly.<UTC-date>`, writes it into
  `Directory.Build.props`, commits the regenerated bindings, tags `v<version>`, and invokes the
  publish workflow.
- `nuget-publish.yml` (reusable; also runs on a `v*` tag push or manual dispatch): builds the whole
  solution (`LibVLC4Sharp.slnx`) in Release — the packable projects (`LibVLC4Sharp.Core` +
  `LibVLC4Sharp.WPF`) emit their `.nupkg`/`.snupkg` via `GeneratePackageOnBuild` — and pushes every
  package under `nupkgs/` to nuget.org. Authentication uses **NuGet Trusted Publishing (OIDC)**: the
  job runs in the `production` environment and exchanges a GitHub OIDC token for a short-lived API key
  via `NuGet/login` — no long-lived `NUGET_API_KEY` secret is needed.

# Repository layout

```
tools/    fetch-headers.ps1, fetch-libvlc.ps1, generate.ps1, generate.rsp   (offline generation + runtime fetch)
src/      LibVLCSharp.Core            (interop + managed wrappers + native-linking loader)
          LibVLCSharp.Core.Generator  (enum + delegate alias source generators)
          LibVLCSharp.WPF             (D3D9/D3D11 + D3DImage VideoView, AnyCPU)
samples/  LibVLCSharp.WPF.Sample
```

# TODO
- Validate the WPF `VideoView` (both D3D9 and D3D11 engines) on a GPU + libvlc 4.x machine.
- Implement the Avalonia `VideoView` renderer (D3D11 output callbacks → Avalonia 11 compositor GPU interop).

# Reference & Thanks

- [vlc](https://github.com/videolan/vlc)
- [LibVLCSharp](https://github.com/videolan/libvlcsharp)
- [ClangSharp](https://github.com/dotnet/ClangSharp)

# License
All source code in this repository is licensed under the MIT License.

This library dynamically links against libVLC at runtime.
libVLC is distributed under the GNU Lesser General Public License v2.1 (LGPLv2.1).
This repository does NOT contain any libVLC binaries or source code; users must install VLC runtime separately.
VLC source code: https://code.videolan.org/videolan/vlc
