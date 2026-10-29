# Microsoft Store submission — ZimAI

Everything to paste into Partner Center → ZimAI, in the submission's order: Properties, Store listings (English
(United States)), Submission options. Pricing and availability, Age ratings and Packages are filled in Partner Center
by hand. Images are beside this file (the screenshots in [desktop/](desktop/)), rendered by
`powershell -ExecutionPolicy Bypass -File store-listing\generator\render.ps1`.

## Properties

- **Category:** Productivity. **Subcategory:** none. (Developer tools until 2026-10-29: developers are now one of two
  audiences, [POSITIONING.md](../docs/POSITIONING.md).)
- **Privacy policy:** paste [PRIVACY.md](PRIVACY.md) as it is; it's plain text, written for Partner Center's box.
- **Website:** `https://github.com/chengmarc/ZimAI`
- **Support contact info:** `https://github.com/chengmarc/ZimAI/issues`
- **System requirements:** what the Store shows under the listing, and a warning on a PC that doesn't meet the
  minimum. It doesn't stop the install; ZimAI's own check does that (`Requirements` in `src/app/Requirements.cs`, which these
  follow). Leave every other row unchecked: no touch, camera, microphone, Bluetooth or the like.

  | | Minimum hardware | Recommended hardware |
  |---|---|---|
  | Keyboard | ✓ (the terminal is typed into) | |
  | Memory | 16 GB | 16 GB |
  | Video memory | leave unchecked: ZimAI runs on the CPU without a GPU | the largest option offered (8 GB or more) |
  | Processor | `64-bit (x64)` | |
  | Graphics | leave empty | `NVIDIA RTX, 8 GB VRAM or more` |
  | DirectX | leave empty: ZimAI uses CUDA, not DirectX | |

  Minimum memory is 16 GB, though 8 GB is enough with a 12 GB GPU: the Store has one value per row, and most PCs
  with 8 GB of RAM can't run ZimAI. The text in Additional system requirements (below) says so. There's no minimum GPU:
  without one, ZimAI runs on the CPU, much slower.

- **Product declarations:** none apply. Leave "This product has been tested to meet accessibility guidelines"
  unchecked unless it has been.

## Store listings — English (United States)

### Description

```
Unlimited AI for small jobs. Out of credits? Not here.

You're halfway through something and your AI stops: "You've reached your usage limit. Resets at 4:00 PM." You can wait hours, pay for the next plan up, or do it by hand, which you stopped doing a while ago.

ZimAI is the fourth option: keep going. It's a free AI that runs on your own PC, so there's no usage limit, no reset clock and no plan to upgrade. Keep your subscription for the big work, and hand ZimAI the small stuff, as often as you like.

Hand it the small jobs
• Reply to this email, reword this paragraph, summarize this, explain this: in the app's own Chat window.
• For developers, a terminal that works in your projects: the commit message after a big rework, "where is this function used?", rename these files, what does this error mean.
• Ask about your PC, too: "my C: drive is 97% full, what's eating the space?" gets you the biggest folders, first to last.

No meter running
• No usage limit, no credits, no daily cap: use it a hundred times a day.
• No account, no API key, no subscription, no ads and no telemetry.
• It sits beside the AI you already use; it doesn't ask you to switch.

Private by default
• It runs on your PC: nothing you ask, type or open is sent to an AI service.
• Your chats are saved on your PC and nowhere else.
• Once the model is downloaded, it works offline. Only web searches, and pages and commands you approve, go online.

You approve every change
• In the terminal, anything that changes a file, runs a command or opens a web page waits for your yes.
• You see each edit as a diff before it's made, and after a no you can tell it why.

On the PC you already have
• It runs on the NVIDIA GeForce you game on, from a GTX 1060 6 GB to an RTX 5090.
• No NVIDIA GPU? It runs on the CPU with 16 GB of RAM, much slower, but it works.
• Before anything downloads, the app checks your PC and tells you plainly whether it can run ZimAI.
• Nothing to set up: the model is fitted to your GPU and RAM automatically. It's one 12 GB download, and it resumes if interrupted, even after a restart.

Know the limits
ZimAI is built on gpt-oss-20b, an open-weight model: 20 billion parameters, about 3.6 billion active per token. It's made for small jobs, not to replace the AI you pay for: give it quick, clear tasks, and keep the big, open-ended ones for the cloud.

Requirements
• Windows 10 or 11, 64-bit
• 16 GB of RAM (8 GB with a 12 GB GPU)
• 13 GB of free disk space for the model
• For speed, an NVIDIA GPU with 6 GB of video memory or more: a GTX 1060 6 GB, a GTX 1660, or any desktop RTX. 8 GB or more is better; with 16 GB the whole model fits on the GPU. It needs a driver with CUDA 12.4 or later.

Without an NVIDIA GPU, including on AMD and Intel graphics, ZimAI runs on the CPU, much slower.

ZimAI is open source under the Apache 2.0 license.

NVIDIA, GeForce, GTX and RTX are trademarks of NVIDIA Corporation. ZimAI is an independent project, not affiliated with or endorsed by NVIDIA, OpenAI or Hugging Face.
```

### What's new in this version

Leave blank until a version is in the Store: 1.0.0 was never published (the Store denied `unvirtualizedResources`),
so 1.0.1 is still the first version customers see.

### Product features

One per line in Partner Center ("Add more" for each); each finishes "With this app, you can …":

