# Microsoft Store submission — Agent 2B

Everything to paste into Partner Center → Agent 2B, in the submission's order: Properties, Store listings (English
(United States)), Submission options. Pricing and availability, Age ratings and Packages are filled in Partner Center
by hand. Images are in [listing/](listing/), rendered by `powershell -ExecutionPolicy Bypass -File store\render.ps1`.

## Properties

- **Category:** Developer tools. **Subcategory:** none.
- **Privacy policy:** paste [PRIVACY.md](PRIVACY.md) as it is; it's plain text, written for Partner Center's box.
- **Website:** `https://github.com/chengmarc/agent-2b`
- **Support contact info:** `https://github.com/chengmarc/agent-2b/issues`
- **System requirements:** what the Store shows under the listing, and a warning on a PC that doesn't meet the
  minimum. It doesn't stop the install; 2B's own check does that (`Requirements` in `src/app/app.cs`, which these
  follow). Leave every other row unchecked: no touch, camera, microphone, Bluetooth or the like.

  | | Minimum hardware | Recommended hardware |
  |---|---|---|
  | Keyboard | ✓ (the terminal is typed into) | |
  | Memory | 16 GB | 16 GB |
  | Video memory | 6 GB | the largest option offered (8 GB or more) |
  | Processor | `64-bit (x64)` | |
  | Graphics | `NVIDIA GTX 1060 6 GB or better, CUDA 12.4+` | `NVIDIA RTX, 8 GB VRAM or more` |
  | DirectX | leave empty: 2B uses CUDA, not DirectX | |

  Minimum memory is 16 GB, though 8 GB is enough with a 12 GB GPU: the Store has one value per row, and most PCs
  with 8 GB of RAM can't run 2B. The text in Additional system requirements (below) says so.

- **Product declarations:** none apply. Leave "This product has been tested to meet accessibility guidelines"
  unchecked unless it has been.

## Store listings — English (United States)

### Description

```
Use an AI coding agent without sending your code to the cloud, and without a subscription.

Agent 2B gives you a coding agent that runs on the gaming PC you already have. Point it at a project and it reads the code, edits files and runs commands for you on your own NVIDIA GPU, so what you ask and the files it reads never leave your PC. You don't need an account, an API key or a plan.

Hand it a task in your project
• Ask it to find a bug, explain a codebase, write a script or tidy a folder. It reads your files, runs commands in Git Bash and searches the web when it needs to.
• Ask about your PC, too: "my C: drive is 97% full, what's eating the space?" gets you the biggest folders, first to last.
• It reads your project's AGENTS.md or CLAUDE.md, so it follows the rules you've already written down.

Stay in control of every change
• Anything that changes a file, runs a command or opens a web page waits for your yes.
• You see each edit as a diff before it's made, and after a no you can tell it why.
• Reading files and searching your code happen without asking, so simple questions stay quick.

Talk it through in Chat
• Ask everyday questions in the app's own window, as you would any chatbot.
• Your chats are saved on your PC and nowhere else.

Use the GPU you game on
• You don't need a workstation or data-center card: a GeForce from a GTX 1060 6 GB to an RTX 5090 is enough.
• Before anything downloads, the app checks your PC and tells you plainly whether it can run it.
• There's nothing to configure: the model is split between your GPU's memory and your RAM to fit whatever is free. Loading takes about 30 seconds.

Download once, then work offline
• The model is a single 12 GB download from Hugging Face on first run. If it's interrupted, it resumes, even after a restart.
• After that, you can work without an internet connection. Only web searches, and pages and commands you approve, go online.

Free, with nothing to sign up for
No account, no API key, no subscription, no ads and no telemetry.

Know the limits
The model is gpt-oss-20b: 20 billion parameters, about 3.6 billion active per token. You'll get the best results from small, scoped tasks in a codebase or on your machine; give it tight tasks rather than open-ended ones.

Requirements
• Windows 10 or 11, 64-bit
• An NVIDIA GPU with 6 GB of video memory or more: a GTX 1060 6 GB, a GTX 1660, or any desktop RTX. 8 GB or more is better; with 16 GB the whole model fits on the GPU.
• An NVIDIA driver with CUDA 12.4 or later
• 16 GB of RAM (8 GB with a 12 GB GPU)
• 13 GB of free disk space for the model

AMD and Intel graphics aren't supported.

Agent 2B is open source under the Apache 2.0 license.

NVIDIA, GeForce, GTX and RTX are trademarks of NVIDIA Corporation. Agent 2B is an independent project, not affiliated with or endorsed by NVIDIA, OpenAI or Hugging Face.
```

### What's new in this version

