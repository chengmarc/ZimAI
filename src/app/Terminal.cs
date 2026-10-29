using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// The terminal: mintty from the portable Git Bash, in the home folder, with the agent running; quitting it leaves a
// normal prompt where `zim` starts it again.
// In the Store app it runs outside the package, like a Git Bash the user started: inside it, what the user and the
// agent run would have its writes to AppData and HKCU redirected into a copy only ZimAI sees (npm install -g, pip
// install --user, setx), and deleted with ZimAI. So the window starts cmd outside the package (Breakaway), cmd starts
// `ZimAI.exe --terminal`, and that starts mintty. Not ZimAI.exe directly: Windows keeps the package's own executables
// inside it when the package starts them, breakaway or not, but not when a process outside it does.
static class Terminal {
    public static void Open() {
        if (Paths.Packaged()) Breakaway.Start(Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                                              "/d /c \"\"" + Paths.Exe + "\" --terminal\"");
        else Start();
    }

    /** ZimAI.exe --terminal, or the portable app's window. Done once the terminal's title bar is colored. */
    public static Task Start() {
        string mintty = Path.Combine(Paths.Git, @"usr\bin\mintty.exe");
        // -c: our look (mintty.conf), -i and -t: our icon and title (the config keeps programs from changing it).
        // First the agent (a Ctrl+C in it is the agent's, not this shell's), then a normal prompt.
        string args = "-c \"" + Paths.MinttyConf + "\" -i \"" + Path.Combine(Paths.Ui, "icon.ico") +
                      "\" -t \"ZimAI\" /usr/bin/bash --login -i -c \"trap : INT; zim; trap - INT; exec bash --login -i\"";
        var start = new ProcessStartInfo(mintty, args) {
            UseShellExecute = false, WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };
        var env = start.EnvironmentVariables;
        // Git Bash keeps the PATH it starts with, after its own folders (unless ORIGINAL_PATH, left by another
        // Git Bash this was started from, says otherwise).
        env["PATH"] = Paths.Node + ";" + env["PATH"];
        env.Remove("ORIGINAL_PATH");
        env["MSYSTEM"] = "MINGW64";   // what git-bash.exe sets
        // The zim command: bash defines a function from each BASH_FUNC_<name>%% variable it starts with, and passes
        // it on to the bashes it starts.
        env["BASH_FUNC_zim%%"] = "() { node \"" + Unix(Paths.Agent) + "\" \"$@\"; }";
        var terminal = Process.Start(start);
        // mintty has no setting for its title bar, so it's colored from here, once its window is up (10 s at most)
        return Task.Run(() => {
            try {
                for (int i = 0; i < 100 && terminal.MainWindowHandle == IntPtr.Zero && !terminal.HasExited; i++) {
                    Thread.Sleep(100);
                    terminal.Refresh();
                }
                if (terminal.MainWindowHandle != IntPtr.Zero) TitleBar.Color(terminal.MainWindowHandle);
            } catch { }
        });
    }

    /** C:\a\b as Git Bash writes it: /c/a/b. */
    static string Unix(string path) {
        return "/" + char.ToLowerInvariant(path[0]) + path.Substring(2).Replace('\\', '/');
    }
}

// Starting a process outside the package, and so everything it starts: CreateProcess with
// PROC_THREAD_ATTRIBUTE_DESKTOP_APP_POLICY set to PROCESS_CREATION_DESKTOP_APP_BREAKAWAY_ENABLE_PROCESS_TREE. It gets
// this process's environment, and no console window.
static class Breakaway {
    const int ExtendedStartupInfoPresent = 0x80000, CreateNoWindow = 0x8000000;
    static readonly IntPtr DesktopAppPolicy = (IntPtr)0x20012;   // PROC_THREAD_ATTRIBUTE_DESKTOP_APP_POLICY
    const int EnableProcessTree = 1;                              // PROCESS_CREATION_DESKTOP_APP_BREAKAWAY_ENABLE_PROCESS_TREE

    [StructLayout(LayoutKind.Sequential)]
    struct StartupInfoEx {
        public int Size;
        public IntPtr Reserved, Desktop, Title;
        public int X, Y, XSize, YSize, XCountChars, YCountChars, FillAttribute, Flags;
        public short ShowWindow, Reserved2Size;
        public IntPtr Reserved2, StdInput, StdOutput, StdError;
        public IntPtr AttributeList;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct ProcessInformation {
        public IntPtr Process, Thread;
        public int ProcessId, ThreadId;
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool InitializeProcThreadAttributeList(IntPtr list, int count, int flags, ref IntPtr size);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool UpdateProcThreadAttribute(IntPtr list, int flags, IntPtr attribute, IntPtr value, IntPtr size,
                                                 IntPtr previous, IntPtr returnSize);
    [DllImport("kernel32.dll")]
    static extern void DeleteProcThreadAttributeList(IntPtr list);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CreateProcess(string application, StringBuilder commandLine, IntPtr processAttributes,
                                     IntPtr threadAttributes, bool inheritHandles, int flags, IntPtr environment,
                                     string directory, ref StartupInfoEx startup, out ProcessInformation info);
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);

    public static void Start(string exe, string args) {
        IntPtr size = IntPtr.Zero;
        InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
        IntPtr list = Marshal.AllocHGlobal(size), policy = Marshal.AllocHGlobal(4);
        try {
            if (!InitializeProcThreadAttributeList(list, 1, 0, ref size)) throw new Win32Exception();
            try {
                Marshal.WriteInt32(policy, EnableProcessTree);
                if (!UpdateProcThreadAttribute(list, 0, DesktopAppPolicy, policy, (IntPtr)4, IntPtr.Zero, IntPtr.Zero))
                    throw new Win32Exception();
                var startup = new StartupInfoEx { Size = Marshal.SizeOf(typeof(StartupInfoEx)), AttributeList = list };
                ProcessInformation info;
                if (!CreateProcess(exe, new StringBuilder("\"" + exe + "\" " + args), IntPtr.Zero, IntPtr.Zero, false,
                                   ExtendedStartupInfoPresent | CreateNoWindow, IntPtr.Zero, null, ref startup, out info))
                    throw new Win32Exception();
                CloseHandle(info.Thread);
                CloseHandle(info.Process);
            } finally {
                DeleteProcThreadAttributeList(list);
            }
        } finally {
            Marshal.FreeHGlobal(policy);
            Marshal.FreeHGlobal(list);
        }
    }
}
