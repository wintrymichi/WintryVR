using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WintryVR.Core;

namespace WintryVR.AI.Providers
{
    /// <summary>Shared plumbing for HTTP-backed providers.</summary>
    public abstract class AIProviderBase : IAIProvider
    {
        public abstract string Name { get; }
        public abstract string Model { get; }
        public virtual bool SupportsVision => true;
        public virtual bool RequiresNetwork => true;
        public abstract bool IsConfigured { get; }

        protected int TimeoutSeconds => WintryConfig.Instance.AiTimeoutSeconds;

        public async Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken ct)
        {
            var response = new AIResponse { ProviderName = Name, Model = string.IsNullOrEmpty(request.ModelOverride) ? Model : request.ModelOverride };
            if (!IsConfigured) { response.Error = Name + " is not configured"; return response; }
            float start = Time.realtimeSinceStartup;
            try
            {
                await CompleteInternalAsync(request, response, ct);
            }
            catch (OperationCanceledException) { response.Error = "cancelled"; }
            catch (Exception ex)
            {
                response.Success = false;
                response.Error = ex.GetType().Name + ": " + ex.Message;
                WintryLog.E("AI", Name + " request failed", ex);
            }
            response.LatencyMs = (Time.realtimeSinceStartup - start) * 1000f;
            AIProviderStats.Record(Name, response);
            return response;
        }

        protected abstract Task CompleteInternalAsync(AIRequest request, AIResponse response, CancellationToken ct);

        protected static string RoleName(AIRole r) => r == AIRole.Assistant ? "assistant" : r == AIRole.System ? "system" : "user";
    }

    /// <summary>Latency/usage stats for the debug panel.</summary>
    public static class AIProviderStats
    {
        public static float LastLatencyMs;
        public static float AverageLatencyMs;
        public static int Requests;
        public static int Failures;
        public static string LastProvider = "";
        public static string LastModel = "";

        public static void Record(string provider, AIResponse r)
        {
            Requests++;
            if (!r.Success) Failures++;
            LastLatencyMs = r.LatencyMs;
            AverageLatencyMs = AverageLatencyMs <= 0 ? r.LatencyMs : Mathf.Lerp(AverageLatencyMs, r.LatencyMs, 0.2f);
            LastProvider = provider;
            LastModel = r.Model;
        }
    }
}
