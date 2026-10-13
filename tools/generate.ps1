<#
.SYNOPSIS
    Generate the libvlc P/Invoke bindings into src/LibVLCSharp.Core/Generated.

.DESCRIPTION
    1. Fetches the latest libvlc headers (fetch-headers.ps1) unless -SkipFetch.
    2. Restores the ClangSharpPInvokeGenerator dotnet tool.
    3. Runs ClangSharpPInvokeGenerator @generate.rsp.

    Compatible with both Windows PowerShell 5.1 (powershell) and PowerShell 7+ (pwsh).

.PARAMETER Ref
    Branch or tag of headers to fetch. Defaults to 'master'.

.PARAMETER SkipFetch
    Skip header fetching and reuse the existing tools/.vlc cache.

.EXAMPLE
    pwsh tools/generate.ps1
    powershell -ExecutionPolicy Bypass -File tools/generate.ps1
#>
param(
    [string]$Ref = 'master',
    [switch]$SkipFetch
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
$repoRoot = Split-Path -Path $scriptDir -Parent

if (-not $SkipFetch) {
    Write-Host "run: fetch-headers.ps1 -Ref $Ref"
    & (Join-Path $scriptDir 'fetch-headers.ps1') -Ref $Ref
}

# dotnet tool restore must run from the repo root (.config/dotnet-tools.json).
Push-Location $repoRoot
try {
    Write-Host 'run: dotnet tool restore'
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet tool restore failed (exit $LASTEXITCODE)"
    }
}
finally {
    Pop-Location
}

# ClangSharp response file paths are relative to the tools directory.
Push-Location $scriptDir
try {
    Write-Host 'run: dotnet tool run ClangSharpPInvokeGenerator @generate.rsp'
    & dotnet tool run ClangSharpPInvokeGenerator '@generate.rsp'
    if ($LASTEXITCODE -ne 0) {
        throw "ClangSharpPInvokeGenerator failed (exit $LASTEXITCODE)"
    }
}
finally {
    Pop-Location
}

Write-Host 'All done.'
