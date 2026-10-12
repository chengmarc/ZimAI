// 2B.exe, the app. With no arguments it's the 2B window: home.html in WebView2, which shows the GPU, the model and
// the model server, and has Download model, Chat (llama-server's own chat page) and Terminal (Git Bash with the
// agent). The window starts the server as soon as it opens, so Chat and Terminal open without a wait. With
// --server it's the model server's keeper (Keeper): the window and each agent ask for the server that way.
// It owns everything outside the agent: where things are (Paths), the model (Model), the server's settings and
// lifetime, the terminal, and the environment the agent gets (AGENT_2B_*, the proxy).
// tools/build.ps1 compiles it with the C# compiler that comes with Windows (.NET Framework 4), which reads C# 5
// (no $"", no ?., no => members) and reads files in the system code page (so ASCII only).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

[assembly: AssemblyTitle("Agent 2B")]
[assembly: AssemblyProduct("Agent 2B")]
[assembly: AssemblyInformationalVersion("1.0.0")]   // the version: MAJOR.MINOR.PATCH; tools/build.ps1 reads it from here

// Where everything is. src/ is the app as it ships; data/ beside it is what the app makes as it runs, except in the
// Store app, whose install folder is read-only. The agent gets these through the environment (Main).
static class Paths {
    public static readonly string Root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
    public static readonly string Exe = Assembly.GetExecutingAssembly().Location;
    public static readonly string Node = Path.Combine(Root, @"runtime\node");
    public static readonly string Git = Path.Combine(Root, @"runtime\git");
    public static readonly string Llama = Path.Combine(Root, @"runtime\llama\llama-server.exe");
    public static readonly string WebView2 = Path.Combine(Root, @"runtime\webview2");
    public static readonly string App = Path.Combine(Root, "app");
    public static readonly string Agent = Path.Combine(Root, @"agent\main.ts");
    public static string Data;   // Main
    public static string Model { get { return Path.Combine(Data, @"model\_model.gguf"); } }
    public static string Browser { get { return Path.Combine(Data, "browser"); } }   // the window's browser data, chat history included
    public static string Logs { get { return Path.Combine(Data, "logs"); } }
    public static string Log { get { return Path.Combine(Logs, "server.log"); } }            // llama-server's own log
    public static string ServerPid { get { return Path.Combine(Logs, "server.pid"); } }      // while the keeper's server runs
    public static string ServerPlan { get { return Path.Combine(Logs, "server-plan"); } }    // how it runs (Plan.Describe)
    public static string LoadSeconds { get { return Path.Combine(Logs, "load-seconds"); } }  // the last load time, for estimates
    public static string Clients { get { return Path.Combine(Logs, "clients"); } }           // <pid>: each open 2B window and agent

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern int GetCurrentPackageFamilyName(ref uint length, StringBuilder name);

    /** data/ beside the app, or in the Store app the package's LocalCache, which is deleted when it's uninstalled. */
    public static string FindData() {
        uint length = 0;
        // ERROR_INSUFFICIENT_BUFFER (122) when packaged; APPMODEL_ERROR_NO_PACKAGE when not.
        if (GetCurrentPackageFamilyName(ref length, null) != 122) return Path.Combine(Root, "data");
        var name = new StringBuilder((int)length);
        GetCurrentPackageFamilyName(ref length, name);
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "Packages", name.ToString(), "LocalCache");
    }
}

// The model.
static class Model {
    public const string Name = "gpt-oss-20b";   // its name in the API
    public const string Title = "GPT OSS 20B";  // its name on screen, whatever the file calls itself
    public const string Url = "https://huggingface.co/ggml-org/gpt-oss-20b-GGUF/resolve/ef9b12f2ff56c69cf32153a02784e7a3c88bf524/gpt-oss-20b-MXFP4.gguf";
    public const long Size = 12109566624;
    public const string Server = "http://127.0.0.1:8080";
}

// The computer's RAM.
static class Memory {
    [StructLayout(LayoutKind.Sequential)]
    class Status {
        public uint Length = 64, Load;
        public ulong Total, Available, TotalPage, FreePage, TotalVirtual, FreeVirtual, FreeExtended;
    }
    [DllImport("kernel32.dll")]
    static extern bool GlobalMemoryStatusEx([In, Out] Status status);

    static Status Now() {
        var s = new Status();
        GlobalMemoryStatusEx(s);
        return s;
    }

    public static long Total { get { return (long)Now().Total; } }
    public static long Available { get { return (long)Now().Available; } }
}

// Whether this computer can run 2B, and how well, before the 12 GB download. The one place the requirements are
// decided; the README and the Store listing say the same: an NVIDIA GPU (10-series or newer) with 6 GB of VRAM or
// more, a driver with CUDA 12.4 or later, and 16 GB of RAM (12 GB+ of VRAM needs less).
// The 6 GB floor: the GPU must hold the dense weights, the 64k context and llama.cpp's working memory (~4.2 GB), plus
// its 1 GB safety margin; the experts can go to RAM, which is why RAM matters below 12 GB of VRAM.
static class Requirements {
    const double MinVram = 5.8;   // GB: what a 6 GB card reports, give or take
    const double FullRam = 15;    // GB: what a 16 GB computer reports, give or take
    const double MinCuda = 12.4;  // the llama.cpp build's

