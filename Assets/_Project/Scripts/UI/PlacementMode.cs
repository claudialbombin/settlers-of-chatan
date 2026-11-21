namespace Chatan.UI
{
    /// <summary>What the next click on the board will place.</summary>
    public enum PlacementMode
    {
        /// <summary>Clicks on the board do nothing.</summary>
        None,

        /// <summary>The next click on a highlighted edge builds a road.</summary>
        Road,

        /// <summary>The next click on a highlighted vertex builds a settlement.</summary>
        Settlement,

        /// <summary>The next click on one of your highlighted settlements upgrades it.</summary>
        City,

        /// <summary>The next click on a highlighted tile moves the robber there.</summary>
        Robber
    }
}
