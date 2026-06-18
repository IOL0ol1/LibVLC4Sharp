# LibVLCSharp.WPF.Sample

A minimal WPF app that validates the `LibVLCSharp.WPF` `VideoView`. The sample drives it with the
**D3D11** engine (`Engine="D3D11"`), whose libvlc output callbacks are composited into a WPF
`D3DImage` (no HWND overlay → no airspace problem). It plays a file/URL and draws a semi-transparent
WPF overlay **on top of** the video to prove the airspace problem is gone.

> The `VideoView` also supports `Engine="D3D9"` (the default, widest compatibility). Set `Engine`
> before the control loads — the output is created once on first use.

## LibVLC 4.x is fetched automatically

The `libvlc_video_set_output_callbacks` rendering API only exists in **LibVLC 4.x**. There is **no
4.x NuGet** (`VideoLAN.LibVLC.Windows` is 3.x only), so on the **first build** this project runs
`tools/fetch-libvlc.ps1`, which downloads the official 4.0 nightly `.zip` from
<https://artifacts.videolan.org/vlc/nightly-win64/> (~100 MB) and stages
`libvlc.dll` + `libvlccore.dll` + `plugins\` into:

```
bin\<Config>\net8.0-windows\libvlc\win-x64\
```

`LibVLC.Initialize()` discovers that folder automatically (app dir / `runtimes/<rid>/native`) — no
code changes, no manual download. The download is cached under `tools/.libvlc-cache/` and only runs
once (until you clean the output).

- **Skip the auto-download:** build with `-p:FetchLibVLC=false`.
- **Use your own build instead:** set `VlcDirectory` at the top of `MainWindow.xaml.cs` to a folder
  containing `libvlc.dll` (it then calls `LibVLC.UsePath(...)`).
- **Re-fetch manually:** `pwsh tools/fetch-libvlc.ps1 -Dest <folder>` (works on Windows PowerShell too).

## Run

```
dotnet run --project samples/LibVLCSharp.WPF.Sample
```

Enter a path or URL in the box and press **Play**, or use **Open file…**. You should see video with
the overlay (text + button) composited above it, with opacity — something a HWND-hosted player
cannot do.