    /** { name, vram, ram, cuda, verdict, reason } for the best NVIDIA GPU. verdict: "great", "good" (supported),
     *  "slow" (supported, slower), "driver" (update the driver first), "unsupported", or "none" (no NVIDIA GPU). */
    public static Dictionary<string, object> Check() {
        double ram = Math.Round(Memory.Total / (double)(1L << 30), 1);
        string name = null, cuda = null;
        double vram = 0, cc = 0;
        try {
            // compute_cap needs a 2021 driver or newer; without it the GPU's generation goes unchecked.
            string list = Output("nvidia-smi", "--query-gpu=name,memory.total,compute_cap --format=csv,noheader,nounits");
            if (!list.Contains(",")) list = Output("nvidia-smi", "--query-gpu=name,memory.total --format=csv,noheader,nounits");
            foreach (string line in list.Split('\n')) {
                string[] f = line.Split(',');
                double v;
                if (f.Length < 2 || !double.TryParse(f[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) continue;
                if (v / 1024 <= vram) continue;   // the one with the most VRAM
                name = f[0].Trim();
                vram = Math.Round(v / 1024, 1);
                if (f.Length < 3 || !double.TryParse(f[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out cc)) cc = 0;
            }
            Match m = Regex.Match(Output("nvidia-smi", ""), @"CUDA Version:\s*([\d.]+)");
            if (m.Success) cuda = m.Groups[1].Value;
        } catch {
            name = null;   // no nvidia-smi: no NVIDIA driver
        }
        string verdict, reason = null;
        double cudaVersion;
        if (name == null) {
            verdict = "none";
            reason = "2B needs an NVIDIA GPU with 6 GB of VRAM or more, such as a GTX 1060 6 GB, a GTX 1660 or any desktop RTX.";
        } else if (cc > 0 && cc < 6) {
            verdict = "unsupported";
            reason = "This GPU is older than the GTX 10-series, which 2B's llama.cpp doesn't support.";
        } else if (vram < MinVram) {
            verdict = "unsupported";
            reason = string.Format(CultureInfo.InvariantCulture, "2B needs 6 GB of VRAM or more; this GPU has {0} GB.", vram);
        } else if (cuda == null || !double.TryParse(cuda, NumberStyles.Float, CultureInfo.InvariantCulture, out cudaVersion) || cudaVersion < MinCuda) {
            verdict = "driver";
            reason = "Update the NVIDIA driver: 2B needs CUDA 12.4 or later" + (cuda != null ? " (this one has " + cuda + ")." : ".");
        } else if (vram >= 15) {
            verdict = "great";   // the whole model fits on the GPU
        } else if (vram >= 11) {
            verdict = ram >= 7.5 ? "good" : "unsupported";
        } else if (ram >= FullRam) {
            verdict = vram >= 7.5 ? "good" : "slow";
        } else {
            verdict = "unsupported";
        }
        if (verdict == "unsupported" && reason == null) {
            reason = string.Format(CultureInfo.InvariantCulture,
                "With {0} GB of VRAM, 2B needs {1} GB of RAM for the rest of the model; this PC has {2} GB.",
                vram, vram >= 11 ? 8 : 16, ram);
        }
        bool pascal = cc >= 6 && cc < 7;   // the 10-series: no tensor cores
        if (pascal && verdict == "great") verdict = "good";
        else if (pascal && verdict == "good") verdict = "slow";
        if (verdict == "slow") {
            reason = pascal ? "A GTX 10-series GPU has no tensor cores, so replies come slower than on an RTX."
                            : "With 6 GB of VRAM, most of the model runs from RAM, so replies come slower.";
        }
        return new Dictionary<string, object> {
            { "name", name }, { "vram", vram }, { "ram", ram }, { "cuda", cuda }, { "verdict", verdict }, { "reason", reason },
        };
    }

    /** Whether the verdict lets 2B download and run the model. */
    public static bool Supported(string verdict) {
        return verdict == "great" || verdict == "good" || verdict == "slow";
    }

    static string Output(string exe, string args) {
        var start = new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
        using (var p = Process.Start(start)) {
            string text = p.StandardOutput.ReadToEnd();
            p.WaitForExit(10000);
            return text;
        }
    }
}

// How llama-server runs the model on this computer, worked out before each start (by the keeper), because free
// VRAM and RAM change from one start to the next. llama.cpp's own fitter (llama-fit-params: a dry run that loads no
// weights) splits the model between the GPU and RAM; this decides the rest, for the same experience everywhere:
//  - context: every supported computer (6 GB of VRAM or more) gets the same 64k, so a task that works on one works
//    on all; the model's full trained context only where it costs nothing, i.e. all of it still fits on the GPU.
//  - RAM: the part that doesn't fit on the GPU is loaded into RAM only if that leaves room for the rest of the
//    computer; otherwise it's memory-mapped, read from disk as needed: slower, but nothing else gets pushed out.
class Plan {
    public const int Guaranteed = 65536;   // tokens
    const long SpareRam = 2L << 30;        // left for the rest of the computer

    public int Context;
    public long GpuMiB, RamMiB;   // what the fitter projects
    public bool AllOnGpu, Mapped;
    string fitted;                // the fitter's arguments: -c, -ngl and which tensors stay in RAM (-ot)

    /** The plan for this start; null if the fitter couldn't make one (Arguments then lets llama-server fit). */
    public static Plan Make() {
        Dictionary<string, object> info = null;
        try { info = Gguf.Read(Paths.Model); } catch { }
        int trained = info != null && info["context"] != null ? Convert.ToInt32(info["context"]) : 0;
        int layers = info != null && info["layers"] != null ? Convert.ToInt32(info["layers"]) + 1 : 0;   // + the output layer
        Plan plan = trained > Guaranteed ? Fit(trained, layers) : null;
        if (plan == null || !plan.AllOnGpu) plan = Fit(Guaranteed, layers);
        if (plan == null) return null;
        plan.Mapped = (plan.RamMiB << 20) + SpareRam > Memory.Available;
        return plan;
    }

    /** llama-fit-params' plan for this context: its arguments (on stdout), and from its log the memory each device
     *  and the host would use (the last breakdown table it prints is the plan's). */
    static Plan Fit(int context, int layers) {
        string exe = Path.Combine(Path.GetDirectoryName(Paths.Llama), "llama-fit-params.exe");
        var start = new ProcessStartInfo(exe, "-m \"" + Paths.Model + "\" -c " + context + " -np 1 -lv 4") {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
        };
        string output, log;
        try {
            using (var p = Process.Start(start)) {
                var err = p.StandardError.ReadToEndAsync();
                output = p.StandardOutput.ReadToEnd();
                log = err.Result;
                p.WaitForExit();
                if (p.ExitCode != 0) return null;
            }
        } catch {
            return null;
        }
        string fitted = output.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.StartsWith("-c "));
        if (fitted == null) return null;
        log = Regex.Replace(log, @"\x1b\[[0-9;]*m", "");
        long gpu = 0, ram = 0, block = 0;
        bool hostSeen = false;
        foreach (string line in log.Split('\n')) {
            Match device = Regex.Match(line, @"\|\s+-\s+(?!Host\b)[^|]+\|\s*\d+\s*=\s*\d+\s*\+\s*\(\s*(\d+)\s*=");
            Match host = Regex.Match(line, @"\|\s+-\s+Host\s+\|\s*(\d+)\s*=");
            if (device.Success) {
                if (hostSeen) { block = 0; hostSeen = false; }   // a new table
                block += long.Parse(device.Groups[1].Value);
            } else if (host.Success) {
                gpu = block;
                ram = long.Parse(host.Groups[1].Value);
                hostSeen = true;
            }
        }
        Match ngl = Regex.Match(fitted, @"-ngl (\d+)");
        return new Plan {
            Context = int.Parse(Regex.Match(fitted, @"-c (\d+)").Groups[1].Value), GpuMiB = gpu, RamMiB = ram,
            AllOnGpu = !fitted.Contains("-ot ") && ngl.Success && layers > 0 && int.Parse(ngl.Groups[1].Value) >= layers,
            fitted = fitted,
        };
    }

    /** llama-server's arguments: this plan's, or with no plan, llama.cpp's own fitting at the guaranteed context. */
    public static string Arguments(Plan plan) {
        var uri = new Uri(Model.Server);
        // --jinja: format every message with the chat template inside the model file.
        string common = "-m \"" + Paths.Model + "\" --log-file \"" + Paths.Log + "\" --alias " + Model.Name +
                        " --parallel 1 --jinja --host " + uri.Host + " --port " + uri.Port;
        if (plan == null) return common + " -c " + Guaranteed + " --fit on --load-mode mmap";
        return common + " " + plan.fitted + " --fit off --load-mode " + (plan.Mapped ? "mmap" : "none");
    }

    /** For the window's Server row: what was chosen (Paths.ServerPlan). */
    public string Describe() {
        return "context=" + Context + "\ngpu=" + GpuMiB + "\nram=" + RamMiB + "\nmapped=" + (Mapped ? 1 : 0) + "\n";
    }
}

// What the model file says about itself, from its GGUF header: the metadata (key-value pairs), then one entry per
// tensor, whose sizes add up to the parameter count.
static class Gguf {
    /** { name, parameters, context, license, layers }, or null if the file isn't GGUF. Reads a few MB (the tokenizer's
     *  tables come before the tensors), so call it off the UI thread. */
    public static Dictionary<string, object> Read(string file) {
        using (var r = new BinaryReader(new BufferedStream(File.OpenRead(file), 1 << 20))) {
            if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "GGUF") return null;
            r.ReadUInt32();   // the format's version
            long tensors = r.ReadInt64(), count = r.ReadInt64();
            var meta = new Dictionary<string, object>();
            for (long i = 0; i < count; i++) {
                string key = Text(r);
                meta[key] = Value(r, r.ReadUInt32());
            }
            long parameters = 0;
            for (long i = 0; i < tensors; i++) {
                Text(r);   // name
                uint dims = r.ReadUInt32();
                long n = 1;
                for (uint d = 0; d < dims; d++) n *= r.ReadInt64();
                r.ReadUInt32();   // type
                r.ReadUInt64();   // offset
                parameters += n;
            }
            object arch;
            meta.TryGetValue("general.architecture", out arch);
            return new Dictionary<string, object> {
                { "name", Get(meta, "general.name") },
                { "parameters", parameters },
                { "context", Get(meta, arch + ".context_length") },   // the most the model was trained for
                { "license", Get(meta, "general.license") },
                { "layers", Get(meta, arch + ".block_count") },
            };
        }
    }

    static object Get(Dictionary<string, object> meta, string key) {
        object v;
        return meta.TryGetValue(key, out v) ? v : null;
    }

    static string Text(BinaryReader r) {
        return Encoding.UTF8.GetString(r.ReadBytes((int)r.ReadInt64()));
    }

    static object Value(BinaryReader r, uint type) {
        switch (type) {
            case 0: return r.ReadByte();
            case 1: return r.ReadSByte();
            case 2: return r.ReadUInt16();
            case 3: return r.ReadInt16();
            case 4: return r.ReadUInt32();
            case 5: return r.ReadInt32();
            case 6: return r.ReadSingle();
            case 7: return r.ReadByte() != 0;
            case 8: return Text(r);
            case 9:   // an array: read past it; none is shown
                uint item = r.ReadUInt32();
                long n = r.ReadInt64();
                for (long i = 0; i < n; i++) Value(r, item);
                return null;
            case 10: return r.ReadUInt64();
            case 11: return r.ReadInt64();
            case 12: return r.ReadDouble();
            default: throw new InvalidDataException("unknown GGUF value type " + type);
        }
    }
}

// Who needs the model server: each open 2B window and agent leaves a file named by its pid in Paths.Clients.
static class Clients {
    public static void Add(int pid) {
        Directory.CreateDirectory(Paths.Clients);
        File.WriteAllText(Path.Combine(Paths.Clients, pid.ToString()), "");
    }

