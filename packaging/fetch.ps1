# For a clone of the repo: downloads what src\ ships but git doesn't hold into src\runtime\: Node.js, Git Bash,
# llama.cpp and the WebView2 SDK's DLLs. Not the model: the app downloads that (its Download model button).
# Run it again any time: it only downloads what's missing, and an interrupted download resumes.
#   powershell -ExecutionPolicy Bypass -File packaging\fetch.ps1
# Each component the same way: download into build\downloads -> unpack into <dest>.new -> rename to <dest>.
$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot
$Runtime = Join-Path $Root "src\runtime"
$Staging = Join-Path $Root "build\downloads"
$Curl = Join-Path $env:SystemRoot "System32\curl.exe"
$Tar = Join-Path $env:SystemRoot "System32\tar.exe"

# The components: their folder in src\runtime, their URLs, and for a zip how many top folders to strip.
# To upgrade one: change its URL, delete its folder, run this again.
$Components = @(
    # Node.js 24.21.0 LTS (the agent needs 22.18+ to run its .ts files as they are)                 ~30 MB
    @{ Dest = "node"; Strip = 1; Urls = @("https://nodejs.org/dist/v24.21.0/node-v24.21.0-win-x64.zip") },
    # Git for Windows 2.55.0.5, portable (Git Bash)                                                 ~60 MB
    @{ Dest = "git"; Urls = @("https://github.com/git-for-windows/git/releases/download/v2.55.0.windows.5/PortableGit-2.55.0.5-64-bit.7z.exe") },
    # llama.cpp b10434, CUDA 12.4 build + its CUDA runtime (NVIDIA driver with CUDA 12.4+)          ~640 MB
    @{ Dest = "llama"; Strip = 0; Urls = @(
        "https://github.com/ggml-org/llama.cpp/releases/download/b10434/llama-b10434-bin-win-cuda-12.4-x64.zip",
        "https://github.com/ggml-org/llama.cpp/releases/download/b10434/cudart-llama-bin-win-cuda-12.4-x64.zip") },
    # WebView2 SDK 1.0.2903.40, for 2B.exe's window (the WebView2 runtime itself comes with Windows)  ~1 MB
    @{ Dest = "webview2"; Urls = @("https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/1.0.2903.40/microsoft.web.webview2.1.0.2903.40.nupkg") }
)

# curl reads HTTPS_PROXY but not the Windows proxy setting (e.g. Clash).
if (-not $env:HTTPS_PROXY) {
    $site = [Uri]"https://github.com/"
    $proxy = [Net.WebRequest]::GetSystemWebProxy().GetProxy($site)
    if ($proxy -ne $site) {
        $env:HTTPS_PROXY = $proxy.GetLeftPart("Authority")
        Write-Host "(using the Windows proxy $env:HTTPS_PROXY)"
    }
}

function Invoke-Native([string]$Exe, [string[]]$Arguments) {
    & $Exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$(Split-Path -Leaf $Exe) failed ($LASTEXITCODE)" }
}

# A finished download is build\downloads\<file>; an unfinished one is still <file>.part, resumed next time.
function Get-Download([string]$Url) {
    $file = Join-Path $Staging ($Url -split "/")[-1]
    if (Test-Path $file) { return $file }
    New-Item -ItemType Directory -Force $Staging | Out-Null
    Write-Host "Downloading $(Split-Path -Leaf $file)..."
    # -C -: resume a partial download; a stalled connection is dropped and retried.
    Invoke-Native $Curl @("-L", "--fail", "--retry", "5", "--retry-all-errors", "-C", "-", "--speed-limit", "10000",
                          "--speed-time", "60", "--progress-bar", "-o", "$file.part", $Url)
    Move-Item "$file.part" $file
    return $file
}

# A .zip is unpacked as is, minus its top folders; a .7z.exe unpacks itself; of a .nupkg (a zip), only the
# .NET Framework assemblies and the x64 native DLLs, side by side.
function Expand-Download([string]$File, [string]$Into, [int]$Strip) {
    New-Item -ItemType Directory -Force $Into | Out-Null
    switch -Regex ($File) {
        "\.zip$" { Invoke-Native $Tar @("-xf", $File, "-C", $Into, "--strip-components", $Strip) }
        "\.7z\.exe$" {
            # A windowed program: & neither waits for it nor sets $LASTEXITCODE, so wait for its exit code here.
            $p = Start-Process -FilePath $File -ArgumentList "-o`"$Into`"", "-y" -Wait -PassThru -WindowStyle Hidden
            if ($p.ExitCode -ne 0) { throw "$(Split-Path -Leaf $File) failed ($($p.ExitCode))" }
        }
        "\.nupkg$" {
            Invoke-Native $Tar @("-xf", $File, "-C", $Into, "--strip-components", 2, "lib/net462/*.dll")
            Invoke-Native $Tar @("-xf", $File, "-C", $Into, "--strip-components", 3, "runtimes/win-x64/native/*.dll")
        }
        default { throw "don't know how to unpack $File" }
    }
}

try {
    foreach ($c in $Components) {
        $dest = Join-Path $Runtime $c.Dest
        if (Test-Path $dest) { continue }   # only a finished one is <dest>
        Write-Host "`n== src\runtime\$($c.Dest) =="
        $files = @($c.Urls | ForEach-Object { Get-Download $_ })
        if (Test-Path "$dest.new") { Remove-Item -Recurse -Force "$dest.new" }
        foreach ($f in $files) { Expand-Download $f "$dest.new" $c.Strip }
        Move-Item "$dest.new" $dest
        $files | Remove-Item
        Write-Host "Ready: src\runtime\$($c.Dest)"
    }
    if ((Test-Path $Staging) -and -not (Get-ChildItem $Staging)) { Remove-Item $Staging }
    Write-Host "`nDone. Build 2B.exe next: powershell -ExecutionPolicy Bypass -File packaging\build.ps1 -Exe"
} catch {
    Write-Host "`n$($_.Exception.Message)`nFailed; run this again to retry (downloads resume)." -ForegroundColor Red
    exit 1
}
