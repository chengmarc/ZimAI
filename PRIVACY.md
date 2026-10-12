# Privacy Policy

**Agent 2B**
Last updated: October 12, 2026

This policy covers Agent 2B everywhere it ships: the Microsoft Store edition, the portable version on GitHub, and
running from source.

## Summary

2B does not collect, store or share personal information. The AI model runs on your own computer, and your
conversations are never sent to the developer or to any AI service.

## What the app does with data

- **No telemetry.** 2B has no analytics, crash reporting, advertising, accounts or update checks.
- **The model runs locally.** Your requests, the files the agent reads, and the model's answers are processed on
  your computer by llama.cpp. Conversations aren't saved: they're gone when you close the window or type `/clear`.
- **Network access happens only in these cases:**
  - **Downloading the model.** 2B downloads it from Hugging Face (huggingface.co) on its first run.
  - **Web search.** When the agent uses WebSearch, the search query is sent to DuckDuckGo (duckduckgo.com).
  - **Web pages.** WebFetch opens a page only after you approve it.
  - **Commands.** Commands the agent runs, such as `git` or `npm`, can go online. 2B asks before running each one,
    unless you've answered "always" in that session.

  These requests go to the services named, under their own privacy policies.
- **Local files.** 2B reads and writes files in the folders you work in, after asking before every change. It keeps
  the model, the model server's log and its load time on your computer:
  - Microsoft Store edition: `%LOCALAPPDATA%\Packages\<package>\LocalCache\`, removed when you uninstall the app.
  - Portable version: the `data\` folder beside `2B.exe`.

## Children

2B does not knowingly collect information from anyone, including children under 13.

## Changes

If this policy changes, the new version will be posted at this address and the "Last updated" date will change.

## Contact

Questions about this policy can go to the [project issue tracker](https://github.com/chengmarc/agent-2b/issues).
