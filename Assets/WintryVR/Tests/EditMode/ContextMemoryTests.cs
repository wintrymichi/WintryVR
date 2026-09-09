using NUnit.Framework;
using UnityEngine;
using WintryVR.AI;
using WintryVR.Core;

namespace WintryVR.Tests
{
    public class ContextMemoryTests
    {
        private ContextMemory _memory;
        private ObservedObject _camera, _phone, _keys;

        [SetUp]
        public void SetUp()
        {
            _memory = new ContextMemory();
            _camera = _memory.Remember(new Detection { Label = "camera", Identity = "Canon EOS R6 Mark II", Category = "electronics", Confidence = 0.94f }, new Vector3(0, 0.8f, 1.2f), true);
            _phone = _memory.Remember(new Detection { Label = "phone", Identity = "iPhone", Category = "electronics", Confidence = 0.8f }, new Vector3(0.3f, 0.8f, 1.2f), true);
            _keys = _memory.Remember(new Detection { Label = "keys", Category = "other", Confidence = 0.7f }, new Vector3(-0.4f, 0.8f, 1.1f), true);
            _memory.SetFocus(_camera);
        }

        [Test]
        public void PronounResolvesToFocus()
        {
            var r = _memory.ResolveReference("How much does it cost?", "en", Vector3.zero, Vector3.forward);
            Assert.AreEqual(1, r.Count);
            Assert.AreSame(_camera, r[0]);
        }

        [Test]
        public void NextToResolvesToNearestOtherObject()
        {
            var r = _memory.ResolveReference("And the one next to it?", "en", Vector3.zero, Vector3.forward);
            Assert.AreEqual(1, r.Count);
            Assert.AreSame(_phone, r[0]);
        }

        [Test]
        public void ExplicitNameWins()
        {
            var r = _memory.ResolveReference("Where are my keys?", "en", Vector3.zero, Vector3.forward);
            Assert.AreEqual(1, r.Count);
            Assert.AreSame(_keys, r[0]);
        }

        [Test]
        public void LocalizedNameResolves()
        {
            var r = _memory.ResolveReference("Dov'è il telefono?", "it", Vector3.zero, Vector3.forward);
            Assert.AreEqual(1, r.Count);
            Assert.AreSame(_phone, r[0]);
        }

        [Test]
        public void GroupOfThreeResolves()
        {
            var r = _memory.ResolveReference("Which of the three is more expensive?", "en", Vector3.zero, Vector3.forward);
            Assert.AreEqual(3, r.Count);
        }

        [Test]
        public void ReobservationMergesNearbyObject()
        {
            int before = _memory.Objects.Count;
            var merged = _memory.Remember(new Detection { Label = "camera", Identity = "Canon EOS R6 Mark II", Confidence = 0.9f }, new Vector3(0.05f, 0.8f, 1.25f), true);
            Assert.AreEqual(before, _memory.Objects.Count);
            Assert.AreSame(_camera, merged);
        }

        [Test]
        public void ContextTextMentionsFocusAndTurns()
        {
            _memory.RememberTurn(new AssistantTurn { UserText = "What is this?", AssistantText = "A camera." });
            string ctx = _memory.BuildContext(10, 5);
            StringAssert.Contains("IN FOCUS", ctx);
            StringAssert.Contains("What is this?", ctx);
        }

        [Test]
        public void ClearContextForgetsEverything()
        {
            _memory.ClearContext();
            Assert.AreEqual(0, _memory.Objects.Count);
            Assert.IsNull(_memory.Focus);
        }
    }
}
