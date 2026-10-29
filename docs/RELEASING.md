# Releasing

ZimAI ships two ways, both built from `src/`:

- **Portable, on GitHub:** `ZimAI-portable.zip`, ready to run. The user unzips it anywhere, a flash drive
  included, and double-clicks `ZimAI.exe`.
- **Microsoft Store:** an MSIX package with the same contents.

Neither includes the model: the app downloads it (Download model).

The tools that build them are PowerShell scripts in [`packaging/`](../packaging/). They need only what comes with Windows,
plus `makeappx.exe` for the Store package. Nothing in `packaging/` ships inside the app. Run them from the repo root with
`powershell -ExecutionPolicy Bypass -File packaging\<script>.ps1`, since Windows doesn't run unsigned scripts by default.

## How-to guides

### Cut a release

1. Pick the version and set it in `AssemblyInformationalVersion` at the top of
   [`src/app/Program.cs`](../src/app/Program.cs). The window shows it, the Store package gets it, and the git tag is
   `v<version>`. The Store needs every new submission to have a higher version than the last.
2. Run `packaging\fetch.ps1` so `src/runtime/` is complete. It only downloads what's missing.
3. Build both artifacts (see [Build prerequisites](#build-prerequisites)):

   ```powershell
   powershell -ExecutionPolicy Bypass -File packaging\build.ps1 -Clean -Msix
   ```

   This takes about ten minutes: five to zip and five to pack.
4. Smoke-test the portable zip: unzip it somewhere new and double-click `ZimAI.exe`. Then smoke-test the Store package
   (below).
5. Write what's new in `docs/release-notes/v<version>.md`, for customers, not developers: what they'll notice since the
   last release, and how to get it. `git log v<last>..HEAD` lists what changed.
6. Tag the release, push the tag, and publish a GitHub release named `ZimAI <version>` from those notes, with
   `dist/ZimAI-portable.zip` attached. GitHub takes release files up to 2 GB; the zip is about 860 MB.

   ```powershell
   gh release create v<version> dist\ZimAI-portable.zip --title "ZimAI <version>" --notes-file docs\release-notes\v<version>.md
   ```

7. Upload `dist/ZimAI.msix` in a new Partner Center submission, with the product name ZimAI, and paste a short form of
   the notes into "What's new in this version" ([`store-listing/LISTING.md`](../store-listing/LISTING.md) keeps it). If
   the window or the demo changed, re-render the listing images first (below).

### Try a change to the app

`packaging\build.ps1 -Exe` builds only `src/ZimAI.exe`, in a few seconds. Then double-click it. Changes to `home.html` or
`palette.css` need no build: reopen the window. To look inside the window, start `ZimAI.exe` with `ZIMAI_DEVTOOLS=1`
set, which turns on DevTools (F12).

### Set up the Store identity (once)

Copy the three values from Partner Center > the app > Product identity into `$IdentityName`, `$Publisher` and
`$PublisherDisplayName` at the top of `packaging\build.ps1`. `$DisplayName` must match the name reserved there. Until
those are filled in, `-Msix` stops before building anything.

### Smoke-test a build

Both tests skip the 12 GB download if you hard-link a model you already have into the app's data folder first:
`mklink /H <data folder>\model\_model.gguf appdata\model\_model.gguf`.

**The portable zip.** Unzip it somewhere new. Its data folder is `data\` beside `ZimAI.exe`.

**The Store package.** With Developer Mode on, register the staged app. That avoids installing the unsigned MSIX:

```powershell
Add-AppxPackage -Register "build\ZimAI\AppxManifest.xml"
```

Start **ZimAI** from the Start menu. Its data folder is `%LOCALAPPDATA%\Packages\<package family name>\LocalCache\`.

In each case, check that:
- the window shows the GPU and the version. Without a model, Download model fills the progress bar, Cancel keeps
  what was downloaded, and Resume download continues from there.
- Chat loads the model and opens the chat page. A message gets an answer, and **← ZimAI** goes back.
- Terminal opens Git Bash with the agent. A request gets an answer, and Bash and Edit work after asking. `/exit`
  leaves a prompt where `zim` starts the agent again.
- with the window and a terminal open, closing either one leaves `llama-server.exe` running, and closing the other
  stops it within a few seconds.
- in the Store app, the terminal runs outside the package: `mkdir "$APPDATA/zimai-check"` there makes a folder that
  Explorer shows in `%APPDATA%` (delete it afterwards).

Remove the Store app afterwards with `Get-AppxPackage <identity name> | Remove-AppxPackage`. That also deletes its data
folder.

### Fill in the Partner Center submission

[`store-listing/LISTING.md`](../store-listing/LISTING.md) has every field to paste, in Partner Center's order: the description,
features, keywords, system requirements, the wording for the restricted capability (`runFullTrust`) and the notes
for certification. Its images are beside it: the logos in `store-listing/`, the screenshots in
`store-listing/desktop/`.

### Re-render the listing images

```powershell
powershell -ExecutionPolicy Bypass -File store-listing\generator\render.ps1
```

It redraws the images in `store-listing/` with Microsoft Edge in headless mode, from `generator/logo.html` (box
art, poster art, tile icon) and `generator/shots.html` (the screenshots). The screenshots show the real home page, `src/ui/home.html`, drawing
the state written in `shots.html`, the README's terminal demo, `docs/demo.svg`, stopped at a moment, and two captures
of the Chat page (below). Their lines come from `docs/POSITIONING.md`. Change the captions or the state in
`shots.html`, and the system requirements in `LISTING.md` when `Requirements` in `src/app/Requirements.cs` changes.

When `src/ui/icon.svg` changes, rebuild `icon.ico` first, then re-render:

```powershell
powershell -ExecutionPolicy Bypass -File store-listing\generator\icon.ps1
```

The Chat screenshots are captures: `store-listing/generator/chat-*.png`, since the Chat page is llama-server's own
and only runs with the model loaded. Retake them when that page, `src/ui/chat.css` or the window's script for it
(`ChatScript` in `src/app/Window.cs`) changes, then re-render. Open ZimAI, click Start, wait for Server Ready, and run:

```powershell
powershell -ExecutionPolicy Bypass -File store-listing\generator\chat.ps1
```

It sends each capture's message (in the script) to the real model in a headless Edge with a profile of its own, so the
app's chat history is untouched, and saves the window's contents at 960 × 640, 2x. The answers differ each run: look
at both before re-rendering, and run it again if one doesn't fit the window.

## Explanation

**One app, three places for its data.** The portable app keeps its data (the model, chat history, logs) in `data/`
beside `ZimAI.exe`. The Store package is installed read-only, so there `ZimAI.exe` uses the package's `LocalCache` folder,
which is deleted when the app is uninstalled. Run from a clone of the repo, `src\ZimAI.exe` uses `appdata/` at the
repo's root, so 12 GB of model and your chat history stay out of the source tree. [`src/README.md`](../src/README.md) has the layout.

**Why the terminal runs outside the package.** A packaged app's processes, and every process they start, have
their new files in AppData and their writes to HKCU redirected into a private copy, which only the package sees and
which is deleted with it. For ZimAI that would include everything the user and the agent run in its terminal
(`npm install -g`, `pip install --user`, `setx`). So the window starts the terminal outside the package, with the
desktop app breakaway policy (`Terminal` and `Breakaway` in `src/app/Terminal.cs`). Turning virtualization off in the
manifest instead needs the `unvirtualizedResources` capability, which the Store denied for ZimAI. Breakaway only works
through a process from outside the package (cmd): one of the package's own executables started directly stays
inside.

**Why PowerShell.** The tools have to run before anything is downloaded, so they use only what Windows has:
PowerShell 5.1, `curl.exe` and `tar.exe`, the C# compiler of .NET Framework 4 (for `ZimAI.exe`), and System.Drawing
(for the Store logos, scaled from `src/ui/icon.ico`). They don't use the Node.js in `src/runtime/`, which belongs to
the app.

## Reference

### Release artifacts

| File | Where it goes |
|---|---|
| `dist/ZimAI-portable.zip` | GitHub release |
| `dist/ZimAI.msix` | Partner Center (only with `-Msix`) |

`build/` holds intermediates: `build/ZimAI/` is the staged app (`src/` without `data/`, plus `LICENSE`), and
`build/downloads/` holds `packaging\fetch.ps1`'s unfinished downloads. `-Clean` deletes both, along with `dist/`.

### Build prerequisites

- `packaging\fetch.ps1` has run, so `src/runtime/` has `node`, `git`, `llama` and `webview2`.
- For `-Msix` only: `makeappx.exe`, from either the full
  [Windows SDK](https://developer.microsoft.com/windows/downloads/windows-sdk/) or, much smaller (~22 MB), the
  [Microsoft.Windows.SDK.BuildTools](https://www.nuget.org/packages/Microsoft.Windows.SDK.BuildTools) NuGet package:
  download the `.nupkg`, rename it to `.zip`, and unzip it to `%LOCALAPPDATA%\ZimAI Build\sdk-buildtools\`.
  No install or admin rights needed.

### Files

| File | Role |
|---|---|
| [`packaging/fetch.ps1`](../packaging/fetch.ps1) | Downloads Node.js, Git Bash, llama.cpp and the WebView2 SDK into `src/runtime/` |
| [`packaging/build.ps1`](../packaging/build.ps1) | Builds `src/ZimAI.exe` and the portable zip; `-Msix` adds the Store package, `-Exe` builds only `ZimAI.exe` |
| [`packaging/AppxManifest.xml`](../packaging/AppxManifest.xml) | The package manifest; `build.ps1` fills in the identity and version |
| [`store-listing/LISTING.md`](../store-listing/LISTING.md) | The Store listing's text, field by field |
| [`store-listing/generator/render.ps1`](../store-listing/generator/render.ps1) | Renders the listing's images into `store-listing/` |
| [`store-listing/generator/icon.ps1`](../store-listing/generator/icon.ps1) | Builds `src/ui/icon.ico` from `src/ui/icon.svg`, with bolder frames under 64 pixels |
