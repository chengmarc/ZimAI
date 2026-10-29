# Builds ZimAI from src\ (docs\RELEASING.md): src\ZimAI.exe, then the ready-to-run zip for GitHub and, with -Msix, the
# Microsoft Store package. Both hold src\ as it is after packaging\fetch.ps1, minus data\ (the app downloads the model).
#   powershell -ExecutionPolicy Bypass -File packaging\build.ps1 [-Clean] [-Msix | -Exe]
# -Exe builds only src\ZimAI.exe, for trying a change to the app; -Clean deletes build\ and dist\ first.
param([switch]$Clean, [switch]$Msix, [switch]$Exe)
$ErrorActionPreference = "Stop"

# From Partner Center > the app > Product identity.
$IdentityName = "chengmarc.ZimAI"   # TODO: from Partner Center once ZimAI is reserved; it was chengmarc.Agent2B
$Publisher = "CN=58F02A86-A694-404A-8E09-F891292262CA"
$PublisherDisplayName = "chengmarc"
$DisplayName = "ZimAI"      # must match the app name reserved in Partner Center

$Root = Split-Path $PSScriptRoot
$Src = Join-Path $Root "src"
$Build = Join-Path $Root "build"
$App = Join-Path $Build "ZimAI"   # the app as it ships: the zip's one folder, then the package
$Zip = Join-Path $Root "dist\ZimAI-portable.zip"
$Package = Join-Path $Root "dist\ZimAI.msix"
$Csc = Join-Path $env:SystemRoot "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$Tar = Join-Path $env:SystemRoot "System32\tar.exe"

function Invoke-Native([string]$Exe, [string[]]$Arguments) {
    Write-Host "> $(Split-Path -Leaf $Exe) $($Arguments -join ' ')"
    & $Exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$(Split-Path -Leaf $Exe) failed ($LASTEXITCODE)" }
}

# The version is Program.cs's (AssemblyInformationalVersion), MAJOR.MINOR.PATCH; the git tag is v<it>.
function Get-Version {
    $m = Select-String -Path (Join-Path $Src "app\Program.cs") -Pattern 'AssemblyInformationalVersion\("(\d+\.\d+\.\d+)"\)'
    if (-not $m) { throw "no MAJOR.MINOR.PATCH AssemblyInformationalVersion in src\app\Program.cs" }
    return $m.Matches[0].Groups[1].Value
}

function Get-Manifest {
    $missing = @("IdentityName", "Publisher", "PublisherDisplayName") | Where-Object { -not (Get-Variable $_ -ValueOnly) }
    if ($missing) { throw "set $($missing -join ', ') in packaging\build.ps1 from Partner Center > Product identity" }
    $values = @{
        identity_name = $IdentityName; publisher = $Publisher; publisher_display_name = $PublisherDisplayName
        display_name = $DisplayName
        version = "$(Get-Version).0"   # the Store wants four parts, the last one 0
    }
    $text = Get-Content -Raw (Join-Path $PSScriptRoot "AppxManifest.xml")
    return [regex]::Replace($text, "\{(\w+)\}", { param($m) $values[$m.Groups[1].Value] })
}

# makeappx.exe from the Windows SDK, or from the Microsoft.Windows.SDK.BuildTools NuGet package unzipped into
# %LOCALAPPDATA%\ZimAI Build\sdk-buildtools (docs\RELEASING.md). The newest one found.
function Find-MakeAppx {
    $found = @("${env:ProgramFiles(x86)}\Windows Kits\10\bin", "$env:LOCALAPPDATA\ZimAI Build\sdk-buildtools\bin") |
        Where-Object { Test-Path $_ } |
        ForEach-Object { Get-ChildItem $_ -Directory -Filter "10.*" } |
        Where-Object { Test-Path "$($_.FullName)\x64\makeappx.exe" } |
        Sort-Object { [version]$_.Name } | Select-Object -Last 1
    if (-not $found) { throw "makeappx.exe wasn't found: install the Windows SDK or its BuildTools package (docs\RELEASING.md)" }
    return "$($found.FullName)\x64\makeappx.exe"
}