Leave blank until a version is in the Store: 1.0.0 was never published (the Store denied `unvirtualizedResources`),
so 1.0.1 is still the first version customers see.

### Product features

One per line in Partner Center ("Add more" for each); each finishes "With this app, you can …":

```
Hand an AI coding agent tasks in your projects: it reads, edits and runs commands
Keep your code on your PC: nothing you ask or open is sent to an AI service
Approve every edit, command and web page first, with each edit shown as a diff
Use the GeForce you game on, from a GTX 1060 6 GB to an RTX 5090
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

1. `listing/screenshot-1-promo.png`: an AI agent for your code that stays on your PC (the terminal)
2. `listing/screenshot-2-chat.png`: everyday questions answered on your PC (Chat)
3. `listing/screenshot-3-home.png`: will my PC run it? (the GPUs it runs on, and the home page)

### Store logos

| Slot | File |
|---|---|
| 9:16 Poster art (1440 × 2160) | `listing/poster-art-1440x2160.png`: the main logo on Windows 10/11 |
| 1:1 Box art (2160 × 2160) | `listing/box-art-2160.png` |
| 1:1 App tile icon (300 × 300) | `listing/store-logo-300.png` |
| 150 × 150, 71 × 71 | Leave empty; the Store takes them from the package. |

### Trailers, Xbox images

Leave empty: 2B has no trailer, and isn't an Xbox app.

### Supplemental fields

- **Short title:** leave empty; "Agent 2B" is short already.
- **Voice title:** leave empty, for the same reason.
- **Short description** (270 characters or fewer):

  ```
  Use an AI coding agent without sending your code to the cloud. It reads, edits and runs things in your projects on your own GeForce GPU, asking before every change. No account, no subscription. Needs an NVIDIA GPU with 6 GB+ of video memory.
  ```

### Additional information

- **Keywords** (up to 7, 40 characters each, 21 words in all):

  ```
  AI coding agent
  local AI
  offline AI
  local LLM
  private AI assistant
  gpt-oss
  llama.cpp
  ```

- **Additional system requirements** (each line one item, 200 characters each):

  Minimum hardware:

  ```
  NVIDIA GPU with 6 GB of video memory or more: GTX 1060 6 GB, GTX 1660 or any desktop RTX
  NVIDIA driver with CUDA 12.4 or later
  16 GB of RAM, or 8 GB with a 12 GB GPU
  13 GB of free disk space for the model, downloaded on first run
  AMD and Intel graphics aren't supported
  ```

  Recommended hardware:

  ```
  NVIDIA RTX GPU with 8 GB of video memory or more
  With 16 GB of video memory, the whole model runs on the GPU
  ```

- **Copyright and trademark info:**

  ```
  © 2026 chengmarc. NVIDIA, GeForce, GTX and RTX are trademarks of NVIDIA Corporation.
  ```

- **Additional license terms:** `Apache License 2.0: https://github.com/chengmarc/agent-2b/blob/main/LICENSE`
- **Developed by:** `chengmarc`

## Submission options

- **Publishing hold options:** publish as soon as it passes certification, unless a launch post is planned; then
  hold it and publish on a weekday morning (US time), when developers browse the Store and GitHub most.
- **Restricted capabilities → runFullTrust:**

  ```
  Agent 2B is a desktop app built with C# (Windows Forms and WebView2), packaged with the Desktop Bridge. runFullTrust is required for any packaged Win32 desktop app to launch its executable (EntryPoint="Windows.FullTrustApplication"). It is used only to run the app itself: 2B.exe, and the llama.cpp model server, Node.js and Git Bash it ships with. It reads and writes files only in the folders the user works in, asking before every change, and it has no telemetry. The terminal (Git Bash) is started outside the package with the documented desktop app breakaway policy (PROC_THREAD_ATTRIBUTE_DESKTOP_APP_POLICY), so the commands the user approves there behave as in any terminal; the app requests no other restricted capability.
  ```

- **Notes for certification:**

  ```
  No account or sign-in is needed, and there's nothing to configure: 2B picks the model (gpt-oss-20b) and its settings itself. On a supported PC (an NVIDIA GPU with 6 GB of video memory or more and CUDA 12.4 or later): 1. Click Download in the Model row (12 GB from Hugging Face; if it's interrupted, click Resume). 2. Wait until the Server row says Ready (about 30 seconds). 3. Click Terminal and type "list the files in this folder"; it asks before running the command (answer y) and then lists them. Or click Chat and ask a question in the window. On a PC without a supported GPU, the System row says "No GPU Detected" or "Not Supported" with the reason, and nothing is downloaded. Since the last submission: the unvirtualizedResources capability, which was denied, has been removed, and the package declares only runFullTrust.
  ```
