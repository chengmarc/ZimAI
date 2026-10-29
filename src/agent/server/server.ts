// The model server (llama-server, run by ZimAI.exe): asking ZimAI.exe for it, and talking to it over HTTP. Its settings
// and its lifetime are ZimAI.exe's (app/app.cs): it stays up while any ZimAI window or agent is running (CLIENTS).
import { spawn } from "node:child_process";
import * as fs from "node:fs";
import * as http from "node:http";
import * as path from "node:path";
import { APP, CLIENTS, MODEL, SERVER, SERVER_PID } from "../paths.ts";

export { SERVER };
const LOCAL = new http.Agent();   // talks to llama-server directly, never through HTTPS_PROXY
export let CTX = 0;               // the server's context window, in tokens (props())

/** Ask ZimAI.exe for the server, unless it's running (for another ZimAI window, or still loading), and count this agent as
 *  one that needs it; or say why it can't be. It only asks: the agent is on screen at once, and its loading screen
 *  waits for the model (loading.ts). */
export async function startServer(): Promise<string | null> {
  if (!APP || !SERVER) return "start zim from the ZimAI window's terminal (its Terminal button)";
  fs.mkdirSync(CLIENTS, { recursive: true });
  const me = path.join(CLIENTS, String(process.pid));
  fs.writeFileSync(me, "");
  process.on("exit", () => fs.rmSync(me, { force: true }));
  if (!fs.existsSync(MODEL)) return "the model isn't downloaded yet: click Download model in the ZimAI window";
  if (!running()) {
    // If another keeper got there first, this one sees it and quits.
    spawn(APP, ["--server"], { detached: true, stdio: "ignore", windowsHide: true }).on("error", () => {}).unref();
  }
  return null;
}

/** The server's context size, from the server itself; call once it's ready. */
export async function props(): Promise<void> {
  const res = await new Promise<http.IncomingMessage>((resolve, reject) =>
    http.get(SERVER + "/props", { agent: LOCAL, timeout: 5000 }, resolve).on("error", reject));
  CTX = JSON.parse(await readAll(res)).default_generation_settings?.n_ctx ?? 0;
}

// ---------- talking to it ----------
export function post(route: string, body: object, signal: AbortSignal | undefined, timeout: number): Promise<http.IncomingMessage> {
  return new Promise((resolve, reject) => {
    const req = http.request(SERVER + route, { method: "POST", headers: { "Content-Type": "application/json" },
                                               agent: LOCAL, signal, timeout }, resolve);
    req.on("timeout", () => req.destroy(new Error(`no answer in ${timeout / 1000} s`)));
    req.on("error", reject);
    req.end(JSON.stringify(body));
  });
}

export async function readAll(res: http.IncomingMessage): Promise<string> {
  res.setEncoding("utf8");
  let text = "";
  for await (const chunk of res) text += chunk;
  return text;
}

/** Whether this install's server has loaded the model and takes requests (another install's may answer the port). */
export function serverReady(): Promise<boolean> {
  if (!running()) return Promise.resolve(false);
  return new Promise(resolve => {
    const req = http.get(SERVER + "/health", { agent: LOCAL, timeout: 2000 }, res =>
      readAll(res).then(body => resolve(res.statusCode === 200 && body.includes('"ok"')), () => resolve(false)));
    req.on("timeout", () => req.destroy(new Error("timeout")));
    req.on("error", () => resolve(false));
  });
}

/** Whether this install's server is running (or still being planned): the keeper (ZimAI.exe --server) leaves its pid
 *  in SERVER_PID for as long as it runs, which is as long as llama-server does. */
export function running(): boolean {
  try {
    const pid = Number(fs.readFileSync(SERVER_PID, "utf8"));
    if (!pid) return false;
    process.kill(pid, 0);
    return true;
  } catch (e) {
    return (e as NodeJS.ErrnoException).code === "EPERM";
  }
}
