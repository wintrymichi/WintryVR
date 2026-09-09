using System.Collections.Generic;
using NUnit.Framework;
using WintryVR.Networking;

namespace WintryVR.Tests
{
    public class MiniJsonTests
    {
        [Test]
        public void RoundTrips()
        {
            var obj = new Dictionary<string, object> { ["a"] = 1L, ["b"] = "x\"y\n", ["c"] = new List<object> { 1.5, true, null }, ["d"] = new Dictionary<string, object> { ["e"] = "ü" } };
            string json = MiniJson.Serialize(obj);
            var back = MiniJson.Deserialize(json) as Dictionary<string, object>;
            Assert.IsNotNull(back);
            Assert.AreEqual(1L, back["a"]);
            Assert.AreEqual("x\"y\n", back["b"]);
            Assert.AreEqual(1.5, (MiniJson.AsArray(back["c"]))[0]);
            Assert.AreEqual("ü", MiniJson.GetString(back["d"], "e"));
        }

        [Test]
        public void PathWalksArraysAndObjects()
        {
            var root = MiniJson.Deserialize("{\"choices\":[{\"message\":{\"content\":\"hi\"}}]}");
            Assert.AreEqual("hi", MiniJson.Path(root, "choices.0.message.content"));
            Assert.IsNull(MiniJson.Path(root, "choices.3.message"));
        }

        [Test]
        public void ExtractsObjectFromProse()
        {
            var obj = MiniJson.ExtractObject("Sure! ```json\n{\"speech\":\"It's a camera\",\"confidence\":0.9}\n``` done");
            Assert.IsNotNull(obj);
            Assert.AreEqual("It's a camera", MiniJson.GetString(obj, "speech"));
            Assert.AreEqual(0.9, MiniJson.GetNumber(obj, "confidence"), 1e-9);
        }

        [Test]
        public void ParsesUnicodeEscapesAndNumbers()
        {
            var obj = MiniJson.Deserialize("{\"s\":\"\\u00e9\",\"n\":-1.25e2,\"i\":42}") as Dictionary<string, object>;
            Assert.AreEqual("é", obj["s"]);
            Assert.AreEqual(-125.0, obj["n"]);
            Assert.AreEqual(42L, obj["i"]);
        }
    }
}
