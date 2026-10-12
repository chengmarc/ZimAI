// 2B.exe: the app's window. It shows home.html in WebView2 and does what the page asks: download the model, open the
// terminal (app.cmd, the 2B agent in Git Bash), or start the model server and show llama-server's own chat page.
// The page only draws; this file pushes it the whole state (State) whenever something changes.
// tools/build.ts compiles it with the C# compiler that comes with Windows (.NET Framework 4), which reads C# 5:
// no string interpolation, no ?. and no => members.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

// Where everything is: the same places as agent/paths.ts and app.cmd.
static class Paths {
    public static readonly string Root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
    public static string Data;   // data/ beside this, or in the Store app the package's own folder (Main)
    public static readonly string Node = Path.Combine(Root, @"runtime\node\node.exe");
    public static readonly string WebView2 = Path.Combine(Root, @"runtime\webview2");
    public static readonly string Page = Path.Combine(Root, "launcher");
    public static string Model { get { return Path.Combine(Data, @"model\_model.gguf"); } }
    public static string Log { get { return Path.Combine(Data, @"logs\server.log"); } }
    public static string LoadSeconds { get { return Path.Combine(Data, @"logs\load-seconds"); } }
    public static string Agents { get { return Path.Combine(Data, @"logs\agents"); } }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern int GetCurrentPackageFamilyName(ref uint length, StringBuilder name);

    /** The package family name when running as the Store app, else null. */
    public static string Package() {
        uint length = 0;
        // ERROR_INSUFFICIENT_BUFFER (122) when packaged; APPMODEL_ERROR_NO_PACKAGE when not.
        if (GetCurrentPackageFamilyName(ref length, null) != 122) return null;
        var name = new StringBuilder((int)length);
        return GetCurrentPackageFamilyName(ref length, name) == 0 ? name.ToString() : null;
    }
}

static class Program {
    [STAThread]
    static void Main() {
        string package = Paths.Package();
        // The Store app's install folder is read-only; LocalCache is its own, deleted when it's uninstalled.
        Paths.Data = package == null ? Path.Combine(Paths.Root, "data")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages", package, "LocalCache");
        Environment.SetEnvironmentVariable("AGENT_2B_DATA", Paths.Data);   // for the agent and the server it starts
        // The WebView2 assemblies are in runtime\webview2, not beside this file.
        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
            string file = Path.Combine(Paths.WebView2, new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(file) ? Assembly.LoadFrom(file) : null;
        };
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Run();
    }

    // Separate, so the WebView2 types are looked up after AssemblyResolve is in place.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run() {
        Application.Run(new Launcher());
    }
}

// The model download: resumable (a .part file beside the model), through the Windows proxy, retried when the
// connection drops or stalls.
class Download {
    // The same file tools/fetch.cmd used to fetch: gpt-oss-20b, MXFP4 (ggml-org/gpt-oss-20b-GGUF).
    public const string Url = "https://huggingface.co/ggml-org/gpt-oss-20b-GGUF/resolve/ef9b12f2ff56c69cf32153a02784e7a3c88bf524/gpt-oss-20b-MXFP4.gguf";
    public const long Size = 12109566624;   // for the disk-space check, before the server says
    const int Tries = 6;
    const int StallSeconds = 60;

    public long Done, Total = Size;
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
            long need = Size - Done + (1L << 30);
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
            var request = new HttpRequestMessage(HttpMethod.Get, Url);
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

// What the page shows. Serialized as is, so the field names are the page's.
class State {
    public object gpu;         // { name, vram (GB), cuda } or null without an NVIDIA GPU; false until checked
    public bool model;         // downloaded
    public object download;    // { done, total, speed (bytes/s), error } while downloading or after it failed
    public string server;      // "stopped", "loading" or "ready"
    public object loading;     // { seconds, estimate } while the model loads for the chat
    public string error;       // the last thing that went wrong, for the page to show
    public string data;        // the data folder
}

class Launcher : Form {
    const string Home = "https://2b.local/home.html";
    const string Server = "http://127.0.0.1:8080";
    // On llama-server's chat page: a way back to the home page, top right, where the page has nothing.
    const string BackButton = @"
if (location.origin === 'http://127.0.0.1:8080') addEventListener('DOMContentLoaded', () => {
  const b = document.createElement('button');
  b.innerHTML = '&larr; 2B';   // ASCII only: csc reads this file in the system code page
  b.title = 'Back to the 2B home page';
  b.style.cssText = 'position:fixed;top:14px;right:16px;z-index:99999;padding:6px 14px;border-radius:999px;' +
    'border:1px solid #e2d8c8;background:#fbf8f3;color:#3d342b;font:600 13px Segoe UI,sans-serif;cursor:pointer;' +
    'box-shadow:0 1px 3px rgba(61,52,43,.12)';
  b.onclick = () => chrome.webview.postMessage({ do: 'home' });
  document.body.appendChild(b);
});";

