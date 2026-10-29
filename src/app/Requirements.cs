using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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

// Whether this computer can run ZimAI, and how well, before the 12 GB download. The one place the requirements are
// decided; the README and the Store listing say the same: an NVIDIA GPU (10-series or newer) with 6 GB of VRAM or
// more, a driver with CUDA 12.4 or later, and 16 GB of RAM (12 GB+ of VRAM needs less). Without such a GPU it runs on
// the CPU, much slower, with 16 GB of RAM.
// The 6 GB floor: the GPU must hold the dense weights, the 64k context and llama.cpp's working memory (~4.2 GB), plus
// its 1 GB safety margin; the experts can go to RAM, which is why RAM matters below 12 GB of VRAM.
static class Requirements {
    const double MinVram = 5.8;   // GB: what a 6 GB card reports, give or take
    const double FullRam = 15;    // GB: what a 16 GB computer reports, give or take
    const double MinCuda = 12.4;  // the llama.cpp build's

    /** { name, vram, ram, cuda, gpuUsable, useGpu, verdict, reason } for the best NVIDIA GPU. verdict: "great", "good"
     *  (supported), "slow" (supported, slower), "cpu" (supported, on the CPU: no GPU it can run on, or Use GPU is
     *  off), or "unsupported". gpuUsable: there's a GPU the model can run on; useGpu: the Use GPU switch. */
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
        // A GPU the model can't run on leaves it to the CPU: verdict "cpu", and why not the GPU
        if (name == null) {
            verdict = "cpu";
            reason = "No NVIDIA GPU, so ZimAI runs on the CPU: replies come much slower than on a GPU.";
        } else if (cc > 0 && cc < 6) {
            verdict = "cpu";
            reason = "This GPU is older than the GTX 10-series, which ZimAI's llama.cpp doesn't support, so ZimAI runs on the CPU: replies come much slower.";
        } else if (vram < MinVram) {
            verdict = "cpu";
            reason = string.Format(CultureInfo.InvariantCulture,
                "This GPU has {0} GB of VRAM and ZimAI needs 6 GB, so ZimAI runs on the CPU: replies come much slower.", vram);
        } else if (cuda == null || !double.TryParse(cuda, NumberStyles.Float, CultureInfo.InvariantCulture, out cudaVersion) || cudaVersion < MinCuda) {
            verdict = "cpu";
            reason = "The NVIDIA driver is older than CUDA 12.4" + (cuda != null ? " (it has " + cuda + ")" : "") +
                     ", so ZimAI runs on the CPU, much slower. Update the driver to run it on the GPU.";
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
                "With {0} GB of VRAM, ZimAI needs {1} GB of RAM for the rest of the model; this PC has {2} GB.",
                vram, vram >= 11 ? 8 : 16, ram);
        }
        bool pascal = cc >= 6 && cc < 7;   // the 10-series: no tensor cores
        if (pascal && verdict == "great") verdict = "good";
        else if (pascal && verdict == "good") verdict = "slow";
        if (verdict == "slow") {
            reason = pascal ? "A GTX 10-series GPU has no tensor cores, so replies come slower than on an RTX."
                            : "With 6 GB of VRAM, most of the model runs from RAM, so replies come slower.";
        }
        bool gpuUsable = verdict != "cpu", useGpu = UseGpu;
        if (gpuUsable && !useGpu && verdict != "unsupported") {
            verdict = "cpu";
            reason = null;   // the switch and the row say it already
        }
        if (verdict == "cpu" && ram < FullRam) {   // on the CPU, all of the model is in RAM or read from disk
            verdict = "unsupported";
            reason = string.Format(CultureInfo.InvariantCulture,
                "On the CPU, ZimAI needs 16 GB of RAM; this PC has {0} GB.", ram) + (gpuUsable ? " Turn on Use GPU." : "");
        }
        return new Dictionary<string, object> {
            { "name", name }, { "vram", vram }, { "ram", ram }, { "cuda", cuda }, { "gpuUsable", gpuUsable },
            { "useGpu", useGpu }, { "verdict", verdict }, { "reason", reason },
        };
    }

    /** The window's Use GPU switch: on unless turned off. Its file in the data folder is there while it's off. */
    public static bool UseGpu {
        get { return !File.Exists(Paths.NoGpu); }
        set {
            if (value) File.Delete(Paths.NoGpu);
            else { Directory.CreateDirectory(Paths.Data); File.WriteAllText(Paths.NoGpu, ""); }
        }
    }

    /** Whether the verdict lets ZimAI download and run the model. */
    public static bool Supported(string verdict) {
        return verdict == "great" || verdict == "good" || verdict == "slow" || verdict == "cpu";
    }

    /** Whether the model runs on the CPU only: there's no GPU it can run on, or Use GPU is off (llama.cpp would use
     *  any GPU it finds). */
    public static bool OnCpu() {
        var check = Check();
        return !((bool)check["gpuUsable"] && (bool)check["useGpu"]);
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
