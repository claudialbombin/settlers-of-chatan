namespace Chatan.AI
{
    /// <summary>
    /// Stable identifier of each of the ten bot personalities.
    /// </summary>
    /// <remarks>
    /// Used to name prompt files, training statistics and logs without depending on
    /// display text. Keep the order stable: Unity serializes enums by their numeric
    /// value, so reordering would change the identity of existing personality assets.
    /// </remarks>
    public enum PersonalityId
    {
        /// <summary>Blocks and attacks the leader ("Agresivo").</summary>
        Aggressive,

        /// <summary>Hoards cards and demands a lot in every trade ("Acaparador").</summary>
        Greedy,

        /// <summary>Likes fair deals and good relationships ("Conciliador").</summary>
        Conciliator,

        /// <summary>Goes for the longest road ("Constructor de carreteras").</summary>
        RoadBuilder,

        /// <summary>Focuses on cities, ore and grain ("Industrial").</summary>
        Industrialist,

        /// <summary>Buys and plays development cards ("Estratega de cartas").</summary>
        CardStrategist,

        /// <summary>Trades a lot and uses ports ("Comerciante").</summary>
        Merchant,

        /// <summary>Remembers who hurt it and pays them back ("Vengativo").</summary>
        Vengeful,

        /// <summary>Avoids risks and keeps few cards ("Cauteloso").</summary>
        Cautious,

        /// <summary>Unpredictable and bluffs often ("Tramposo / caótico").</summary>
        Trickster
    }
}
