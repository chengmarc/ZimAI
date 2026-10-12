# src/ — the app as it ships

Everything in `src/` ships in the GitHub zip and in the Store package, except `data/`. The parts git doesn't hold
(`runtime/` and `2B.exe`) come from `tools/` ([`docs/RELEASING.md`](../docs/RELEASING.md)).

## Explanation

One language per layer, and each layer owns what it does:

| Layer | Language | Owns |
|---|---|---|
| `2B.exe` ([`app/`](app/)) | C# (.NET Framework 4, which comes with Windows) | where things are; the model, its download and the server's settings; starting and stopping the server; the window; opening the terminal; the proxy; the version |
| the agent ([`agent/`](agent/)) | TypeScript, run as is by Node.js | the conversation with the model and the tools it uses. It gets everything else from `2B.exe` |
| the tools ([`../tools/`](../tools/)) | PowerShell (comes with Windows) | fetching `runtime/`, building `2B.exe` and the release artifacts; nothing in `src/` depends on them |

**The model server.** `2B.exe --server` is the server's keeper. It starts llama-server with the settings in
`app.cs` (`Model`), writes its pid to `data/logs/server.pid`, and stops it, by that pid, a few seconds after the last
2B window or agent is gone. Each of those leaves a file named by its pid in `data/logs/clients/` while it runs. The
window's Chat and each `2b` ask for the server by running `2B.exe --server`; if a keeper is already running, the new
one quits. A server counts as this install's only if its pid is the one in `server.pid`, so two 2B installs on one
computer never stop each other's server. They do share its port, so only one of them can have a server running.

**What the agent gets.** `2B.exe` sets these for everything it starts, so `2b` in its terminal has them:

| Variable | What |
|---|---|
| `AGENT_2B_EXE` | `2B.exe`, to ask for the server (`--server`) |
| `AGENT_2B_SERVER` | the server's address |
| `AGENT_2B_DATA` | the data folder (below) |
| `HTTPS_PROXY`, `HTTP_PROXY`, `NO_PROXY` | the Windows proxy, if one is on and they aren't set already |

**The look.** [`app/palette.css`](app/palette.css) is where 2B's colors are chosen. The page uses it as it is. The
terminal (`app/mintty.conf`), the agent (`agent/terminal/theme.ts`), the window's background and the chat page's back
button (`app.cs`) repeat its values. `media/badges.ps1` reads it.

## Reference

```
src/
  2B.exe                the app, built from app/ by tools/build.ps1 (untracked)
  app/
    app.cs              2B.exe's source
    home.html           the page the window shows
    palette.css         the colors
    icon.svg  icon.ico  the icon (the .ico is built into 2B.exe and shown by the terminal)
    mintty.conf         the terminal's look
  agent/                the coding agent: `2b` in the terminal
  runtime/              downloaded by tools/fetch.ps1 (untracked)
    node/  git/  llama/ Node.js, portable Git Bash, llama.cpp (CUDA build)
    webview2/           the WebView2 SDK's DLLs, for the window
  data/                 made as the app runs (untracked); in the Store app, the package's LocalCache instead
    model/_model.gguf   the model, from the window's Download model
    browser/            the window's browser data, chat history included
    logs/
      server.log        llama-server's log, overwritten at each start
      server.pid        the running server's pid
      load-seconds      how long the last load took, for the loading estimates
      clients/<pid>     one empty file per open 2B window and agent
```

The Store app is installed read-only (under `C:\Program Files\WindowsApps\`), so its data folder is
`%LOCALAPPDATA%\Packages\<package family name>\LocalCache\`, which Windows deletes when the app is uninstalled.
