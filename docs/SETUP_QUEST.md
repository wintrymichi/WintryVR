# Quest 3 / 3S setup

## Packages

`Packages/manifest.json` references the Meta scoped registry (`https://npm.developer.oculus.com`) for
`com.meta.xr.sdk.core` and `com.meta.xr.mrutilitykit` (v74). If Unity cannot resolve those versions:

* update the version numbers to the one listed for your account (any v74+ should work), or
* import *Meta XR All-in-One SDK* from the Asset Store (it installs the same packages).

Without the Meta packages, `WintryVR.Meta` is compiled with `WINTRY_META_XR` undefined (the `versionDefines`
in its asmdef) and the app runs with the generic rig: no passthrough, no hands, no room model.

## Project settings

1. **WintryVR → Setup → Configure Player Settings for Quest 3 & 3S**.
2. **XR Plug-in Management (Android)** → tick **OpenXR**. Under *OpenXR → Features* enable the Meta Quest
   feature group (Meta Quest Support, hand tracking, passthrough as applicable) and add the *Oculus Touch
   Controller Profile* + *Hand Interaction Profile*. Alternatively tick the **Oculus** loader.
3. **Meta → Tools → Project Setup Tool** and apply the recommended fixes (it configures Android manifest merges,
   graphics jobs, etc.). Keep *Passthrough*, *Hand Tracking*, *Scene* and *Anchor* support enabled in the
   OVR project config (Meta → Tools → OVR Project Config): Passthrough *Required*, Hand tracking *Controllers and Hands*,
   Scene *Required*, Anchor *Enabled*.
4. **Graphics**: assign a URP pipeline asset (the shaders in `Assets/WintryVR/Shaders` are URP). Recommended
   URP asset settings for Quest: no HDR, MSAA 4x, no post-processing, shadows off.
5. **Android manifest**: `Assets/Plugins/Android/AndroidManifest.xml` is merged by Unity. It declares
   `RECORD_AUDIO`, `CAMERA`, `horizonos.permission.HEADSET_CAMERA`, hand tracking, scene, anchor permissions and
   the passthrough feature. If the Meta *Project Setup Tool* generates its own manifest, merge the permissions.

## Build & deploy

```
File → Build Settings → Android → Build And Run
```

or with adb:

```
adb install -r WintryVR.apk
adb shell am start -n com.wintry.wintryvr/com.unity3d.player.UnityPlayerActivity
adb logcat -s Unity | grep Wintry
```

Push configuration/secrets without rebuilding:

```
adb push wintry.config.json  /sdcard/Android/data/com.wintry.wintryvr/files/wintry.config.json
adb push wintry.secrets.json /sdcard/Android/data/com.wintry.wintryvr/files/wintry.secrets.json
```

Export logs from the debug panel; they land in `/sdcard/Android/data/com.wintry.wintryvr/files/logs/`.

## First run on device

* Permissions are requested lazily: microphone when voice starts, camera/headset camera before the first
  capture, scene when the room model is requested. Complete *Space Setup* in Horizon OS for a full room model.
* Passthrough Camera API needs Horizon OS **v74+**. Older builds: capture falls back to render capture
  (virtual content only) and the assistant answers from the room model and memory; the debug panel shows the
  active capture provider.
* No TTS engine exists on Quest, so without a cloud/proxy TTS endpoint Wintry uses the synthesised fallback voice
  with subtitles.

## Performance budget (targets)

* 72/90 Hz stable, < 350 draw calls, character ≤ 3 materials, textures ≤ 512 px on device, no realtime shadows.
* `PerformanceMonitor` + debug panel report FPS/CPU/GPU/RAM/battery; `CharacterLOD` switches at 35 / 15 / 6 / 1 %
  screen height.
