# Builds src\ui\icon.ico from src\ui\icon.svg: one PNG frame per size Windows asks for, rendered by Microsoft Edge
# in headless mode. Under 64 pixels the loop's thin lines would blur away, so those frames reweight its layers (#halo,
# #tube, #core, #flare in icon.svg): a bolder tube, less glow. Re-run render.ps1 after, for the Store logo.
# Run: powershell -ExecutionPolicy Bypass -File store-listing\generator\icon.ps1
# ASCII only: Windows PowerShell reads a script in the system code page.
$ErrorActionPreference = "Stop"

$Ui = Join-Path $PSScriptRoot "..\..\src\ui" | Resolve-Path
$Work = Join-Path $PSScriptRoot "..\..\build\icon"
$EdgeProfile = Join-Path $PSScriptRoot "..\..\build\edge-render"   # a profile of its own, so a running Edge isn't involved

$Edge = @("${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe", "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe") |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $Edge) { throw "Microsoft Edge wasn't found." }

# Each weight: the attributes it sets on the loop's layers, and the frame sizes that use it. None = icon.svg as drawn.
$Weights = @(
    @{ Name = "small"; Sizes = 16, 20, 24
       Set = @{ halo = @{ "stroke-width" = 190; opacity = 0.5 }; tube = @{ "stroke-width" = 132 }; core = @{ "stroke-width" = 44 }; flare = @{ opacity = 0.45 } } },
    @{ Name = "medium"; Sizes = 32, 40, 48
       Set = @{ halo = @{ "stroke-width" = 150; opacity = 0.7 }; tube = @{ "stroke-width" = 92 }; core = @{ "stroke-width" = 28 }; flare = @{ opacity = 0.7 } } },
    @{ Name = "full"; Sizes = 64, 128, 256; Set = @{} }
)

New-Item -ItemType Directory -Force $Work | Out-Null
$svg = [IO.File]::ReadAllText((Join-Path $Ui "icon.svg"))
Add-Type -AssemblyName System.Drawing
$frames = @()

foreach ($weight in $Weights) {
    $text = $svg
    foreach ($id in $weight.Set.Keys) {
        foreach ($attr in $weight.Set[$id].Keys) {
            $pattern = "(<[a-z]+ id=`"$id`"[^>]*? $attr=`")[^`"]*"
            if ($text -notmatch $pattern) { throw "icon.svg has no #$id with $attr." }
            $text = $text -replace $pattern, "`${1}$($weight.Set[$id][$attr])"
        }
    }
    $file = Join-Path $Work "icon-$($weight.Name).svg"
    [IO.File]::WriteAllText($file, $text)

    foreach ($size in $weight.Sizes) {
        # Headless Edge keeps a minimum window size, larger than the small frames, so the icon sits in the top-left
        # corner and the shot is cropped to it.
        $page = Join-Path $Work "frame-$size.html"
        [IO.File]::WriteAllText($page, "<!doctype html><style>html,body{margin:0;background:transparent}" +
            "img{position:fixed;left:0;top:0;width:${size}px;height:${size}px}</style><img src=`"icon-$($weight.Name).svg`">")
        $shot = Join-Path $Work "shot-$size.png"
        Start-Process $Edge -ArgumentList @("--headless=new", "--disable-gpu", "--hide-scrollbars", "--allow-file-access-from-files",
            "--user-data-dir=`"$EdgeProfile`"", "--force-device-scale-factor=1", "--default-background-color=00000000",
            "--window-size=500,500", "--virtual-time-budget=3000", "--screenshot=`"$shot`"", "`"$(([Uri]$page).AbsoluteUri)`"") -Wait -WindowStyle Hidden
        if (-not (Test-Path $shot)) { throw "Edge didn't render the $size-pixel frame." }

        $image = [Drawing.Image]::FromFile($shot)
        $frame = New-Object Drawing.Bitmap $size, $size, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [Drawing.Graphics]::FromImage($frame)
        $g.DrawImage($image, (New-Object Drawing.Rectangle 0, 0, $size, $size), (New-Object Drawing.Rectangle 0, 0, $size, $size), [Drawing.GraphicsUnit]::Pixel)
        $g.Dispose(); $image.Dispose()
        $png = New-Object IO.MemoryStream
        $frame.Save($png, [Drawing.Imaging.ImageFormat]::Png); $frame.Dispose()
        $frames += @{ Size = $size; Png = $png.ToArray() }
    }
}

# The .ico: a header, one directory entry per frame (256 is written as 0), then each frame's PNG.
$frames = $frames | Sort-Object { $_.Size }
$ico = New-Object IO.MemoryStream
$w = New-Object IO.BinaryWriter $ico
$w.Write([UInt16]0); $w.Write([UInt16]1); $w.Write([UInt16]$frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($f in $frames) {
    $w.Write([byte]($f.Size % 256)); $w.Write([byte]($f.Size % 256)); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([UInt16]1); $w.Write([UInt16]32); $w.Write([UInt32]$f.Png.Length); $w.Write([UInt32]$offset)
    $offset += $f.Png.Length
}
foreach ($f in $frames) { $w.Write($f.Png) }
$w.Flush()
[IO.File]::WriteAllBytes((Join-Path $Ui "icon.ico"), $ico.ToArray())
Write-Host "src\ui\icon.ico  $(($frames | ForEach-Object { $_.Size }) -join ', ') px"