    public static void Remove(int pid) {
        try { File.Delete(Path.Combine(Paths.Clients, pid.ToString())); } catch { }
    }

    /** How many are still running; the files of the others are removed. */
    public static int Alive() {
        int alive = 0;
        if (!Directory.Exists(Paths.Clients)) return 0;
        foreach (string file in Directory.GetFiles(Paths.Clients)) {
            int pid;
            if (int.TryParse(Path.GetFileName(file), out pid) && Running(pid)) alive++;
            else try { File.Delete(file); } catch { }
        }
        return alive;
    }

    public static bool Running(int pid) {
        try { return !Process.GetProcessById(pid).HasExited; } catch { return false; }
    }
}

// JSON for what 2B.exe sends: dictionaries, strings, numbers, booleans and null.
static class Json {
    public static string Of(object value) {
        if (value == null) return "null";
        if (value is bool) return (bool)value ? "true" : "false";
        if (value is string) {
            var s = new StringBuilder("\"");
            foreach (char c in (string)value) {
                if (c == '"' || c == '\\') s.Append('\\').Append(c);
                else if (c < ' ') s.AppendFormat("\\u{0:x4}", (int)c);
                else s.Append(c);
            }
            return s.Append('"').ToString();
        }
        var map = value as IDictionary<string, object>;
        if (map != null) return "{" + string.Join(",", map.Select(p => Of(p.Key) + ":" + Of(p.Value))) + "}";
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}

// What every client of the model server gets, as llama-server's defaults: who the model is (identity.md), so Chat
// and the agent are the same 2B, and for llama-server's own chat page, 2B's look and settings (chat.css). Passed in
// the environment (LLAMA_ARG_*), which keeps the JSON off the command line.
static class Defaults {
    public static void Apply(System.Collections.Specialized.StringDictionary env) {
        // model_identity and reasoning_effort are variables of gpt-oss's chat template. model_identity replaces its
        // built-in "You are ChatGPT..." line, which outranks the developer message; the trailing \n leaves a blank
        // line before the template's own "Knowledge cutoff" line. The agent sends its own reasoning_effort (/effort).
        env["LLAMA_ARG_CHAT_TEMPLATE_KWARGS"] = Json.Of(new Dictionary<string, object> {
            { "model_identity", File.ReadAllText(Path.Combine(Paths.App, "identity.md")).Trim() + "\n" },
            { "reasoning_effort", "medium" },
        });
        // The chat page's settings (its own names). The page takes them on a user's first visit only; its look
        // (chat.css) the window adds every time instead (Window.ChatScript).
        env["LLAMA_ARG_UI_CONFIG"] = Json.Of(new Dictionary<string, object> {
            { "theme", "light" },   // 2B's look is light only
            { "showBuildVersion", false },
        });
    }
}

// The model server's keeper (2B.exe --server): starts llama-server, and stops it, by its pid, once no client has
// been running for a few seconds. One per install (the mutex); asking again while it runs does nothing.
static class Keeper {
    const int GraceSeconds = 10;   // after starting, before the first client must be there
    const int IdleSeconds = 3;     // with no clients, before stopping

