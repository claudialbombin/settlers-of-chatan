using System.Threading;
using Chatan.LLM;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="MockLanguageModel"/> and the LLM data types.</summary>
    /// <remarks>
    /// Blocking on the task with <c>GetAwaiter().GetResult()</c> is fine here because the
    /// mock without delay finishes instantly. Never block like this in game code: await.
    /// </remarks>
    public class MockLanguageModelTests
    {
        private static LlmResponse Ask(LlmPurpose purpose, string userText, MockLanguageModel model = null)
        {
            var request = new LlmRequest { Purpose = purpose, ExpectJson = purpose == LlmPurpose.ParseIntent };
            request.Messages.Add(LlmMessage.User(userText));
            return (model ?? new MockLanguageModel()).CompleteAsync(request).GetAwaiter().GetResult();
        }

        [Test]
        public void ParseIntent_Proposal_ReturnsProposeJson()
        {
            LlmResponse response = Ask(LlmPurpose.ParseIntent, "te doy 1 trigo por 1 mineral");

            Assert.IsTrue(response.Success);
            StringAssert.Contains("\"intent\":\"propose\"", response.Text);
            StringAssert.Contains("\"grain\":1", response.Text);
        }

        [TestCase("acepto")]
        [TestCase("Vale, trato hecho")]
        public void ParseIntent_Acceptance_ReturnsAcceptJson(string text)
        {
            StringAssert.Contains("\"intent\":\"accept\"", Ask(LlmPurpose.ParseIntent, text).Text);
        }

        [TestCase("no")]
        [TestCase("No acepto, paso")]
        public void ParseIntent_Rejection_ReturnsRejectJson(string text)
        {
            StringAssert.Contains("\"intent\":\"reject\"", Ask(LlmPurpose.ParseIntent, text).Text);
        }

        [Test]
        public void ParseIntent_WordContainingNo_IsNotReadAsRejection()
        {
            StringAssert.Contains("\"intent\":\"propose\"", Ask(LlmPurpose.ParseIntent, "te doy uno por dos").Text);
        }

        [Test]
        public void ParseIntent_NoKeywords_ReturnsSmallTalkJson()
        {
            StringAssert.Contains("\"intent\":\"smalltalk\"", Ask(LlmPurpose.ParseIntent, "hola, ¿qué tal?").Text);
        }

        [Test]
        public void GenerateReply_UsesThePoolOfTheDecisionMarker()
        {
            CollectionAssert.Contains(MockLanguageModel.AcceptLines, Ask(LlmPurpose.GenerateReply, MockLanguageModel.AcceptMarker).Text);
            CollectionAssert.Contains(MockLanguageModel.RejectLines, Ask(LlmPurpose.GenerateReply, MockLanguageModel.RejectMarker).Text);
            CollectionAssert.Contains(MockLanguageModel.CounterLines, Ask(LlmPurpose.GenerateReply, MockLanguageModel.CounterMarker).Text);
            CollectionAssert.Contains(MockLanguageModel.SmallTalkLines, Ask(LlmPurpose.GenerateReply, "hola").Text);
        }

        [Test]
        public void GenerateReply_SameSeed_GivesTheSameLines()
        {
            var first = new MockLanguageModel(seed: 7);
            var second = new MockLanguageModel(seed: 7);

            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(
                    Ask(LlmPurpose.GenerateReply, MockLanguageModel.RejectMarker, first).Text,
                    Ask(LlmPurpose.GenerateReply, MockLanguageModel.RejectMarker, second).Text);
            }
        }

        [Test]
        public void EmptyRequest_FailsWithoutThrowing()
        {
            var model = new MockLanguageModel();

            LlmResponse empty = model.CompleteAsync(new LlmRequest()).GetAwaiter().GetResult();
            LlmResponse missing = model.CompleteAsync(null).GetAwaiter().GetResult();

            Assert.IsFalse(empty.Success);
            Assert.IsFalse(missing.Success);
            Assert.IsNotEmpty(empty.Error);
        }

        [Test]
        public void CancelledRequest_FailsWithoutThrowing()
        {
            var request = new LlmRequest { Purpose = LlmPurpose.GenerateReply };
            request.Messages.Add(LlmMessage.User("hola"));
            var cancelled = new CancellationToken(canceled: true);

            LlmResponse response = new MockLanguageModel(fakeDelaySeconds: 1f).CompleteAsync(request, cancelled).GetAwaiter().GetResult();

            Assert.IsFalse(response.Success);
        }

        [Test]
        public void MessageFactories_SetTheRole()
        {
            Assert.AreEqual(LlmRole.System, LlmMessage.System("x").Role);
            Assert.AreEqual(LlmRole.User, LlmMessage.User("x").Role);
            Assert.AreEqual(LlmRole.Assistant, LlmMessage.Assistant("x").Role);
        }

        [Test]
        public void ResponseFactories_FillTheRightFields()
        {
            LlmResponse ok = LlmResponse.Ok("hola", 0.5f);
            LlmResponse fail = LlmResponse.Fail("sin red");

            Assert.IsTrue(ok.Success);
            Assert.AreEqual("hola", ok.Text);
            Assert.AreEqual(0.5f, ok.LatencySeconds);
            Assert.IsFalse(fail.Success);
            Assert.AreEqual("sin red", fail.Error);
            Assert.AreEqual(string.Empty, fail.Text);
        }
    }
}