    readonly WebView2 view = new WebView2();
    readonly JavaScriptSerializer json = new JavaScriptSerializer();
    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    readonly HttpClient local = new HttpClient(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(2) };
    readonly Download download = new Download();
    object gpu = false;
    string server = "stopped", error, shown;
    DateTime? chatSince;   // when Chat was clicked, until the chat page is up
    long lastDone;
    DateTime lastTick = DateTime.Now;
    double speed;
    bool checking;

    public Launcher() {
        Text = "Agent 2B";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        ClientSize = new Size(940, 680);
        MinimumSize = new Size(640, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(251, 248, 243);   // the page's background, so opening doesn't flash white
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
        timer.Interval = 500;
        timer.Tick += async (s, e) => await Tick();
    }

    async Task Start() {
        try {
            CoreWebView2Environment.SetLoaderDllFolderPath(Paths.WebView2);
            var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Paths.Data, "webview2"));
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
        core.Settings.AreDevToolsEnabled = Debugger.IsAttached || Environment.GetEnvironmentVariable("AGENT_2B_DEVTOOLS") != null;
        core.SetVirtualHostNameToFolderMapping("2b.local", Paths.Page, CoreWebView2HostResourceAccessKind.Allow);
        await core.AddScriptToExecuteOnDocumentCreatedAsync(BackButton);
        core.WebMessageReceived += (s, e) => OnMessage(e.Source, e.WebMessageAsJson);
        core.NewWindowRequested += (s, e) => {   // links that open a new window go to the browser
            e.Handled = true;
            Process.Start(e.Uri);
        };
        core.NavigationCompleted += (s, e) => { shown = null; };   // a new page: send it the state again
        core.Navigate(Home);
        Register();
        CheckGpu();
        timer.Start();
    }

    /** Count this window as a 2B window, so the model server stays up while it's open and stops after the last one
     *  (agent/server/stopper.ts, the same as each terminal agent). */
    void Register() {
        try {
            Directory.CreateDirectory(Paths.Agents);
            int pid = Process.GetCurrentProcess().Id;
            File.WriteAllText(Path.Combine(Paths.Agents, pid.ToString()), "");
            Hidden(Paths.Node, Quote(Path.Combine(Paths.Root, @"agent\server\stopper.ts")) + " " + pid, false);
        } catch (Exception e) {
            error = "Couldn't start the server's stopper (" + e.Message + "): stop llama-server in Task Manager after quitting.";
        }
    }

    /** The GPU, from nvidia-smi (it comes with the NVIDIA driver), in the background. */
    void CheckGpu() {
        Task.Run(() => {
            try {
                string list = Output("nvidia-smi", "--query-gpu=name,memory.total --format=csv,noheader,nounits");
                string first = list.Split('\n')[0].Trim();
                if (first == "") { gpu = null; return; }
                string[] parts = first.Split(',');
                Match cuda = Regex.Match(Output("nvidia-smi", ""), @"CUDA Version:\s*([\d.]+)");
                gpu = new Dictionary<string, object> {
                    { "name", parts[0].Trim() },
                    { "vram", parts.Length > 1 ? Math.Round(double.Parse(parts[1].Trim()) / 1024, 1) : 0 },
                    { "cuda", cuda.Success ? cuda.Groups[1].Value : null },
                };
            } catch {
                gpu = null;
            }
        });
    }

    void OnMessage(string source, string message) {
        Dictionary<string, object> m;
        try {
            m = json.Deserialize<Dictionary<string, object>>(message);
        } catch {
            return;
        }
        string what = m != null && m.ContainsKey("do") ? m["do"] as string : null;
        if (source != Home && what != "home") return;   // the chat page only gets the way back
        error = null;
        switch (what) {
            case "hello": shown = null; break;
            case "download":
                if (!download.Active) { lastDone = download.Done; Task.Run(() => download.Run()); }
                break;
            case "cancel": download.Cancel(); break;
            case "terminal": Terminal(); break;
            case "chat": Chat(); break;
            case "home": chatSince = null; view.CoreWebView2.Navigate(Home); break;
            case "folder": Directory.CreateDirectory(Paths.Data); Process.Start("explorer.exe", Quote(Paths.Data)); break;
            case "log": if (File.Exists(Paths.Log)) Process.Start("notepad.exe", Quote(Paths.Log)); break;
        }
        Push();
    }

