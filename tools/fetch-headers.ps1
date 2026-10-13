<#
.SYNOPSIS
    Fetch the libvlc public C headers (vlc/include/vlc) from the VideoLAN repo.

.DESCRIPTION
    Replaces the old `vlc` git submodule. Performs a shallow, blobless, sparse
    clone of videolan/vlc and checks out only the `include/vlc` directory into
    tools/.vlc. The result is tools/.vlc/include/vlc/*.h, consumed by generate.rsp.

    Compatible with both Windows PowerShell 5.1 (powershell) and PowerShell 7+ (pwsh).

.PARAMETER Ref
    Branch or tag to fetch. Defaults to 'master' (latest).

.PARAMETER Url
    Git URL of the VLC repository.

.EXAMPLE
    pwsh tools/fetch-headers.ps1
    powershell -ExecutionPolicy Bypass -File tools/fetch-headers.ps1 -Ref master
#>
param(
    [string]$Ref = 'master',
    [string]$Url = 'https://code.videolan.org/videolan/vlc.git'
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$cacheDir = Join-Path $scriptDir '.vlc'
$includeDir = Join-Path $cacheDir 'include/vlc'

function Invoke-Git {
    param([string[]]$GitArgs)
    Write-Host "git $($GitArgs -join ' ')"
    & git @GitArgs
    if ($LASTEXITCODE -ne 0) {
        throw "git $($GitArgs -join ' ') failed (exit $LASTEXITCODE)"
    }
}

if (Test-Path -Path (Join-Path $cacheDir '.git')) {
    # Existing cache: update in place.
    Write-Host "Updating existing header cache in $cacheDir"
    Invoke-Git @('-C', $cacheDir, 'sparse-checkout', 'set', 'include/vlc')
    Invoke-Git @('-C', $cacheDir, 'fetch', '--depth', '1', 'origin', $Ref)
    Invoke-Git @('-C', $cacheDir, 'checkout', '-f', 'FETCH_HEAD')
}
else {
    if (Test-Path -Path $cacheDir) {
        Remove-Item -Path $cacheDir -Recurse -Force
    }
    Write-Host "Cloning $Url ($Ref) into $cacheDir (sparse: include/vlc)"
    Invoke-Git @('clone', '--depth', '1', '--filter=blob:none', '--sparse', '--branch', $Ref, $Url, $cacheDir)
    Invoke-Git @('-C', $cacheDir, 'sparse-checkout', 'set', 'include/vlc')
}

if (-not (Test-Path -Path $includeDir)) {
    throw "Expected headers not found at $includeDir"
}

$count = (Get-ChildItem -Path $includeDir -Filter '*.h' -File).Count
Write-Host "Fetched $count header(s) into $includeDir"
