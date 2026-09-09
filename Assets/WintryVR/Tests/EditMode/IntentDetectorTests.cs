using NUnit.Framework;
using WintryVR.AI;
using WintryVR.Core;

namespace WintryVR.Tests
{
    public class IntentDetectorTests
    {
        [TestCase("What is that?", Intent.IDENTIFY)]
        [TestCase("Cosa sto guardando?", Intent.IDENTIFY)]
        [TestCase("Read this.", Intent.OCR)]
        [TestCase("Leggimi questo", Intent.OCR)]
        [TestCase("How much does this cost?", Intent.SEARCH)]
        [TestCase("Quanto costa?", Intent.SEARCH)]
        [TestCase("Where is my phone?", Intent.LOCATE)]
        [TestCase("Dov'è il telefono?", Intent.LOCATE)]
        [TestCase("How many chairs are there?", Intent.COUNT)]
        [TestCase("Quante sedie ci sono?", Intent.COUNT)]
        [TestCase("Translate this", Intent.TRANSLATE)]
        [TestCase("Traduci", Intent.TRANSLATE)]
        [TestCase("Which of the three is more expensive?", Intent.COMPARE)]
        [TestCase("Explain it to me", Intent.EXPLAIN)]
        [TestCase("Spiegamelo", Intent.EXPLAIN)]
        [TestCase("Which is the nearest door?", Intent.NAVIGATE)]
        [TestCase("Thanks Wintry, that's all", Intent.DISMISS)]
        [TestCase("Wintry, create a new look for you", Intent.CHANGE_LOOK)]
        [TestCase("Was ist das?", Intent.IDENTIFY)]
        [TestCase("Où est la porte ?", Intent.LOCATE)]
        [TestCase("¿Cuánto cuesta?", Intent.SEARCH)]
        public void DetectsIntent(string text, Intent expected)
        {
            Assert.AreEqual(expected, IntentDetector.Detect(text).Intent, text);
        }

        [TestCase("Cosa sto guardando?", "it")]
        [TestCase("What am I looking at?", "en")]
        [TestCase("Was ist das da?", "de")]
        [TestCase("Qu'est-ce que c'est ?", "fr")]
        [TestCase("¿Qué es esto?", "es")]
        public void DetectsLanguage(string text, string lang)
        {
            Assert.AreEqual(lang, IntentDetector.DetectLanguage(text));
        }

        [Test]
        public void VisionRequirement()
        {
            Assert.IsTrue(IntentDetector.NeedsVision(Intent.IDENTIFY, ""));
            Assert.IsTrue(IntentDetector.NeedsVision(Intent.OCR, ""));
            Assert.IsFalse(IntentDetector.NeedsVision(Intent.GENERAL_CONVERSATION, ""));
        }
    }
}
