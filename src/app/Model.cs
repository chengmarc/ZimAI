using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

// The model: the one file ZimAI uses, pinned by its URL, so its facts are fixed here with it.
static class Model {
    public const string Name = "ZimAI";         // its name in the API (--alias), which Chat shows; the agent sends none
    public const string Title = "GPT OSS 20B";  // its name on screen
    public const string Url = "https://huggingface.co/ggml-org/gpt-oss-20b-GGUF/resolve/ef9b12f2ff56c69cf32153a02784e7a3c88bf524/gpt-oss-20b-MXFP4.gguf";
    public const long Size = 12109566624;
    public const string Parameters = "20.9";   // billions; this and License are for the Model row
    public const string License = "Apache-2.0";
    public const int TrainedContext = 131072;   // tokens: the most it was trained for
    public const string Server = "http://127.0.0.1:8080";
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
                throw new IOException(string.Format("ZimAI needs {0:0.0} GB free on {1} for the model; there are {2:0.0} GB",
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
