# agent/ — the ZimAI coding agent

The source of the `zim` command: TypeScript run as is by `src/runtime/node/`, with no build step and no npm packages.
It owns the conversation and the tools; everything else (where things are, the model server) comes from `ZimAI.exe`
through the environment ([`src/README.md`](../README.md)). Each file starts with a comment saying what it owns. This
README covers what no single file says.

## How-to guides

### Run it and see your change
- **Run it:** click Terminal in the ZimAI window, or type `zim` in that terminal. `zim` needs the environment `ZimAI.exe` gives
  its terminal, so it doesn't run from another Git Bash.
- **When edits take effect:** `.ts` files at the next `zim`; the text files in `prompt/` at the next `/clear`.
- **See what the model reads:** run `/prompt` in the agent.

### Check a change without the model server
There are no tests and no CI. Node strips types without checking them, so a wrong annotation passes silently. Every
module except the model call runs without the server (from the repo root):

```sh
src/runtime/node/node.exe --input-type=module -e "
const { Agent } = await import('./src/agent/agent.ts');
const ag = new Agent(true);   // true: approve edits, writes and fetches (commands still ask)
console.log(await ag.tools.get('Grep').run(ag, { pattern: 'MAX_LIST', output_mode: 'content' }));
process.exit(0);
"
```

### Add a tool
Write its module in `tools/`, following the contract at the top of `tools/index.ts`, and add it to `TOOLS`. The
schema, tip line and argument checks follow from that. Anything that changes, runs or fetches something must call
`ag.confirm()` first.

### Use a different model
The model, its download and the server's settings are `ZimAI.exe`'s (`Model` in `app/Model.cs`, `Defaults` in `app/Server.cs`), and so
is who the model is ([`prompt/identity.md`](prompt/identity.md)), which Chat shares. gpt-oss is also assumed here:
- `chat_template_kwargs` in `agent.ts`: `reasoning_effort` is a variable of the gpt-oss chat template, like the
  server's `model_identity`, so `/effort` and `prompt/identity.md` do nothing under another template.

## Explanation

Imports point down:
- `main.ts` → `agent.ts` → `tools/`, `prompt/`, `server/`, `terminal/`.
- Tools get a `Session` (`tools/_types.ts`), never the `Agent` itself.
- `server/` knows nothing about the terminal, which is why the loading screen is `loading.ts`, one level up.

An import that points back up means the code belongs somewhere else.

The agent asks `ZimAI.exe --server` for the model server and shows up at once; its loading screen waits for the model.
It counts as one of the server's clients while it runs, so the server stays up for it even after the ZimAI window
closes. It reads the context size from the server (`/props`) rather than keeping its own copy.
