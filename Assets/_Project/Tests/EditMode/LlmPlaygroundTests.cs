using System.Linq;
using Chatan.Dev;
using Chatan.LLM;
using NUnit.Framework;
using UnityEditor;

namespace Chatan.Tests
{
    /// <summary>Tests for the LLM playground requests and the shared settings asset.</summary>
    public class LlmPlaygroundTests
    {
        private const string SettingsPath = "Assets/_Project/Data/Settings/LlmSettings.asset";

        [Test]
        public void Parse_BuildsAParseRequest()
        {
            LlmRequest r = LlmPlayground.BuildRequest("hola", false, 0, null);

            Assert.AreEqual(LlmPurpose.ParseIntent, r.Purpose);
        }

        [TestCase(0, "decision: accept")]
        [TestCase(1, "decision: reject")]
        [TestCase(2, "decision: counter")]
        [TestCase(3, "decision: none")]
        public void Reply_UsesTheChosenDecision(int index, string marker)
        {
            LlmRequest r = LlmPlayground.BuildRequest("2 trigo por 1 mineral", true, index, null);

            Assert.AreEqual(LlmPurpose.GenerateReply, r.Purpose);
            StringAssert.Contains(marker, r.Messages.Last().Content);
        }

        [Test]
        public void SharedSettings_KeepTheMockOn()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LlmSettings>(SettingsPath);

            Assert.IsNotNull(settings, $"missing {SettingsPath}");
            Assert.IsTrue(settings.useMock, "the repo copy must not need Ollama");
        }
    }
}
