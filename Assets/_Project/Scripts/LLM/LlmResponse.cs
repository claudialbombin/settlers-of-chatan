namespace Chatan.LLM
{
    /// <summary>
    /// The result of a call to a language model. A failure is reported here instead of
    /// being thrown, so a network problem can never break the match.
    /// </summary>
    /// <remarks>
    /// Always check <see cref="Success"/> before reading <see cref="Text"/>.
    /// Create instances with <see cref="Ok"/> or <see cref="Fail"/>.
    /// </remarks>
    public class LlmResponse
    {
        /// <summary>True if the model answered.</summary>
        public bool Success { get; private set; }

        /// <summary>The model's answer (plain text, or JSON as a string). Empty on failure.</summary>
        public string Text { get; private set; } = string.Empty;

        /// <summary>Human-readable reason for the failure. Empty on success.</summary>
        public string Error { get; private set; } = string.Empty;

        /// <summary>How long the call took, in seconds. Useful for logs and tuning.</summary>
        public float LatencySeconds { get; private set; }

        // Private so the only ways to build a response are the two factories below.
        private LlmResponse()
        {
        }

        /// <summary>Creates a successful response.</summary>
        /// <param name="text">The model's answer.</param>
        /// <param name="latencySeconds">How long the call took.</param>
        public static LlmResponse Ok(string text, float latencySeconds = 0f) =>
            new LlmResponse { Success = true, Text = text ?? string.Empty, LatencySeconds = latencySeconds };

        /// <summary>Creates a failed response.</summary>
        /// <param name="error">Why the call failed.</param>
        /// <param name="latencySeconds">How long it took before failing.</param>
        public static LlmResponse Fail(string error, float latencySeconds = 0f) =>
            new LlmResponse { Success = false, Error = error ?? string.Empty, LatencySeconds = latencySeconds };

        /// <summary>Short description for logs.</summary>
        public override string ToString() => Success ? $"OK ({LatencySeconds:0.00}s): {Text}" : $"FAIL: {Error}";
    }
}
