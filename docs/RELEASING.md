# Releasing

2B ships two ways, both built from `src/`:

- **Portable, on GitHub:** `Agent-2B-portable.zip`, ready to run. The user unzips it anywhere, a flash drive
  included, and double-clicks `2B.exe`.
- **Microsoft Store:** an MSIX package with the same contents.

Neither includes the model. The app downloads it on its first run.

[`tools/build.ts`](../tools/build.ts) builds both. Nothing in `tools/` ships inside the app.

## How-to guides

### Cut a release

1. Pick the version and set `VERSION` in [`tools/build.ts`](../tools/build.ts). The git tag is `v<VERSION>`.
   The Store needs every new submission to have a higher version than the last.
2. Run `tools\fetch.cmd` so `src/runtime/` is complete. It only downloads what's missing.
3. Build both artifacts (see [Build prerequisites](#build-prerequisites)):

   ```sh
   src/runtime/node/node.exe tools/build.ts --clean --msix
   ```

   This takes about ten minutes: five to zip and five to pack.
4. Smoke-test the portable zip: unzip it somewhere new and double-click `2B.exe`. Then smoke-test the Store package
   (below).
5. Tag the release, push the tag, and publish a GitHub release with `dist/release/Agent-2B-portable.zip` attached.
   GitHub takes release files up to 2 GB; the zip is about 860 MB.
6. Upload `dist/store/Agent 2B.msix` in a new Partner Center submission.

### Set up the Store identity (once)

Copy the three values from Partner Center > the app > Product identity into `IDENTITY_NAME`, `PUBLISHER` and
`PUBLISHER_DISPLAY_NAME` in `tools/build.ts`. `DISPLAY_NAME` must match the name reserved there. Until those values
are filled in, `--msix` stops before building anything.

### Smoke-test a build

Both tests skip the 12 GB download if you hard-link a model you already have into the app's data folder first:
`mklink /H <data folder>\model\_model.gguf src\data\model\_model.gguf`.

**The portable zip.** Unzip it somewhere new. Its data folder is `data\` beside `2B.exe`.

**The Store package.** With Developer Mode on, register the staged app. That avoids installing the unsigned MSIX:

```powershell
Add-AppxPackage -Register "build\Agent 2B\AppxManifest.xml"
```

Start **Agent 2B** from the Start menu. Its data folder is `%LOCALAPPDATA%\Packages\<package family name>\LocalCache\`.

In each case, check that:
- the window shows the GPU. Without a model, Download model fills the progress bar, Cancel keeps what was downloaded,
  and Resume download continues from there.
- Chat loads the model and opens the chat page. A message gets an answer, and **← 2B** goes back.
- Terminal opens Git Bash with the agent. A request gets an answer, and Bash and Edit work after asking. `/exit`
  leaves a prompt where `2b` starts the agent again.
- with the window and a terminal open, closing either one leaves `llama-server.exe` running, and closing the other
  stops it.

To look inside the window, start `2B.exe` with `AGENT_2B_DEVTOOLS=1` set, which turns on DevTools (F12).

Remove the Store app afterwards with `Get-AppxPackage <identity name> | Remove-AppxPackage`. That also deletes its data
folder.

### Fill in the Partner Center submission

- **Restricted capabilities.** The package declares two, and the submission asks why:
  - `runFullTrust`: 2B is a desktop app (a terminal, Node.js, Git Bash and llama.cpp).
  - `unvirtualizedResources`: suggested wording: "Agent 2B is a coding agent that runs commands in a terminal on the
    user's behalf, such as git, npm and pip. With write virtualization, their changes to AppData and the registry
    would be hidden from the rest of the system, so tools installed or configured in 2B wouldn't work outside it."
- **System requirements.** An NVIDIA GPU (RTX 5060 or better) with a driver for CUDA 12.4 or later, and about 13 GB
  of free disk space for the model download. Say both in the description too, along with the first-run download.
- **Privacy policy URL:** [PRIVACY.md](../PRIVACY.md) on GitHub.

## Explanation

**One app, two places for its data.** `2B.exe` ([`src/launcher/launcher.cs`](../src/launcher/launcher.cs)) is the
entry point of both: the window with Download model, Chat and Terminal. The portable app keeps the model, logs and
chat history in `data/` beside it. The Store package is installed read-only (under `C:\Program Files\WindowsApps\`),
so there `2B.exe` uses the package's `LocalCache` folder instead, which is deleted when the app is uninstalled. It
passes the folder on in `AGENT_2B_DATA`, which [`src/agent/paths.ts`](../src/agent/paths.ts) reads.

**Why write virtualization is off.** A packaged app's processes, and every process they start, normally have their
writes to AppData and the registry redirected into a private copy. For 2B that would include everything the user
and the agent run in its terminal. The manifest turns that off (`desktop6:FileSystemWriteVirtualization` and
`desktop6:RegistryWriteVirtualization`), which needs the `unvirtualizedResources` capability.

**Why the build needs no new tools.** It runs on the Node.js in `src/runtime/`. It compiles `2B.exe` with the C#
compiler that comes with Windows (.NET Framework 4), against the WebView2 SDK DLLs that `tools\fetch.cmd` downloads.
It zips with Windows' `tar.exe` and draws the logos from `src/launcher/app-icon.svg` with Edge. Only `makeappx.exe`
comes from somewhere else. For a change to the window alone, `tools/build.ts --exe` builds just `src/2B.exe`.

## Reference

### Release artifacts

| File | Where it goes |
|---|---|
| `dist/release/Agent-2B-portable.zip` | GitHub release |
| `dist/store/Agent 2B.msix` | Partner Center (only with `--msix`) |

`build/` holds intermediates: `build/Agent 2B/` is the staged app (`src/` without `data/`, plus `LICENSE`), and
`build/downloads/` holds `tools\fetch.cmd`'s unfinished downloads. `--clean` deletes both, along with `dist/`.

### Build prerequisites

- `tools\fetch.cmd` has run, so `src/runtime/` has `node`, `git`, `llama` and `webview2`.
- For `--msix` only: `makeappx.exe`, from either the full
  [Windows SDK](https://developer.microsoft.com/windows/downloads/windows-sdk/) or, much smaller (~22 MB), the
  [Microsoft.Windows.SDK.BuildTools](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools) NuGet package:
  download the `.nupkg`, rename it to `.zip`, and unzip it to `%LOCALAPPDATA%\Agent 2B Build\sdk-buildtools\`.
  No install or admin rights needed.

### Files

| File | Role |
|---|---|
| [`tools/fetch.cmd`](../tools/fetch.cmd) | Downloads Node.js, Git Bash, llama.cpp and the WebView2 SDK into `src/runtime/` |
| [`tools/build.ts`](../tools/build.ts) | Builds `src/2B.exe` and the portable zip, with `--msix` the Store package too, with `--exe` only `2B.exe` |
| [`tools/AppxManifest.xml`](../tools/AppxManifest.xml) | The package manifest; `build.ts` fills in the identity and version |
| [`src/launcher/launcher.cs`](../src/launcher/launcher.cs) | `2B.exe`, the app's window and entry point |
| [`src/launcher/home.html`](../src/launcher/home.html) | The page the window shows |
