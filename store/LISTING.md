# Microsoft Store listing — English (United States)

Everything to paste into Partner Center → Agent 2B → Store listing, in the page's order, then the other fields the
submission asks for. Images are in [listing/](listing/), rendered by
`powershell -ExecutionPolicy Bypass -File store\render.ps1`.

## Description

```
AI in Your Flash Drive.

Agent 2B is an AI coding agent that runs entirely on the gaming PC you already have. The model, gpt-oss-20b, runs on your NVIDIA GPU through llama.cpp, so what you ask and the files it reads never leave your PC. No account, no API key, no subscription.

Two ways in
• Terminal: the coding agent in Git Bash. Point it at a project and it reads the code, edits files, runs commands, and searches the web when it needs to.
• Chat: talk with the model in the app's own window. Your chats stay on your computer.

It asks first
Anything that changes a file, runs a command or opens a web page waits for your yes. Edits are shown as a diff before they're made, and after a no you can tell it why. Reading files and searching your code don't need asking.

Runs on your gaming PC
No workstation or data-center card needed: the GeForce you game on is enough, from a GTX 1060 6 GB to an RTX 5090. 2B checks your GPU before anything is downloaded, and tells you plainly if your PC can't run it. Then it splits the model between your GPU's memory and your RAM to fit whatever is free, so there's nothing to configure. Loading takes about 30 seconds.

One download
The model is a 12 GB download from Hugging Face on first run. If it's interrupted, it resumes, even after a restart. After that the model works without an internet connection.

Know the limits
gpt-oss-20b is a 20-billion-parameter model (about 3.6 billion active per token). It does well at small, scoped tasks in a codebase or on your machine; give it tight tasks rather than open-ended ones.

Requirements
• Windows 10 or 11, 64-bit
• An NVIDIA GPU with 6 GB of video memory or more: a GTX 1060 6 GB, a GTX 1660, or any desktop RTX. 8 GB or more is better; with 16 GB the whole model fits on the GPU.
• An NVIDIA driver with CUDA 12.4 or later
• 16 GB of RAM (8 GB with a 12 GB GPU)
• 13 GB of free disk space for the model

AMD and Intel graphics aren't supported.

Agent 2B is open source under the Apache 2.0 license.
```

## What's new in this version

Leave blank: this is the first submission.

## Product features

One per line in Partner Center ("Add more" for each):

```
An AI coding agent that runs on your gaming PC
No workstation card needed: the GeForce you game on is enough
Nothing you ask or open is sent to an AI service
No account, no API key, no subscription
Terminal: reads, edits and runs things in your projects
Chat: talk with the model in the app's window
Asks before every edit, command and web page; edits shown as a diff
Checks your PC before downloading anything
Fits the model to your GPU and RAM automatically
One 12 GB model download that resumes if interrupted
Works offline once the model is downloaded
Open source (Apache 2.0)
```

## Screenshots

Upload in this order (the first is shown first):

1. `listing/screenshot-1-promo.png`: the coding agent
2. `listing/screenshot-2-chat.png`: the chatbot
3. `listing/screenshot-3-home.png`: the GPUs it runs on

## Store logos

| Slot | File |
|---|---|
| 9:16 Poster art (1440 × 2160) | `listing/poster-art-1440x2160.png`: the main logo on Windows 10/11 |
| 1:1 Box art (2160 × 2160) | `listing/box-art-2160.png` |
| 1:1 App tile icon (300 × 300) | `listing/store-logo-300.png` |
| 150 × 150, 71 × 71 | Leave empty; the Store takes them from the package. |

## Trailers, Xbox images

Leave empty.

## Supplemental fields

- **Short title:** leave empty.
- **Voice title:** leave empty.
- **Short description** (270 characters or fewer):

  ```
  AI in Your Flash Drive: a coding agent that runs on your gaming PC. It reads, edits and runs things in your projects, asking before every change, and nothing you ask leaves your PC. No account, no API key, no subscription. Needs an NVIDIA GPU with 6 GB+ of VRAM.
  ```

## Additional information

- **Keywords** (up to 7, 40 characters each, 21 words in all):

  ```
  local AI
  offline AI
  AI coding agent
  local LLM
  gpt-oss
  llama.cpp
  private AI assistant
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

- **Copyright and trademark info:** `© 2026 chengmarc`
- **Additional license terms:** `Apache License 2.0: https://github.com/chengmarc/agent-2b/blob/main/LICENSE`
- **Developed by:** `chengmarc`

## Properties

- **Category:** Developer tools. **Subcategory:** none.
- **Privacy policy URL:** `https://github.com/chengmarc/agent-2b/blob/main/PRIVACY.md`
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
  with 8 GB of RAM can't run 2B. The text in Additional system requirements (above) says so.

- **Product declarations:** none apply. Leave "This product has been tested to meet accessibility guidelines" unchecked unless it has been.

## Age ratings

In the questionnaire, the parts that apply: the app lets users talk with an AI model that generates text (Chat), and
it can open web pages and search the web, both after the user approves them. It has no user-to-user communication,
no purchases and no location sharing.

## Submission options

- **Restricted capabilities → runFullTrust:**

  ```
  Agent 2B is a desktop app built from a Win32 launcher (2B.exe) that starts Node.js, Git Bash and llama.cpp, packaged with the Desktop Bridge. runFullTrust is required for any packaged Win32 desktop app to launch its executable (EntryPoint="Windows.FullTrustApplication"). It runs the model locally on the user's GPU and opens a terminal where the coding agent works in the user's projects, asking before every file change, command and web page.
  ```

- **Restricted capabilities → unvirtualizedResources:**

  ```
  Agent 2B is a coding agent that runs commands in a terminal on the user's behalf, such as git, npm and pip. With write virtualization, their changes to AppData and the registry would be hidden from the rest of the system, so tools installed or configured in 2B wouldn't work outside it.
  ```

- **Notes for certification:**

  ```
  No account or sign-in is needed. Agent 2B needs an NVIDIA GPU with 6 GB of video memory or more and CUDA 12.4 or later; on a PC without one, the window says so (System: "No GPU Detected" or "Not Supported") and nothing is downloaded. On a supported PC: click Download model (12 GB from Hugging Face; it resumes if interrupted). Once the Server row says Ready, click Chat to talk with the model in the window, or Terminal to open the coding agent in Git Bash. Try "list the files in this folder" in Terminal; it asks before running the command (answer y).
  ```
