using System.Threading;
using System.Threading.Tasks;

namespace Chatan.LLM
{
    /// <summary>
    /// Any language model the game can talk to (the offline mock, a local Ollama model,
    /// a remote API...).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rest of the game depends only on this interface, never on a concrete model,
    /// so switching models means writing one new class and changing one line where it
    /// is created (Dependency Inversion / Open-Closed).
    /// </para>
    /// <para>
    /// Golden rule: a model only translates (text → structured offer, decision → text).
    /// It never decides a trade and never changes the game state.
    /// </para>
    /// </remarks>
    public interface ILanguageModel
    {
        /// <summary>Name for logs and debugging, e.g. "Mock" or "Ollama qwen2.5:3b".</summary>
        string DisplayName { get; }

        /// <summary>
        /// Sends <paramref name="request"/> to the model and returns its answer.
        /// </summary>
        /// <remarks>
        /// Contract: this method NEVER throws. Any problem (bad request, network error,
        /// timeout, cancellation) is returned as <see cref="LlmResponse.Fail"/>, so callers
        /// only need to check <see cref="LlmResponse.Success"/>. In game code always
        /// <c>await</c> it; never use <c>.Result</c>, which would freeze Unity.
        /// </remarks>
        /// <param name="request">The conversation and generation settings.</param>
        /// <param name="cancellationToken">Cancels the call, e.g. when the chat window closes.</param>
        Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default);
    }
}