    public static int Run() {
        string id = Convert.ToBase64String(Encoding.UTF8.GetBytes(Paths.Data.ToLowerInvariant())).Replace('/', '_');
        using (var mutex = new Mutex(false, @"Local\Agent2B-" + (id.Length > 200 ? id.Substring(id.Length - 200) : id))) {
            if (!mutex.WaitOne(0)) return 0;
            if (!File.Exists(Paths.Model) || !File.Exists(Paths.Llama)) return 1;
            Directory.CreateDirectory(Paths.Logs);
            var started = DateTime.Now;
            Plan plan = Plan.Make();
            if (plan != null) File.WriteAllText(Paths.ServerPlan, plan.Describe());
            else try { File.Delete(Paths.ServerPlan); } catch { }
            var start = new ProcessStartInfo(Paths.Llama, Plan.Arguments(plan)) {
                UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Paths.Root,
            };
            Defaults.Apply(start.EnvironmentVariables);
            var server = Process.Start(start);
            File.WriteAllText(Paths.ServerPid, server.Id.ToString());
            bool loaded = false;
            int idle = 0;
            while (!server.WaitForExit(1000)) {
                if (!loaded && Server.Healthy()) {
                    loaded = true;
                    File.WriteAllText(Paths.LoadSeconds, ((int)(DateTime.Now - started).TotalSeconds).ToString());
                }
                idle = Clients.Alive() > 0 ? 0 : idle + 1;
                if (idle >= IdleSeconds && (DateTime.Now - started).TotalSeconds > GraceSeconds) {
                    try { server.Kill(); } catch { }
                    break;
                }
            }
            try { File.Delete(Paths.ServerPid); File.Delete(Paths.ServerPlan); } catch { }
            return 0;
        }
    }
}

// The model server as the window sees it.
static class Server {
    /** Start it, unless it's running: a keeper of its own (2B.exe --server). */
    public static void Start() {
        Process.Start(new ProcessStartInfo(Paths.Exe, "--server") { UseShellExecute = false, CreateNoWindow = true });
    }

