// What the agent gets from 2B.exe (app/app.cs), which owns where things are and the model server: it sets
// AGENT_2B_* for the terminal it opens, so `2b` there has them. Only Git Bash's place is the agent's own business.
import * as path from "node:path";

export const ROOT = path.join(import.meta.dirname, "..");
export const GIT_BASH = path.join(ROOT, "runtime", "git", "usr", "bin", "bash.exe");

export const APP = process.env.AGENT_2B_EXE ?? "";        // 2B.exe; `2B.exe --server` starts the model server
export const SERVER = process.env.AGENT_2B_SERVER ?? "";  // the model server's address
const DATA = process.env.AGENT_2B_DATA ?? "";             // the app's data folder (Paths in app.cs)

export const MODEL = path.join(DATA, "model", "_model.gguf");
export const LOG = path.join(DATA, "logs", "server.log");
export const SERVER_PID = path.join(DATA, "logs", "server.pid");
export const LOAD_SECONDS = path.join(DATA, "logs", "load-seconds");
export const CLIENTS = path.join(DATA, "logs", "clients");   // <pid>: each open 2B window and agent; the server stays up for them
