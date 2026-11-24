using System.Threading;
using System.Threading.Tasks;

namespace Chatan.Negotiation
{
    /// <summary>Writes the bot's chat answer for a decision that is already taken.</summary>
    /// <remarks>Implemented in feature/response-generator (LLM with canned fallbacks per personality).</remarks>
    public interface IReplyGenerator
    {
        /// <summary>Writes the answer. It must never change the decision in <paramref name="context"/>.</summary>
        Task<string> GenerateAsync(ReplyContext context, CancellationToken ct);
    }
}
