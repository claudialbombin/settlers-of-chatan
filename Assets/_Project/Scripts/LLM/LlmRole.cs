namespace Chatan.LLM
{
    /// <summary>
    /// Who wrote a message in a conversation with a language model. These are the three
    /// roles that almost every chat-style LLM API understands.
    /// </summary>
    public enum LlmRole
    {
        /// <summary>Instructions that set up the model's behaviour (personality, output format...).</summary>
        System,

        /// <summary>A message from the player, or the input we want the model to process.</summary>
        User,

        /// <summary>A previous answer from the model, kept so it remembers the conversation.</summary>
        Assistant
    }
}
