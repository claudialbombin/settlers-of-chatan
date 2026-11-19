using System;

namespace Chatan.LLM
{
    /// <summary>
    /// One message of a conversation sent to a language model: a role plus its text.
    /// </summary>
    /// <remarks>
    /// It is a small immutable-by-convention value type. Prefer the factory methods
    /// (<see cref="User"/>, <see cref="System"/>, <see cref="Assistant"/>) over the
    /// constructor: they read better at the call site.
    /// </remarks>
    [Serializable]
    public struct LlmMessage
    {
        /// <summary>Who wrote the message.</summary>
        public LlmRole Role;

        /// <summary>The text of the message.</summary>
        public string Content;

        /// <summary>Creates a message with the given role and text.</summary>
        public LlmMessage(LlmRole role, string content)
        {
            Role = role;
            Content = content;
        }

        /// <summary>Creates a <see cref="LlmRole.System"/> message (instructions for the model).</summary>
        public static LlmMessage System(string content) => new LlmMessage(LlmRole.System, content);

        /// <summary>Creates a <see cref="LlmRole.User"/> message (player input).</summary>
        public static LlmMessage User(string content) => new LlmMessage(LlmRole.User, content);

        /// <summary>Creates an <see cref="LlmRole.Assistant"/> message (a previous model answer).</summary>
        public static LlmMessage Assistant(string content) => new LlmMessage(LlmRole.Assistant, content);

        /// <summary>Short description for logs, e.g. "User: te doy 1 trigo".</summary>
        public override string ToString() => $"{Role}: {Content}";
    }
}
