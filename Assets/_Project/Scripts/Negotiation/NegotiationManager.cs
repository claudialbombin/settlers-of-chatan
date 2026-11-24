using System;
using System.Collections.Generic;

namespace Chatan.Negotiation
{
    /// <summary>
    /// Keeps one <see cref="NegotiationSession"/> per bot during the human's turn.
    /// </summary>
    /// <remarks>
    /// In Catan you only trade on the active player's turn, so deals never survive the
    /// turn: the chat panel calls <see cref="ResetAll"/> when another player's turn starts.
    /// </remarks>
    public class NegotiationManager
    {
        private readonly Dictionary<int, NegotiationSession> _sessions = new Dictionary<int, NegotiationSession>();
        private readonly Func<int, NegotiationSession> _factory;

        /// <summary>Creates a manager.</summary>
        /// <param name="factory">Builds a new session for a bot id.</param>
        public NegotiationManager(Func<int, NegotiationSession> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>Number of sessions currently kept.</summary>
        public int Count => _sessions.Count;

        /// <summary>The session with that bot. A new one is created if there was none or the old one was not open.</summary>
        public NegotiationSession Get(int botId)
        {
            if (!_sessions.TryGetValue(botId, out NegotiationSession session) || session.Status != SessionStatus.Open)
            {
                session = _factory(botId);
                _sessions[botId] = session;
            }

            return session;
        }

        /// <summary>The current session with that bot without creating one, or null.</summary>
        public NegotiationSession Peek(int botId) =>
            _sessions.TryGetValue(botId, out NegotiationSession session) ? session : null;

        /// <summary>Forgets every session. Call it when the human's turn ends.</summary>
        public void ResetAll() => _sessions.Clear();
    }
}
