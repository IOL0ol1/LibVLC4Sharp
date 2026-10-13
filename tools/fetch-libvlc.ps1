<#
.SYNOPSIS
    Download a LibVLC 4.x (nightly) Windows x64 build and stage libvlc.dll + libvlccore.dll +
    plugins\ into a destination folder.

.DESCRIPTION
    There is no LibVLC 4.x NuGet package (VideoLAN.LibVLC.Windows is 3.x only, and 3.x lacks
    libvlc_video_set_output_callbacks). This script fetches the official 4.0 nightly .zip from
    artifacts.videolan.org and extracts just the runtime bits the sample needs.

    The result layout (under -Dest) matches what LibVLCLoader auto-discovers:
        <Dest>\libvlc.dll
        <Dest>\libvlccore.dll
        <Dest>\plugins\...

    Compatible with both Windows PowerShell 5.1 and PowerShell 7+.

.PARAMETER Dest
    Folder to stage the runtime into (created if missing).

.PARAMETER Build
    Nightly build id (YYYYMMDD-HHMM) or 'latest' (default).
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Dest,
    [string]$Build = 'latest'
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # faster Invoke-WebRequest on Windows PowerShell

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$cache = Join-Path $scriptDir '.libvlc-cache'
$baseUrl = 'https://artifacts.videolan.org/vlc/nightly-win64/'

if ($Build -eq 'latest') {
    Write-Host "Resolving latest nightly from $baseUrl"
    $index = Invoke-WebRequest -Uri $baseUrl -UseBasicParsing
    $folders = [regex]::Matches($index.Content, '(\d{8}-\d{4})/') | ForEach-Object { $_.Groups[1].Value }
    $folders = $folders | Sort-Object -Unique
    if ($folders.Count -eq 0) { throw "No nightly build folders found at $baseUrl" }
    $Build = $folders[-1]
}
Write-Host "Using nightly build: $Build"

$buildUrl = $baseUrl + $Build + '/'
$page = Invoke-WebRequest -Uri $buildUrl -UseBasicParsing
$zipName = [regex]::Matches($page.Content, 'vlc-[^"<>]*-win64-[0-9a-f]+\.zip') |
    ForEach-Object { $_.Value } |
    Where-Object { $_ -notmatch 'debug' } |
    Select-Object -First 1
if (-not $zipName) { throw "Could not find a win64 .zip in $buildUrl" }

New-Item -ItemType Directory -Force -Path $cache | Out-Null
$zipPath = Join-Path $cache $zipName
if (-not (Test-Path $zipPath)) {
    Write-Host "Downloading $zipName (~100 MB)..."
    Invoke-WebRequest -Uri ($buildUrl + $zipName) -OutFile $zipPath -UseBasicParsing
}
else {
    Write-Host "Using cached $zipName"
}

$extractDir = Join-Path $cache ([System.IO.Path]::GetFileNameWithoutExtension($zipName))
if (-not (Test-Path $extractDir)) {
    Write-Host "Extracting..."
    Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force
}

$libvlc = Get-ChildItem -Path $extractDir -Recurse -Filter 'libvlc.dll' | Select-Object -First 1
if (-not $libvlc) { throw "libvlc.dll not found inside $zipName" }
$srcDir = $libvlc.Directory.FullName

New-Item -ItemType Directory -Force -Path $Dest | Out-Null
Copy-Item -Path (Join-Path $srcDir 'libvlc.dll') -Destination $Dest -Force
Copy-Item -Path (Join-Path $srcDir 'libvlccore.dll') -Destination $Dest -Force
Copy-Item -Path (Join-Path $srcDir 'plugins') -Destination $Dest -Recurse -Force

Write-Host "Staged LibVLC 4.x into $Dest"
