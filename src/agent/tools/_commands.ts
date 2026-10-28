// Running a command for the tools that do: Bash, PowerShell, Git, GitHub; and what a Bash or PowerShell command deletes.
import { spawnSync } from "node:child_process";
import * as os from "node:os";
import * as path from "node:path";
import type { Session } from "./_types.ts";
import { fill } from "../text.ts";

const COMMAND_TIMEOUT = 180;   // seconds
// No pagers, colors or interactive prompts in commands the agent runs (they would hang or clutter output).
const QUIET = { GIT_PAGER: "cat", PAGER: "cat", GH_PAGER: "", NO_COLOR: "1",
                GH_PROMPT_DISABLED: "1", GIT_TERMINAL_PROMPT: "0" };
const MESSAGES = {
  command_timeout: "Error: command timed out after {seconds} s.",
  command_output: "{output}\n[exit code {code}]",
  command_no_output: "[no output, exit code {code}]",
  delete_outside: "Error: refused: this command deletes {target}, which isn't inside the project folder ({root}). " +
                  "Only paths inside the project can be deleted; anything else is for the user to delete themselves.",
  delete_unknown: "Error: refused: can't tell what this command deletes ({target}). Name each path to delete " +
                  "literally (no variables or pipes), inside the project folder.",
};

/** Run a command, asking first (a command always asks: "always" at a question doesn't cover it). */
export async function runCommand(ag: Session, argv: string[], ask = true, what = "this command"): Promise<string> {
  if (ask) {
    const denied = await ag.confirm(what, true);
    if (denied) return denied;
  }
  const r = spawnSync(argv[0], argv.slice(1), {
    cwd: ag.root, encoding: "utf8", timeout: COMMAND_TIMEOUT * 1000, stdio: ["ignore", "pipe", "pipe"],
    env: { ...process.env, ...QUIET }, maxBuffer: 64 * 1024 * 1024, windowsHide: true,
  });
  if ((r.error as NodeJS.ErrnoException | undefined)?.code === "ETIMEDOUT") {
    return fill(MESSAGES.command_timeout, { seconds: COMMAND_TIMEOUT });
  }
  if (r.error) throw r.error;
  const code = r.status ?? r.signal;
  const out = (r.stdout + (r.stderr ? "\n" + r.stderr : "")).trimEnd().replace(/^\n+/, "");   // keep leading spaces (git status)
  return out ? fill(MESSAGES.command_output, { output: out, code }) : fill(MESSAGES.command_no_output, { code });
}

/** Run a Bash or PowerShell command line: refused if it deletes anything but paths inside the project, and asked
 *  about as a recursive delete if it is one. */
export async function runShell(ag: Session, argv: string[], command: string): Promise<string> {
  const { refused, recursive } = checkDeletes(ag.root, command);
  if (refused) return refused;
  return runCommand(ag, argv, true, recursive ? "this command, which deletes recursively" : "this command");
}

// ---------- what a command line deletes ----------
// Read from its words before it runs: a guess, not a shell, so a delete it can't place is refused rather than run.
const DELETERS = new Set(["rm", "rmdir", "rd", "del", "erase", "unlink", "shred", "rimraf", "remove-item", "ri"]);
const CMD_DELETERS = new Set(["rd", "rmdir", "del", "erase"]);   // cmd's, whose switches are /s /q
const WRAPPERS = new Set(["sudo", "env", "nohup", "time", "command", "exec", "xargs", "cmd", "powershell", "pwsh",
                          "bash", "sh"]);   // run the command that follows them
const CD = new Set(["cd", "chdir", "pushd", "set-location", "sl"]);
const PS_PATH = new Set(["-path", "-literalpath", "-lp", "-pspath"]);   // Remove-Item's: the next word is a path
const PS_VALUE = new Set(["-include", "-exclude", "-filter", "-stream"]);   // the next word isn't

/** The simple commands of a command line (split at ; & | newlines and brackets), each as its words, without
 *  redirections. Quotes group words; nothing escapes, as backslashes are Windows path separators. */
function simpleCommands(line: string): string[][] {
  const out: string[][] = [];
  let words: string[] = [], word = "", inWord = false, quote = "";
  const endWord = () => {
    if (inWord) words.push(word);
    word = "";
    inWord = false;
  };
  const endCommand = () => {
    endWord();
    const kept: string[] = [];
    for (let i = 0; i < words.length; i++) {
      if (/^\d*[<>]+$/.test(words[i])) i++;   // "> file": the file is the next word
      else if (!/^\d*[<>]/.test(words[i])) kept.push(words[i]);
    }
    if (kept.length) out.push(kept);
    words = [];
  };
  for (const c of line) {
    if (quote) {
      if (c === quote) quote = ""; else word += c;
    } else if (c === "'" || c === '"') {
      quote = c;
      inWord = true;
    } else if (";&|\n(){}".includes(c)) {
      endCommand();
    } else if (/\s/.test(c)) {
      endWord();
    } else {
      word += c;
      inWord = true;
    }
  }
  endCommand();
  return out;
}