    /** "ready", "loading" or "stopped": this install's server, by its pid (another install's may answer the port). */
    public static string State() {
        int pid;
        try { pid = int.Parse(File.ReadAllText(Paths.ServerPid).Trim()); } catch { return "stopped"; }
        if (!Clients.Running(pid)) return "stopped";
        return Healthy() ? "ready" : "loading";
    }

    public static bool Healthy() {
        try {
            var request = (HttpWebRequest)WebRequest.Create(Model.Server + "/health");
            request.Proxy = null;
            request.Timeout = 2000;
            using (var response = (HttpWebResponse)request.GetResponse()) return response.StatusCode == HttpStatusCode.OK;
        } catch {
            return false;
        }
    }
}

// The terminal: mintty from the portable Git Bash, in the home folder, with the agent running; quitting it leaves a
// normal prompt where `2b` starts it again.
static class Terminal {
    public static void Open() {
        string mintty = Path.Combine(Paths.Git, @"usr\bin\mintty.exe");
        // -c: our look (mintty.conf), -i and -t: our icon and title (the config keeps programs from changing it).
        // First the agent (a Ctrl+C in it is the agent's, not this shell's), then a normal prompt.
        string args = "-c \"" + Path.Combine(Paths.App, "mintty.conf") + "\" -i \"" + Path.Combine(Paths.App, "icon.ico") +
                      "\" -t \"Agent 2B\" /usr/bin/bash --login -i -c \"trap : INT; 2b; trap - INT; exec bash --login -i\"";
        var start = new ProcessStartInfo(mintty, args) {
            UseShellExecute = false, WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };
        var env = start.EnvironmentVariables;
        // Git Bash keeps the PATH it starts with, after its own folders (unless ORIGINAL_PATH, left by another
        // Git Bash this was started from, says otherwise).
        env["PATH"] = Paths.Node + ";" + env["PATH"];
        env.Remove("ORIGINAL_PATH");
        env["MSYSTEM"] = "MINGW64";   // what git-bash.exe sets
        // The 2b command: bash defines a function from each BASH_FUNC_<name>%% variable it starts with, and passes
        // it on to the bashes it starts.
        env["BASH_FUNC_2b%%"] = "() { node \"" + Unix(Paths.Agent) + "\" \"$@\"; }";
        Process.Start(start);
    }

    /** C:\a\b as Git Bash writes it: /c/a/b. */
    static string Unix(string path) {
        return "/" + char.ToLowerInvariant(path[0]) + path.Substring(2).Replace('\\', '/');
    }
}

static class Program {
    [STAThread]
    static int Main(string[] args) {
        Paths.Data = Paths.FindData();
        // What the agent, and everything started from here, gets.
        Environment.SetEnvironmentVariable("AGENT_2B_DATA", Paths.Data);
        Environment.SetEnvironmentVariable("AGENT_2B_SERVER", Model.Server);
        Environment.SetEnvironmentVariable("AGENT_2B_EXE", Paths.Exe);
        UseProxy();
        if (args.Length == 1 && args[0] == "--server") return Keeper.Run();
        // The WebView2 assemblies are in runtime\webview2, not beside this file.
        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
            string file = Path.Combine(Paths.WebView2, new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(file) ? Assembly.LoadFrom(file) : null;
        };
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Run();
        return 0;
    }

    // Separate, so the WebView2 types are looked up after AssemblyResolve is in place.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run() {
        Application.Run(new Window());
    }

