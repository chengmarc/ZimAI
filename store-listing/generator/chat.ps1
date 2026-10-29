# Captures the Chat screenshots' window contents (chat-*.png beside this script) from the real Chat page: llama-server's
# own page, made ZimAI's the way the window makes it (src/ui/chat.css, and Window.ChatScript from src/app/Window.cs), at
# the window's size (960 x 640) and 2x. Each capture sends one message and waits for the model's whole answer, so the
# server must be running with the model loaded: open ZimAI and click Start first. Then run render.ps1.
# It uses a headless Microsoft Edge with a profile of its own, emptied each run, driven over its DevTools protocol: the
# app's own chat history is never touched.
# Run: powershell -ExecutionPolicy Bypass -File store-listing\generator\chat.ps1 [-Only chat-limit.png]
# -Only retakes one capture and keeps the other: the answers differ each run.
# ASCII only: Windows PowerShell reads a script in the system code page.
param([string]$Only)
$ErrorActionPreference = "Stop"

$Generator = $PSScriptRoot
$Src = Join-Path $Generator "..\..\src" | Resolve-Path
$EdgeProfile = Join-Path $Generator "..\..\build\edge-chat"
$Server = "http://127.0.0.1:8080"   # Model.Server in src/app/Model.cs
$Port = 9340

# Each capture: its file, and the message sent: two lines at most, asking for a short answer, so the whole exchange fits
# the window without scrolling.
$Captures = @(
    @{ File = "chat-limit.png"
       Message = "Cut off mid-email. Firm but polite note to my landlord: plumber came Monday, sink still leaks, fix it by Friday. Under 40 words." },
    @{ File = "chat-everyday.png"
       Message = "Hit my limit halfway through my budget sheet. Excel formula: total of column C where A is ""Paid"" and the date in B is this month. Just the formula." }
)

$Edge = @("${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe", "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe") |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $Edge) { throw "Microsoft Edge wasn't found." }
if ((curl.exe -s --noproxy "*" -m 3 "$Server/health") -notmatch '"ok"') {
    throw "The model server isn't ready at $Server. Open ZimAI, click Start, and wait for Server Ready."
}

# The window's script for the Chat page, as Window.cs has it: a C# verbatim string with the server's address and the
# CSS put in.
$windowCs = [IO.File]::ReadAllText((Join-Path $Src "app\Window.cs"))
$script = [regex]::Match($windowCs, '(?s)ChatScript = @"(.*?)";').Groups[1].Value
if (-not $script) { throw "Window.cs has no ChatScript." }
$css = [IO.File]::ReadAllText((Join-Path $Src "ui\chat.css"))
# The window serves src/ui/ as https://zimai.local/ (Window.cs); here the icon chat.css takes from there goes in as is.
$icon = [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $Src "ui\icon.svg")))
$css = $css.Replace("https://zimai.local/icon.svg", "data:image/svg+xml;base64,$icon")
$script = $script.Replace("'"" + Model.Server + @""'", "'$Server'").Replace('""', '"').Replace("CSS;", (ConvertTo-Json $css) + ";")

if (Test-Path $EdgeProfile) { Remove-Item -Recurse -Force $EdgeProfile }
$edgeProcess = Start-Process $Edge -PassThru -WindowStyle Hidden -ArgumentList @("--headless=new", "--remote-debugging-port=$Port",
    "--user-data-dir=`"$EdgeProfile`"", "--no-first-run", "--no-proxy-server", "--hide-scrollbars", "about:blank")

$ws = New-Object System.Net.WebSockets.ClientWebSocket
$none = [Threading.CancellationToken]::None
$id = 0

# One DevTools call, and its reply's raw JSON (a screenshot is too large for ConvertFrom-Json in PowerShell 5.1).
function Invoke-Raw([string]$Method, [hashtable]$Params = @{}) {
    $script:id++
    $message = @{ id = $script:id; method = $Method; params = $Params } | ConvertTo-Json -Depth 10 -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($message)
    $ws.SendAsync((New-Object ArraySegment[byte] (, $bytes)), "Text", $true, $none).Wait()
    $buffer = New-Object byte[] 65536
    while ($true) {
        $stream = New-Object IO.MemoryStream
        do {
            $result = $ws.ReceiveAsync((New-Object ArraySegment[byte] (, $buffer)), $none).Result
            $stream.Write($buffer, 0, $result.Count)
        } until ($result.EndOfMessage)
        $text = [Text.Encoding]::UTF8.GetString($stream.ToArray())
        if ($text -match "^\{`"id`":$($script:id),") {   # skip events
            if ($text -match '^\{"id":\d+,"error"') { throw "$Method failed: $text" }
            return $text
        }
    }
}

