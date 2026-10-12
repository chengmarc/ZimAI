# Generates the README badges, media\badge-<name>.svg, in the stats strip's style, with src\app\palette.css's colors.
# Add a badge to $Badges and run: powershell -ExecutionPolicy Bypass -File media\badges.ps1
# ASCII only: Windows PowerShell reads a script in the system code page; [char]0xB7 is the middle dot.
$ErrorActionPreference = "Stop"
$Dot = [string][char]0xB7

# Icons are paths drawn in a 24x24 box.
$Badges = @(
    @{
        Name = "windows"
        Icon = "M0 0h11.4v11.4H0zM12.6 0H24v11.4H12.6zM0 12.6h11.4V24H0zM12.6 12.6H24V24H12.6z"
        Label = "Windows"
        Value = "10 $Dot 11"
    },
    @{
        Name = "nvidia"
        # Simple Icons (CC0)
        Icon = "M8.948 8.798v-1.43a6.7 6.7 0 0 1 .424-.018c3.922-.124 6.493 3.374 6.493 3.374s-2.774 3.851-5.75 3.851c-.398 0-.787-.062-1.158-.185v-4.346c1.528.185 1.837.857 2.747 2.385l2.04-1.714s-1.492-1.952-4-1.952a6.016 6.016 0 0 0-.796.035m0-4.735v2.138l.424-.027c5.45-.185 9.01 4.47 9.01 4.47s-4.08 4.964-8.33 4.964c-.37 0-.733-.035-1.095-.097v1.325c.3.035.61.062.91.062 3.957 0 6.82-2.023 9.593-4.408.459.371 2.34 1.263 2.73 1.652-2.633 2.208-8.772 3.984-12.253 3.984-.335 0-.653-.018-.971-.053v1.864H24V4.063zm0 10.326v1.131c-3.657-.654-4.673-4.46-4.673-4.46s1.758-1.944 4.673-2.262v1.237H8.94c-1.528-.186-2.73 1.245-2.73 1.245s.68 2.412 2.739 3.11M2.456 10.9s2.164-3.197 6.5-3.533V6.201C4.153 6.59 0 10.653 0 10.653s2.35 6.802 8.948 7.42v-1.237c-4.84-.6-6.492-5.936-6.492-5.936z"
        Label = "NVIDIA GPU"
        Value = "6 GB VRAM+"
    }
)

# The colors, from palette.css: the gradient, then background, lines and text.
$css = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot "..\src\app\palette.css")
$P = @{}
foreach ($m in [regex]::Matches($css, "--([\w-]+):\s*(#[0-9a-fA-F]{6})")) { $P[$m.Groups[1].Value] = $m.Groups[2].Value }

$Font = "-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif"
$H = 30; $Size = 13; $IconSize = 16; $Pad = 11; $Gap = 8

# Helvetica advance widths per 1000 em. The text is also given textLength, so a font that runs wider or narrower
# is fitted to the same box instead of overflowing it.
$Widths = New-Object "Collections.Generic.Dictionary[string,int]"   # case-sensitive, unlike @{}
foreach ($w in @{ " " = 278; $Dot = 278; "+" = 584; "." = 278; "-" = 333 }.GetEnumerator()) { $Widths[$w.Key] = $w.Value }
foreach ($d in "0123456789".ToCharArray()) { $Widths["$d"] = 556 }
$upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray()
$upperW = 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611
$lowerW = 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556, 556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500
for ($i = 0; $i -lt 26; $i++) {
    $Widths["$($upper[$i])"] = $upperW[$i]
    $Widths["$([char]::ToLower($upper[$i]))"] = $lowerW[$i]
}
$Bold = 1.06   # bold runs about this much wider

function Measure-Text([string]$Text, [bool]$IsBold = $false) {
    $em = 0
    foreach ($ch in $Text.ToCharArray()) { $em += $(if ($Widths.ContainsKey("$ch")) { $Widths["$ch"] } else { 600 }) }
    $w = $em / 1000 * $Size * $(if ($IsBold) { $Bold } else { 1 })
    return [Math]::Floor($w * 10 + 0.5) / 10
}

function N($x) { return ([Math]::Round([double]$x, 4)).ToString([Globalization.CultureInfo]::InvariantCulture) }
function Esc([string]$s) { return $s.Replace("&", "&amp;").Replace("<", "&lt;") }

function Format-Badge($b) {
    $lw = Measure-Text $b.Label $true; $vw = Measure-Text $b.Value
    $labelX = $Pad + $IconSize + $Gap
    $divX = $labelX + $lw + $Pad
    $valueX = $divX + $Pad
    $W = [Math]::Ceiling($valueX + $vw + $Pad)
    $y = $H / 2 + $Size * 0.35
    $iy = ($H - $IconSize) / 2
    $alt = Esc "$($b.Label) $($b.Value)"
    return @"
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 $W $H" width="$W" height="$H" role="img" aria-label="$alt">
<title>$alt</title>
<defs><linearGradient id="brand" gradientUnits="userSpaceOnUse" x1="0" y1="0" x2="24" y2="0"><stop offset="0" stop-color="$($P.gold)"/><stop offset=".5" stop-color="$($P.rose)"/><stop offset="1" stop-color="$($P.violet)"/></linearGradient></defs>
<rect x=".5" y=".5" width="$($W - 1)" height="$($H - 1)" rx="8" fill="$($P.bg)" stroke="$($P.line)"/>
<path transform="translate($Pad $(N $iy)) scale($(N ($IconSize / 24)))" fill="url(#brand)" d="$($b.Icon)"/>
<g font-family="$Font" font-size="$Size">
<text x="$labelX" y="$(N $y)" font-weight="600" fill="$($P.strong)" textLength="$(N $lw)" lengthAdjust="spacing">$(Esc $b.Label)</text>
<text x="$(N $valueX)" y="$(N $y)" fill="$($P.muted)" textLength="$(N $vw)" lengthAdjust="spacing">$(Esc $b.Value)</text>
</g>
<line x1="$(N $divX)" y1="8" x2="$(N $divX)" y2="$($H - 8)" stroke="$($P.line)"/>
</svg>

"@
}

foreach ($b in $Badges) {
    $svg = (Format-Badge $b) -replace "`r`n", "`n"
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot "badge-$($b.Name).svg"), $svg, (New-Object Text.UTF8Encoding $false))
}
Write-Host "wrote $($Badges.Count) badges"
