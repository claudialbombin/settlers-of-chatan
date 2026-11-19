using System.Collections.Generic;

namespace Chatan.LLM
{
    /// <summary>
    /// Everything sent to a language model in a single call: the conversation plus the
    /// generation settings.
    /// </summary>
    public class LlmRequest
    {
        /// <summary>Default <see cref="Temperature"/>: some variety without going off-topic.</summary>
        public const float DefaultTemperature = 0.7f;

        /// <summary>Default <see cref="MaxTokens"/>: answers are short, it is an in-game chat.</summary>
        public const int DefaultMaxTokens = 200;

        /// <summary>The conversation, oldest message first. The last one is what the model answers.</summary>
        public List<LlmMessage> Messages = new List<LlmMessage>();

        /// <summary>Why the model is being called.</summary>
        public LlmPurpose Purpose;

        /// <summary>True when the answer must be ONLY a JSON object, with no surrounding text.</summary>
        public bool ExpectJson;

        /// <summary>Randomness of the answer: 0 is deterministic, 1 is very creative.</summary>
        public float Temperature = DefaultTemperature;

        /// <summary>Maximum length of the answer, in tokens.</summary>
        public int MaxTokens = DefaultMaxTokens;

        /// <summary>True when there is at least one message to answer.</summary>
        public bool HasMessages => Messages != null && Messages.Count > 0;
    }
}
