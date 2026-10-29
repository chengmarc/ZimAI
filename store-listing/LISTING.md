# Microsoft Store submission — ZimAI

Everything to paste into Partner Center → ZimAI, in the submission's order: Properties, Store listings (English
(United States)), Submission options. Pricing and availability, Age ratings and Packages are filled in Partner Center
by hand. Images are beside this file (the screenshots in [desktop/](desktop/)), rendered by
`powershell -ExecutionPolicy Bypass -File store-listing\generator\render.ps1`.

## Properties

- **Category:** Developer tools. **Subcategory:** none.
- **Privacy policy:** paste [PRIVACY.md](PRIVACY.md) as it is; it's plain text, written for Partner Center's box.
- **Website:** `https://github.com/chengmarc/ZimAI`
- **Support contact info:** `https://github.com/chengmarc/ZimAI/issues`
- **System requirements:** what the Store shows under the listing, and a warning on a PC that doesn't meet the
  minimum. It doesn't stop the install; ZimAI's own check does that (`Requirements` in `src/app/app.cs`, which these
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

ZimAI is a free AI that runs on your own PC, for the jobs your cloud AI's limits shouldn't go to. Keep your subscription for the big work; hand ZimAI the small stuff, as often as you like. It runs on your NVIDIA GPU, or on the CPU if you don't have one, so there are no credits, no rate limits, and nothing you ask or open leaves your PC. You don't need an account, an API key or a plan.

Hand it the small jobs
• Finished a big rework and out of quota? Ask it to draft the commit message from your changes.
• "Where is this function used?", "rename these files", "what does this error mean?", "add this folder to .gitignore": the quick jobs that aren't worth your cloud AI's limits.
• Ask about your PC, too: "my C: drive is 97% full, what's eating the space?" gets you the biggest folders, first to last.
• It reads your files, runs commands in Git Bash and searches the web when it needs to, and follows your project's AGENTS.md or CLAUDE.md.

No meter running
• No credits to buy and no daily cap: use it a hundred times a day, or let it work through a pile of small jobs.
• It sits beside the AI you already use; it doesn't ask you to switch.

Stay in control of every change
• Anything that changes a file, runs a command or opens a web page waits for your yes.
• You see each edit as a diff before it's made, and after a no you can tell it why.
• Reading files and searching your code happen without asking, so simple questions stay quick.

Quick questions in Chat
• Ask everyday questions in the app's own window, as you would any chatbot, with no quota.
• Your chats are saved on your PC and nowhere else.

On the PC you already have: the GPU you game on, or just the CPU
• You don't need a workstation or data-center card: a GeForce from a GTX 1060 6 GB to an RTX 5090 is enough.
• No NVIDIA GPU? It runs on the CPU with 16 GB of RAM, much slower, but it works.
• Want the GPU for a game? Turn off Use GPU and ZimAI moves to the CPU.
• Before anything downloads, the app checks your PC and tells you plainly whether it can run it.
• There's nothing to configure: the model is split between your GPU's memory and your RAM to fit whatever is free. Loading takes about 30 seconds on a GPU.

Download once, then work offline
• The model is a single 12 GB download from Hugging Face on first run. If it's interrupted, it resumes, even after a restart.
• After that, you can work without an internet connection. Only web searches, and pages and commands you approve, go online.

Free, with nothing to sign up for
No account, no API key, no subscription, no ads and no telemetry.

Know the limits
The model is gpt-oss-20b: 20 billion parameters, about 3.6 billion active per token. It's made for small jobs, not to replace your cloud AI: give it tight, scoped tasks in a codebase or on your machine, and keep the big, open-ended ones for the cloud.

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
Use AI for small jobs as often as you like: no credits, no rate limits
Hand off the jobs not worth your cloud AI's quota, like a commit message after a big rework
Have it read, edit and run commands in your projects and on your PC
Keep your code on your PC: nothing you ask or open is sent to an AI service
Approve every edit, command and web page first, with each edit shown as a diff
Use the GeForce you game on, from a GTX 1060 6 GB to an RTX 5090, or just the CPU
Chat with the model in the app's own window, with your chats saved only on your PC
Find out whether your PC can run it before anything downloads
Skip the setup: the model is fitted to your GPU and RAM automatically
Resume the 12 GB model download if it's interrupted, even after a restart
Work offline once the model is downloaded
Use it with no account, no API key and no subscription
Read and change every line: it's open source under Apache 2.0
```

### Screenshots

Upload in this order (the first is shown first):

1. `desktop/screenshot-1-promo.png`: out of credits? not here (the terminal, and what it costs: nothing)
2. `desktop/screenshot-2-chat.png`: quick questions with no quota (Chat)
3. `desktop/screenshot-3-home.png`: on the PC you already have (the GPUs it runs on, and the home page)

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
  Unlimited AI for small jobs. When your cloud AI is out of credits, or the job isn't worth it, ZimAI reads, edits and runs things in your projects on your own PC, asking before every change. No limits, no account. Fastest on an NVIDIA GPU; runs on the CPU too.
  ```

### Additional information

- **Keywords** (up to 7, 40 characters each, 21 words in all):

  ```
  unlimited AI
  free AI
  local AI
  offline AI
  AI coding assistant
  local LLM
  gpt-oss
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
