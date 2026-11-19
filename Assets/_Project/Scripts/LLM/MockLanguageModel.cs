using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chatan.LLM
{
    /// <summary>
    /// Fake language model with canned answers. It lets the parser, the reply generator
    /// and the chat be built and tested without Ollama, without internet and without
    /// waiting seconds for each answer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="LlmPurpose.ParseIntent"/>: a tiny keyword-based parse that always returns
    /// the same JSON shape the real intent parser expects. Resource names in the JSON are
    /// lower-case English (<c>grain</c>, <c>ore</c>...) on purpose, to match the parser's
    /// data class.
    /// </para>
    /// <para>
    /// <see cref="LlmPurpose.GenerateReply"/>: a random line from a fixed pool, chosen by
    /// the <c>decision: accept|reject|counter</c> marker that the prompt builder writes in
    /// the last message. If that marker format changes there, change it here too.
    /// </para>
    /// </remarks>
    public class MockLanguageModel : ILanguageModel
    {
        /// <summary>Marker the prompt builder writes for an accepted trade.</summary>
        public const string AcceptMarker = "decision: accept";

        /// <summary>Marker the prompt builder writes for a rejected trade.</summary>
        public const string RejectMarker = "decision: reject";

        /// <summary>Marker the prompt builder writes for a counter-offer.</summary>
        public const string CounterMarker = "decision: counter";

        private const string AcceptJson = "{\"intent\":\"accept\",\"give\":{},\"receive\":{}}";
        private const string RejectJson = "{\"intent\":\"reject\",\"give\":{},\"receive\":{}}";
        private const string SmallTalkJson = "{\"intent\":\"smalltalk\",\"give\":{},\"receive\":{}}";

        // The mock does not read amounts: any proposal becomes "1 grain for 1 ore".
        private const string ProposeJson = "{\"intent\":\"propose\",\"give\":{\"grain\":1},\"receive\":{\"ore\":1}}";

        /// <summary>Lines used when the decision is to accept.</summary>
        public static readonly IReadOnlyList<string> AcceptLines = new[] { "¡Trato hecho!", "Me parece justo, acepto.", "Venga, va." };

        /// <summary>Lines used when the decision is to reject.</summary>
        public static readonly IReadOnlyList<string> RejectLines = new[] { "Ni lo sueñes.", "No me interesa.", "Paso, gracias." };

        /// <summary>Lines used when the bot makes a counter-offer.</summary>
        public static readonly IReadOnlyList<string> CounterLines = new[] { "Casi... ¿y si me das uno más?", "Te hago una contraoferta." };

        /// <summary>Lines used when there is no decision marker (small talk).</summary>
        public static readonly IReadOnlyList<string> SmallTalkLines = new[] { "Mmm, a ver qué me ofreces.", "Hablemos de negocios." };

        private static readonly HashSet<string> RejectWords = new HashSet<string> { "no", "paso" };
        private static readonly HashSet<string> AcceptWords = new HashSet<string> { "acepto", "vale" };

        private const string AcceptPhrase = "trato hecho";
        private const string ProposeWord = "por";

        private static readonly char[] WordSeparators =
            { ' ', '\t', '\n', ',', '.', ';', ':', '!', '¡', '?', '¿', '"', '(', ')' };

        private readonly TimeSpan _fakeDelay;
        private readonly Random _random;

        /// <summary>Creates the mock.</summary>
        /// <param name="fakeDelaySeconds">
        /// Artificial wait before answering. Use a value above 0 to see the chat's
        /// "typing..." indicator; keep it at 0 in tests.
        /// </param>
        /// <param name="seed">Seed for picking reply lines, so runs are reproducible.</param>
        public MockLanguageModel(float fakeDelaySeconds = 0f, int seed = 42)
        {
            _fakeDelay = TimeSpan.FromSeconds(Math.Max(0f, fakeDelaySeconds));
            _random = new Random(seed);
        }

        /// <inheritdoc/>
        public string DisplayName => "Mock";

        /// <inheritdoc/>
        public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            if (request == null || !request.HasMessages)
            {
                return LlmResponse.Fail("Empty request: there is no message to answer.");
            }

            try
            {
                if (_fakeDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_fakeDelay, cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();

                string lastMessage = (request.Messages.Last().Content ?? string.Empty).ToLowerInvariant();
                float latency = (float)stopwatch.Elapsed.TotalSeconds;

                return request.Purpose switch
                {
                    LlmPurpose.ParseIntent => LlmResponse.Ok(FakeParse(lastMessage), latency),
                    LlmPurpose.GenerateReply => LlmResponse.Ok(FakeReply(lastMessage), latency),
                    _ => LlmResponse.Fail($"Unknown purpose: {request.Purpose}.", latency)
                };
            }
            catch (OperationCanceledException)
            {
                return LlmResponse.Fail("Cancelled.", (float)stopwatch.Elapsed.TotalSeconds);
            }
            catch (Exception exception)
            {
                // The contract forbids throwing: report anything unexpected as a failure.
                return LlmResponse.Fail($"Mock error: {exception.Message}", (float)stopwatch.Elapsed.TotalSeconds);
            }
        }

        /// <summary>
        /// Keyword-based intent guess. Rejection is checked first so that "no acepto"
        /// is read as a rejection, and whole words are compared so that "uno" does not
        /// count as "no".
        /// </summary>
        private static string FakeParse(string text)
        {
            HashSet<string> words = SplitWords(text);

            if (words.Overlaps(RejectWords))
            {
                return RejectJson;
            }

            if (words.Overlaps(AcceptWords) || text.Contains(AcceptPhrase))
            {
                return AcceptJson;
            }

            if (words.Contains(ProposeWord))
            {
                return ProposeJson;
            }

            return SmallTalkJson;
        }

        /// <summary>Picks a canned line matching the decision marker in the text.</summary>
        private string FakeReply(string text)
        {
            IReadOnlyList<string> pool = SelectReplyPool(text);
            return pool[_random.Next(pool.Count)];
        }

        private static IReadOnlyList<string> SelectReplyPool(string text)
        {
            if (text.Contains(AcceptMarker))
            {
                return AcceptLines;
            }

            if (text.Contains(RejectMarker))
            {
                return RejectLines;
            }

            if (text.Contains(CounterMarker))
            {
                return CounterLines;
            }

            return SmallTalkLines;
        }

        private static HashSet<string> SplitWords(string text) =>
            new HashSet<string>(text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries));
    }
}