# src\ZimAI.exe, with the C# compiler that comes with Windows (.NET Framework 4).
function Build-Exe {
    $webview2 = Join-Path $Src "runtime\webview2"
    if (-not (Test-Path $webview2)) { throw "src\runtime\webview2 is missing: run packaging\fetch.ps1 first" }
    $refs = @("System.Net.Http.dll", "System.Windows.Forms.dll", "System.Drawing.dll",
              "$webview2\Microsoft.Web.WebView2.Core.dll", "$webview2\Microsoft.Web.WebView2.WinForms.dll")
    Invoke-Native $Csc (@("/nologo", "/target:winexe", "/optimize+", "/win32icon:$Src\ui\icon.ico", "/out:$Src\ZimAI.exe") +
                        ($refs | ForEach-Object { "/r:$_" }) + @("$Src\app\*.cs"))
}

# build\ZimAI: src\ without data\, and the license.
function Build-Stage {
    foreach ($dir in "node", "git", "llama", "webview2") {
        if (-not (Test-Path "$Src\runtime\$dir")) { throw "src\runtime\$dir is missing: run packaging\fetch.ps1 first" }
    }
    Write-Host "Staging the app..."
    if (Test-Path $App) { Remove-Item -Recurse -Force $App }
    robocopy $Src $App /E /XD (Join-Path $Src "data") /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed ($LASTEXITCODE)" }
    Copy-Item (Join-Path $Root "LICENSE") $App
}

function Build-Zip {
    New-Item -ItemType Directory -Force (Split-Path $Zip) | Out-Null
    if (Test-Path $Zip) { Remove-Item $Zip }
    Push-Location $Build
    try { Invoke-Native $Tar @("-a", "-c", "-f", $Zip, (Split-Path -Leaf $App)) } finally { Pop-Location }
}

# The Store logos: icon.ico's 256-pixel image (stored as a PNG inside it), scaled down.
function Build-Logos([string]$Into) {
    Add-Type -AssemblyName System.Drawing
    $ico = [IO.File]::ReadAllBytes((Join-Path $Src "ui\icon.ico"))
    $png = $null
    for ($i = 0; $i -lt [BitConverter]::ToUInt16($ico, 4); $i++) {
        $entry = 6 + 16 * $i
        if ($ico[$entry] -eq 0) {   # width 0 means 256
            $size = [BitConverter]::ToInt32($ico, $entry + 8)
            $offset = [BitConverter]::ToInt32($ico, $entry + 12)
            $png = New-Object IO.MemoryStream(, $ico[$offset..($offset + $size - 1)])
        }
    }
    if (-not $png) { throw "src\ui\icon.ico has no 256-pixel image" }
    $source = [Drawing.Image]::FromStream($png)
    New-Item -ItemType Directory -Force $Into | Out-Null
    foreach ($logo in @{ "StoreLogo.png" = 50; "Square44x44Logo.png" = 44; "Square150x150Logo.png" = 150 }.GetEnumerator()) {
        $bitmap = New-Object Drawing.Bitmap $logo.Value, $logo.Value
        $g = [Drawing.Graphics]::FromImage($bitmap)
        $g.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.DrawImage($source, 0, 0, $logo.Value, $logo.Value)
        $bitmap.Save((Join-Path $Into $logo.Key), [Drawing.Imaging.ImageFormat]::Png)
        $g.Dispose(); $bitmap.Dispose()
    }
    $source.Dispose()
}

# The package: the staged app, its manifest and logos.
function Build-Package([string]$Manifest) {
    $makeappx = Find-MakeAppx
    [IO.File]::WriteAllText((Join-Path $App "AppxManifest.xml"), $Manifest)
    Build-Logos (Join-Path $App "Assets")
    New-Item -ItemType Directory -Force (Split-Path $Package) | Out-Null
    Invoke-Native $makeappx @("pack", "/o", "/d", $App, "/p", $Package)
}

try {
    if ($Msix -and $Exe) { throw "-Msix and -Exe don't go together" }
    if ($Clean) { $Build, (Join-Path $Root "dist") | Where-Object { Test-Path $_ } | Remove-Item -Recurse -Force }
    New-Item -ItemType Directory -Force $Build | Out-Null
    $manifest = if ($Msix) { Get-Manifest } else { $null }   # before the slow part: fail on a missing identity first
    Build-Exe
    if ($Exe) { exit 0 }
    Build-Stage
    Build-Zip   # before Build-Package, which adds the package's own files to the staged app
    if ($Msix) { Build-Package $manifest }
    Write-Host "`nBuilt (version $(Get-Version)):`n- $Src\ZimAI.exe`n- $Zip"
    if ($Msix) { Write-Host "- $Package (Microsoft Store upload)" }
} catch {
    Write-Host "build: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
