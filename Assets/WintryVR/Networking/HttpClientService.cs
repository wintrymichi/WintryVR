using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using WintryVR.Core;

namespace WintryVR.Networking
{
    public class HttpResponse
    {
        public bool Success;
        public long StatusCode;
        public string Body = "";
        public byte[] Bytes;
        public string Error = "";
        public float LatencyMs;
    }

    /// <summary>
    /// Async wrapper over UnityWebRequest (which must be driven from the main thread) with timeouts,
    /// bounded retries for transient failures and latency measurement for the debug panel.
    /// </summary>
    public static class HttpClientService
    {
        public static float LastLatencyMs { get; private set; }
        public static int InFlight { get; private set; }

        public static Task<HttpResponse> PostJsonAsync(string url, string json, Dictionary<string, string> headers, int timeoutSeconds, CancellationToken ct, int retries = 1)
            => SendAsync("POST", url, Encoding.UTF8.GetBytes(json ?? ""), "application/json", headers, timeoutSeconds, ct, retries);

        public static Task<HttpResponse> PostBytesAsync(string url, byte[] body, string contentType, Dictionary<string, string> headers, int timeoutSeconds, CancellationToken ct, int retries = 1)
            => SendAsync("POST", url, body, contentType, headers, timeoutSeconds, ct, retries);

        public static Task<HttpResponse> GetAsync(string url, Dictionary<string, string> headers, int timeoutSeconds, CancellationToken ct, int retries = 1)
            => SendAsync("GET", url, null, null, headers, timeoutSeconds, ct, retries);

        /// <summary>Multipart POST (used for audio transcription endpoints).</summary>
        public static Task<HttpResponse> PostMultipartAsync(string url, List<IMultipartFormSection> form, Dictionary<string, string> headers, int timeoutSeconds, CancellationToken ct)
        {
            byte[] boundary = UnityWebRequest.GenerateBoundary();
            byte[] body = UnityWebRequest.SerializeFormSections(form, boundary);
            string contentType = "multipart/form-data; boundary=" + Encoding.UTF8.GetString(boundary);
            return SendAsync("POST", url, body, contentType, headers, timeoutSeconds, ct, 0);
        }

        private static async Task<HttpResponse> SendAsync(string method, string url, byte[] body, string contentType, Dictionary<string, string> headers, int timeoutSeconds, CancellationToken ct, int retries)
        {
            HttpResponse last = null;
            for (int attempt = 0; attempt <= retries; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                last = await SendOnceAsync(method, url, body, contentType, headers, timeoutSeconds, ct);
                if (last.Success) return last;
                bool transient = last.StatusCode == 0 || last.StatusCode == 408 || last.StatusCode == 429 || last.StatusCode >= 500;
                if (!transient || attempt == retries) return last;
                int delayMs = 500 * (1 << attempt);
                WintryLog.W("Http", "Transient failure (" + last.StatusCode + ") on " + url + ", retrying in " + delayMs + "ms");
                await Task.Delay(delayMs, ct);
            }
            return last;
        }

        private static Task<HttpResponse> SendOnceAsync(string method, string url, byte[] body, string contentType, Dictionary<string, string> headers, int timeoutSeconds, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<HttpResponse>();
            MainThreadDispatcher.Enqueue(() =>
            {
                UnityWebRequest req;
                try
                {
                    req = new UnityWebRequest(url, method);
                    if (body != null) req.uploadHandler = new UploadHandlerRaw(body);
                    if (!string.IsNullOrEmpty(contentType)) req.SetRequestHeader("Content-Type", contentType);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.timeout = Mathf.Max(5, timeoutSeconds);
                    if (headers != null) foreach (var kv in headers) req.SetRequestHeader(kv.Key, kv.Value);
                }
                catch (Exception ex)
                {
                    tcs.SetResult(new HttpResponse { Success = false, Error = ex.Message });
                    return;
                }

                float start = Time.realtimeSinceStartup;
                InFlight++;
                var op = req.SendWebRequest();
                CancellationTokenRegistration reg = default;
                if (ct.CanBeCanceled) reg = ct.Register(() => MainThreadDispatcher.Enqueue(() => { try { if (!req.isDone) req.Abort(); } catch { } }));
                op.completed += _ =>
                {
                    InFlight--;
                    reg.Dispose();
                    var r = new HttpResponse
                    {
                        StatusCode = req.responseCode,
                        LatencyMs = (Time.realtimeSinceStartup - start) * 1000f,
                        Bytes = req.downloadHandler != null ? req.downloadHandler.data : null,
                        Body = req.downloadHandler != null ? (req.downloadHandler.text ?? "") : ""
                    };
                    LastLatencyMs = r.LatencyMs;
                    r.Success = req.result == UnityWebRequest.Result.Success;
                    if (!r.Success) r.Error = req.error + (string.IsNullOrEmpty(r.Body) ? "" : " :: " + Truncate(r.Body, 400));
                    req.Dispose();
                    if (ct.IsCancellationRequested) tcs.TrySetCanceled();
                    else tcs.TrySetResult(r);
                };
            });
            return tcs.Task;
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "…";
    }
}
