# Releasing

2B ships two ways, both built from `src/`:

- **Portable, on GitHub:** `Agent-2B-portable.zip`, ready to run. The user unzips it anywhere, a flash drive
  included, and double-clicks `2B.exe`.
- **Microsoft Store:** an MSIX package with the same contents.

Neither includes the model: the app downloads it (Download model).

The tools that build them are PowerShell scripts in [`packaging/`](../packaging/). They need only what comes with Windows,
plus `makeappx.exe` for the Store package. Nothing in `packaging/` ships inside the app. Run them from the repo root with
`powershell -ExecutionPolicy Bypass -File packaging\<script>.ps1`, since Windows doesn't run unsigned scripts by default.

## How-to guides

### Cut a release

1. Pick the version and set it in `AssemblyInformationalVersion` at the top of
   [`src/app/app.cs`](../src/app/app.cs). The window shows it, the Store package gets it, and the git tag is
   `v<version>`. The Store needs every new submission to have a higher version than the last.
2. Run `packaging\fetch.ps1` so `src/runtime/` is complete. It only downloads what's missing.
3. Build both artifacts (see [Build prerequisites](#build-prerequisites)):

   ```powershell
   powershell -ExecutionPolicy Bypass -File packaging\build.ps1 -Clean -Msix
   ```

   This takes about ten minutes: five to zip and five to pack.
4. Smoke-test the portable zip: unzip it somewhere new and double-click `2B.exe`. Then smoke-test the Store package
   (below).
5. Tag the release, push the tag, and publish a GitHub release with `dist/release/Agent-2B-portable.zip` attached.
   GitHub takes release files up to 2 GB; the zip is about 860 MB.
6. Upload `dist/store/Agent 2B.msix` in a new Partner Center submission. If the window or the demo changed, re-render
   the listing images first (below).

### Try a change to the app

`packaging\build.ps1 -Exe` builds only `src/2B.exe`, in a few seconds. Then double-click it. Changes to `home.html` or
`palette.css` need no build: reopen the window. To look inside the window, start `2B.exe` with `AGENT_2B_DEVTOOLS=1`
set, which turns on DevTools (F12).

### Set up the Store identity (once)

Copy the three values from Partner Center > the app > Product identity into `$IdentityName`, `$Publisher` and
`$PublisherDisplayName` at the top of `packaging\build.ps1`. `$DisplayName` must match the name reserved there. Until
those are filled in, `-Msix` stops before building anything.

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
- the window shows the GPU and the version. Without a model, Download model fills the progress bar, Cancel keeps
  what was downloaded, and Resume download continues from there.
- Chat loads the model and opens the chat page. A message gets an answer, and **← 2B** goes back.
- Terminal opens Git Bash with the agent. A request gets an answer, and Bash and Edit work after asking. `/exit`
  leaves a prompt where `2b` starts the agent again.
- with the window and a terminal open, closing either one leaves `llama-server.exe` running, and closing the other
  stops it within a few seconds.
- in the Store app, the terminal runs outside the package: `mkdir "$APPDATA/2b-check"` there makes a folder that
  Explorer shows in `%APPDATA%` (delete it afterwards).

Remove the Store app afterwards with `Get-AppxPackage <identity name> | Remove-AppxPackage`. That also deletes its data
folder.

### Fill in the Partner Center submission

[`store/LISTING.md`](../store/LISTING.md) has every field to paste, in Partner Center's order: the description,
features, keywords, system requirements, the wording for the restricted capability (`runFullTrust`) and the notes
for certification. Its images are in `store/listing/`.

### Re-render the listing images

```powershell
powershell -ExecutionPolicy Bypass -File store\render.ps1
```

It redraws `store/listing/` with Microsoft Edge in headless mode, from `store/logo.html` (box art, poster art, tile
icon) and `store/shots.html` (the screenshots). The screenshots show the real home page, `src/app/home.html`, drawing
the state written in `shots.html`, and the README's terminal demo, `docs/demo.svg`, stopped at a moment. Change the
captions or the state there, and the system requirements in `LISTING.md` when `Requirements` in `src/app/app.cs`
changes.

The Chat screenshot is the exception: `store/chat.png` is a capture, since the Chat page is llama-server's own and
only runs with the model loaded. Retake it when that page or `src/app/chat.css` changes, then re-render. In the app,
click Chat, start a new chat and send:

```text
Three of us got dinner: I had a $22 pasta, Sam had a $38 steak, Lee had a $15 salad. We shared $30 of wine, and tip is 18%. What does each person owe?
```

Once the answer has finished, capture the window's contents without its title bar at 200% scaling, so the file is
1880 × 1360 (940 × 680 at 2x), and save it over `store/chat.png`.

## Explanation

**One app, two places for its data.** The portable app keeps its data (the model, chat history, logs) in `data/`
beside `2B.exe`. The Store package is installed read-only, so there `2B.exe` uses the package's `LocalCache` folder,
which is deleted when the app is uninstalled. [`src/README.md`](../src/README.md) has the layout.

**Why the terminal runs outside the package.** A packaged app's processes, and every process they start, have
their new files in AppData and their writes to HKCU redirected into a private copy, which only the package sees and
which is deleted with it. For 2B that would include everything the user and the agent run in its terminal
(`npm install -g`, `pip install --user`, `setx`). So the window starts the terminal outside the package, with the
desktop app breakaway policy (`Terminal` and `Breakaway` in `src/app/app.cs`). Turning virtualization off in the
manifest instead needs the `unvirtualizedResources` capability, which the Store denied for 2B. Breakaway only works
through a process from outside the package (cmd): one of the package's own executables started directly stays
inside.

**Why PowerShell.** The tools have to run before anything is downloaded, so they use only what Windows has:
PowerShell 5.1, `curl.exe` and `tar.exe`, the C# compiler of .NET Framework 4 (for `2B.exe`), and System.Drawing
(for the Store logos, scaled from `src/app/icon.ico`). They don't use the Node.js in `src/runtime/`, which belongs to
the app.

## Reference

### Release artifacts

| File | Where it goes |
|---|---|
| `dist/release/Agent-2B-portable.zip` | GitHub release |
| `dist/store/Agent 2B.msix` | Partner Center (only with `-Msix`) |

`build/` holds intermediates: `build/Agent 2B/` is the staged app (`src/` without `data/`, plus `LICENSE`), and
`build/downloads/` holds `packaging\fetch.ps1`'s unfinished downloads. `-Clean` deletes both, along with `dist/`.

### Build prerequisites

- `packaging\fetch.ps1` has run, so `src/runtime/` has `node`, `git`, `llama` and `webview2`.
- For `-Msix` only: `makeappx.exe`, from either the full
  [Windows SDK](https://developer.microsoft.com/windows/downloads/windows-sdk/) or, much smaller (~22 MB), the
  [Microsoft.Windows.SDK.BuildTools](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools) NuGet package:
  download the `.nupkg`, rename it to `.zip`, and unzip it to `%LOCALAPPDATA%\Agent 2B Build\sdk-buildtools\`.
  No install or admin rights needed.

### Files

| File | Role |
|---|---|
| [`packaging/fetch.ps1`](../packaging/fetch.ps1) | Downloads Node.js, Git Bash, llama.cpp and the WebView2 SDK into `src/runtime/` |
| [`packaging/build.ps1`](../packaging/build.ps1) | Builds `src/2B.exe` and the portable zip; `-Msix` adds the Store package, `-Exe` builds only `2B.exe` |
| [`packaging/AppxManifest.xml`](../packaging/AppxManifest.xml) | The package manifest; `build.ps1` fills in the identity and version |
| [`store/LISTING.md`](../store/LISTING.md) | The Store listing's text, field by field |
| [`store/render.ps1`](../store/render.ps1) | Renders the listing's images into `store/listing/` |
