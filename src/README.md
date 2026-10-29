# src/ — the app as it ships

Everything in `src/` ships in the GitHub zip and in the Store package. The parts git doesn't hold (`runtime/` and
`ZimAI.exe`) come from `packaging/` ([`docs/RELEASING.md`](../docs/RELEASING.md)); they sit here, beside the code, because
the app finds them beside itself.

## Explanation

One language per layer, and each layer owns what it does:

| Layer                                                    | Language                                        | Owns                                                                                                                                                            |
| -------------------------------------------------------- | ----------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ZimAI.exe` ([`app/`](app/))                                | C# (.NET Framework 4, which comes with Windows) | where things are; the model, its download and the server's settings; starting and stopping the server; the window; opening the terminal; the proxy; the version |
| the agent ([`agent/`](agent/))                           | TypeScript, run as is by Node.js                | the conversation with the model and the tools it uses. It gets everything else from `ZimAI.exe`                                                                    |
| the packaging scripts ([`../packaging/`](../packaging/)) | PowerShell (comes with Windows)                 | fetching `runtime/`, building `ZimAI.exe` and the release artifacts; nothing in `src/` depends on them                                                             |

**The model server.** `ZimAI.exe --server` is the server's keeper. Before each start it fits the model to this
computer (`Plan`, in `app/Plan.cs`): llama.cpp's dry run (`llama-fit-params`) splits it between GPU and RAM; the context is
the model's full 128k on the GPU, whatever its VRAM, and 64k on the CPU; and the RAM part is loaded into RAM only
if that leaves 2 GB free, otherwise it's memory-mapped. The plan goes to `data/logs/server-plan` for the window's Server
row. The keeper writes its own pid to `data/logs/server.pid` before it plans, so the server counts as starting from
then; it starts llama-server with the plan, and stops it a few seconds after the last ZimAI window or agent is gone,
then exits. A memory-mapped part is read only as the model first uses it, which would stall the first message, so
while the server loads, the keeper reads the model file once into Windows' file cache; it writes
`data/logs/server-ready` once the server takes requests and that read is done, and the window and the agent wait for it. Each of those leaves a file named by its pid in `data/logs/clients/` while it runs. The
window's Chat and each `zim` ask for the server by running `ZimAI.exe --server`; if a keeper is already running, the new
one quits. A server counts as this install's only while the keeper in `server.pid` runs, so two ZimAI installs on one computer
never stop each other's server. They do share its port, so only one of them can have a server running.

**What the agent gets.** `ZimAI.exe` sets these for everything it starts, so `zim` in its terminal has them:

| Variable | What |
|---|---|
| `ZIMAI_EXE` | `ZimAI.exe`, to ask for the server (`--server`) |
| `ZIMAI_SERVER` | the server's address |
| `ZIMAI_DATA` | the data folder (below) |
| `HTTPS_PROXY`, `HTTP_PROXY`, `NO_PROXY` | the Windows proxy, if one is on and they aren't set already |

**The look.** [`ui/palette.css`](ui/palette.css) is where ZimAI's colors are chosen. The page uses it as it is. The
terminal (`ui/mintty.conf`), the agent (`agent/terminal/theme.ts`), the window's background, title bar and
the chat page's back button (`app/Window.cs`) and llama.cpp's chat page (`ui/chat.css`) repeat its values.

**Chat.** It's llama-server's own chat page, made ZimAI's: the window adds `ui/chat.css` (the palette, the icon, the
brand gradient on Send, no MCP Servers) and renames its "Hello there" to ZimAI, every time the page opens. Who the
model is (`agent/prompt/identity.md`) is a server default (`Defaults`, in `app/Server.cs`), so Chat and the agent are the same ZimAI.

## Reference

```
src/
  ZimAI.exe             the app, built from app/ by packaging/build.ps1 (untracked)
  app/                  ZimAI.exe's source, C# 5 and ASCII only (Program.cs says why), one part per file:
    Program.cs            Main: the window, --server or --terminal; the proxy; the version
    Paths.cs              where everything is
    Model.cs              the model's facts, and its download
    Requirements.cs       whether this PC can run it, and how well
    Plan.cs               how the server runs the model on this PC, worked out before each start
    Server.cs             the server's keeper, its clients, its defaults, and the server as the window sees it
    Terminal.cs           the terminal, and starting it outside the Store package
    Window.cs             the window, its title bar
    Json.cs               the JSON the window sends its page
  ui/                   ZimAI's look: what the window serves at https://zimai.local/, and the terminal's
    home.html             the page the window shows
    chat.css              the look of llama.cpp's chat page in the window
    palette.css           the colors
    mintty.conf           the terminal's look
    icon.svg  icon.ico    the icon (the .ico, built from the .svg by store-listing/generator/icon.ps1, is also in
                          ZimAI.exe and the terminal)
  agent/                the coding agent: `zim` in the terminal; also who the model is (prompt/identity.md),
                        which ZimAI.exe reads
  runtime/              downloaded by packaging/fetch.ps1 (untracked)
    node/  git/  llama/ Node.js, portable Git Bash, llama.cpp (CUDA build)
    webview2/           the WebView2 SDK's DLLs, for the window
  data/                 made as the app runs, beside ZimAI.exe as it ships; in the Store app, the package's LocalCache,
                        and run from a clone, appdata/ at the repo's root (untracked)
    model/_model.gguf   the model, from the window's Download model
    browser/            the window's browser data, chat history included
    logs/
      server.log        llama-server's log, overwritten at each start
      server.pid        the keeper's pid, while it runs (planning included)
      server-plan       how it was fitted to this computer: context, GPU and RAM use
      server-ready      there once the server takes requests, the mapped part of the model read in (Keeper)
      load-seconds      how long the last load took, for the agent's loading screen
      clients/<pid>     one empty file per open ZimAI window and agent
```

The Store app is installed read-only (under `C:\Program Files\WindowsApps\`), so its data folder is
`%LOCALAPPDATA%\Packages\<package family name>\LocalCache\`, which Windows deletes when the app is uninstalled.
