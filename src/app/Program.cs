// ZimAI.exe, the app. With no arguments it's the ZimAI window: home.html in WebView2, which shows the GPU, the model and
// the model server, and has Download model, Chat (llama-server's own chat page) and Terminal (Git Bash with the
// agent). The server starts when the home page's Start is clicked; Chat and Terminal wait for it. With
// --server it's the model server's keeper (Keeper): the window and each agent ask for the server that way. With
// --terminal it starts the terminal and exits (Terminal).
// It owns everything outside the agent: where things are (Paths), the model (Model), the server's settings and
// lifetime, the terminal, and the environment the agent gets (ZIMAI_*, the proxy).
// Its source is app/*.cs, one part per file (src/README.md lists them); this one is where it starts.
// packaging/build.ps1 compiles them with the C# compiler that comes with Windows (.NET Framework 4), which reads C# 5
// (no $"", no ?., no => members) and reads files in the system code page (so ASCII only).
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

[assembly: AssemblyTitle("ZimAI")]
[assembly: AssemblyProduct("ZimAI")]
[assembly: AssemblyInformationalVersion("1.1.0")]   // the version: MAJOR.MINOR.PATCH; packaging/build.ps1 reads it from here

static class Program {
    [STAThread]
    static int Main(string[] args) {
        Paths.Data = Paths.FindData();
        // What the agent, and everything started from here, gets.
        Environment.SetEnvironmentVariable("ZIMAI_DATA", Paths.Data);
        Environment.SetEnvironmentVariable("ZIMAI_SERVER", Model.Server);
        Environment.SetEnvironmentVariable("ZIMAI_EXE", Paths.Exe);
        UseProxy();
        if (args.Length == 1 && args[0] == "--server") return Keeper.Run();
        if (args.Length == 1 && args[0] == "--terminal") { Terminal.Start().Wait(); return 0; }
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
