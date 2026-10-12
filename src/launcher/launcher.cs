// 2B.exe, the app's entry point (a Store package must start an .exe): it runs app.cmd. In the Store app it first
// points AGENT_2B_DATA at the package's data folder, since the install folder is read-only; elsewhere the data
// stays in data/ beside it. tools/build.ts compiles it with the C# compiler that comes with Windows (.NET Framework 4).
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

static class Launcher {
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern int GetCurrentPackageFamilyName(ref uint length, StringBuilder name);

    static int Main() {
        string root = AppDomain.CurrentDomain.BaseDirectory;
        uint length = 0;
        // ERROR_INSUFFICIENT_BUFFER (122) when packaged; APPMODEL_ERROR_NO_PACKAGE when not.
        if (GetCurrentPackageFamilyName(ref length, null) == 122) {
            var name = new StringBuilder((int)length);
            GetCurrentPackageFamilyName(ref length, name);
            // LocalCache: the package's own folder, deleted when the app is uninstalled.
            string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                       "Packages", name.ToString(), "LocalCache");
            Environment.SetEnvironmentVariable("AGENT_2B_DATA", data);
        }
        var start = new ProcessStartInfo("cmd.exe", "/c \"\"" + Path.Combine(root, "app.cmd") + "\"\"");
        start.UseShellExecute = false;
        start.CreateNoWindow = true;   // app.cmd only opens the 2B window
        start.WorkingDirectory = root;
        Process.Start(start).WaitForExit();
        return 0;
    }
}
