using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

// The title bar of ZimAI's windows, the app's and the terminal's, in palette.css's colors, so it runs into what's
// below it: --bg, with --strong text and a --line border. Windows 11 only; Windows 10 ignores it and keeps its own.
static class TitleBar {
    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    /** Any window's, this process's or not. */
    public static void Color(IntPtr window) {
        Set(window, 34, 0xe2d8c8);   // DWMWA_BORDER_COLOR
        Set(window, 35, 0xfbf8f3);   // DWMWA_CAPTION_COLOR
        Set(window, 36, 0x3d342b);   // DWMWA_TEXT_COLOR
    }

    static void Set(IntPtr window, int attribute, int rgb) {
        int color = (rgb >> 16 & 0xff) | (rgb & 0xff00) | (rgb & 0xff) << 16;   // a COLORREF: 0x00BBGGRR
        DwmSetWindowAttribute(window, attribute, ref color, 4);
    }
}

// The ZimAI window. The page only draws: this sends it the whole state as JSON whenever something changes (Push), and
// the page sends back the name of what was clicked ("download", "chat", ...).
class Window : Form {
    const string Home = "https://zimai.local/home.html";
    // On llama-server's chat page: its look (chat.css, which takes the place of CSS below); a way back to the home
    // page, top right, where the page has nothing; and the start screen's "Hello there" heading renamed to ZimAI,
    // whenever the page draws it. Colors are palette.css's.
    const string ChatScript = @"
if (location.origin === '" + Model.Server + @"') addEventListener('DOMContentLoaded', () => {
  const style = document.createElement('style');
  style.textContent = CSS;
  document.head.appendChild(style);
  const b = document.createElement('button');
  b.innerHTML = '&larr; ZimAI';
  b.title = 'Back to the ZimAI home page';
  b.style.cssText = 'position:fixed;top:14px;right:16px;z-index:99999;padding:6px 14px;border-radius:999px;' +
    'border:1px solid #e2d8c8;background:#fbf8f3;color:#3d342b;font:600 13px Segoe UI,sans-serif;cursor:pointer;' +
    'box-shadow:0 1px 3px rgba(61,52,43,.12)';
  b.onclick = () => chrome.webview.postMessage('home');
  document.body.appendChild(b);
  const rename = () => {
    for (const h of document.querySelectorAll('h1')) {
      if (h.textContent.trim() === 'Hello there') h.innerHTML = 'Zim<span class=""ai"">AI</span>';
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
    string server = "stopped", error, shown;
    DateTime? asked;    // when this window asked for the server (Ask), until it's ready or has failed
    bool restarting;    // Use GPU changed while the server ran: stopping it, to start it again with the new setting
    long lastDone;
    DateTime lastTick = DateTime.Now;
    double speed;
    bool ticking;

    public Window() {
        Text = "ZimAI";
        Icon = Icon.ExtractAssociatedIcon(Paths.Exe);
        // 960 x 640 inside the frame, and no smaller: it fits a 1080p laptop at 150% scaling, the smallest common screen
        ClientSize = new Size(960, 640);
        MinimumSize = Size;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(0xfb, 0xf8, 0xf3);   // palette.css --bg, so opening doesn't flash white
        view.Dock = DockStyle.Fill;
        view.DefaultBackgroundColor = BackColor;
        Controls.Add(view);
        Load += async (s, e) => await Start();
        FormClosing += (s, e) => {
            if (download.Active && MessageBox.Show("Stop downloading the model? It picks up where it left off next time.",
                                                   "ZimAI", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No) {
                e.Cancel = true;
            }
        };
        FormClosed += (s, e) => Clients.Remove(pid);
        timer.Interval = 500;
        timer.Tick += (s, e) => Tick();
    }

    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        TitleBar.Color(Handle);
    }

    async Task Start() {
        Clients.Add(pid);   // the model server stays up while this window is open
        try {
            CoreWebView2Environment.SetLoaderDllFolderPath(Paths.WebView2);
            var env = await CoreWebView2Environment.CreateAsync(null, Paths.Browser);
            await view.EnsureCoreWebView2Async(env);
        } catch (WebView2RuntimeNotFoundException) {
            MessageBox.Show("ZimAI needs the Microsoft Edge WebView2 Runtime, which comes with Windows 11 and most of Windows 10. " +
                            "The download page opens next.", "ZimAI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Process.Start("https://developer.microsoft.com/microsoft-edge/webview2/");
            Close();
            return;
        }
        var core = view.CoreWebView2;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.AreDevToolsEnabled = Environment.GetEnvironmentVariable("ZIMAI_DEVTOOLS") != null;
        core.SetVirtualHostNameToFolderMapping("zimai.local", Paths.Ui, CoreWebView2HostResourceAccessKind.Allow);
        string css = File.ReadAllText(Path.Combine(Paths.Ui, "chat.css"));
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
        timer.Start();
    }

    /** Whether this PC can run ZimAI (Requirements), in the background: nvidia-smi takes a moment. */
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
                case "gpu":
                    Requirements.UseGpu = !Requirements.UseGpu;
                    if (system != null) {   // the switch moves at once; the verdict follows from CheckSystem
                        var now = new Dictionary<string, object>(system);
                        now["useGpu"] = Requirements.UseGpu;
                        system = now;
                    }
                    CheckSystem();
                    if (server != "stopped" || asked.HasValue) { restarting = true; asked = null; Server.Stop(); }
                    break;
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

    /** Start the model server, unless it's running: when Start is clicked, or again after Use GPU changes while it
     *  runs. Until it's ready, Chat and Terminal are grayed out. */
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
        if (restarting) {
            // Again each tick: a keeper still planning starts its llama-server after the first Stop.
            if (server == "stopped") { restarting = false; Ask(); }
            else Server.Stop();
            Push();
            return;
        }
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
                { "name", Model.Title }, { "size", Model.Size }, { "parameters", Model.Parameters }, { "license", Model.License },
                { "ready", File.Exists(Paths.Model) },
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

    /** The server's state for the page, and how it runs. */
    Dictionary<string, object> ServerState() {
        string state = restarting ? "loading" : server == "stopped" && asked.HasValue ? "loading" : server;   // asked, not started yet
        return new Dictionary<string, object> { { "state", state }, { "plan", ReadPlan() } };
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
}
