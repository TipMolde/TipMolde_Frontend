param(
    [Parameter(Mandatory = $true)]
    [string]$NugetPackagesPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path $NugetPackagesPath)) {
    Write-Host "NuGet packages path not found. Nothing to clean."
    exit 0
}

$packageVersionDirs = Get-ChildItem -Path $NugetPackagesPath -Directory -ErrorAction SilentlyContinue |
    ForEach-Object {
        Get-ChildItem -Path $_.FullName -Directory -ErrorAction SilentlyContinue
    }

$removedCount = 0

foreach ($packageVersionDir in $packageVersionDirs) {
    $aarFolder = Join-Path $packageVersionDir.FullName 'aar'
    if (-not (Test-Path $aarFolder)) {
        continue
    }

    Write-Host "Removing Android AAR-backed package cache: $($packageVersionDir.FullName)"
    Remove-Item -LiteralPath $packageVersionDir.FullName -Recurse -Force
    $removedCount++
}

Write-Host "Removed $removedCount Android AAR-backed package cache folder(s)."
