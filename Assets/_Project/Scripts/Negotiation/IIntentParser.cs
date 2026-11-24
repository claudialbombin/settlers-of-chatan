using System.Threading;
using System.Threading.Tasks;
using Chatan.Core;

namespace Chatan.Negotiation
{
    /// <summary>
    /// Turns a free-text chat message of the human into a <see cref="ParsedIntent"/>.
    /// </summary>
    /// <remarks>Implemented in feature/intent-parser (with the LLM and a keyword fallback).</remarks>
    public interface IIntentParser
    {
        /// <summary>Parses <paramref name="humanText"/>.</summary>
        /// <param name="humanText">What the human wrote.</param>
        /// <param name="humanId">The human's player id (the offer's <c>FromPlayerId</c>).</param>
        /// <param name="botId">The bot being talked to (the offer's <c>ToPlayerId</c>).</param>
        /// <param name="state">Current game state. It is not modified.</param>
        /// <param name="ct">Cancels the request, e.g. when the chat is closed.</param>
        Task<ParsedIntent> ParseAsync(string humanText, int humanId, int botId, GameState state, CancellationToken ct);
    }
}
