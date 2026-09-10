# Look sheet

Renders Wintry and the spatial UI to PNG from a headless Unity, so the look can be reviewed without putting
a headset on. It builds everything through the project's own procedural constructors — `WintryCharacterBody.Build`,
`WintryCoreOrb.Build`, `GlassPanel.Create`, `WintryButton.Create`, `UIPointerCursor.Create` — so what comes out
is what the app builds, not a mock-up of it.

## Running it

```bash
cp tools/preview/WintryCaptureTool.cs Assets/WintryVR/Editor/
"<Unity>/Unity.exe" -batchmode -projectPath "<project>" \
  -executeMethod WintryVR.EditorTools.WintryCaptureTool.CaptureAll -logFile capture.log
rm Assets/WintryVR/Editor/WintryCaptureTool.cs
```

**Do not pass `-nographics`** — it is the flag that would leave you with a black image, because there is no
device to render with. PNGs land in `Captures/` at the project root.

The file sits outside `Assets/` so Unity never compiles it and `tools/offline-check` never sees it (that project
takes `Assets/**` and `Stubs/**` only). Copy it in when you need it; it is not meant to ship in a build.

It creates and assigns a URP asset if the project has none, since Wintry's glass and glow shaders are URP-only
and `WintryMaterials.FindShader` correctly refuses to hand them to the built-in pipeline. That means the sheet
shows the intended look rather than the fallback.

## What it produces

| File | Shows |
|---|---|
| `character_<Variant>.png`, `_34.png` | full body, front and three-quarter |
| `face_<Variant>.png` | head close-up — the visor, lenses, brow and mouth |
| `core_orb.png` | the Core, Wintry's minimal form |
| `textures_<Variant>.png` | the seven generated maps side by side: base colour, normal, roughness, metallic, emission, occlusion, detail |
| `ui_panels.png`, `ui_buttons.png` | glass panels, buttons, a world label and the pointer cursor |

## Why it is worth keeping

Four defects reached a green test suite and a clean compile, and were only visible in a render:

* every face feature — visor, lenses, brow, mouth — sat **inside** the head sphere, leaving a blank ball;
* `WintryVR/Glow` multiplied emission by its strength a second time, so every glowing part clipped to white
  and lost Wintry's blue;
* the `smooth` texture preset drew a hard line at `v = 0.5`, which on a head is a bar straight across the eyes;
* panel body text ran off the bottom of the plate and printed over the buttons.

None of these fail a test, and none of them stop a build.

## Edit-mode caveat

`Update`/`LateUpdate` do not run in edit mode, and `Time.deltaTime` there is effectively zero, so components
that ease into place would stay at their starting value. The tool pre-sets those smoothed fields to their
settled value by reflection and then invokes the update once. If you add a component that animates itself in,
give it the same treatment or it will render invisible.
