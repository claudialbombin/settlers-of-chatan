namespace Chatan.Core
{
    /// <summary>
    /// The five resources produced by the terrain tiles of the board.
    /// </summary>
    /// <remarks>
    /// Keep the order stable: Unity serializes enums by their numeric value, so
    /// reordering would silently change data already saved in assets and scenes.
    /// </remarks>
    public enum ResourceType
    {
        /// <summary>Produced by forests ("madera").</summary>
        Wood,

        /// <summary>Produced by hills ("ladrillo").</summary>
        Brick,

        /// <summary>Produced by pastures ("lana").</summary>
        Wool,

        /// <summary>Produced by fields ("trigo").</summary>
        Grain,

        /// <summary>Produced by mountains ("mineral").</summary>
        Ore
    }
}
