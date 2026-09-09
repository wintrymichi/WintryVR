using WintryVR.Core;

namespace WintryVR.AI.Providers
{
    /// <summary>
    /// A model server on the local network (Ollama, LM Studio, vLLM, llama.cpp server) exposing the
    /// OpenAI-compatible API. Runs without internet but needs LAN reachability. Quest itself does not run
    /// multimodal LLMs locally; this keeps images on the user's own network.
    /// </summary>
    public class LocalAIProvider : CloudAIProvider
    {
        public LocalAIProvider(string baseUrl, string model) : base("Local", baseUrl, model, null) { }
        public override bool RequiresNetwork => false; // LAN only
    }
}
