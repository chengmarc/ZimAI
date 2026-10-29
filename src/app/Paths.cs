using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

// Where everything is. src/ is the app as it ships; data/ beside it is what the app makes as it runs, except in the
// Store app, whose install folder is read-only, and in a clone of the repo, which keeps it out of src/ in appdata/.
// The agent gets these through the environment (Main).
static class Paths {
    public static readonly string Root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
    public static readonly string Exe = Assembly.GetExecutingAssembly().Location;
    public static readonly string Node = Path.Combine(Root, @"runtime\node");
    public static readonly string Git = Path.Combine(Root, @"runtime\git");
    public static readonly string Llama = Path.Combine(Root, @"runtime\llama\llama-server.exe");
    public static readonly string WebView2 = Path.Combine(Root, @"runtime\webview2");
    public static readonly string Ui = Path.Combine(Root, "ui");   // the look: what the window serves, the terminal's config
    public static readonly string Identity = Path.Combine(Root, @"agent\prompt\identity.md");
    public static readonly string Agent = Path.Combine(Root, @"agent\main.ts");
    public static string Data;   // Main
    public static string Model { get { return Path.Combine(Data, @"model\_model.gguf"); } }
    public static string Browser { get { return Path.Combine(Data, "browser"); } }   // the window's browser data, chat history included
    public static string Logs { get { return Path.Combine(Data, "logs"); } }
    public static string NoGpu { get { return Path.Combine(Data, "no-gpu"); } }   // there while Use GPU is off
    public static string Log { get { return Path.Combine(Logs, "server.log"); } }            // llama-server's own log
    public static string ServerPid { get { return Path.Combine(Logs, "server.pid"); } }      // the keeper's, while it runs
    public static string ServerPlan { get { return Path.Combine(Logs, "server-plan"); } }    // how it runs (Plan.Describe)
    public static string LoadSeconds { get { return Path.Combine(Logs, "load-seconds"); } }  // the last load time, for the agent's loading screen
    public static string ServerReady { get { return Path.Combine(Logs, "server-ready"); } }  // there once the server takes requests (Keeper)
    public static string Clients { get { return Path.Combine(Logs, "clients"); } }           // <pid>: each open ZimAI window and agent

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern int GetCurrentPackageFamilyName(ref uint length, StringBuilder name);

    /** Whether this is the Store app, running inside its package. */
    public static bool Packaged() {
        uint length = 0;
        // ERROR_INSUFFICIENT_BUFFER (122) when packaged; APPMODEL_ERROR_NO_PACKAGE when not.
        return GetCurrentPackageFamilyName(ref length, null) == 122;
    }

    /** data/ beside the app, or in the Store app the package's LocalCache, which is deleted when it's uninstalled,
     *  or in a clone of the repo (src\ZimAI.exe, with .git beside src\) appdata\ at the repo's root, so the model and
     *  the logs stay out of the source tree. A ZimAI.exe started by the terminal or the agent runs outside the package
     *  (Terminal), so it takes the one its ZimAI window passed down. */
    public static string FindData() {
        string passed = Environment.GetEnvironmentVariable("ZIMAI_DATA");
        if (!string.IsNullOrEmpty(passed)) return passed;
        string repo = Path.GetDirectoryName(Root);
        if (Path.GetFileName(Root) == "src" && repo != null && Directory.Exists(Path.Combine(repo, ".git")))
            return Path.Combine(repo, "appdata");
        if (!Packaged()) return Path.Combine(Root, "data");
        uint length = 0;
        GetCurrentPackageFamilyName(ref length, null);
        var name = new StringBuilder((int)length);
        GetCurrentPackageFamilyName(ref length, name);
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "Packages", name.ToString(), "LocalCache");
    }
}
