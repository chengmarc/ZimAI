// Builds 2B from src/ (docs/RELEASING.md): src/2B.exe, then the ready-to-run zip for GitHub and, with --msix, the
// Microsoft Store package. Both hold src/ as it is after tools/fetch.cmd, minus data/ (the model is downloaded on
// the first run). Run from the repo root: src/runtime/node/node.exe tools/build.ts [--clean] [--msix | --exe]
// --exe builds only src/2B.exe, for trying a change to the launcher.
import { execFileSync } from "node:child_process";
import * as fs from "node:fs";
import * as path from "node:path";

const VERSION = "1.0.0";   // MAJOR.MINOR.PATCH; the git tag is v<this>

// From Partner Center > the app > Product identity.
const IDENTITY_NAME = "";
const PUBLISHER = "";            // CN=...
const PUBLISHER_DISPLAY_NAME = "";
const DISPLAY_NAME = "Agent 2B";   // must match the app name reserved in Partner Center

const ROOT = path.join(import.meta.dirname, "..");
const SRC = path.join(ROOT, "src");
const BUILD = path.join(ROOT, "build");
const APP = path.join(BUILD, "Agent 2B");   // the app as it ships; the zip's one folder, then the package
const DIST = path.join(ROOT, "dist");
const ZIP = path.join(DIST, "release", "Agent-2B-portable.zip");
const MSIX = path.join(DIST, "store", "Agent 2B.msix");
const EXE = path.join(SRC, "2B.exe");
const LOGOS: Record<string, number> = { "StoreLogo.png": 50, "Square44x44Logo.png": 44, "Square150x150Logo.png": 150 };

const WINDOWS = process.env.SystemRoot ?? "C:\\Windows";
const CSC = path.join(WINDOWS, "Microsoft.NET", "Framework64", "v4.0.30319", "csc.exe");
const TAR = path.join(WINDOWS, "System32", "tar.exe");
const EDGE = path.join(process.env["ProgramFiles(x86)"] ?? "C:\\Program Files (x86)", "Microsoft", "Edge", "Application", "msedge.exe");

function fail(why: string): never {
  console.error(`build: ${why}`);
  process.exit(1);
}

function run(exe: string, args: string[], cwd = ROOT) {
  console.log(`> ${path.basename(exe)} ${args.join(" ")}`);
  execFileSync(exe, args, { cwd, stdio: "inherit" });
}

function manifest(): string {
  const ids = { IDENTITY_NAME, PUBLISHER, PUBLISHER_DISPLAY_NAME };
  const missing = Object.entries(ids).filter(([, v]) => !v).map(([k]) => k);
  if (missing.length) fail(`set ${missing.join(", ")} in tools/build.ts from Partner Center > Product identity`);
  if (!/^\d+\.\d+\.\d+$/.test(VERSION)) fail(`VERSION ${VERSION} isn't MAJOR.MINOR.PATCH`);
  const values: Record<string, string> = {
    identity_name: IDENTITY_NAME, publisher: PUBLISHER, publisher_display_name: PUBLISHER_DISPLAY_NAME,
    display_name: DISPLAY_NAME,
    version: `${VERSION}.0`,   // the Store wants four parts, the last one 0
  };
  return fs.readFileSync(path.join(import.meta.dirname, "AppxManifest.xml"), "utf8").replace(/\{(\w+)\}/g, (_, k) => values[k]);
}

/** makeappx.exe from the Windows SDK, or from the Microsoft.Windows.SDK.BuildTools NuGet package unzipped
 *  into %LOCALAPPDATA%\Agent 2B Build\sdk-buildtools (docs/RELEASING.md). The newest one found. */
function makeappx(): string {
  const bins = [path.join(process.env["ProgramFiles(x86)"] ?? "", "Windows Kits", "10", "bin"),
                path.join(process.env.LOCALAPPDATA ?? "", "Agent 2B Build", "sdk-buildtools", "bin")];
  const found = bins.flatMap(bin => {
    try {
      return fs.readdirSync(bin).filter(v => /^10\./.test(v)).map(v => path.join(bin, v, "x64", "makeappx.exe"));
    } catch {
      return [];
    }
  }).filter(f => fs.existsSync(f));
  const version = (f: string) => path.basename(path.dirname(path.dirname(f))).split(".").map(Number);
  found.sort((a, b) => {
    const [x, y] = [version(a), version(b)];
    return x.map((n, i) => n - y[i]).find(d => d) ?? 0;
  });
  if (!found.length) fail("makeappx.exe wasn't found: install the Windows SDK or its BuildTools package (docs/RELEASING.md)");
  return found.at(-1)!;
}

