# WintryVR Architecture

## Layers

| Layer | Namespace | Responsibilities |
|---|---|---|
| **MR** | `WintryVR.MR`, `WintryVR.MetaPlatform` | rig, passthrough, tracking, anchors, room model, virtual objects |
| **AI** | `WintryVR.AI`, `WintryVR.OCR` | conversation, vision, OCR, translation, reasoning, search, memory |
| **Voice** | `WintryVR.Voice`, `WintryVR.Audio` | microphone, VAD, wake word, STT, TTS, continuous conversation, spatial audio |
| **Spatial Intelligence** | `WintryVR.Spatial`, `WintryVR.SceneUnderstanding` | user pose & gaze, objects, surfaces, rooms, distances, coordinates, anchors, pointers, highlights |
| **Character** | `WintryVR.Character` | Wintry Core, 3D form, animation, materials/textures, expressions, lip sync, visual states, variants |
| **Asset Generation** | `WintryVR.AssetGeneration` | concept, textures, materials, variants, cache, import |
| **UI** | `WintryVR.UI` | glass panels, cards, menus, HUD, overlays, onboarding, settings |
| **Core** | `WintryVR.Core` | bootstrap, orchestrator, interfaces, events, config, secrets, capabilities, localisation |

Every consumer depends on an interface from `Core/Interfaces`. Implementations are registered in
`WintryServices` (locator) at bootstrap. Vendor implementations register factories in `ProviderRegistry`
from their own assembly (`WintryVR.Meta`) at load time; `WintryBootstrap` resolves the highest-priority
available provider and otherwise falls back.

## Runtime object graph (built by `WintryBootstrap`)

```
WintryVR (scene object)
├─ OVRCameraRig | XRRig            ← IMRRigProvider (Meta or generic)
├─ Wintry                          ← SpatialAudioService + CharacterService
│   ├─ WintryCore                  ← WintryCoreOrb
│   └─ WintryCharacter             ← WintryCharacterBody (+ CharacterAnimator, Face, LipSync)
├─ WintryUI                        ← UIInteractionManager, Cards, Highlights, Pointers, HUD, Translations,
│                                     QuickActions, History, Settings, Debug, Onboarding
├─ DemoRoomColliders               ← DemoSceneProvider (editor/demo only)
└─ (components) ConnectivityMonitor, VoiceService, InputService, PerformanceMonitor, LocationService,
                WintryOrchestrator, DemoModeController
```

## The turn pipeline (`WintryOrchestrator.HandleUtteranceAsync`)

1. Cancel the previous turn (barge-in), detect language, publish `UserTurnEvent`.
2. Ensure the character form (Core → character morph).
3. Pending "new look" confirmation? → apply/discard.
4. Intent: rule-based multilingual detector (`IntentDetector`); if unsure and cloud is usable, ask the provider.
5. Control intents: `CLEAR_CONTEXT`, `DISMISS`, `CHANGE_LOOK`.
6. Spatial intents (`LOCATE`, `COUNT`, `NAVIGATE`, `DESCRIBE`) are answered **locally** by `SpatialQueryResolver`
   from the scene graph + memory whenever possible (works offline), pointing at the target. If the answer is not
   in memory, the pipeline continues with a vision hint ("find X", "count X").
7. `RunAssistantTurnAsync`:
   * resolve references in memory ("it", "the one next to it", "the second one");
   * decide whether a frame is needed (`IntentDetector.NeedsVision` + memory state);
   * **Vision**: capture one frame (`CameraCaptureService`, throttled, indicator on) →
     OCR/translation path (speak text, overlay translation near the text, card) or object recognition
     (`VisionService` → detections → `ObjectLocalizer` raycast into the room → `ContextMemory.Remember` →
     `SceneUnderstandingService.RegisterObject`);
   * **Search** when the intent is SEARCH (or when the assistant asks for it in its structured answer);
   * **Context**: memory summary + scene description + user pose (+ opt-in location);
   * **Reasoning**: `AssistantService.AskAsync` → `StructuredAnswer` (speech, identified object, confidence,
     focus point, card, search request, translation);
   * **Spatial UI**: focus object → Smart Highlight, character looks at it, information card beside it;
   * **Speak**: `VoiceService.SpeakAsync` (spatial AudioSource on Wintry) with amplitude events for lip sync;
     `AssistantReplyEvent` drives subtitles and history.
8. `finally`: state back to Idle (or Offline); after the conversation window closes and a quiet period passes
   the character collapses back to the Core.

## State machine

`AssistantState` drives everything visual and audible through `StateChangedEvent`: `WintryCoreOrb`,
`CharacterAnimator` (+ facial expression), `SpatialAudioService` (cues) and `HudOverlay` (status chip).
`PresenceForm` (Hidden · Core · Transforming · Character) is owned by `CharacterService`.

## Events (`WintryEvents`)

`StateChangedEvent`, `PresenceChangedEvent`, `WakeWordDetectedEvent`, `TranscriptEvent`, `UserTurnEvent`,
`UserMessageEvent` (a request to handle), `AssistantReplyEvent`, `ObjectsObservedEvent`, `PrivacyIndicatorEvent`,
`ConnectivityChangedEvent`, `SettingsChangedEvent`, `SpeechAmplitudeEvent`, `PointAtEvent`,
`TranslationReadyEvent`, `CapabilitiesDetectedEvent`, UI events (`QuickActionEvent`, `OpenSettingsEvent`,
`ToggleHistoryEvent`, `ClearSessionEvent`, `ClearHistoryEvent`, `ClearAllDataEvent`).

## Threading

All network/AI/voice work is `async`/`await` on Tasks. `UnityWebRequest` is driven on the main thread through
`MainThreadDispatcher`; providers return plain data objects; anything touching Unity objects is marshalled with
`MainThreadDispatcher.RunAsync`. Long loops in texture generation run at bootstrap or in the asset pipeline
(256–512 px on device).

## Extension points (future features)

* New intents: add keywords to `IntentDetector`, handle in `WintryOrchestrator`.
* New knowledge domains (plants, art, monuments, cooking…): extend the system prompt in `AssistantService` and
  the card builder; the vision detector already returns categories.
* Places: `ILocationService` + search.
* Object history / personal knowledge base: persist `ContextMemory` (`ObservedObject` is serialisable) and
  attach objects to `IAnchorService` anchors.
* Live subtitles: `TranscriptEvent` + `HudOverlay.ShowSubtitle`.
* On-device detector: register another `IVisionService` implementation.
* Multi-user / shared sessions: the scene graph and memory are plain data; share through a session service.
