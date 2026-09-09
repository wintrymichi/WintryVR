# Offline check

Compiles every C# file under `Assets/` and runs the EditMode tests **without Unity installed**, against a
hand-written stand-in for the Unity API. Useful for catching compile errors and logic regressions in seconds,
on a machine that has only the .NET SDK.

## Running it

Needs the .NET SDK 8 or newer (`winget install Microsoft.DotNet.SDK.8`).

```bash
cd tools/offline-check && dotnet build editor.csproj && dotnet build android.csproj && dotnet build meta.csproj && dotnet run --project tests.csproj
```

Four configurations, each mirroring one way Unity compiles this project:

| Project | Defines | Covers |
|---|---|---|
| `editor.csproj` | `UNITY_EDITOR` | runtime, editor tools and tests |
| `android.csproj` | `UNITY_ANDROID` | the device code paths, editor and test folders excluded |
| `meta.csproj` | `WINTRY_META_XR`, `WINTRY_MRUK` | the `WintryVR.Meta` assembly |
| `tests.csproj` | `UNITY_EDITOR` | builds an executable that runs the EditMode tests |

The test host discovers `[Test]` and `[TestCase]` methods and honours `[SetUp]` and `[TearDown]`, the way
Unity's EditMode runner does. It exits non-zero when a test fails, so it drops straight into CI.

## What the stand-in is

`Stubs/` declares the Unity types the project uses, with **Unity's real signatures**. That is the point: a
call that would not compile in Unity does not compile here either. Wrong argument counts, wrong overloads,
missing members, name collisions with `UnityEngine` types and C# version violations all surface.

The math and data types (`Mathf`, `Vector2/3/4`, `Quaternion`, `Matrix4x4`, `Color`, `Bounds`, `Rect`,
`Texture2D` pixels, `Mesh` buffers, `AudioClip` samples, `PlayerPrefs`) carry **real implementations**, so the
tests exercise the project's own logic rather than dummy values. Everything else returns neutral values.

`MetaStubs/` approximates the Meta XR SDK and MRUK. Meta's signatures move between SDK versions, so treat
that file as a way to check WintryVR's own cross-references, never as a statement about Meta's API. See
`docs/META_SDK_NOTES.md`.

## What it cannot tell you

- Shader compilation, scene and prefab loading, serialised inspector references.
- Anything about real Unity runtime behaviour: rendering, physics stepping, coroutine scheduling, frame timing.
- Whether the Meta and MRUK calls match the SDK version you actually install.
- Project settings, the XR loader and the render pipeline asset. Use **WintryVR → Setup → Verify project setup**
  inside Unity for those.

A green run here means the code compiles and its logic passes its tests. It does not mean the app runs on a
headset.

## Keeping it working

When project code starts using a Unity API the stand-in does not declare yet, the build fails with `CS0117`
or `CS0246` naming the missing member. Add it to the matching file in `Stubs/` **with Unity's real
signature**, taken from the scripting reference, and rebuild. Resist the temptation to loosen a signature to
make an error disappear: that is exactly the error this harness exists to catch.

Unity never sees this folder, since it sits outside `Assets/`.