/** The app icon at each logo size, drawn from src/launcher/app-icon.svg by Edge (it comes with Windows). */
function logos(dir: string) {
  fs.mkdirSync(dir, { recursive: true });
  const svg = "file:///" + path.join(SRC, "launcher", "app-icon.svg").replaceAll("\\", "/");
  for (const [name, size] of Object.entries(LOGOS)) {
    const page = path.join(BUILD, `logo-${size}.html`);
    fs.writeFileSync(page, `<body style="margin:0"><img src="${svg}" width="${size}" height="${size}" style="display:block">`);
    execFileSync(EDGE, ["--headless", "--disable-gpu", "--hide-scrollbars", "--default-background-color=00000000",
                        `--window-size=${size},${size}`, `--screenshot=${path.join(dir, name)}`, page],
                 { stdio: "ignore" });
    if (!fs.existsSync(path.join(dir, name))) fail(`Edge didn't draw ${name}`);
  }
}

/** src/2B.exe, with the C# compiler that comes with Windows (.NET Framework 4). */
function launcher() {
  const webview2 = path.join(SRC, "runtime", "webview2");
  if (!fs.existsSync(webview2)) fail("src/runtime/webview2 is missing: run tools\\fetch.cmd first");
  const refs = ["System.Net.Http.dll", "System.Web.Extensions.dll", "System.Windows.Forms.dll", "System.Drawing.dll",
                path.join(webview2, "Microsoft.Web.WebView2.Core.dll"), path.join(webview2, "Microsoft.Web.WebView2.WinForms.dll")];
  run(CSC, ["/nologo", "/target:winexe", "/optimize+", `/win32icon:${path.join(SRC, "app-icon.ico")}`, `/out:${EXE}`,
            ...refs.map(r => `/r:${r}`), path.join(SRC, "launcher", "launcher.cs")]);
}

/** build/Agent 2B: src/ without data/, and the license. */
function stage() {
  for (const dir of ["node", "git", "llama"]) {
    if (!fs.existsSync(path.join(SRC, "runtime", dir))) fail(`src/runtime/${dir} is missing: run tools\\fetch.cmd first`);
  }
  console.log("Staging the app...");
  fs.rmSync(APP, { recursive: true, force: true });
  fs.cpSync(SRC, APP, { recursive: true, filter: f => path.relative(SRC, f).split(path.sep)[0] !== "data" });
  fs.copyFileSync(path.join(ROOT, "LICENSE"), path.join(APP, "LICENSE"));
}

function portable() {
  fs.mkdirSync(path.dirname(ZIP), { recursive: true });
  fs.rmSync(ZIP, { force: true });
  run(TAR, ["-a", "-c", "-f", ZIP, path.basename(APP)], BUILD);
}

/** The package: the staged app, its manifest and logos. */
function store(text: string) {
  const pack = makeappx();
  fs.writeFileSync(path.join(APP, "AppxManifest.xml"), text);
  logos(path.join(APP, "Assets"));
  fs.mkdirSync(path.dirname(MSIX), { recursive: true });
  run(pack, ["pack", "/o", "/d", APP, "/p", MSIX]);
}

const args = process.argv.slice(2);
const unknown = args.find(a => !["--clean", "--msix", "--exe"].includes(a));
if (unknown) fail(`unknown option ${unknown} (options: --clean, --msix, --exe)`);
const msix = args.includes("--msix");
if (args.includes("--clean")) for (const dir of [BUILD, DIST]) fs.rmSync(dir, { recursive: true, force: true });
fs.mkdirSync(BUILD, { recursive: true });
const text = msix ? manifest() : "";   // before the slow part: fail on a missing identity first
launcher();
if (args.includes("--exe")) process.exit(0);
stage();
portable();   // before store(), which adds the package's own files to the staged app
if (msix) store(text);
console.log(`\nBuilt:\n- ${EXE}\n- ${ZIP}${msix ? `\n- ${MSIX} (Microsoft Store upload)` : ""}`);
