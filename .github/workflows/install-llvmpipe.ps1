param(
    [Parameter(Mandatory=$true)]
    [string]$EditorPath
)

# Software GL driver for GPU-less Windows containers. Without a graphics device,
# Unity bakes a degraded shader variant set into player builds (missing shadows
# and lighting features). Adapted from game-ci/unity-builder install_llvmpipe.ps1.
$ErrorActionPreference = 'Stop'

$repo = "mmozeiko/build-mesa"
$version = "25.1.0"
$downloadPath = "$env:TEMP\mesa.zip"
$extractPath = "$env:TEMP\mesa"

$release = Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/tags/$version" -Headers @{ "User-Agent" = "PowerShell" }
$zipUrl = ($release.assets | Where-Object { $_.name -like "mesa-llvmpipe-x64*.zip" } | Select-Object -First 1).browser_download_url
if (-not $zipUrl) { throw "No mesa-llvmpipe-x64 zip found in release $version" }

Invoke-WebRequest -Uri $zipUrl -OutFile $downloadPath
Expand-Archive -Path $downloadPath -DestinationPath $extractPath -Force
Copy-Item -Path "$extractPath\*" -Destination $EditorPath -Recurse -Force

Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue
Remove-Item $extractPath -Recurse -Force -ErrorAction SilentlyContinue
