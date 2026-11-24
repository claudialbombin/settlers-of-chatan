using Chatan.LLM;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Chatan.Tests
{
    /// <summary>
    /// Unit tests for <see cref="OllamaClient"/>'s JSON in and out and for
    /// <see cref="LanguageModelFactory"/>. No network: the real calls are checked by hand
    /// in the LLM playground.
    /// </summary>
    public class OllamaClientTests
    {
        private LlmSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<LlmSettings>();
            _settings.model = "qwen3.5:4b";
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_settings);

        private static LlmRequest Request(bool json)
        {
            var r = new LlmRequest { ExpectJson = json, Temperature = 0.3f, MaxTokens = 50 };
            r.Messages.Add(LlmMessage.System("sys"));
            r.Messages.Add(LlmMessage.User("hola"));
            r.Messages.Add(LlmMessage.Assistant("qué tal"));
            return r;
        }

        [Test]
        public void RequestJson_HasOllamasShape()
        {
            JObject body = JObject.Parse(OllamaClient.BuildRequestJson(Request(true), _settings));

            Assert.AreEqual("qwen3.5:4b", (string)body["model"]);
            Assert.AreEqual(false, (bool)body["stream"]);
            Assert.AreEqual("json", (string)body["format"]);
            Assert.AreEqual(false, (bool)body["think"]);
            Assert.AreEqual(0.3f, (float)body["options"]["temperature"], 1e-6f);
            Assert.AreEqual(50, (int)body["options"]["num_predict"]);
            Assert.AreEqual("system", (string)body["messages"][0]["role"]);
            Assert.AreEqual("user", (string)body["messages"][1]["role"]);
            Assert.AreEqual("assistant", (string)body["messages"][2]["role"]);
            Assert.AreEqual("hola", (string)body["messages"][1]["content"]);
        }

        [Test]
        public void RequestJson_PlainText_OmitsFormat_AndThinkWhenAllowed()
        {
            _settings.disableThinking = false;

            JObject body = JObject.Parse(OllamaClient.BuildRequestJson(Request(false), _settings));

            Assert.IsNull(body["format"]);
            Assert.IsNull(body["think"]);
        }

        [Test]
        public void Response_Ok_IsTrimmed()
        {
            LlmResponse r = OllamaClient.ParseResponse("{\"message\":{\"role\":\"assistant\",\"content\":\"  ¡Trato hecho!\\n\"},\"done\":true}", 1.5f);

            Assert.IsTrue(r.Success);
            Assert.AreEqual("¡Trato hecho!", r.Text);
            Assert.AreEqual(1.5f, r.LatencySeconds);
        }

        [TestCase("")]
        [TestCase("{\"message\":{\"role\":\"assistant\",\"content\":\"   \"}}")]
        [TestCase("<html>not json</html>")]
        [TestCase("{\"error\":\"model 'x' not found\"}")]
        public void Response_Bad_IsAFailureNotAnException(string body)
        {
            LlmResponse r = null;

            Assert.DoesNotThrow(() => r = OllamaClient.ParseResponse(body));

            Assert.IsFalse(r.Success);
            Assert.IsNotEmpty(r.Error);
        }

        [Test]
        public void ChatUrl_HasNoDoubleSlash()
        {
            _settings.baseUrl = "http://localhost:11434/";

            Assert.AreEqual("http://localhost:11434/api/chat", _settings.ChatUrl);
        }

        [Test]
        public void Factory_UsesTheMockUnlessToldOtherwise()
        {
            Assert.IsInstanceOf<MockLanguageModel>(LanguageModelFactory.Create(null));

            _settings.useMock = true;
            Assert.IsInstanceOf<MockLanguageModel>(LanguageModelFactory.Create(_settings));

            _settings.useMock = false;
            ILanguageModel model = LanguageModelFactory.Create(_settings);
            Assert.IsInstanceOf<OllamaClient>(model);
            Assert.AreEqual("Ollama qwen3.5:4b", model.DisplayName);
        }

        [Test]
        public void EmptyRequest_FailsWithoutCallingAnything()
        {
            LlmResponse r = new OllamaClient(_settings).CompleteAsync(new LlmRequest()).GetAwaiter().GetResult();

            Assert.IsFalse(r.Success);
        }
    }
}
