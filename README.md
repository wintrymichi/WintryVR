# WintryVR — *Your world, understood.*

WintryVR is a **spatial AI assistant** for **Meta Quest 3 and Meta Quest 3S**. Wintry sees what you see through
passthrough, understands where things are in your room, and explains the world around you by voice and with
minimal spatial UI. This repository is the complete Unity project: MR layer, AI layer, voice layer, spatial
intelligence, character (Core ↔ full 3D form), asset-generation pipeline, privacy system, offline mode,
demo mode and debug tooling.

> Wintry is not "a chatbot inside a headset". The real world is the context, the voice is the interaction,
> computer vision is the eyes, spatial intelligence is the memory of the space, the AI is the brain, Wintry
> is the presence. UI stays subordinate to the environment.

---

## Contents

1. [Feature overview](#feature-overview)
2. [Quick start](#quick-start)
3. [Configuration & API keys](#configuration--api-keys)
4. [Project structure](#project-structure)
5. [Architecture](#architecture)
6. [Interaction](#interaction)
7. [Demo mode & debug](#demo-mode--debug)
8. [Privacy](#privacy)
9. [What is real, what is mocked](#what-is-real-what-is-mocked)
10. [Development phases](#development-phases)
11. [Documentation](#documentation)

---

## Feature overview

| Area | What ships |
|---|---|
| **MR** | Passthrough (Meta), capability detection (Quest 3 / 3S / editor), hand + controller tracking, spatial anchors (Meta + local fallback), MRUK room model + scene mesh raycasts, generic XR fallback rig |
| **Wintry Core** | Procedural AI orb with 8 states (IDLE, LISTENING, THINKING, VISION, SEARCHING, SPEAKING, ERROR, OFFLINE): animation, light, motion, sound, colour |
| **Wintry character** | Original procedural 3D form (teardrop body, visor head, lens eyes, halo), Core ↔ character morph on "Hey Wintry", code-driven animations per state, 7 facial expressions, amplitude lip sync, 4-level LODs, personal space / follow logic |
| **Voice** | Microphone + VAD, wake word ("Hey Wintry"), continuous conversation window, barge-in interrupt, push-to-talk, STT/TTS providers (cloud OpenAI-compatible, Android, mock), spatial voice |
| **AI** | Provider abstraction (`IAIProvider`): Anthropic Messages API, OpenAI-compatible cloud, local LAN model server, mock. Structured answers, confidence hedging, safety disclaimers, 5 languages |
| **Vision** | Smart camera capture (on trigger only), object recognition with confidence + bounding boxes, 3D localisation via room raycasts, OCR, translation with in-world overlay |
| **Spatial intelligence** | Scene graph (room → surfaces → objects), local answers for *where / how many / nearest / what's on my left / what's on the table*, spatial pointer (marker + beam + arrow + label), Smart Highlight, world-first layout rules |
| **UI** | Glass panels, information cards (anchored, movable, resizable, closable, "Tell me more"), quick actions, conversation history, HUD privacy indicators + status chip + subtitles, settings menu, onboarding |
| **Memory** | Contextual memory of objects, turns, search results; reference resolution ("it", "the one next to it", "the second one", "the three") |
| **Web search** | `ISearchService` with Tavily, generic proxy and mock providers; price / source / availability cards |
| **Asset generation** | Concept → generation → 3D → textures → materials → optimisation → LOD → import → prefab, procedural PBR texture system, asset cache, editor pipeline window, 8 look variants + AI-proposed looks under a Core Identity |
| **Privacy** | Camera / microphone / cloud indicators, permission manager, privacy dashboard (clear session / history / all data), cloud toggle, opt-in location |
| **Offline** | MR, tracking, UI, anchors, local spatial queries, settings keep working; cloud features say so honestly |
| **Demo / Debug** | Full scripted scenario without headset or network; hidden debug panel with FPS/CPU/GPU/RAM/battery/latency/state and log export |

---

## Quick start

**Requirements**

* Unity **6000.0.x LTS** (2022.3 LTS also works; the code uses no Unity-6-only API)
* Android Build Support (SDK/NDK/OpenJDK)
* Meta XR Core SDK + MR Utility Kit (resolved from the Meta scoped registry in `Packages/manifest.json`)
* A Meta Quest 3 or 3S in developer mode (optional: everything runs in the editor in Demo Mode)

**Steps**

1. Clone and open the project in Unity Hub. Package resolution downloads URP, XR Management, OpenXR, Oculus XR, Meta XR Core SDK and MRUK.
   *If the Meta registry version is not available on your account, change the two `com.meta.xr.*` versions in `Packages/manifest.json` to the version you have (v74 or newer). Without the Meta packages the project still compiles and runs with the generic XR rig, but without passthrough, hands and room model.*
2. Menu **WintryVR → Setup → Configure Player Settings for Quest 3 & 3S** (Android, ARM64, IL2CPP, Vulkan, linear, ASTC, min SDK 32).
3. **Project Settings → XR Plug-in Management → Android**: enable **OpenXR** (recommended) and, in OpenXR features, the Meta Quest feature group / interaction profiles — or enable the **Oculus** loader. Add a **Universal Render Pipeline** asset (Project Settings → Graphics) — a default URP asset from the URP package works.
4. Menu **WintryVR → Setup → Create or Repair Main Scene** (creates/validates `Assets/WintryVR/Scenes/WintryVR_Main.unity` and adds it to Build Settings). The scene contains one object: `WintryVR` with `WintryBootstrap`; everything else is built at runtime from detected capabilities.
5. Press **Play** in the editor: Demo Mode starts automatically when no XR device is active. Keys: `N` next demo step, `1..9` jump, `T` type a request, `Space` push-to-talk, `M` menu, `F1` debug, right mouse = look, `WASD` move.
6. Build & Run to the headset. First launch shows onboarding ("Look around." → the Core appears → "Hi. I'm Wintry."), then say **"Hey Wintry, what am I looking at?"**

---

## Configuration & API keys

Non-secret settings live in `Assets/StreamingAssets/wintry.config.json` (copy from `wintry.config.example.json`; menu *WintryVR → Setup → Create wintry.config.json*). A copy at `<persistentDataPath>/wintry.config.json` on the headset overrides it without rebuilding (`adb push`). Every field can also be overridden with an environment variable `WINTRY_CFG_<FIELD>`.

**API keys are never in source control.** `SecretStore` resolves them in this order:

1. environment variables `WINTRY_AI_API_KEY`, `WINTRY_STT_API_KEY`, `WINTRY_TTS_API_KEY`, `WINTRY_SEARCH_API_KEY`, `WINTRY_ASSETGEN_API_KEY`, `WINTRY_PROXY_URL`
2. `<persistentDataPath>/wintry.secrets.json` on the device (push with adb, never inside the APK)
3. `Assets/StreamingAssets/wintry.secrets.json` (git-ignored, local builds only)
4. values entered from the app (PlayerPrefs)

**Recommended production setup:** set `PROXY_URL` to your backend that holds the vendor keys and exposes the same
endpoints (`/v1/messages` or `/chat/completions`, `/audio/transcriptions`, `/audio/speech`, search, images).
The headset then never holds a vendor key.

Provider selection (`AiProvider`): `auto` (picks Anthropic when the key starts with `sk-ant-` or the URL contains
"anthropic", otherwise OpenAI-compatible; mock when nothing is configured), `anthropic`, `openai-compatible`,
`local` (LAN Ollama / LM Studio / vLLM via `LocalAiBaseUrl`), `mock`. See `docs/PROVIDERS.md`.

---

## Project structure

```
Assets/WintryVR/
  AI/                 AssistantService, IntentDetector, ContextMemory, SearchService, ResponseSafety, Providers/ (Anthropic, Cloud, Local, Mock)
  Audio/              SpatialAudioService, ProceduralSfx
  Camera/             CameraCaptureService (smart capture) + Providers/ (Passthrough camera via WebCamTexture, Render, Demo)
  Character/          CharacterService, WintryCoreOrb, WintryCharacterBody, CharacterAnimator, FacialExpressionController, LipSyncController, CharacterLOD, CharacterVariants (+ CoreIdentity)
  Core/               WintryBootstrap, WintryOrchestrator (pipeline + state machine), interfaces, events, config, secrets, capabilities, localization, materials, procedural meshes
  Input/              InputService (gestures), EditorInputSource, GenericXRInputSource, IXRInputSource
  MR/                 IMRRigProvider, GenericMRRig (+ head tracker, no-passthrough stub), LocationService
  OCR/                VisionService (+ ObjectLocalizer), OCRService, TranslationService
  SceneUnderstanding/ SceneUnderstandingService (scene graph), DemoSceneProvider, SpatialQueryResolver
  Spatial/            SpatialService, SpatialPointer(+Manager), SmartHighlight, WorldFirstLayout, LocalAnchorService
  UI/                 GlassPanel, WintryButton, UIInteractionManager, InformationCard(+Manager), QuickActionsMenu, HudOverlay, TranslationOverlay, ConversationHistoryPanel, SettingsMenu, OnboardingFlow, WorldLabel
  Voice/              VoiceService, VoiceActivityDetector, WakeWordDetector, WavUtility, Providers/ (Cloud/Android/Mock STT & TTS)
  Networking/         MiniJson, HttpClientService, ConnectivityMonitor
  AssetGeneration/    AssetGenerationService (pipeline), ProceduralTextureGenerator, MeshOptimizer, AssetCache, Providers/ (Mock, HttpImage)
  Settings/           WintrySettings (AI/Voice/Vision/MR/Privacy/Accessibility), PermissionManager
  Debug/              PerformanceMonitor, DebugPanel
  Demo/               DemoModeController, DemoScenario
  Meta/               WintryVR.Meta assembly: Meta XR rig/passthrough, hands/controllers, spatial anchors, MRUK scene provider (compiled only when the Meta packages are present)
  Editor/             Quest project setup, scene builder, Wintry Asset Pipeline window
  Shaders/            WintryVR/Glow, WintryVR/Glass (URP, mobile, single-pass instanced)
  Scenes/             WintryVR_Main.unity
  Tests/EditMode/     NUnit tests (intents, memory, JSON, voice, assistant parsing, asset pipeline)
Assets/Plugins/Android/AndroidManifest.xml   permissions & Quest features
Assets/StreamingAssets/                      config & secrets examples
docs/                                        architecture, Quest setup, providers, privacy, phases, Meta SDK notes
tools/generate_meta.py                       deterministic .meta generation
```

Service interfaces (all in `Core/Interfaces`): `IAssistantService`, `IVisionService`, `IVoiceService`, `IOCRService`,
`ITranslationService`, `ISceneUnderstandingService`, `ISpatialService`, `IAnchorService`, `ISearchService`,
`IMemoryService`, `ICharacterService`, `IAssetGenerationService`, `IAIProvider`, `ISpeechToTextProvider`,
`ITextToSpeechProvider`, `ICameraCaptureProvider`, `ICameraCaptureService`, `IInputService`, `IPassthroughService`,
`ILocationService`.

---

## Architecture

```
Voice Input → STT → Intent Detection → Determine Visual Context → Capture Relevant Frame
   → Scene/Object Analysis → Spatial Context → AI Reasoning (→ Web Search) → Response
   → Spatial UI (cards · pointers · highlights · overlays) → TTS → Spatial Audio
```

* **`WintryBootstrap`** (the only scene component) probes capabilities, builds the rig through the best
  `IMRRigProvider` (Meta or generic), wires every layer through interfaces and runs onboarding.
* **`WintryOrchestrator`** runs the asynchronous pipeline per utterance and owns the state machine
  (Idle · Listening · Thinking · Vision · Searching · Speaking · Error · Offline) and the presence logic
  (Core ↔ character, return to Core after the conversation ends).
* **`ProviderRegistry`** lets vendor assemblies (WintryVR.Meta) register implementations at load time; the
  runtime never references Meta types directly, so missing packages degrade to generic/mock providers.
* **World-first UX** (`WorldFirstLayout`): virtual content avoids the centre of the view, cards sit beside
  their objects, Wintry moves aside when covering something and keeps a natural personal space instead of
  teleporting in front of the face.

Full description: `docs/ARCHITECTURE.md`.

---

## Interaction

| Action | Hands | Touch controllers | Editor |
|---|---|---|---|
| Select / press button | index pinch | trigger | left mouse |
| Move a panel | pinch-hold on its title bar | trigger-hold | left mouse hold |
| Push-to-talk | left-hand pinch-hold | grip | Space |
| Quick actions menu | palm toward face + middle-finger pinch | B / Y | M |
| Dismiss | ring-finger pinch (right) | A | Esc |
| Debug panel | both index + left middle pinch | both thumbstick clicks | F1 |
| Talk | "Hey Wintry, …" (wake word) or push-to-talk; follow-ups need no wake word while the conversation window is open | | |

Voice examples (any of it/en/de/fr/es, Wintry answers in the same language):
*"Hey Wintry, cosa sto guardando?" · "Quanto costa?" · "E quello accanto?" · "Leggimi questo." · "Traduci." · "Dov'è il mio telefono?" · "Quante sedie ci sono?" · "Cosa c'è sul tavolo?" · "Qual è la porta più vicina?" · "Wintry, crea un nuovo look per te." · "Grazie Wintry, basta così."*

---

## Demo mode & debug

Demo Mode (`DemoMode` in config, the `ForceDemoMode` toggle on `WintryBootstrap`, or automatically in the
editor without an XR device) uses the mock AI/STT/TTS/search/asset providers, a synthetic room with colliders,
and a demo camera image, so the entire pipeline — wake word, morph, vision, cards, search, OCR, translation,
locate, count, compare, new look, dismiss — runs without a headset. `DemoScenario` lists the scripted steps.

The debug panel (F1 / controller combo / `DebugPanel: true` in config) shows FPS, frame/CPU/GPU ms, RAM,
battery, network & AI latency, provider/model, tracking, passthrough, microphone, camera, scene entities,
anchors, memory objects, and exports logs to `<persistentDataPath>/logs/`.

---

## Privacy

Camera, microphone and cloud indicators are always visible when active (HUD). Frames are captured only on a
trigger (voice command, gaze, selection, scene change, low-confidence retry), never streamed; nothing is stored
permanently. The Privacy section of the settings menu toggles camera / microphone / cloud / location / history and
offers *Clear session*, *Clear history* and *Clear all data*. Details: `docs/PRIVACY.md`.

---

## What is real, what is mocked

Everything in the pipeline is implemented; where a capability needs an external service or hardware, there is an
interface, a real provider and a working mock:

| Capability | Real implementation | Fallback / mock |
|---|---|---|
| Passthrough, hands, controllers, anchors, room model | Meta XR SDK + MRUK (WintryVR.Meta) | generic XR rig (no passthrough), XR InputDevices controllers, local anchors, synthetic room |
| Camera frames | Passthrough Camera API via `WebCamTexture` (Horizon OS v74+, `HEADSET_CAMERA` permission) | render capture (virtual content only), demo image |
| LLM / vision / OCR / translation | Anthropic Messages API, OpenAI-compatible endpoints, LAN model server | deterministic mock provider |
| STT / TTS | OpenAI-compatible `/audio/*` endpoints (or a proxy); Android engines when the device has them (Quest normally does not) | scripted STT, synthesised tone voice with subtitles |
| Web search | Tavily / generic proxy | mock results |
| Concept-art generation | OpenAI-compatible `/images/generations` (or proxy) | procedural concept card |
| 3D model generation | **not claimed**: Quest cannot generate heavy 3D locally; `IAssetGenerationProvider.Supports3DModels` is `false` and the identity-locked procedural mesh is used | — |
| Texture/material/LOD/prefab generation | fully procedural on device and in the editor | — |

Known limitations and Meta API points to verify against your SDK version are listed in `docs/META_SDK_NOTES.md`.

---

## Development phases

All 20 phases from the product brief have a concrete implementation in this repository; `docs/PHASES.md`
maps each phase to files and states what remains to be tuned on hardware (voice thresholds, camera intrinsics,
LOD distances, performance budgets).

---

## Documentation

* `docs/ARCHITECTURE.md` — layers, data flow, state machine, events, extension points
* `docs/SETUP_QUEST.md` — Unity/Meta setup, XR plug-in, build & deploy, adb tips
* `docs/PROVIDERS.md` — AI/STT/TTS/search/asset providers, endpoints, proxy contract
* `docs/PRIVACY.md` — privacy model, permissions, data lifecycle
* `docs/PHASES.md` — phase-by-phase status
* `docs/META_SDK_NOTES.md` — exact Meta APIs used and what to check per SDK version

Tests: **Window → General → Test Runner → EditMode** (intents, memory, JSON, voice, assistant parsing, textures/LODs).

---

## Verification status

The C# in this repository has been compiled and the EditMode tests executed outside Unity, against a
hand-written stand-in for the Unity API that keeps Unity's real signatures. Four configurations build with
zero errors and zero warnings: editor, Android player, the Meta platform assembly, and the test assembly.
All 56 EditMode test cases pass while running the project's own logic.

| Verified offline | Still needs Unity or the headset |
|---|---|
| C# compiles for editor, Android and Meta configurations | Shader compilation (`WintryGlass`, `WintryGlow`) |
| 56/56 EditMode test cases pass | Scene and prefab loading, serialised references |
| JSON, asmdef and Android manifest parse | Meta XR and MRUK API shapes for your SDK version |
| Every asset has a unique `.meta` GUID | Player settings, XR loader and URP asset assignment |

`ProjectSettings/` holds only the editor version, so Unity generates default settings on first open. Run
**WintryVR → Setup → Configure Player Settings for Quest 3 & 3S**, then
**WintryVR → Setup → Verify project setup** to see what is still missing, and
**WintryVR → Setup → Enable XR loader for Android** to switch the loader on when XR Plug-in Management is installed.
