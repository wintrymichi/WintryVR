// Preview harness. Not compiled by the project: it lives outside Assets/ on purpose, so neither Unity nor
// tools/offline-check picks it up. Copy it into Assets/WintryVR/Editor/ when you want a look sheet, and take
// it back out afterwards. See tools/preview/README.md.
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using WintryVR.AssetGeneration;
using WintryVR.Character;
using WintryVR.Core;
using WintryVR.UI;

namespace WintryVR.EditorTools
{
    public static class WintryCaptureTool
    {
        private const string OutDir = "Captures";
        private static Transform _stage;

        public static void CaptureAll()
        {
            try
            {
                Directory.CreateDirectory(OutDir);
                EnsureUrp();
                Debug.Log("[Capture] URP active: " + WintryMaterials.UniversalPipelineActive);

                foreach (WintryVariant v in Enum.GetValues(typeof(WintryVariant)))
                    ExportTextureSheet(v);

                CaptureCharacters();
                CaptureUi();
                Debug.Log("[Capture] done -> " + Path.GetFullPath(OutDir));
            }
            catch (Exception ex)
            {
                Debug.LogError("[Capture] failed: " + ex);
            }
            EditorApplication.Exit(0);
        }

        // ---------------------------------------------------------------- URP

        private static void EnsureUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline != null) return;
            var asm = "Unity.RenderPipelines.Universal.Runtime";
            var dataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalRendererData, " + asm);
            var assetType = Type.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, " + asm);
            if (dataType == null || assetType == null) { Debug.LogWarning("[Capture] URP types not found"); return; }

