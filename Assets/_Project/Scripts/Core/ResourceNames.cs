using System;

namespace Chatan.Core
{
    /// <summary>
    /// Player-facing (Spanish) names for each <see cref="ResourceType"/>.
    /// </summary>
    /// <remarks>
    /// Presentation text lives here, apart from the game logic, so wording can be
    /// changed or localised without touching <see cref="ResourceBundle"/> or the rules
    /// (Single Responsibility Principle).
    /// </remarks>
    public static class ResourceNames
    {
        /// <summary>
        /// Returns the singular Spanish name of a resource, e.g. "trigo" for <see cref="ResourceType.Grain"/>.
        /// </summary>
        /// <param name="type">The resource to name.</param>
        /// <returns>The lower-case Spanish name.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="type"/> is not a defined value.</exception>
        public static string ToSpanish(ResourceType type) => type switch
        {
            ResourceType.Wood => "madera",
            ResourceType.Brick => "ladrillo",
            ResourceType.Wool => "lana",
            ResourceType.Grain => "trigo",
            ResourceType.Ore => "mineral",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown resource type.")
        };
    }
}
