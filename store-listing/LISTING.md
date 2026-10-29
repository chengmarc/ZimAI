# Microsoft Store submission — ZimAI

Everything to paste into Partner Center → ZimAI, in the submission's order: Properties, Store listings (English
(United States)), Submission options. Pricing and availability, Age ratings and Packages are filled in Partner Center
by hand. 

## Properties

- **Category:** Productivity. 
- **Subcategory:** Developer tools.
- **Privacy policy:** paste [PRIVACY.md](PRIVACY.md) as it is; it's plain text, written for Partner Center's box.
- **Website:** `https://github.com/chengmarc/ZimAI`
- **Support contact info:** `https://github.com/chengmarc/ZimAI/issues`
- **System requirements:** 
  | | Minimum hardware | Recommended hardware |
  |---|---|---|
  | Mouse | ✓ | ✓ |
  | Keyboard | ✓ | ✓ |
  | Memory | 16 GB | 32 GB |
  | Video memory | leave unchecked: ZimAI runs on the CPU without a GPU | the largest option offered (8 GB or more) |
  | Graphics | leave empty | `NVIDIA RTX` |

## Store listings — English (United States)

### Description

```
Unlimited AI for Small Jobs. Out of credits? Not here.

Hit your AI's usage limit halfway through something? Don't wait hours for the reset or pay for a bigger plan. ZimAI is a free AI that runs on your own PC, so it never runs out: pick up the job where you were cut off and finish it.

• No usage limit, no credits, no account: use it as often as you like
• Emails, rewrites, formulas and quick questions, in the app's own chat window
• For developers, a terminal that works in your projects and asks before every change
• Private: nothing you type leaves your PC, and it works offline
• Runs on the NVIDIA GeForce you game on, or on the CPU, and checks your PC before downloading

Made for small jobs: it's a 20B open-weight model, not a replacement for the AI you pay for.
```

### What's new in this version

Leave blank until a version is in the Store: 1.0.0 was never published (the Store denied `unvirtualizedResources`),
so 1.0.1 is still the first version customers see.

### Product features

One per line in Partner Center ("Add more" for each); each finishes "With this app, you can …":

```
Keep going when your AI hits its usage limit
Use it as often as you like: no limits, no credits, no account
Write emails, formulas and rewrites in its chat window
Work in your projects from a terminal, approving every change
Keep everything private and offline, on your own PC
Run it on your GeForce, or just the CPU
```

### Screenshots

Upload in this order (the first is shown first). The line on each is fixed in
[POSITIONING.md](../docs/POSITIONING.md) → The four screenshots:

1. `desktop/screenshot-1-limit.png`: Hit your limit? Keep going. (a generic usage-limit banner, and ZimAI picking up the job)
2. `desktop/screenshot-2-chat.png`: Resets at 4 PM? Not here. (Chat finishing an everyday job)
3. `desktop/screenshot-3-home.png`: Your AI runs out. Your PC didn't. (the home page, on your own PC)
4. `desktop/screenshot-4-terminal.png`: The AI that never says *come back later*. (the terminal, a developer's small job)

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

- **Copyright and trademark info:**

  ```
  © 2026 chengmarc.
  ```

- **Additional license terms:** `Apache License 2.0: https://github.com/chengmarc/ZimAI/blob/main/LICENSE`

- **Developed by:** `chengmarc`

## Submission options

- **Publishing hold options:** publish as soon as it passes certification, unless a launch post is planned; then
  hold it and publish on a weekday morning (US time), when developers browse the Store and GitHub most.
- **Restricted capabilities → runFullTrust:**

  ```
  ZimAI is a packaged Win32 desktop app (C#, Windows Forms and WebView2); runFullTrust is required to launch it. It runs only its own executables: ZimAI.exe and the llama.cpp model server, Node.js and Git Bash it ships with. It asks before every file change or command, and has no telemetry. Its terminal starts outside the package through the documented desktop app breakaway policy. It requests no other restricted capability.
  ```

- **Notes for certification:**

  ```
  No sign-in or setup needed. 1. Click Download in the Model row (the 12 GB model). 2. Click Start in the Server row and wait for Server Ready (about 30 seconds on a GPU). 3. Click Chat and ask a question, or click Terminal and type "list the files in this folder", answering y when it asks. It runs on an NVIDIA GPU with 6 GB of video memory or more, or on the CPU with 16 GB of RAM; on a PC with neither, the System row says Not Supported and nothing is downloaded. Since the last submission, the denied unvirtualizedResources capability has been removed.
  ```
