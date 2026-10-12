# Renders the images uploaded to Partner Center into store\listing\, from the pages in store\ (logo.html, shots.html),
# which draw them from the app's own files: its icon, its home page and the README's terminal demo. Uses Microsoft
# Edge, which comes with Windows, in headless mode. LISTING.md says where each image goes.
# Run: powershell -ExecutionPolicy Bypass -File store\render.ps1
# ASCII only: Windows PowerShell reads a script in the system code page.
$ErrorActionPreference = "Stop"

$Store = $PSScriptRoot
$Out = Join-Path $Store "listing"
$EdgeProfile = Join-Path $Store "..\build\edge-render"   # a profile of its own, so a running Edge isn't involved

$Edge = @("${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe", "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe") |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $Edge) { throw "Microsoft Edge wasn't found." }

# Each image: file, page (with its hash), and its size in CSS pixels, rendered at 2x unless Scale says otherwise.
# Partner Center's sizes: screenshots 1366 x 768 or larger, box art 2160 x 2160, poster art 1440 x 2160, tile 300 x 300.
$Images = @(
    @{ File = "screenshot-1-promo.png"; Page = "shots.html#promo"; Width = 1366; Height = 768 },
    @{ File = "screenshot-2-home.png"; Page = "shots.html#home"; Width = 1366; Height = 768 },
    @{ File = "screenshot-3-download.png"; Page = "shots.html#download"; Width = 1366; Height = 768 },
    @{ File = "screenshot-4-permission.png"; Page = "shots.html#permission"; Width = 1366; Height = 768 },
    @{ File = "box-art-2160.png"; Page = "logo.html"; Width = 1080; Height = 1080 },
    @{ File = "poster-art-1440x2160.png"; Page = "logo.html"; Width = 720; Height = 1080 },
    @{ File = "store-logo-300.png"; Page = "logo.html#tile"; Width = 300; Height = 300; Scale = 1; Transparent = $true }
)

Add-Type -AssemblyName System.Drawing
if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
New-Item -ItemType Directory -Force $Out | Out-Null

foreach ($image in $Images) {
    $scale = if ($image.Scale) { $image.Scale } else { 2 }
    $file = Join-Path $Out $image.File
    $url = ([Uri](Join-Path $Store ($image.Page -replace "#.*$", ""))).AbsoluteUri + ($image.Page -replace "^[^#]*", "")
    # --virtual-time-budget lets the page load what it fetches before the shot is taken, without waiting in real time.
    $edgeArgs = @("--headless=new", "--disable-gpu", "--hide-scrollbars", "--allow-file-access-from-files",
        "--user-data-dir=`"$EdgeProfile`"", "--force-device-scale-factor=$scale",
        "--window-size=$($image.Width),$($image.Height)", "--virtual-time-budget=5000", "--screenshot=`"$file`"")
    if ($image.Transparent) { $edgeArgs += "--default-background-color=00000000" }
    Start-Process $Edge -ArgumentList ($edgeArgs + "`"$url`"") -Wait -WindowStyle Hidden

    if (-not (Test-Path $file)) { throw "Edge didn't write $($image.File)." }
    $png = [Drawing.Image]::FromFile($file)
    $size = "$($png.Width) x $($png.Height)"
    $png.Dispose()
    $expected = "$($image.Width * $scale) x $($image.Height * $scale)"
    if ($size -ne $expected) { throw "$($image.File) is $size, expected $expected." }
    Write-Host "store\listing\$($image.File)  $size"
}
