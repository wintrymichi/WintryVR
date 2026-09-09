# Development phases — status

| Phase | Scope | Implemented in | Notes / hardware tuning left |
|---|---|---|---|
| 1 | Unity project + Quest compatibility | `Packages/manifest.json`, `Editor/WintryProjectSetup.cs`, `Plugins/Android/AndroidManifest.xml`, asmdefs | tick XR loader in Project Settings |
| 2 | Passthrough + MR | `Meta/MetaMRRigProvider.cs` (OVRCameraRig + OVRPassthroughLayer), `MR/GenericMRRig.cs` | brightness from settings |
| 3 | Core + UI | `Character/WintryCoreOrb.cs`, `UI/*`, `Shaders/*` | — |
| 4 | Voice | `Voice/*` (VAD, wake word, STT/TTS providers, barge-in, continuous conversation) | tune `VoiceActivityDetector` thresholds on device |
| 5 | AI conversation | `AI/AssistantService.cs`, `AI/Providers/*`, `AI/ContextMemory.cs` | — |
| 6 | Vision | `Camera/*` (smart capture), `OCR/VisionService.cs` | camera intrinsics from the Passthrough Camera API can refine `CapturedFrame` FOV |
| 7 | Object recognition | `VisionService` + `ObjectLocalizer` + memory + `SmartHighlight` | — |
| 8 | OCR + translation | `OCR/OCRService.cs`, `TranslationService`, `UI/TranslationOverlay.cs` | — |
| 9 | Scene understanding | `SceneUnderstanding/*`, `Meta/MrukSceneProvider.cs` | verify MRUK API names for your SDK version (docs/META_SDK_NOTES.md) |
| 10 | Spatial markers | `Spatial/SpatialPointer.cs`, `SpatialQueryResolver` | — |
| 11 | Information cards | `UI/InformationCard.cs`, `GlassPanel` | — |
| 12 | Web search | `AI/SearchService.cs` | — |
| 13 | Wintry 3D character | `Character/WintryCharacterBody.cs`, `CharacterService` (morph, personal space) | — |
| 14 | Animation + lip sync | `CharacterAnimator`, `FacialExpressionController`, `LipSyncController` | — |
| 15 | Asset generation system | `AssetGeneration/*`, `Editor/WintryAssetPipelineWindow.cs` | plug a 3D provider when one is available |
| 16 | Character variants | `Character/CharacterVariants.cs` (+ Core Identity), settings | — |
| 17 | Privacy + settings | `Settings/*`, `UI/SettingsMenu.cs`, `UI/HudOverlay.cs` | — |
| 18 | Performance optimisation | LODs, material count, no shadows, texture caps, `PerformanceMonitor` | profile on device, adjust `CharacterLOD.ScreenHeights` |
| 19 | Demo + debug | `Demo/*`, `Debug/*` | — |
| 20 | Final polish | onboarding, localisation (it/en/de/fr/es), safety, error phrasing | on-device UX pass |

The project is compilable after every phase because every phase only adds providers/components behind the
interfaces in `Core/Interfaces`.
