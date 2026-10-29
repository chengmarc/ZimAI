using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Who needs the model server: each open ZimAI window and agent leaves a file named by its pid in Paths.Clients.
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

// What every client of the model server gets, as llama-server's defaults: who the model is (identity.md), so Chat
// and the agent are the same ZimAI, and for llama-server's own chat page, ZimAI's look and settings (chat.css). Passed in
// the environment (LLAMA_ARG_*), which keeps the JSON off the command line.
static class Defaults {
    public static void Apply(System.Collections.Specialized.StringDictionary env) {
        // model_identity and reasoning_effort are variables of gpt-oss's chat template. model_identity replaces its
        // built-in "You are ChatGPT..." line, which outranks the developer message; the trailing \n leaves a blank
        // line before the template's own "Knowledge cutoff" line. The agent sends its own reasoning_effort (/effort).
        env["LLAMA_ARG_CHAT_TEMPLATE_KWARGS"] = Json.Of(new Dictionary<string, object> {
            { "model_identity", File.ReadAllText(Paths.Identity).Trim() + "\n" },
            { "reasoning_effort", "medium" },
        });
        // The chat page's settings (its own names). The page takes them on a user's first visit only; its look
        // (chat.css) the window adds every time instead (Window.ChatScript).
        env["LLAMA_ARG_UI_CONFIG"] = Json.Of(new Dictionary<string, object> {
            { "theme", "light" },   // ZimAI's look is light only
            { "showBuildVersion", false },
        });
    }
}

// The model server's keeper (ZimAI.exe --server): starts llama-server, and stops it, by its pid, once no client has
// been running for a few seconds. One per install (the mutex); asking again while it runs does nothing.
// It says the server is ready (Paths.ServerReady) once it takes requests and, if part of the model is memory-mapped
// (Plan), once that part is read too: llama.cpp reads a mapped part from disk only as the model first uses it, so the
// first message would stall while a prompt pulls most of it in, in small scattered reads. Read here, start to end,
// while the server loads, it's in Windows' file cache before anyone types, and the wait is part of loading.
static class Keeper {
    const int GraceSeconds = 10;   // after starting, before the first client must be there
    const int IdleSeconds = 3;     // with no clients, before stopping

    public static int Run() {
        string id = Convert.ToBase64String(Encoding.UTF8.GetBytes(Paths.Data.ToLowerInvariant())).Replace('/', '_');
        using (var mutex = new Mutex(false, @"Local\ZimAI-" + (id.Length > 200 ? id.Substring(id.Length - 200) : id))) {
            if (!mutex.WaitOne(0)) return 0;
            if (!File.Exists(Paths.Model) || !File.Exists(Paths.Llama)) return 1;
            Directory.CreateDirectory(Paths.Logs);
            var started = DateTime.Now;
            // This process's pid, from the start: it runs exactly as long as the server, planning included, so the
            // window and the agent see one start time (the file's) and one process to check.
            File.WriteAllText(Paths.ServerPid, Process.GetCurrentProcess().Id.ToString());
            try { File.Delete(Paths.ServerReady); } catch { }
            bool cpu = Requirements.OnCpu();
            Plan plan = Plan.Make(cpu);
            if (plan != null) File.WriteAllText(Paths.ServerPlan, plan.Describe());
            else try { File.Delete(Paths.ServerPlan); } catch { }
            var start = new ProcessStartInfo(Paths.Llama, Plan.Arguments(plan, cpu)) {
                UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Paths.Root,
            };
            Defaults.Apply(start.EnvironmentVariables);
            var server = Process.Start(start);
            Task warm = plan == null || plan.Mapped ? Task.Run(() => Warm()) : null;   // with no plan, it's mapped too
            bool loaded = false;
            int idle = 0;
            while (!server.WaitForExit(1000)) {
                if (!loaded && (warm == null || warm.IsCompleted) && Server.Healthy()) {
                    loaded = true;
                    File.WriteAllText(Paths.LoadSeconds, ((int)(DateTime.Now - started).TotalSeconds).ToString());
                    File.WriteAllText(Paths.ServerReady, "");
                }
                idle = Clients.Alive() > 0 ? 0 : idle + 1;
                if (idle >= IdleSeconds && (DateTime.Now - started).TotalSeconds > GraceSeconds) {
                    try { server.Kill(); } catch { }
                    break;
                }
            }
            try { File.Delete(Paths.ServerReady); File.Delete(Paths.ServerPid); File.Delete(Paths.ServerPlan); } catch { }
            return 0;
        }
    }

    /** Read the model file once, start to end, into Windows' file cache; a failure only leaves the first message slow. */
    static void Warm() {
        try {
            var buffer = new byte[4 << 20];
            using (var file = new FileStream(Paths.Model, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096,
                                             FileOptions.SequentialScan)) {
                while (file.Read(buffer, 0, buffer.Length) > 0) { }
            }
        } catch { }
    }
}

// The model server as the window sees it.
static class Server {
    /** Start it, unless it's running: a keeper of its own (ZimAI.exe --server). */
    public static void Start() {
        Process.Start(new ProcessStartInfo(Paths.Exe, "--server") { UseShellExecute = false, CreateNoWindow = true });
    }

    /** Stop this install's llama-server (by its path: another install's may be running); its keeper then exits. */
    public static void Stop() {
        foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Paths.Llama))) {
            try {
                if (string.Equals(p.MainModule.FileName, Paths.Llama, StringComparison.OrdinalIgnoreCase)) p.Kill();
            } catch { }
        }
    }

    /** "ready", "loading" or "stopped": this install's server, by its pid (another install's may answer the port), and
     *  ready once its keeper says so (Keeper). */
    public static string State() {
        int pid;
        try { pid = int.Parse(File.ReadAllText(Paths.ServerPid).Trim()); } catch { return "stopped"; }
        if (!Clients.Running(pid)) return "stopped";
        return File.Exists(Paths.ServerReady) && Healthy() ? "ready" : "loading";
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
