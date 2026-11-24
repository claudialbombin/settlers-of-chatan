namespace Chatan.Negotiation
{
    /// <summary>What the human meant with a chat message, as the intent parser understood it.</summary>
    public enum IntentType
    {
        /// <summary>A new trade offer ("te doy 2 trigo por 1 mineral").</summary>
        Propose,

        /// <summary>Accepts the offer on the table ("vale, trato hecho").</summary>
        Accept,

        /// <summary>Rejects the offer on the table ("ni de broma").</summary>
        Reject,

        /// <summary>Changes the bot's last offer ("mejor 3 trigo").</summary>
        Counter,

        /// <summary>Asks what the bot needs ("¿qué te hace falta?").</summary>
        AskNeeds,

        /// <summary>Greetings and chatter. It never uses up a negotiation round.</summary>
        SmallTalk,

        /// <summary>The parser could not tell.</summary>
        Unknown
    }
}