    /** The Windows proxy (e.g. Clash), for what doesn't read it itself: the agent's Node.js, and the commands it
     *  runs (curl, git, npm). The model server stays direct. */
    static void UseProxy() {
        if (Environment.GetEnvironmentVariable("HTTPS_PROXY") != null) return;
        var site = new Uri("https://huggingface.co/");
        Uri proxy;
        try { proxy = WebRequest.GetSystemWebProxy().GetProxy(site); } catch { return; }
        if (proxy == null || proxy == site) return;
        string url = proxy.GetLeftPart(UriPartial.Authority);
        Environment.SetEnvironmentVariable("HTTPS_PROXY", url);
        Environment.SetEnvironmentVariable("HTTP_PROXY", url);
        Environment.SetEnvironmentVariable("NO_PROXY", "127.0.0.1,localhost");
    }

    public static string Version {
        get {
            var a = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(
                Assembly.GetExecutingAssembly(), typeof(AssemblyInformationalVersionAttribute));
            return a.InformationalVersion;
        }
    }
}

// The model download: resumable (a .part file beside the model), through the Windows proxy, retried when the
// connection drops or stalls.
class Download {
    const int Tries = 6;
    const int StallSeconds = 60;

    public long Done, Total = Model.Size;
    public string Error;
    public bool Active;
    public DateTime LastProgress;
    CancellationTokenSource cancel;
    Stream body;

    public string Part { get { return Paths.Model + ".part"; } }

    public void Cancel() {
        if (cancel != null) cancel.Cancel();
        Drop();
    }

    /** Called by the window's timer: a connection with no data for a minute is dropped, and Run retries it. */
    public void Watch() {
        if (Active && body != null && (DateTime.Now - LastProgress).TotalSeconds > StallSeconds) Drop();
    }

    void Drop() {
        Stream b = body;
        if (b != null) try { b.Dispose(); } catch { }
    }

    public async Task Run() {
        Active = true;
        Error = null;
        cancel = new CancellationTokenSource();
        Directory.CreateDirectory(Path.GetDirectoryName(Paths.Model));
        Done = File.Exists(Part) ? new FileInfo(Part).Length : 0;
        LastProgress = DateTime.Now;
        try {
            var drive = new DriveInfo(Path.GetPathRoot(Paths.Model));
            long need = Model.Size - Done + (1L << 30);
            if (drive.AvailableFreeSpace < need) {
                throw new IOException(string.Format("2B needs {0:0.0} GB free on {1} for the model; there are {2:0.0} GB",
                                                    need / 1e9, drive.Name, drive.AvailableFreeSpace / 1e9));
            }
            for (int attempt = 1; ; attempt++) {
                try {
                    if (await Attempt()) break;
                } catch (Exception) {
                    if (cancel.IsCancellationRequested) throw new OperationCanceledException();
                    if (attempt >= Tries) throw;
                }
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, 2 * attempt)), cancel.Token);
            }
            if (File.Exists(Paths.Model)) File.Delete(Paths.Model);
            File.Move(Part, Paths.Model);
        } catch (OperationCanceledException) {
        } catch (Exception e) {
            Error = e.InnerException != null ? e.InnerException.Message : e.Message;
        } finally {
            Active = false;
            body = null;
        }
    }

    /** One request, from where the .part file ends. True once the file is complete. */
    async Task<bool> Attempt() {
        using (var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true, UseProxy = true })) {
            client.Timeout = TimeSpan.FromSeconds(StallSeconds);   // until the headers; Watch covers the body
            var request = new HttpRequestMessage(HttpMethod.Get, Model.Url);
            Done = File.Exists(Part) ? new FileInfo(Part).Length : 0;
            if (Done > 0) request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(Done, null);
            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel.Token)) {
                if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable) return true;   // nothing left
                response.EnsureSuccessStatusCode();
                bool resumed = response.StatusCode == HttpStatusCode.PartialContent;
                if (!resumed) Done = 0;   // the server sent it all again
                long? length = response.Content.Headers.ContentLength;
                if (length.HasValue) Total = Done + length.Value;
                using (var file = new FileStream(Part, resumed ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read, 1 << 20))
                using (body = await response.Content.ReadAsStreamAsync()) {
                    var buffer = new byte[1 << 20];
                    int n;
                    while ((n = await body.ReadAsync(buffer, 0, buffer.Length, cancel.Token)) > 0) {
                        await file.WriteAsync(buffer, 0, n);
                        Done += n;
                        LastProgress = DateTime.Now;
                    }
                }
                body = null;
                if (Done < Total) throw new IOException("the connection closed early");
                return true;
            }
        }
    }
}

// The 2B window. The page only draws: this sends it the whole state as JSON whenever something changes (Push), and
// the page sends back the name of what was clicked ("download", "chat", ...).
class Window : Form {
    const string Home = "https://2b.local/home.html";
    // On llama-server's chat page: its look (chat.css, which takes the place of CSS below); a way back to the home
    // page, top right, where the page has nothing; and the start screen's "Hello there" heading renamed to Agent 2B,
    // whenever the page draws it. Colors are palette.css's.
    const string ChatScript = @"
if (location.origin === '" + Model.Server + @"') addEventListener('DOMContentLoaded', () => {
  const style = document.createElement('style');
  style.textContent = CSS;
  document.head.appendChild(style);
  const b = document.createElement('button');
  b.innerHTML = '&larr; 2B';
  b.title = 'Back to the 2B home page';
  b.style.cssText = 'position:fixed;top:14px;right:16px;z-index:99999;padding:6px 14px;border-radius:999px;' +
    'border:1px solid #e2d8c8;background:#fbf8f3;color:#3d342b;font:600 13px Segoe UI,sans-serif;cursor:pointer;' +
    'box-shadow:0 1px 3px rgba(61,52,43,.12)';
  b.onclick = () => chrome.webview.postMessage('home');
  document.body.appendChild(b);
  const rename = () => {
    for (const h of document.querySelectorAll('h1')) {
      if (h.textContent.trim() === 'Hello there') h.innerHTML = 'Agent <span class=""two-b"">2B</span>';
    }
  };
  new MutationObserver(rename).observe(document.body, { childList: true, subtree: true });
  rename();
});";

