# Meta XR SDK notes (WintryVR.Meta)

The `WintryVR.Meta` assembly is compiled only when `com.meta.xr.sdk.core` (`WINTRY_META_XR`) and
`com.meta.xr.mrutilitykit` (`WINTRY_MRUK`) are present. It uses the following API surface (Meta XR SDK v74):

| Feature | API used | Where |
|---|---|---|
| Rig | `OVRCameraRig` (+ `EnsureGameObjectIntegrity`, `centerEyeAnchor`, `trackingSpace`, hand/controller anchors), `OVRManager` (`trackingOriginType`, `isInsightPassthroughEnabled`) | `MetaMRRigProvider` |
| Passthrough | `OVRPassthroughLayer` (`overlayType`, `textureOpacity`, `edgeRenderingEnabled`, `edgeColor`, `SetBrightnessContrastSaturation`), `OVRManager.IsInsightPassthroughSupported()` | `MetaPassthroughService` |
| Headset detection | `OVRPlugin.GetSystemHeadsetType()` (`Meta_Quest_3`, `Meta_Quest_3S`) | `MetaCapabilityProbe` |
| Hands | `OVRHand` (`GetHand()`, `IsTracked`, `IsPointerPoseValid`, `PointerPose`, `GetFingerIsPinching`) | `MetaHandInputSource` |
| Controllers | `OVRInput.Get(Button.*, Controller.LTouch/RTouch)`, `OVRInput.IsControllerConnected` | `MetaControllerInputSource` |
| Anchors | `OVRSpatialAnchor` (`Created`, `Uuid`, `Localized`, `SaveAnchorAsync`, `EraseAnchorAsync`, `LoadUnboundAnchorsAsync`, `UnboundAnchor.LocalizeAsync/BindTo`) | `MetaAnchorService` |
| Room model | `MRUK.Instance`, `SceneLoadedEvent`, `LoadSceneFromDevice`, `GetCurrentRoom()`, `MRUKRoom.Anchors`, `MRUKRoom.Raycast`, `MRUKAnchor.Label/HasVolume/VolumeBounds/HasPlane/PlaneRect`, `GlobalMeshAnchor` | `MrukSceneProvider` |
| Passthrough camera | Android `WebCamTexture` + `horizonos.permission.HEADSET_CAMERA` (Passthrough Camera API, Horizon OS v74+) | `WebCamTextureCaptureProvider` (runtime assembly) |

## Assembly and module requirements

`WintryVR.Meta.asmdef` references the assemblies **as Meta names them in v74**:

| Reference | Package |
|---|---|
| `Oculus.VR` | `com.meta.xr.sdk.core` |
| `meta.xr.mrutilitykit` (lower-case) | `com.meta.xr.mrutilitykit` |

The MRUK assembly is `meta.xr.mrutilitykit`, not `Meta.XR.MRUtilityKit`; using the latter makes the whole
`Meta.XR.MRUtilityKit` namespace unresolvable while every other assembly still compiles, so the failure reads
like a missing package rather than a wrong reference name.

The Meta XR SDK also needs two built-in modules that a hand-written `manifest.json` easily omits, because
nothing in WintryVR's own code forces them in:

| Module | Needed by |
|---|---|
| `com.unity.modules.animation` | `OVRUnityHumanoidSkeletonRetargeter`, `OVRSkeletonMetadata` (`Animator`, `HumanBodyBones`) |
| `com.unity.modules.physics2d` | `OVRRaycaster` (`Physics2D`) |

Without them the SDK fails with `CS1069`/`CS0103` inside the package itself, `Oculus.VR` never builds, and
`WintryVR.Meta` is silently skipped along with it.

## What to verify when changing SDK version

* `OVRHand.HandType` is `internal` from v70 on. `MetaHandInputSource` reads the side with the public
  `GetHand()`, and `MetaMRRigProvider.AssignHandSide` writes the serialised field by reflection when it builds
  the rig from code (Meta's own prefabs set it in the inspector). If a future SDK exposes a public setter,
  replace the reflection with it; if the field disappears, the code logs a warning and falls back to controllers.
* Spatial anchor async signatures changed across v62–v65 (`SaveAsync` → `SaveAnchorAsync`, `OVRResult`). If your
  version differs, only `MetaAnchorService` needs adjusting; the runtime falls back to `LocalAnchorService`
  (session-only anchors) if the Meta service fails to construct.
* MRUK `MRUKAnchor.Label` became a `SceneLabels` flags enum in v69 (previously `AnchorLabels` strings).
  `MrukSceneProvider` uses `Label.ToString()`.
* `MRUKRoom.Raycast(Ray, float, out RaycastHit, out MRUKAnchor)` — if your version exposes only the
  `LabelFilter` overload, pass `LabelFilter.Included(...)`/`new LabelFilter()`.
* The palm-menu gesture assumes the hand anchor's `up` axis is the palm normal (left hand) / its negation
  (right hand). Adjust in `MetaHandInputSource.Menu` if your hand prefab orientation differs.
* Eye tracking: Quest 3 / 3S have no eye tracker; `DeviceCapabilities.EyeTracking` stays false and gaze is the
  head ray.
* Quest 3S is detected through `SystemHeadset.Meta_Quest_3S` (v68+) or the device model string as a fallback.

## Honest limitations

* No 3D model generation on the headset (`Supports3DModels = false`); looks change materials/textures/params on
  the identity-locked procedural mesh. A cloud 3D provider can be added behind `IAssetGenerationProvider`.
* Render capture on a headset contains virtual content only; real frames require the Passthrough Camera API.
* Quest ships no system TTS/STT engines; without cloud/proxy endpoints the mock voice (tones + subtitles) is used.
