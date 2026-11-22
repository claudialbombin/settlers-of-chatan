namespace Chatan.UI
{
    /// <summary>The four things the build menu can buy.</summary>
    public enum BuildKind
    {
        /// <summary>A road (1 wood, 1 brick).</summary>
        Road,

        /// <summary>A settlement (1 wood, 1 brick, 1 wool, 1 grain).</summary>
        Settlement,

        /// <summary>Upgrade a settlement to a city (2 grain, 3 ore).</summary>
        City,

        /// <summary>A development card (1 wool, 1 grain, 1 ore).</summary>
        DevCard
    }
}