# A JavaScript expression's value on the page.
function Invoke-Js([string]$Expression) {
    $reply = Invoke-Raw "Runtime.evaluate" @{ expression = $Expression; returnByValue = $true; awaitPromise = $true } | ConvertFrom-Json
    return $reply.result.result.value
}

try {
    $page = $null
    for ($i = 0; $i -lt 50 -and -not $page; $i++) {
        Start-Sleep -Milliseconds 200
        try { $page = (curl.exe -s --noproxy "*" "http://127.0.0.1:$Port/json" | ConvertFrom-Json) | Where-Object { $_.type -eq "page" } | Select-Object -First 1 } catch { }
    }
    if (-not $page) { throw "Edge didn't open its DevTools port." }
    $ws.ConnectAsync([Uri]$page.webSocketDebuggerUrl, $none).Wait()

    Invoke-Raw "Page.enable" | Out-Null
    Invoke-Raw "Runtime.enable" | Out-Null
    Invoke-Raw "Emulation.setDeviceMetricsOverride" @{ width = 960; height = 640; deviceScaleFactor = 2; mobile = $false } | Out-Null
    Invoke-Raw "Page.addScriptToEvaluateOnNewDocument" @{ source = $script } | Out-Null

    foreach ($capture in $Captures | Where-Object { -not $Only -or $_.File -eq $Only }) {
        Invoke-Raw "Page.navigate" @{ url = "$Server/?new_chat=true#/" } | Out-Null
        Start-Sleep -Seconds 4
        if (-not (Invoke-Js "[...document.querySelectorAll('button')].some(b => b.title === 'Back to the ZimAI home page')")) {
            throw "Window.ChatScript didn't run on the Chat page."
        }
        # Type the message as the page's own input would get it, then click Send.
        $typed = Invoke-Js @"
(() => {
  const box = document.querySelector('[data-slot="input-area"] textarea');
  if (!box) return false;
  Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype, 'value').set.call(box, $(ConvertTo-Json $capture.Message));
  box.dispatchEvent(new Event('input', { bubbles: true }));
  return true;
})()
"@
        if (-not $typed) { throw "The Chat page has no message box." }
        Start-Sleep -Milliseconds 500
        Invoke-Js "document.querySelector('button:has(> svg.lucide-arrow-up)').click(), true" | Out-Null

        # The answer is done once Send is back in place of Stop (3 minutes at most); the page shows no text while the
        # model reads the prompt, so a pause in the text isn't the end.
        Start-Sleep -Seconds 2
        $done = $false
        for ($t = 0; $t -lt 180 -and -not $done; $t++) {
            Start-Sleep -Seconds 1
            $done = Invoke-Js "!!document.querySelector('button:has(> svg.lucide-arrow-up)')"
        }
        if (-not $done) { throw "The answer for $($capture.File) didn't finish in 3 minutes." }
        Start-Sleep -Seconds 2
        Invoke-Js "document.activeElement.blur(), true" | Out-Null   # the page stays where the answer ends
        Start-Sleep -Seconds 1

        $raw = Invoke-Raw "Page.captureScreenshot" @{ format = "png" }
        $data = [regex]::Match($raw, '"data":"([^"]+)"').Groups[1].Value
        [IO.File]::WriteAllBytes((Join-Path $Generator $capture.File), [Convert]::FromBase64String($data))
        Write-Host "store-listing\generator\$($capture.File)"
    }
} finally {
    $ws.Dispose()
    if (-not $edgeProcess.HasExited) { Stop-Process -Id $edgeProcess.Id -Force }
    Get-CimInstance Win32_Process -Filter "Name = 'msedge.exe'" | Where-Object { $_.CommandLine -like "*$EdgeProfile*" } |
        ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
}