/** Where a path in the command points, from the directory it runs in; null when that's only known once it runs. */
function resolvePath(cwd: string | null, p: string): string | null {
  if (/[$%`]/.test(p) || /^[a-z]{2,}:/i.test(p)) return null;   // a variable, a command's output, a PowerShell drive
  if (p === "~" || /^~[\/\\]/.test(p)) return path.join(os.homedir(), p.slice(1));
  const drive = /^\/([a-z])(\/|$)/i.exec(p);   // Git Bash's /c/... is C:\...
  if (drive) p = drive[1] + ":/" + p.slice(3);
  if (path.isAbsolute(p)) return path.resolve(p);
  return cwd === null ? null : path.resolve(cwd, p);
}

/** Strictly inside root: not root itself, a folder above it, or anywhere else. */
function inside(root: string, p: string): boolean {
  const r = path.relative(root, p);
  return !!r && !r.startsWith("..") && !path.isAbsolute(r);
}

/** What a delete command's arguments name, and whether it deletes recursively. */
function deleteTargets(verb: string, args: string[]): { paths: string[]; recursive: boolean } {
  const paths: string[] = [];
  let recursive = verb === "rimraf", flags = true;
  for (let i = 0; i < args.length; i++) {
    const a = args[i], low = a.toLowerCase();
    if (flags && a === "--") {
      flags = false;
    } else if (flags && CMD_DELETERS.has(verb) && /^\/[a-z]$/i.test(a)) {
      if (low === "/s") recursive = true;
    } else if (flags && a.startsWith("-") && a.length > 1) {
      const [name, value] = low.split(/:(.*)/s);   // PowerShell's -Path:x
      if (PS_PATH.has(name)) {
        if (value !== undefined) paths.push(a.slice(name.length + 1)); else if (i + 1 < args.length) paths.push(args[++i]);
      } else if (PS_VALUE.has(name)) {
        if (value === undefined) i++;
      } else if (low === "--recursive" || low.startsWith("-rec") || /^-[a-z]{1,3}$/i.test(a) && /r/i.test(a)) {
        recursive = true;   // rm -r, -rf, -Rf; Remove-Item -Recurse
      }
    } else {
      paths.push(...a.split(",").filter(Boolean));   // PowerShell takes a list: a,b
    }
  }
  return { paths, recursive };
}

/** Whether a command line may run, as far as its deletes go: refused (the reason) if it deletes anything but paths
 *  inside root, or if it deletes something that can't be placed before it runs. */
export function checkDeletes(root: string, line: string): { refused: string | null; recursive: boolean } {
  let cwd: string | null = root, recursive = false;
  const queue = simpleCommands(line);
  const unknown = (target: string) => ({ refused: fill(MESSAGES.delete_unknown, { target }), recursive });
  while (queue.length) {
    const words = queue.shift()!;
    const verb = path.basename(words[0]).toLowerCase().replace(/\.exe$/, "");
    const rest = words.slice(1);
    if (WRAPPERS.has(verb)) {
      while (rest.length && (/^[-\/]/.test(rest[0]) || rest[0].includes("="))) rest.shift();   // its options
      if (!rest.length) continue;
      const next = path.basename(rest[0]).toLowerCase().replace(/\.exe$/, "");
      if (verb === "xargs" && DELETERS.has(next)) return unknown(words.join(" "));   // deletes what comes in
      if (rest.length === 1 && /\s/.test(rest[0])) queue.unshift(...simpleCommands(rest[0]));   // bash -c "..."
      else queue.unshift(rest);
      continue;
    }
    if (CD.has(verb)) {
      const to = rest.find(w => !w.startsWith("-") && !/^\/[a-z]$/i.test(w));
      cwd = resolvePath(cwd, to ?? "~");
      continue;
    }
    let found: { paths: string[]; recursive: boolean; under?: boolean };   // under: deletes below the paths, not them
    if (DELETERS.has(verb)) {
      found = deleteTargets(verb, rest);
    } else if (verb === "find" && rest.some((w, i) => w === "-delete" ||
               (w === "-exec" || w === "-execdir") && DELETERS.has((rest[i + 1] ?? "").toLowerCase()))) {
      const start = rest.findIndex(w => /^[-!]/.test(w));
      const paths = start === -1 ? rest : rest.slice(0, start);
      found = { paths: paths.length ? paths : ["."], recursive: true, under: true };
    } else {
      continue;
    }
    recursive ||= found.recursive;
    if (!found.paths.length) return unknown(words.join(" "));   // from a pipe
    for (const t of found.paths) {
      const p = resolvePath(cwd, t);
      if (p === null) return unknown(t);
      if (!inside(root, p) && !(found.under && path.relative(root, p) === "")) return { refused: fill(MESSAGES.delete_outside, { target: t, root }), recursive };
    }
  }
  return { refused: null, recursive };
}

/** Words of a command line, like Python's shlex.split: quotes and backslashes as in a POSIX shell. */
export function shlexSplit(s: string): string[] {
  const words: string[] = [];
  let word = "", inWord = false, quote = "";
  for (let i = 0; i < s.length; i++) {
    const c = s[i];
    if (quote === "'") {
      if (c === "'") quote = ""; else word += c;
    } else if (quote === '"') {
      if (c === '"') quote = "";
      else if (c === "\\" && (s[i + 1] === '"' || s[i + 1] === "\\")) word += s[++i];
      else word += c;
    } else if (c === "'" || c === '"') {
      quote = c;
      inWord = true;
    } else if (c === "\\") {
      if (i + 1 >= s.length) throw new SyntaxError("No escaped character");
      word += s[++i];
      inWord = true;
    } else if (/\s/.test(c)) {
      if (inWord) words.push(word);
      word = "";
      inWord = false;
    } else {
      word += c;
      inWord = true;
    }
  }
  if (quote) throw new SyntaxError("No closing quotation");
  if (inWord) words.push(word);
  return words;
}
