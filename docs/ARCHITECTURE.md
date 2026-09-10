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
(`CharacterService.TextureResolution`, 512 px on device).

## Character surfacing

`ProceduralTextureGenerator` synthesises a full PBR set per look — base colour, normal, roughness,
metallic/smoothness, emission, detail and occlusion — from the look's parameters and one of four presets
(`smooth`, `frost`, `circuit`, `noise`). Two details make the difference between a surface that reads as a
material and one that reads as tinted plastic:

* **Cavity occlusion.** Derived from the height field by comparing each texel against the ring around it at two
  radii. It ships as its own map and is also multiplied lightly into the base colour, so the form still reads
  when the shader has fallen back to unlit and there is no occlusion slot to sample.
* **Seam-free UVs.** `ProceduralMeshes.Icosphere` maps latitude/longitude, which wraps `u` from 1 back to 0 along
  one meridian. With vertices shared, every triangle crossing it replays the whole texture backwards in a band
  down the model, and the poles smear for the same reason in the other axis. `SplitUvSeam` duplicates those
  vertices so neither happens; `SphereUvsDoNotWrapAcrossTheSeam` keeps it that way.

Maps are bound by `WintryCharacterBody.AssignMaps`, which writes both the URP Lit and built-in property names
and tiles the visor denser than the body so the pattern keeps its own scale on a much smaller surface. Sets are
released on look change and on destroy — six textures per look leak quickly otherwise.

## Spatial UI

No Canvas and no EventSystem: panels, buttons and labels are meshes with trigger colliders, and
`UIInteractionManager` raycasts them from whichever pointer is active (hand, controller, gaze, editor mouse).
That keeps the UI in world space where it belongs and keeps it batchable.

Three things carry most of the perceived quality:

* **Glyph raster size.** A dynamic font is rasterised at `fontSize` pixels and then scaled to metres by
  `characterSize`; the two multiply, so world size depends on the product while sharpness depends on
  `fontSize` alone. Everything goes through `WorldLabel.Configure`, which rasters at
  `WorldLabel.RasterFontSize` (160 px) and divides `characterSize` by the same factor — identical layout,
  roughly three times the texel density, no more shimmering as the head moves.
* **The pointer has a visible end.** `UIPointerCursor` draws a small ring where the ray lands plus a faint beam
  back to the hand. Without it, aiming is guesswork and a near-miss is indistinguishable from the app ignoring
  you. Both parts fade out when there is nothing to point at.
* **Edges measured in metres.** `WintryVR/Glass` evaluates a rounded-rectangle signed distance in the plate's
  real dimensions, passed in by `WintryMaterials.SetPanelShape`. Measured in uv instead, the highlight band
  came out thicker on a panel's short axis and squared off across the rounded corners.

Text meshes do not rebuild until the end of the frame their string was assigned in, so anything that measures
text — the button's label fit, the label's backing plate — defers to `LateUpdate` and retries while the bounds
are still degenerate, rather than measuring the previous string.

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
