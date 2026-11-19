namespace Chatan.LLM
{
    /// <summary>
    /// What a request to the language model is for. Real models ignore it; it lets the
    /// mock (and logs) know which kind of answer is expected.
    /// </summary>
    public enum LlmPurpose
    {
        /// <summary>Turn the player's free text into a structured JSON intent (text → offer).</summary>
        ParseIntent,

        /// <summary>Write the bot's chat line for a decision already taken in C# (decision → text).</summary>
        GenerateReply
    }
}
