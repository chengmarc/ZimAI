// The first run: the app ships without the model (12 GB), so the agent downloads it into the data folder
// (paths.ts) before starting the server: curl, resumable, through the proxy.
import { spawnSync } from "node:child_process";
import * as fs from "node:fs";
import * as path from "node:path";
import { MODEL, MODEL_URL } from "./paths.ts";
import { DIM, GRN, RED, RST } from "./terminal/theme.ts";

const CURL = path.join(process.env.SystemRoot ?? "C:\\Windows", "System32", "curl.exe");

/** Download the model if it's missing; false, after saying why, if that failed. */
export function downloadModel(): boolean {
  if (fs.existsSync(MODEL)) return true;
  const part = MODEL + ".part";
  fs.mkdirSync(path.dirname(MODEL), { recursive: true });
  console.log(`First run: downloading the model (gpt-oss-20b, 12.1 GB) into\n  ${DIM}${path.dirname(MODEL)}${RST}\n` +
              `${DIM}If it stops, start 2b again: it resumes where it left off.${RST}`);
  // -C -: resume a partial download; a stalled connection is dropped and retried. curl reads HTTPS_PROXY (proxy.ts).
  const res = spawnSync(CURL, ["-L", "--fail", "--retry", "5", "--retry-all-errors", "-C", "-", "--speed-limit", "10000",
                               "--speed-time", "60", "--progress-bar", "-o", part, MODEL_URL], { stdio: "inherit" });
  if (res.status !== 0) {
    console.log(`${RED}✗${RST} The download stopped${res.error ? ` (${res.error.message})` : ""}. Start 2b again to resume it.`);
    return false;
  }
  fs.renameSync(part, MODEL);
  console.log(`${GRN}✓${RST} Model downloaded`);
  return true;
}