    void Terminal() {
        try {
            Hidden("cmd.exe", "/c \"" + Quote(Path.Combine(Paths.Root, "app.cmd")) + "\"", false);
        } catch (Exception e) {
            error = "Couldn't open the terminal: " + e.Message;
        }
    }

    void Chat() {
        if (server == "ready") { view.CoreWebView2.Navigate(Server); return; }
        chatSince = DateTime.Now;
        // agent/server/server.ts starts llama-server with the agent's settings, unless it's running already.
        Task.Run(() => {
            try {
                var p = Hidden(Paths.Node, Quote(Path.Combine(Paths.Root, @"agent\server\server.ts")), true);
                string problem = p.StandardError.ReadToEnd().Trim();
                p.WaitForExit();
                if (p.ExitCode != 0) { error = problem != "" ? problem : "the model server didn't start"; chatSince = null; }
            } catch (Exception e) {
                error = "Couldn't start the model server: " + e.Message;
                chatSince = null;
            }
        });
    }

    async Task Tick() {
        if (checking) return;
        checking = true;
        try {
            server = await Health();
            if (server != "ready" && !Process.GetProcessesByName("llama-server").Any()) server = "stopped";
            download.Watch();
            double secs = (DateTime.Now - lastTick).TotalSeconds;
            if (secs >= 1) {
                double now = (download.Done - lastDone) / secs;
                speed = speed == 0 ? now : speed * 0.7 + now * 0.3;
                lastDone = download.Done;
                lastTick = DateTime.Now;
            }
            if (chatSince.HasValue) {
                double waited = (DateTime.Now - chatSince.Value).TotalSeconds;
                if (server == "ready") {
                    if (waited > 1) TryWrite(Paths.LoadSeconds, ((int)waited).ToString());
                    chatSince = null;
                    view.CoreWebView2.Navigate(Server);
                } else if (server == "stopped" && waited > 20) {
                    chatSince = null;
                    error = "The model server stopped before it was ready. Out of memory? Close programs that use the GPU " +
                            "or a lot of RAM, and try again.";
                }
            }
            Push();
        } finally {
            checking = false;
        }
    }

    async Task<string> Health() {
        try {
            var res = await local.GetAsync(Server + "/health");
            return res.IsSuccessStatusCode ? "ready" : "loading";
        } catch {
            return "loading";   // or stopped: Tick looks for the process
        }
    }

    /** Send the page the state, if it changed. */
    void Push() {
        if (view.CoreWebView2 == null || view.CoreWebView2.Source != Home) return;
        bool partial = File.Exists(download.Part);
        var state = new State {
            gpu = gpu,
            model = File.Exists(Paths.Model),
            download = download.Active || download.Error != null || partial ? new Dictionary<string, object> {
                { "active", download.Active },
                { "done", download.Active ? download.Done : partial ? new FileInfo(download.Part).Length : 0 },
                { "total", download.Total },
                { "speed", download.Active ? Math.Round(speed) : 0 },
                { "error", download.Error },
            } : null,
            server = server,
            loading = chatSince.HasValue ? new Dictionary<string, object> {
                { "seconds", (int)(DateTime.Now - chatSince.Value).TotalSeconds },
                { "estimate", ReadInt(Paths.LoadSeconds) },
            } : null,
            error = error,
            data = Paths.Data,
        };
        string text = json.Serialize(state);
        if (text == shown) return;
        shown = text;
        view.CoreWebView2.PostWebMessageAsJson(text);
    }

    // ---------- helpers ----------
    static string Quote(string s) {
        return "\"" + s + "\"";
    }

    static Process Hidden(string exe, string args, bool readError) {
        var start = new ProcessStartInfo(exe, args) {
            UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Paths.Root, RedirectStandardError = readError,
        };
        return Process.Start(start);
    }

    static string Output(string exe, string args) {
        var start = new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
        using (var p = Process.Start(start)) {
            string text = p.StandardOutput.ReadToEnd();
            p.WaitForExit(10000);
            return text;
        }
    }

    static int ReadInt(string file) {
        int n;
        try { return int.TryParse(File.ReadAllText(file).Trim(), out n) ? n : 0; } catch { return 0; }
    }

    static void TryWrite(string file, string text) {
        try { File.WriteAllText(file, text); } catch { }
    }
}
