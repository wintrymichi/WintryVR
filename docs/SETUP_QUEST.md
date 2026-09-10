# Quest 3 / 3S setup

## Packages

`Packages/manifest.json` references the Meta scoped registry (`https://npm.developer.oculus.com`) for
`com.meta.xr.sdk.core` and `com.meta.xr.mrutilitykit` (v74). If Unity cannot resolve those versions:

* update the version numbers to the one listed for your account (any v74+ should work), or
* import *Meta XR All-in-One SDK* from the Asset Store (it installs the same packages).

Without the Meta packages, `WintryVR.Meta` is compiled with `WINTRY_META_XR` undefined (the `versionDefines`
in its asmdef) and the app runs with the generic rig: no passthrough, no hands, no room model.

## Project settings

1. **WintryVR → Setup → Configure Player Settings for Quest 3 & 3S**. Among other things this pins
   *Player → Android → Other Settings → Application Entry Point* to **Activity**. Unity 6 defaults it to
   *GameActivity*, and with that the APK contains no `UnityPlayerActivity` — which is the activity our manifest
   marks with `com.oculus.intent.category.VR`, and therefore the one the headset launches. The symptom is an app
   that installs fine and then does nothing when tapped in the library. **Verify project setup** reports the
   value under "Application entry".
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

One command does install, launch and diagnosis together, and removes the previous copy first so no stale
launcher entry survives:

```
powershell -ExecutionPolicy Bypass -File tools/deploy-quest.ps1
```

It finds `adb` inside the Unity Android SDK, says whether the headset is connected, unauthorised or asleep,
and after launching reports either the process id or the crash log, instead of leaving you with a silent icon.
Pass `-Keep` to install over the existing app and preserve its pushed config and settings.

If the icon does nothing when tapped, the second command above tells you why within a second: an
`Error type 3 / Activity class {...UnityPlayerActivity} does not exist` means the APK was built with the
GameActivity entry point (see step 1 above); `adb logcat -b crash` shows any other startup crash. Uninstall
first (`adb uninstall com.wintry.wintryvr`) when switching entry points, so no stale activity survives.

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
