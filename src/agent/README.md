# agent/ — the 2B coding agent

The source of the `2b` command: TypeScript run by `src/runtime/node/`, with no build step and no npm packages. Each file
starts with a comment saying what it owns. This README covers what no single file says.

## How-to guides

### Run it and see your change
- **Run it:** in the app window, type `2b`. From another Git Bash, run `src/runtime/node/node.exe src/agent/main.ts`.
- **When edits take effect:** `.ts` files at the next `2b`; the text files in `prompt/` at the next `/clear`.
- **See what the model reads:** run `/prompt` in the agent.

### Check a change without the model server
There are no tests and no CI. Node strips types without checking them, so a wrong annotation passes silently. Every
module except the model call runs without llama-server (from the repo root):

```sh
src/runtime/node/node.exe --input-type=module -e "
const { Agent } = await import('./src/agent/agent.ts');
const ag = new Agent(true);   // true: approve everything, so tools don't stop to ask
console.log(await ag.tools.get('Grep').run(ag, { pattern: 'MAX_LIST', output_mode: 'content' }));
process.exit(0);
"
```

### Add a tool
Write its module in `tools/`, following the contract at the top of `tools/index.ts`, and add it to `TOOLS`. The
schema, tip line and argument checks follow from that. Anything that changes, runs or fetches something must call
`ag.confirm()` first.

### Change the server's settings (context, port)
They're in `server/server.ts`. A server that's already running keeps its old settings, so stop it first:
`taskkill /F /IM llama-server.exe`.

### Upgrade a component, or use a different model
- **Any component:** change its URL in `tools/fetch.cmd`, delete its folder under `src/runtime/`, then run `tools\fetch.cmd`.
  Node must stay at 22.18 or later so it can run `.ts` files as they are.
- **A different model:** gpt-oss is also assumed in these places:
  - `MODEL_URL` in `paths.ts`, which the first run downloads.
  - `ALIAS` in `server/server.ts`.
  - `chat_template_kwargs` in `agent.ts`: `reasoning_effort` and `model_identity` are variables of the gpt-oss chat
    template, so `/effort` and `prompt/identity.md` do nothing under another template.
  - `prompt/identity.md`, which describes gpt-oss-20b.

## Explanation

Imports point down:
- `main.ts` → `agent.ts` → `tools/`, `prompt/`, `server/`, `terminal/`.
- Tools get a `Session` (`tools/_types.ts`), never the `Agent` itself.
- `server/` knows nothing about the terminal, which is why the loading screen is `loading.ts`, one level up.

An import that points back up means the code belongs somewhere else.

llama-server runs as a hidden process of its own. The agent appears at once, and several 2B windows share one server.
`server/stopper.ts` stops the server after the last window closes.

## Reference

The layout of `src/`, the app as it ships. `paths.ts` defines it for the agent; `app.cmd` and
`tools/fetch.cmd` spell out the same paths, so a rename touches all three.

```
src/
  agent/                this folder
  app.cmd               starts the terminal with 2B (2B.exe runs it)
  2B.exe                built from launcher/ by tools/build.ts (untracked)
  runtime/              downloaded by tools/fetch.cmd (untracked)
    node/  git/  llama/ Node.js, portable Git Bash, llama.cpp (CUDA build)
  data/                 written at runtime (untracked; the Store app keeps it elsewhere, see below)
    model/_model.gguf   the model, downloaded on the first run (download.ts)
    logs/
      server.log        llama-server's log, overwritten at each start
      load-seconds      the last load time, for the loading bar's estimate
      agents/<pid>      one empty file per running agent, for the stopper
```

In the Microsoft Store app, the install folder is read-only, so `data/` is the package's own data folder instead
(`AGENT_2B_DATA`, set by `2B.exe`). [`docs/RELEASING.md`](../../docs/RELEASING.md) covers building and publishing.