    readonly WebView2 view = new WebView2();
    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    readonly Download download = new Download();
    readonly int pid = Process.GetCurrentProcess().Id;
    Dictionary<string, object> system;   // Requirements.Check(), null until it's done
    object info;          // what the model file says about itself (Gguf), once read
    string server = "stopped", error, shown;
    DateTime? asked;    // when this window asked for the server (Ask), until it's ready or has failed
    bool modelReady;
    long lastDone;
    DateTime lastTick = DateTime.Now;
    double speed;
    bool ticking;

    public Window() {
        Text = "Agent 2B";
        Icon = Icon.ExtractAssociatedIcon(Paths.Exe);
        ClientSize = new Size(940, 680);
        MinimumSize = new Size(640, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(0xfb, 0xf8, 0xf3);   // palette.css --bg, so opening doesn't flash white
        view.Dock = DockStyle.Fill;
        view.DefaultBackgroundColor = BackColor;
        Controls.Add(view);
        Load += async (s, e) => await Start();
        FormClosing += (s, e) => {
            if (download.Active && MessageBox.Show("Stop downloading the model? It picks up where it left off next time.",
                                                   "Agent 2B", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) {
                e.Cancel = true;
            }
        };
        FormClosed += (s, e) => Clients.Remove(pid);
        timer.Interval = 500;
        timer.Tick += (s, e) => Tick();
    }

    async Task Start() {
        Clients.Add(pid);   // the model server stays up while this window is open
        try {
            CoreWebView2Environment.SetLoaderDllFolderPath(Paths.WebView2);
            var env = await CoreWebView2Environment.CreateAsync(null, Paths.Browser);
            await view.EnsureCoreWebView2Async(env);
        } catch (WebView2RuntimeNotFoundException) {
            MessageBox.Show("2B needs the Microsoft Edge WebView2 Runtime, which comes with Windows 11 and most of Windows 10. " +
                            "The download page opens next.", "Agent 2B", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Process.Start("https://developer.microsoft.com/microsoft-edge/webview2/");
            Close();
            return;
        }
        var core = view.CoreWebView2;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreDevToolsEnabled = Environment.GetEnvironmentVariable("AGENT_2B_DEVTOOLS") != null;
        core.SetVirtualHostNameToFolderMapping("2b.local", Paths.App, CoreWebView2HostResourceAccessKind.Allow);
        string css = File.ReadAllText(Path.Combine(Paths.App, "chat.css"));
        await core.AddScriptToExecuteOnDocumentCreatedAsync(ChatScript.Replace("CSS;", Json.Of(css) + ";"));
        core.WebMessageReceived += (s, e) => {
            string what;
            try { what = e.TryGetWebMessageAsString(); } catch { return; }
            if (e.Source == Home || what == "home") OnMessage(what);   // the chat page only gets the way back
        };
        core.NewWindowRequested += (s, e) => {   // links that open a new window go to the browser
            e.Handled = true;
            Process.Start(e.Uri);
        };
        core.NavigationCompleted += (s, e) => { shown = null; };   // a new page: send it the state again
        core.Navigate(Home);
        CheckSystem();
        modelReady = File.Exists(Paths.Model);
        if (modelReady) { Ask(); ReadInfo(); }
        timer.Start();
    }

    /** Whether this PC can run 2B (Requirements), in the background: nvidia-smi takes a moment. */
    void CheckSystem() {
        Task.Run(() => { system = Requirements.Check(); });
    }

    /** Whether the model can be downloaded: the computer can run it (Requirements), and the disk has room. */
    bool CanDownload() {
        return system != null && Requirements.Supported((string)system["verdict"]) && Need() <= Free();
    }

    /** Bytes the download still needs on the data folder's drive, with 1 GB to spare. */
    long Need() {
        long part = File.Exists(download.Part) ? new FileInfo(download.Part).Length : 0;
        return Model.Size - part + (1L << 30);
    }

    long Free() {
        try { return new DriveInfo(Path.GetPathRoot(Paths.Data)).AvailableFreeSpace; } catch { return long.MaxValue; }
    }

    /** The model's name, parameters, context length and license, from its file, in the background. */
    void ReadInfo() {
        Task.Run(() => {
            try {
                var read = Gguf.Read(Paths.Model);
                if (read != null) {
                    read["size"] = new FileInfo(Paths.Model).Length;   // on disk, not the download's
                    // Files name themselves as they please (the shipped one says "gpt-oss-20b"); on screen it's 2B's.
                    read["name"] = Model.Title;
                }
                info = read;
            } catch {
                info = null;
            }
        });
    }

    void OnMessage(string what) {
        error = null;
        try {
            switch (what) {
                case "hello": shown = null; break;
                case "download":
                    if (!download.Active && CanDownload()) { lastDone = download.Done; Task.Run(() => download.Run()); }
                    break;
                case "cancel": download.Cancel(); break;
                case "start": Ask(); break;
                case "terminal": if (server == "ready") Terminal.Open(); break;
                case "chat": if (server == "ready") view.CoreWebView2.Navigate(Model.Server); break;
                case "home": view.CoreWebView2.Navigate(Home); break;
                case "folder": Directory.CreateDirectory(Paths.Data); Process.Start("explorer.exe", "\"" + Paths.Data + "\""); break;
                case "log": if (File.Exists(Paths.Log)) Process.Start("notepad.exe", "\"" + Paths.Log + "\""); break;
            }
        } catch (Exception e) {
            error = "Couldn't " + what + ": " + e.Message;
        }
        Push();
    }

    /** Start the model server, unless it's running. The window asks as soon as it opens, and again once the model is
     *  downloaded, so Chat and Terminal are ready by the time they're clicked; until then they're grayed out. */
    void Ask() {
        if (server != "stopped") return;
        asked = DateTime.Now;
        Server.Start();
    }

    void Tick() {
        if (ticking) return;
        ticking = true;
        Task.Run(() => {
            string state = Server.State();   // off the UI thread: it may wait on /health
            if (IsDisposed) return;
            BeginInvoke((Action)(() => {
                string was = server;
                server = state;
                ticking = false;
                Changed(was);
            }));
        });
    }

    void Changed(string was) {
        download.Watch();
        double secs = (DateTime.Now - lastTick).TotalSeconds;
        if (secs >= 1) {
            double now = (download.Done - lastDone) / secs;
            speed = speed == 0 ? now : speed * 0.7 + now * 0.3;
            lastDone = download.Done;
            lastTick = DateTime.Now;
        }
        bool ready = File.Exists(Paths.Model);
        if (ready && !modelReady) { Ask(); ReadInfo(); }   // just downloaded
        modelReady = ready;
        if (asked.HasValue && server == "ready") asked = null;
        // The keeper takes a moment to start llama-server, so "stopped" right after asking isn't a failure yet.
        if (asked.HasValue && server == "stopped" && (DateTime.Now - asked.Value).TotalSeconds > 30) {
            asked = null;
            error = "The model server stopped before it was ready. Out of memory? Close programs that use the GPU or a " +
                    "lot of RAM, and click Start.";
        }
        if (was == "ready" && server == "stopped") error = "The model server stopped. Click Start to start it again.";
        Push();
    }

    /** Send the page the state, if it changed. */
    void Push() {
        if (view.CoreWebView2 == null || view.CoreWebView2.Source != Home) return;
        long part = File.Exists(download.Part) ? new FileInfo(download.Part).Length : 0;
        var state = new Dictionary<string, object> {
            { "version", Program.Version },
            { "system", system },
            { "model", new Dictionary<string, object> {
                { "name", Model.Title }, { "size", Model.Size }, { "ready", File.Exists(Paths.Model) }, { "info", info },
                { "need", Need() }, { "free", Free() }, { "drive", Path.GetPathRoot(Paths.Data).TrimEnd('\\') } } },
            { "download", download.Active || download.Error != null || part > 0 ? new Dictionary<string, object> {
                { "active", download.Active },
                { "done", download.Active ? download.Done : part },
                { "total", download.Total },
                { "speed", download.Active ? Math.Round(speed) : 0 },
                { "error", download.Error },
            } : null },
            { "server", ServerState() },
            { "error", error },
        };
        string text = Json.Of(state);
        if (text == shown) return;
        shown = text;
        view.CoreWebView2.PostWebMessageAsJson(text);
    }

    /** The server's state for the page; while it loads, for how long, and how long the last load took. */
    Dictionary<string, object> ServerState() {
        string state = server == "stopped" && asked.HasValue ? "loading" : server;   // asked, not started yet
        var s = new Dictionary<string, object> { { "state", state }, { "plan", ReadPlan() } };
        if (state == "loading") {
            DateTime since = File.Exists(Paths.ServerPid) ? File.GetLastWriteTime(Paths.ServerPid) : asked ?? DateTime.Now;
            s["seconds"] = Math.Max(0, (int)(DateTime.Now - since).TotalSeconds);
            s["estimate"] = ReadInt(Paths.LoadSeconds);
        }
        return s;
    }

    /** The running server's plan (Plan.Describe), as { context, gpu, ram, mapped }; null before it has one. */
    static Dictionary<string, object> ReadPlan() {
        try {
            var plan = new Dictionary<string, object>();
            foreach (string line in File.ReadAllLines(Paths.ServerPlan)) {
                string[] kv = line.Split('=');
                if (kv.Length == 2) plan[kv[0]] = long.Parse(kv[1]);
            }
            return plan;
        } catch {
            return null;
        }
    }

    // ---------- helpers ----------
    static int ReadInt(string file) {
        int n;
        try { return int.TryParse(File.ReadAllText(file).Trim(), out n) ? n : 0; } catch { return 0; }
    }
}
