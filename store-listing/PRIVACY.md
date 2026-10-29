ZimAI Privacy Policy
Last updated: October 15, 2026

This policy covers ZimAI everywhere it ships: the Microsoft Store edition, the portable version on GitHub, and running from source.

Summary

ZimAI does not collect, store or share personal information. The AI model runs on your own computer, and what you ask it, the files it reads and its answers are never sent to the developer or to any AI service.

What the App Does With Data

• No telemetry. ZimAI has no analytics, crash reporting, advertising, accounts or update checks.
• The model runs locally. Your requests, the files the agent reads and the model's answers are processed on your computer by llama.cpp.
• Conversations in the terminal aren't saved: they're gone when you close the window or type /clear. Conversations in Chat are kept on your computer, in ZimAI's data folder (below), so you can return to them. Delete them from the chat page, or delete the data folder's "browser" folder.

ZimAI goes online only in these cases:

• Downloading the model. When you click Download in the Model row, the model is downloaded from Hugging Face (huggingface.co).
• Web search. When the agent searches the web, the search query is sent to DuckDuckGo (duckduckgo.com).
• Web pages. The agent opens a web page only after you approve it.
• Commands. Commands the agent runs, such as git or npm, can go online. ZimAI asks before running each one, unless you've answered "always" in that session.
• WebView2. If your PC doesn't have the Microsoft Edge WebView2 Runtime, ZimAI opens Microsoft's download page in your browser.

These requests go to the services named, under their own privacy policies.

Files on your computer. ZimAI reads and writes files in the folders you work in, asking before every change. It keeps the model, the Chat history, the model server's log and its load time on your computer:

• Microsoft Store edition: %LOCALAPPDATA%\Packages\<package>\LocalCache\, removed when you uninstall the app.
• Portable version: the data folder beside ZimAI.exe.

Changes to This Policy

If this policy changes, the new version will be posted here and the "Last updated" date will change.

Contact

Questions about this policy can go to the project's issue tracker: https://github.com/chengmarc/ZimAI/issues