            Directory.CreateDirectory("Assets/Rendering");
            var data = ScriptableObject.CreateInstance(dataType);
            AssetDatabase.CreateAsset(data, "Assets/Rendering/WintryRenderer.asset");
            var create = assetType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
            var pipeline = (RenderPipelineAsset)create.Invoke(null, new object[] { data });
            AssetDatabase.CreateAsset(pipeline, "Assets/Rendering/WintryURP.asset");
            AssetDatabase.SaveAssets();

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            QualitySettings.antiAliasing = 8;
        }

        // ---------------------------------------------------------------- texture sheets

        private static void ExportTextureSheet(WintryVariant variant)
        {
            var look = CharacterVariants.GetLook(variant);
            var set = ProceduralTextureGenerator.Generate(look, 256);
            var maps = new[] { set.BaseColor, set.Normal, set.Roughness, set.Metallic, set.Emission, set.Occlusion, set.Detail };
            const int cell = 200, cols = 7;
            var sheet = new Texture2D(cell * cols, cell, TextureFormat.RGB24, false);
            var px = new Color[cell * cols * cell];
            for (int c = 0; c < cols; c++)
            {
                var m = maps[c];
                for (int y = 0; y < cell; y++)
                    for (int x = 0; x < cell; x++)
                    {
                        Color col = m != null ? m.GetPixelBilinear(x / (float)cell, y / (float)cell) : Color.magenta;
                        px[y * cell * cols + c * cell + x] = col;
                    }
            }
            sheet.SetPixels(px); sheet.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, "textures_" + variant + ".png"), sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
            ProceduralTextureGenerator.Release(set);
        }

        // ---------------------------------------------------------------- stage

        private static Camera BuildStage(Color background)
        {
            if (_stage != null) UnityEngine.Object.DestroyImmediate(_stage.gameObject);
            _stage = new GameObject("CaptureStage").transform;

            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(_stage, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 30f;

            var keyGo = new GameObject("Key");
            keyGo.transform.SetParent(_stage, false);
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.5f;
            key.color = new Color(1f, 0.97f, 0.92f);
            keyGo.transform.rotation = Quaternion.Euler(38f, 150f, 0f);

            var rimGo = new GameObject("Rim");
            rimGo.transform.SetParent(_stage, false);
            var rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Directional; rim.intensity = 1.1f;
            rim.color = new Color(0.55f, 0.75f, 1f);
            rimGo.transform.rotation = Quaternion.Euler(15f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.16f, 0.19f, 0.26f);
            RenderSettings.ambientEquatorColor = new Color(0.10f, 0.12f, 0.17f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.05f, 0.07f);
            return cam;
        }

        private static void Shoot(Camera cam, string file, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(OutDir, file), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log("[Capture] wrote " + file);
        }

        // ---------------------------------------------------------------- character

        private static void CaptureCharacters()
        {
            foreach (WintryVariant variant in Enum.GetValues(typeof(WintryVariant)))
            {
                var cam = BuildStage(new Color(0.045f, 0.05f, 0.065f));
                var look = CharacterVariants.GetLook(variant);
                var set = ProceduralTextureGenerator.Generate(look, 512);

                var bodyGo = new GameObject("Wintry_" + variant);
                bodyGo.transform.SetParent(_stage, false);
                var body = bodyGo.AddComponent<WintryCharacterBody>();
                body.Build(look, set);
                body.SetVisibility(1f);

                // frame the character: it stands about 0.42 m tall around the origin
                cam.transform.position = new Vector3(0f, 0.30f, 0.95f);
                cam.transform.rotation = Quaternion.Euler(6f, 180f, 0f);
                Shoot(cam, "character_" + variant + ".png", 900, 1200);

                // three-quarter view
                bodyGo.transform.rotation = Quaternion.Euler(0f, 34f, 0f);
                Shoot(cam, "character_" + variant + "_34.png", 900, 1200);

                // head close-up: the face is the identity, so it gets its own frame
                bodyGo.transform.rotation = Quaternion.identity;
                cam.transform.position = new Vector3(0f, 0.325f, 0.34f);
                cam.transform.rotation = Quaternion.Euler(2f, 180f, 0f);
                Shoot(cam, "face_" + variant + ".png", 900, 900);
            }

            // the Core orb, Wintry's minimal form
            var camOrb = BuildStage(new Color(0.045f, 0.05f, 0.065f));
            var orbLook = CharacterVariants.GetLook(WintryVariant.Default);
            var orbGo = new GameObject("Core");
            orbGo.transform.SetParent(_stage, false);
            var orb = orbGo.AddComponent<WintryCoreOrb>();
            orb.Build(orbLook);
            camOrb.transform.position = new Vector3(0f, 0f, 0.42f);
            camOrb.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Shoot(camOrb, "core_orb.png", 900, 900);
        }

        // ---------------------------------------------------------------- spatial UI

        private static void CaptureUi()
        {
            var cam = BuildStage(new Color(0.05f, 0.055f, 0.07f));
            cam.transform.position = new Vector3(0f, 0f, -0.62f);
            cam.transform.rotation = Quaternion.identity;
            var head = cam.transform;

            var panel = GlassPanel.Create("Card", 0.34f, 0.21f, head);
            panel.transform.SetParent(_stage, false);
            panel.transform.position = new Vector3(-0.085f, 0.02f, 0f);
            panel.Billboard = false;
            panel.SetTitle("Canon EOS R6 Mark II");
            panel.SetBody("Mirrorless camera - 24.2 MP full frame.\nIn stock at four retailers, from 2,299 EUR.\nConfidence 94%.");
            panel.AddButton("Tell me more", null, 0.1f);
            panel.AddButton("Compare", null, 0.09f);
            Settle(panel);

            var small = GlassPanel.Create("History", 0.2f, 0.16f, head);
            small.transform.SetParent(_stage, false);
            small.transform.position = new Vector3(0.16f, 0.0f, 0.03f);
            small.Billboard = false;
            small.SetTitle("Conversation");
            small.SetBody("YOU\nWhat is this?\n\nWINTRY\nIt looks like a Canon.");
            Settle(small);

            var label = WorldLabel.Create("Table - 3 objects", _stage, new Vector3(-0.03f, -0.16f, -0.05f), 0.55f, head);
            Invoke(label, "Refit");

            var cursor = UIPointerCursor.Create(_stage, head);
            var handPos = new Vector3(0.16f, -0.26f, -0.35f);
            var aimAt = new Vector3(-0.02f, -0.015f, 0f);
            var ray = new Ray(handPos, (aimAt - handPos).normalized);
            SetField(cursor, "_visible", 1f);
            SetField(cursor, "_engaged", 1f);
            cursor.Point(ray, Vector3.Distance(handPos, aimAt), true);
            Invoke(cursor, "LateUpdate");

            foreach (var b in _stage.GetComponentsInChildren<WintryButton>(true)) Invoke(b, "FitText");

            Shoot(cam, "ui_panels.png", 1400, 900);

            // a single button row, close up, to show the plate edge and label crispness
            var cam2 = BuildStage(new Color(0.05f, 0.055f, 0.07f));
            cam2.transform.position = new Vector3(0f, 0f, -0.16f);
            var row = new GameObject("Row").transform;
            row.SetParent(_stage, false);
            string[] labels = { "Identify", "Read", "Translate", "Search", "Explain" };
            for (int i = 0; i < labels.Length; i++)
            {
                var b = WintryButton.Create(labels[i], row, new Vector3((i - 2) * 0.048f, 0f, 0f), 0.044f, 0.022f, null, cam2.transform);
                Invoke(b, "FitText");
            }
            Shoot(cam2, "ui_buttons.png", 1400, 500);
        }

        // ---------------------------------------------------------------- edit-mode helpers

        /// <summary>
        /// These components settle their own state over several frames in Update/LateUpdate. Edit mode runs
        /// neither, and its Time.deltaTime is effectively zero anyway, so the smoothed value is pre-set to where
        /// it would have landed and the update is then invoked once to lay everything out from it.
        /// </summary>
        private static void Settle(GlassPanel panel)
        {
            SetField(panel, "_fade", 1f);
            Invoke(panel, "Update");
        }

        private static void SetField(object o, string name, object value)
        {
            var f = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null) f.SetValue(o, value); else Debug.LogWarning("[Capture] no field " + name + " on " + o.GetType().Name);
        }

        private static void Invoke(object o, string name)
        {
            var m = o.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (m != null) m.Invoke(o, null); else Debug.LogWarning("[Capture] no method " + name + " on " + o.GetType().Name);
        }
    }
}
