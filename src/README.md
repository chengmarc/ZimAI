# src/ — the app as it ships

Everything in `src/` ships in the GitHub zip and in the Store package. The parts git doesn't hold (`runtime/` and
`2B.exe`) come from `packaging/` ([`docs/RELEASING.md`](../docs/RELEASING.md)); they sit here, beside the code, because
the app finds them beside itself.

## Explanation

One language per layer, and each layer owns what it does:

| Layer                                                    | Language                                        | Owns                                                                                                                                                            |
| -------------------------------------------------------- | ----------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `2B.exe` ([`app/`](app/))                                | C# (.NET Framework 4, which comes with Windows) | where things are; the model, its download and the server's settings; starting and stopping the server; the window; opening the terminal; the proxy; the version |
| the agent ([`agent/`](agent/))                           | TypeScript, run as is by Node.js                | the conversation with the model and the tools it uses. It gets everything else from `2B.exe`                                                                    |
| the packaging scripts ([`../packaging/`](../packaging/)) | PowerShell (comes with Windows)                 | fetching `runtime/`, building `2B.exe` and the release artifacts; nothing in `src/` depends on them                                                             |

**The model server.** `2B.exe --server` is the server's keeper. Before each start it fits the model to this
computer (`Plan` in `app.cs`): llama.cpp's dry run (`llama-fit-params`) splits it between GPU and RAM; the context is
the model's full 128k on the GPU, whatever its VRAM, and 64k on the CPU; and the RAM part is loaded into RAM only
if that leaves 2 GB free, otherwise it's memory-mapped. The plan goes to `data/logs/server-plan` for the window's Server
row. The keeper writes its own pid to `data/logs/server.pid` before it plans, so the server counts as starting from
then; it starts llama-server with the plan, and stops it a few seconds after the last 2B window or agent is gone,
then exits. Each of those leaves a file named by its pid in `data/logs/clients/` while it runs. The
window's Chat and each `2b` ask for the server by running `2B.exe --server`; if a keeper is already running, the new
one quits. A server counts as this install's only while the keeper in `server.pid` runs, so two 2B installs on one computer
never stop each other's server. They do share its port, so only one of them can have a server running.

**What the agent gets.** `2B.exe` sets these for everything it starts, so `2b` in its terminal has them:

| Variable | What |
|---|---|
| `AGENT_2B_EXE` | `2B.exe`, to ask for the server (`--server`) |
| `AGENT_2B_SERVER` | the server's address |
| `AGENT_2B_DATA` | the data folder (below) |
| `HTTPS_PROXY`, `HTTP_PROXY`, `NO_PROXY` | the Windows proxy, if one is on and they aren't set already |

**The look.** [`app/palette.css`](app/palette.css) is where 2B's colors are chosen. The page uses it as it is. The
terminal (`app/mintty.conf`), the agent (`agent/terminal/theme.ts`), the window's background and the chat page's back
button (`app.cs`) and llama.cpp's chat page (`app/chat.css`) repeat its values.

**Chat.** It's llama-server's own chat page, made 2B's: the window adds `app/chat.css` (the palette, the icon, the
brand gradient on Send, no MCP Servers) and renames its "Hello there" to Agent 2B, every time the page opens. Who the
model is (`app/identity.md`) is a server default (`Defaults` in `app.cs`), so Chat and the agent are the same 2B.

## Reference

```
src/
  2B.exe                the app, built from app/ by packaging/build.ps1 (untracked)
  app/
    app.cs              2B.exe's source
    home.html           the page the window shows
    palette.css         the colors
    chat.css            the look of llama.cpp's chat page in the window
    identity.md         who the model is, for Chat and the agent
    icon.svg  icon.ico  the icon (the .ico is built into 2B.exe and shown by the terminal)
    mintty.conf         the terminal's look
  agent/                the coding agent: `2b` in the terminal
  runtime/              downloaded by packaging/fetch.ps1 (untracked)
    node/  git/  llama/ Node.js, portable Git Bash, llama.cpp (CUDA build)
    webview2/           the WebView2 SDK's DLLs, for the window
  data/                 made as the app runs, beside 2B.exe as it ships; in the Store app, the package's LocalCache,
                        and run from a clone, appdata/ at the repo's root (untracked)
    model/_model.gguf   the model, from the window's Download model
    browser/            the window's browser data, chat history included
    logs/
      server.log        llama-server's log, overwritten at each start
      server.pid        the keeper's pid, while it runs (planning included)
      server-plan       how it was fitted to this computer: context, GPU and RAM use
      load-seconds      how long the last load took, for the loading estimates
      clients/<pid>     one empty file per open 2B window and agent
```

The Store app is installed read-only (under `C:\Program Files\WindowsApps\`), so its data folder is
`%LOCALAPPDATA%\Packages\<package family name>\LocalCache\`, which Windows deletes when the app is uninstalled.
