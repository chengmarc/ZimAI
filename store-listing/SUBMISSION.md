# Microsoft Store submission — ZimAI

How to fill in a Partner Center submission for ZimAI, in its order: Properties, Store listings, Submission options.
What's entered once for the product or the submission is here, to paste. What's entered per language is that language's
Store listing: its text in `localization/<code>.md`, and its images in `dist/<code>/`. Pricing and availability, Age
ratings and Packages are filled in Partner Center by hand. The images are made by
`store-listing\generator\render.ps1`, and only the README's screenshots, in `docs/screenshots/`, are kept in git.

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

## Store listings

One per language, each filled in the same way.

### Text

The fields in `localization/<code>.md`, within Partner Center's limits:

- **Description**
- **What's new in this version** (1500 characters or fewer): the short form of this version's notes
  ([docs/release-notes/](../docs/release-notes/)). Rewrite this for each submission, in every file.
- **Product features:** one per line in Partner Center ("Add more" for each). Each finishes "With this app, you
  can …".
- **Short description** (270 characters or fewer), under Supplemental fields.
- **Keywords** (up to 7, 40 characters each, 21 words in all), under Additional information.
- **Copyright and trademark info**, **Additional license terms** and **Developed by**, under Additional information:
  the same in every language.

### Screenshots

From `dist/<code>/`, in this order (the first is shown first). The line on each is fixed in
[POSITIONING.md](../docs/POSITIONING.md) → The four screenshots:

1. `screenshot-1-limit.png`: Hit your limit? Keep going. (a generic usage-limit banner, and ZimAI picking up the job)
2. `screenshot-2-chat.png`: Resets at 4 PM? Not here. (Chat finishing an everyday job)
3. `screenshot-3-home.png`: Your AI runs out. Your PC didn't. (the home page, on your own PC)
4. `screenshot-4-terminal.png`: The AI that never says *come back later*. (the terminal, a developer's small job)

### Store logos

From `dist/<code>/`.

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
