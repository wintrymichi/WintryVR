using NUnit.Framework;
using UnityEngine;
using WintryVR.AI;
using WintryVR.AssetGeneration;
using WintryVR.Character;
using WintryVR.Core;

namespace WintryVR.Tests
{
    public class AssistantAndAssetTests
    {
        [Test]
        public void ParsesStructuredAnswer()
        {
            var a = new StructuredAnswer();
            AssistantService.Parse("{\"speech\":\"It looks like a Canon.\",\"language\":\"en\",\"object_name\":\"Canon EOS R6 Mark II\",\"confidence\":0.94,\"card_title\":\"Canon\",\"card_lines\":[\"24 MP\",\"Full frame\"],\"needs_search\":true,\"search_query\":\"canon r6 price\",\"focus_x\":0.4,\"focus_y\":0.6}", a);
            Assert.AreEqual("It looks like a Canon.", a.Speech);
            Assert.AreEqual("Canon EOS R6 Mark II", a.ObjectName);
            Assert.AreEqual(0.94f, a.Confidence, 0.001f);
            Assert.AreEqual(2, a.CardLines.Count);
            Assert.IsTrue(a.NeedsSearch);
            Assert.IsTrue(a.HasFocusPoint);
            Assert.AreEqual(0.4f, a.FocusPoint.x, 0.001f);
        }

        [Test]
        public void PlainTextAnswerStillSpeaks()
        {
            var a = new StructuredAnswer();
            AssistantService.Parse("Just a sentence.", a);
            Assert.AreEqual("Just a sentence.", a.Speech);
        }

        [Test]
        public void LowConfidenceIsHedged()
        {
            string s = ResponseSafety.ApplyConfidence("This is an iPhone.", 0.4f, "en");
            StringAssert.StartsWith("I'm not completely sure, but", s);
            string keep = ResponseSafety.ApplyConfidence("This is an iPhone.", 0.9f, "en");
            Assert.AreEqual("This is an iPhone.", keep);
        }

        [Test]
        public void MedicalDisclaimerAppended()
        {
            string s = ResponseSafety.AppendDisclaimers("Take two.", "what dosage of this medicine", "en");
            StringAssert.Contains("not medical advice", s);
        }

        [Test]
        public void CoreIdentityConstrainsLooks()
        {
            var wild = new WintryLookDefinition { EmissionStrength = 10f, Metallic = 3f, Smoothness = 0f, EyeColor = Color.black };
            var c = CoreIdentity.Constrain(wild);
            Assert.LessOrEqual(c.EmissionStrength, CoreIdentity.MaxEmission);
            Assert.LessOrEqual(c.Metallic, 1f);
            Assert.GreaterOrEqual(c.Smoothness, 0.3f);
            Color.RGBToHSV(c.EyeColor, out _, out _, out float v);
            Assert.GreaterOrEqual(v, 0.8f);
        }

        [Test]
        public void AllVariantsShareIdentity()
        {
            foreach (WintryVariant v in System.Enum.GetValues(typeof(WintryVariant)))
            {
                var look = CharacterVariants.GetLook(v);
                Assert.IsNotNull(look);
                Assert.That(look.EmissionStrength, Is.InRange(CoreIdentity.MinEmission, CoreIdentity.MaxEmission), v.ToString());
            }
        }

        [Test]
        public void ProceduralTexturesHaveFullSet()
        {
            var set = ProceduralTextureGenerator.Generate(CharacterVariants.GetLook(WintryVariant.Cyber), 64);
            Assert.IsNotNull(set.BaseColor); Assert.IsNotNull(set.Normal); Assert.IsNotNull(set.Roughness);
            Assert.IsNotNull(set.Metallic); Assert.IsNotNull(set.Emission); Assert.IsNotNull(set.Detail);
            Assert.IsNotNull(set.Occlusion);
            Assert.AreEqual(64, set.Resolution);
        }

        [Test]
        public void OcclusionVariesAcrossTheSurface()
        {
            // A flat occlusion map means the cavity term contributed nothing and the surface will read as
            // plastic. The circuit preset has hard raised traces, so its creases must come out clearly darker.
            var set = ProceduralTextureGenerator.Generate(CharacterVariants.GetLook(WintryVariant.Cyber), 64);
            var px = set.Occlusion.GetPixels32();
            byte min = 255, max = 0;
            foreach (var p in px) { if (p.r < min) min = p.r; if (p.r > max) max = p.r; }
            Assert.Greater(max - min, 12, "occlusion is nearly flat: min=" + min + " max=" + max);
        }

        [Test]
        public void SphereUvsDoNotWrapAcrossTheSeam()
        {
            // A latitude/longitude sphere with shared vertices interpolates u the long way round inside every
            // triangle that crosses the wrap meridian, replaying the whole texture backwards in a band down
            // the model. After the seam split no triangle may span more than half the u range.
            var mesh = ProceduralMeshes.Icosphere(0.1f, 2);
            var uv = mesh.uv;
            var tris = mesh.triangles;
            for (int i = 0; i < tris.Length; i += 3)
            {
                float a = uv[tris[i]].x, b = uv[tris[i + 1]].x, c = uv[tris[i + 2]].x;
                float span = Mathf.Max(a, Mathf.Max(b, c)) - Mathf.Min(a, Mathf.Min(b, c));
                Assert.Less(span, 0.5f, "triangle " + (i / 3) + " wraps the uv seam (span " + span + ")");
            }
        }

        [Test]
        public void LodChainDecreases()
        {
            var mesh = ProceduralMeshes.Icosphere(0.1f, 3);
            var lods = MeshOptimizer.GenerateLods(mesh);
            Assert.AreEqual(4, lods.Length);
            for (int i = 1; i < lods.Length; i++) Assert.LessOrEqual(lods[i].triangles.Length, lods[i - 1].triangles.Length, "LOD" + i);
            Assert.Greater(lods[3].triangles.Length, 0);
        }

        [Test]
        public void LookFromPromptReadsColoursAndFinish()
        {
            var look = WintryVR.AssetGeneration.Providers.MockAssetGenerationProvider.LookFromPrompt("frosted white with soft blue glow, metallic", CharacterVariants.GetLook(WintryVariant.Default));
            Assert.AreEqual("frost", look.TexturePreset);
            Assert.Greater(look.Metallic, 0.5f);
        }
    }
}
