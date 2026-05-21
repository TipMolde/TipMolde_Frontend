param(
    [Parameter(Mandatory = $true)]
    [string]$NugetPackagesPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression.FileSystem

$corruptPackageRoots = New-Object 'System.Collections.Generic.HashSet[string]'
$aarFiles = Get-ChildItem -Path $NugetPackagesPath -Recurse -Filter '*.aar' -ErrorAction SilentlyContinue

if (-not $aarFiles) {
    Write-Host "No Android AAR files found in NuGet cache yet."
    'false'
    exit 0
}

foreach ($aarFile in $aarFiles) {
    try {
        $archive = [System.IO.Compression.ZipFile]::OpenRead($aarFile.FullName)
        $archive.Dispose()
    }
    catch {
        Write-Warning "Corrupt Android AAR detected: $($aarFile.FullName)"
        $packageVersionRoot = Split-Path (Split-Path $aarFile.FullName -Parent) -Parent
        if ($packageVersionRoot) {
            [void]$corruptPackageRoots.Add($packageVersionRoot)
        }
    }
}

if ($corruptPackageRoots.Count -eq 0) {
    Write-Host "Android AAR cache looks healthy."
    'false'
    exit 0
}

foreach ($packageVersionRoot in $corruptPackageRoots) {
    if (Test-Path $packageVersionRoot) {
        Write-Host "Removing corrupt package cache: $packageVersionRoot"
        Remove-Item -LiteralPath $packageVersionRoot -Recurse -Force
    }
}

'true'