```
Keep going when your AI hits its usage limit: ZimAI has none
Hand off small jobs as often as you like: emails, rewrites, summaries, quick questions
Skip the hours-long wait for a reset, and the upgrade to a bigger plan
Have it draft a commit message, find where code is used or rename files in your projects
Keep everything on your PC: nothing you ask or open is sent to an AI service
Approve every edit, command and web page first, with each edit shown as a diff
Use the GeForce you game on, from a GTX 1060 6 GB to an RTX 5090, or just the CPU
Find out whether your PC can run it before anything downloads
Skip the setup: the model is fitted to your GPU and RAM automatically
Work offline once the model is downloaded
Use it with no account, no API key and no subscription
Read and change every line: it's open source under Apache 2.0
```

### Screenshots

Upload in this order (the first is shown first). The line on each is fixed in
[POSITIONING.md](../docs/POSITIONING.md) → The four screenshots:

1. `desktop/screenshot-1-limit.png`: Hit your limit? Keep going. (a generic usage-limit banner, and ZimAI picking up the job)
2. `desktop/screenshot-2-chat.png`: Resets at 4 PM? Not here. (Chat finishing an everyday job)
3. `desktop/screenshot-3-terminal.png`: The AI that never says *come back later*. (the terminal, a developer's small job)
4. `desktop/screenshot-4-home.png`: Your AI ran out. Yours didn't. (the home page, on your own PC)

### Store logos

| Slot | File |
|---|---|
| 9:16 Poster art (1440 × 2160) | `poster-art-1440x2160.png`: the main logo on Windows 10/11 |
| 1:1 Box art (2160 × 2160) | `box-art-2160.png` |
| 1:1 App tile icon (300 × 300) | `store-logo-300.png` |
| 150 × 150, 71 × 71 | Leave empty; the Store takes them from the package. |

### Trailers, Xbox images

Leave empty: ZimAI has no trailer, and isn't an Xbox app.

### Supplemental fields

- **Short title:** leave empty; "ZimAI" is short already.
- **Voice title:** leave empty, for the same reason.
- **Short description** (270 characters or fewer):

  ```
  Hit your usage limit mid-task? Keep going. ZimAI is a free AI on your own PC for the small jobs: emails, rewrites, quick questions, commit messages. No usage limit, no account, nothing leaves your PC. Fastest on an NVIDIA GPU; runs on the CPU too.
  ```

### Additional information

- **Keywords** (up to 7, 40 characters each, 21 words in all):

  ```
  unlimited AI
  usage limit
  free AI
  local AI
  offline AI
  AI chat
  AI coding assistant
  ```

- **Additional system requirements** (each line one item, 200 characters each):

  Minimum hardware:

  ```
  16 GB of RAM, or 8 GB with a 12 GB GPU
  13 GB of free disk space for the model, downloaded on first run
  Without an NVIDIA GPU (AMD and Intel graphics included), it runs on the CPU, much slower
  ```

  Recommended hardware:

  ```
  NVIDIA RTX GPU with 8 GB of video memory or more, and a driver with CUDA 12.4 or later
  With 16 GB of video memory, the whole model runs on the GPU
  ```

- **Copyright and trademark info:**

  ```
  © 2026 chengmarc. NVIDIA, GeForce, GTX and RTX are trademarks of NVIDIA Corporation.
  ```

- **Additional license terms:** `Apache License 2.0: https://github.com/chengmarc/ZimAI/blob/main/LICENSE`
- **Developed by:** `chengmarc`

## Submission options

- **Publishing hold options:** publish as soon as it passes certification, unless a launch post is planned; then
  hold it and publish on a weekday morning (US time), when developers browse the Store and GitHub most.
- **Restricted capabilities → runFullTrust:**

  ```
  ZimAI is a desktop app built with C# (Windows Forms and WebView2), packaged with the Desktop Bridge. runFullTrust is required for any packaged Win32 desktop app to launch its executable (EntryPoint="Windows.FullTrustApplication"). It is used only to run the app itself: ZimAI.exe, and the llama.cpp model server, Node.js and Git Bash it ships with. It reads and writes files only in the folders the user works in, asking before every change, and it has no telemetry. The terminal (Git Bash) is started outside the package with the documented desktop app breakaway policy (PROC_THREAD_ATTRIBUTE_DESKTOP_APP_POLICY), so the commands the user approves there behave as in any terminal; the app requests no other restricted capability.
  ```

- **Notes for certification:**

  ```
  No account or sign-in is needed, and there's nothing to configure: ZimAI picks the model (gpt-oss-20b) and its settings itself. It runs on an NVIDIA GPU (6 GB of video memory or more, CUDA 12.4 or later) or, without one, on the CPU with 16 GB of RAM, much slower; the System row says GPU Available or CPU Only. 1. Click Download in the Model row (12 GB from Hugging Face; if it's interrupted, click Resume). 2. Wait until the Server row says Ready (about 30 seconds on a GPU, longer on the CPU). 3. Click Terminal and type "list the files in this folder"; it asks before running the command (answer y) and then lists them. Or click Chat and ask a question in the window. On a PC with less than 16 GB of RAM and no GPU ZimAI can use, the System row says "Not Supported" with the reason, and nothing is downloaded. Since the last submission: the unvirtualizedResources capability, which was denied, has been removed, and the package declares only runFullTrust.
  ```
