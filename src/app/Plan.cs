using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// How llama-server runs the model on this computer, worked out before each start (by the keeper), because free
// VRAM and RAM change from one start to the next. llama.cpp's own fitter (llama-fit-params: a dry run that loads no
// weights) splits the model between the GPU and RAM; this decides the rest, for the same experience everywhere:
//  - context: decided by one thing, whether the GPU is used. On the GPU, the model's full trained context (128k),
//    however much VRAM there is: what doesn't fit goes to RAM. On the CPU, 64k: reading a prompt is slow there, so a
//    longer one would take minutes before the reply.
//  - RAM: the part that doesn't fit on the GPU is loaded into RAM only if that leaves room for the rest of the
//    computer; otherwise it's memory-mapped, read from disk as needed: slower, but nothing else gets pushed out.
//  - no GPU it can run on, or Use GPU off (Requirements.OnCpu): all of it in RAM. Loaded, the weights are repacked
//    for the CPU (prompts read twice as fast); memory-mapped, they aren't, since repacking copies them out of the
//    file and so leaves nothing mapped. The fitter is asked without repacking, which puts all of it in its Host row.
class Plan {
    public const int CpuContext = 65536;   // tokens
    const long SpareRam = 2L << 30;        // left for the rest of the computer
    const string CpuOnly = " --device none";

    public int Context;
    public long GpuMiB, RamMiB;   // what the fitter projects
    public bool Mapped;
    string fitted;                // the fitter's arguments: -c, -ngl and which tensors stay in RAM (-ot)

    /** Tokens of context: the model's whole trained context on the GPU, 64k on the CPU. */
    static int ContextFor(bool cpu) {
        return cpu ? CpuContext : Model.TrainedContext;
    }

    /** The plan for this start; null if the fitter couldn't make one (Arguments then lets llama-server fit). */
    public static Plan Make(bool cpu) {
        Plan plan = Fit(ContextFor(cpu), cpu);
        if (plan == null) return null;
        plan.Mapped = (plan.RamMiB << 20) + SpareRam > Memory.Available;
        return plan;
    }

    /** llama-fit-params' plan for this context: its arguments (on stdout), and from its log the memory each device
     *  and the host would use (the last breakdown table it prints is the plan's). */
    static Plan Fit(int context, bool cpu) {
        string exe = Path.Combine(Path.GetDirectoryName(Paths.Llama), "llama-fit-params.exe");
        var start = new ProcessStartInfo(exe, "-m \"" + Paths.Model + "\" -c " + context + " -np 1 -lv 4" +
                                              (cpu ? CpuOnly + " --no-repack" : "")) {
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
        return new Plan {
            Context = int.Parse(Regex.Match(fitted, @"-c (\d+)").Groups[1].Value), GpuMiB = gpu, RamMiB = ram,
            fitted = fitted,
        };
    }

    /** llama-server's arguments: this plan's, or with no plan, llama.cpp's own fitting at the same context. */
    public static string Arguments(Plan plan, bool cpu) {
        var uri = new Uri(Model.Server);
        // --jinja: format every message with the chat template inside the model file.
        string common = "-m \"" + Paths.Model + "\" --log-file \"" + Paths.Log + "\" --alias " + Model.Name +
                        " --parallel 1 --jinja --host " + uri.Host + " --port " + uri.Port + (cpu ? CpuOnly : "");
        if (plan == null) return common + (cpu ? " --no-repack" : "") + " -c " + ContextFor(cpu) + " --fit on --load-mode mmap";
        return common + (cpu && plan.Mapped ? " --no-repack" : "") + " " + plan.fitted + " --fit off --load-mode " +
               (plan.Mapped ? "mmap" : "none");
    }

    /** For the window's Server row: what was chosen (Paths.ServerPlan). */
    public string Describe() {
        return "context=" + Context + "\ngpu=" + GpuMiB + "\nram=" + RamMiB + "\nmapped=" + (Mapped ? 1 : 0) + "\n";
    }
}
