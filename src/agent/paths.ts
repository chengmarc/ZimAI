// Where everything is. src/ is the app as it ships: runtime/ (Node, Git Bash, llama.cpp; tools/fetch.cmd downloads
// them into a clone) next to agent/. What the app makes as it runs (the model, logs/) is in data/ beside them, or
// in the Store app, whose install folder is read-only, in the folder its launcher names in AGENT_2B_DATA.
// app.cmd and the launcher (launcher/launcher.cs) name the same places.
import * as path from "node:path";

export const ROOT = path.join(import.meta.dirname, "..");
const DATA = process.env.AGENT_2B_DATA || path.join(ROOT, "data");

export const LLAMA = path.join(ROOT, "runtime", "llama", "llama-server.exe");
export const GIT_BASH = path.join(ROOT, "runtime", "git", "usr", "bin", "bash.exe");

export const MODEL = path.join(DATA, "model", "_model.gguf");   // downloaded by the launcher

export const LOG = path.join(DATA, "logs", "server.log");
export const LOAD_SECONDS = path.join(DATA, "logs", "load-seconds");   // how long the last load took, for the next estimate
export const AGENTS = path.join(DATA, "logs", "agents");               // one empty file per running agent, named by its pid
