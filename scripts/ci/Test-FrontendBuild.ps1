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
$windowsRuntimeId = 'win10-x64'

New-Item -ItemType Directory -Force -Path $env:DOTNET_CLI_HOME | Out-Null
New-Item -ItemType Directory -Force -Path $env:NUGET_PACKAGES | Out-Null

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
        & (Join-Path $PSScriptRoot 'Clear-AndroidAarPackages.ps1') -NugetPackagesPath $env:NUGET_PACKAGES | Out-Null
        & (Join-Path $PSScriptRoot 'Repair-AndroidAarCache.ps1') -NugetPackagesPath $env:NUGET_PACKAGES | Out-Null
        dotnet workload install maui-android --skip-manifest-update
        dotnet restore TipMolde/TipMolde.csproj --disable-build-servers -p:TargetFramework=net8.0-android --force
        & (Join-Path $PSScriptRoot 'Repair-AndroidAarCache.ps1') -NugetPackagesPath $env:NUGET_PACKAGES | Out-Null
        dotnet restore TipMolde/TipMolde.csproj --disable-build-servers -p:TargetFramework=net8.0-android --force
        dotnet publish TipMolde/TipMolde.csproj --configuration Release --framework net8.0-android --no-restore --output (Join-Path $repoRoot 'artifacts\Android')
    }
    else {
        dotnet workload install maui-windows --skip-manifest-update
        dotnet publish TipMolde/TipMolde.csproj --configuration Release --framework net8.0-windows10.0.19041.0 -p:RuntimeIdentifierOverride=$windowsRuntimeId -p:WindowsPackageType=None --output (Join-Path $repoRoot 'artifacts\Windows')
    }
}
finally {
    if ($createdTemporaryGlobalJson -and (Test-Path $temporaryGlobalJson)) {
        Remove-Item -LiteralPath $temporaryGlobalJson -Force
    }
}
