param(
    [ValidateSet('windows', 'android')]
    [string]$Target = 'android'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location $repoRoot

$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.dotnet-cli-home'
$env:NUGET_PACKAGES = Join-Path $repoRoot '.nuget\packages'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $repoRoot '.nuget\http-cache'
$env:NUGET_SCRATCH = Join-Path $repoRoot '.nuget\scratch'

New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME | Out-Null
New-Item -ItemType Directory -Force -Path $env:NUGET_PACKAGES | Out-Null
New-Item -ItemType Directory -Force -Path $env:NUGET_HTTP_CACHE_PATH | Out-Null
New-Item -ItemType Directory -Force -Path $env:NUGET_SCRATCH | Out-Null

$temporaryGlobalJson = Join-Path $repoRoot 'global.json'
$createdTemporaryGlobalJson = $false

if (-not (Test-Path $temporaryGlobalJson)) {
    @'
{
  "sdk": {
    "version": "8.0.414",
    "rollForward": "latestPatch"
  }
}
'@ | Set-Content -LiteralPath $temporaryGlobalJson -Encoding utf8
    $createdTemporaryGlobalJson = $true
}

try {
    if ($Target -eq 'android') {
        dotnet build-server shutdown

        $pathsToReset = @(
            (Join-Path $repoRoot 'TipMolde\bin'),
            (Join-Path $repoRoot 'TipMolde\obj')
        )
        foreach ($path in $pathsToReset) {
            if (Test-Path $path) {
                try {
                    Remove-Item -LiteralPath $path -Recurse -Force
                }
                catch {
                    Write-Warning "Could not fully remove $path. Continuing with build."
                }
            }
        }

        dotnet nuget locals http-cache --clear
        dotnet nuget locals temp --clear
        & (Join-Path $PSScriptRoot 'Clear-AndroidAarPackages.ps1') -NugetPackagesPath $env:NUGET_PACKAGES | Out-Null
        & (Join-Path $PSScriptRoot 'Repair-AndroidAarCache.ps1') -NugetPackagesPath $env:NUGET_PACKAGES | Out-Null
        $installed = dotnet workload list | Out-String
        if ($installed -notmatch '(?m)^\s*maui-android\s') {
            dotnet workload install maui-android --skip-manifest-update
        }
        else {
            Write-Host "Workload(s) 'maui-android' are already installed."
        }
        dotnet restore TipMolde/TipMolde.csproj --disable-build-servers --disable-parallel --force --force-evaluate
        $removed = & (Join-Path $PSScriptRoot 'Repair-AndroidAarCache.ps1') -NugetPackagesPath $env:NUGET_PACKAGES
        if ($removed -eq 'true') {
            dotnet restore TipMolde/TipMolde.csproj --disable-build-servers --disable-parallel --force --force-evaluate
            $removed = & (Join-Path $PSScriptRoot 'Repair-AndroidAarCache.ps1') -NugetPackagesPath $env:NUGET_PACKAGES
            if ($removed -eq 'true') {
                throw 'Android AAR cache is still corrupt after restore retry.'
            }
        }
        dotnet build TipMolde/TipMolde.csproj --configuration Release --framework net8.0-android --disable-build-servers --disable-parallel --no-restore
    }
    else {
        dotnet build-server shutdown

        $pathsToReset = @(
            (Join-Path $repoRoot 'TipMolde\bin'),
            (Join-Path $repoRoot 'TipMolde\obj')
        )
        foreach ($path in $pathsToReset) {
            if (Test-Path $path) {
                try {
                    Remove-Item -LiteralPath $path -Recurse -Force
                }
                catch {
                    Write-Warning "Could not fully remove $path. Continuing with build."
                }
            }
        }

        dotnet nuget locals http-cache --clear
        dotnet nuget locals temp --clear
        $installed = dotnet workload list | Out-String
        if ($installed -notmatch '(?m)^\s*maui-windows\s') {
            dotnet workload install maui-windows --skip-manifest-update
        }
        else {
            Write-Host "Workload(s) 'maui-windows' are already installed."
        }
        dotnet restore TipMolde/TipMolde.csproj --disable-build-servers --disable-parallel --force --force-evaluate
        dotnet build TipMolde/TipMolde.csproj --configuration Release --framework net8.0-windows10.0.19041.0 --disable-build-servers --disable-parallel --no-restore
    }
}
finally {
    if ($createdTemporaryGlobalJson -and (Test-Path $temporaryGlobalJson)) {
        Remove-Item -LiteralPath $temporaryGlobalJson -Force
    }
}
