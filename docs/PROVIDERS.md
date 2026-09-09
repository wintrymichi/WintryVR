# Providers

WintryVR never hardcodes a vendor. Each capability has an interface, one or more real providers and a mock.

## AI (`IAIProvider`)

| Provider | Config | Notes |
|---|---|---|
| `AnthropicAIProvider` | `AiProvider: anthropic`, `AiBaseUrl` (default `https://api.anthropic.com`), `AiModel` (default `claude-opus-5`), key `AI_API_KEY` | Messages API over HTTPS, images as base64 blocks, `output_config.effort` low/medium for fast spoken answers, `stop_reason: refusal` handled |
| `CloudAIProvider` | `AiProvider: openai-compatible`, `AiBaseUrl` (default `https://api.openai.com/v1`), `AiModel` | `/chat/completions` with `image_url` data URLs; works with OpenAI, Azure (URL), OpenRouter, Groq, Mistral, vLLM, LM Studio, Ollama `/v1` |
| `LocalAIProvider` | `AiProvider: local`, `LocalAiBaseUrl`, `LocalAiModel` | OpenAI-compatible LAN server (Ollama `llava`, etc.), no internet needed, images stay on the LAN |
| `MockAIProvider` | `AiProvider: mock` or nothing configured | deterministic structured answers for demo/offline |

The assistant asks for a JSON object (`speech`, `object_name`, `confidence`, `focus_x/y`, `card_*`,
`needs_search`, `translated`…) — see `AssistantService.BuildSystemPrompt`. Any model that can follow that contract works.

## Speech (`ISpeechToTextProvider`, `ITextToSpeechProvider`)

| Provider | Endpoint |
|---|---|
| `CloudSttProvider` | `POST {SttBaseUrl}/audio/transcriptions` (multipart WAV 16 kHz, `model`, `language`) → `{text, language}` |
| `CloudTtsProvider` | `POST {TtsBaseUrl}/audio/speech` `{model, input, voice, response_format: "wav", speed}` → WAV bytes |
| `AndroidSttProvider` / `AndroidTtsProvider` | on-device engines when present (Quest: usually not) |
| `MockSttProvider` / `MockTtsProvider` | scripted transcripts / synthesised voice |

## Search (`ISearchService`)

* `tavily` — `POST https://api.tavily.com/search` with `SEARCH_API_KEY`
* `generic` — `POST {SearchBaseUrl}` `{query}` → `{answer, results:[{title, snippet|content, url, price, availability}]}`
* `mock`

## Asset generation (`IAssetGenerationProvider`)

* `HttpImageGenerationProvider` — `POST {AssetGenBaseUrl}/images/generations` (OpenAI-compatible) for concept art;
  `Supports3DModels = false`.
* `MockAssetGenerationProvider` — parses colours/finish/mood from the prompt and renders a concept card.

Texture, material, LOD, cache and prefab stages are always local (`ProceduralTextureGenerator`, `MeshOptimizer`,
`AssetCache`, `CharacterLOD`).

## Camera (`ICameraCaptureProvider`)

* `WebCamTextureCaptureProvider` — Quest Passthrough Camera API (`HEADSET_CAMERA`), also desktop webcams
* `RenderCaptureProvider` — renders the Unity camera (virtual content only on a headset)
* `DemoImageCaptureProvider` — synthetic desk image or `Resources/WintryDemo/desk.png`

## Backend proxy contract

Set `PROXY_URL` (secret) or the `*BaseUrl` fields to a backend that forwards to your vendors and injects keys.
Expected routes: `/v1/messages` (Anthropic shape) **or** `/chat/completions`, `/audio/transcriptions`,
`/audio/speech`, `/images/generations`, and a search route. The client sends `Authorization: Bearer <PROXY_TOKEN>`
when `PROXY_TOKEN` is set as `AI_API_KEY`.
