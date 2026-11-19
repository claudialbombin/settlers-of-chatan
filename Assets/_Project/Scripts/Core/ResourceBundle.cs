using System;
using System.Collections.Generic;

namespace Chatan.Core
{
    /// <summary>
    /// An amount of each of the five resources: a player's hand, a building cost,
    /// one side of a trade, a dice payout...
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is a <c>struct</c> (value type) on purpose: assigning or passing it copies the
    /// amounts, so changing one bundle can never modify another one by accident.
    /// </para>
    /// <para>
    /// The five amounts are public fields (not properties) so Unity can serialize them
    /// and show them in the Inspector. Amounts may become negative after a subtraction;
    /// callers that need a valid hand should check <see cref="Contains"/> first.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct ResourceBundle : IEquatable<ResourceBundle>
    {
        /// <summary>Every resource type, in display order. Handy for loops in UI and logic.</summary>
        public static readonly IReadOnlyList<ResourceType> AllTypes = new[]
        {
            ResourceType.Wood, ResourceType.Brick, ResourceType.Wool, ResourceType.Grain, ResourceType.Ore
        };

        /// <summary>A bundle with zero of everything.</summary>
        public static readonly ResourceBundle Empty = default;

        /// <summary>Cost of a road: 1 wood, 1 brick.</summary>
        public static readonly ResourceBundle RoadCost = new ResourceBundle(wood: 1, brick: 1);

        /// <summary>Cost of a settlement: 1 wood, 1 brick, 1 wool, 1 grain.</summary>
        public static readonly ResourceBundle SettlementCost = new ResourceBundle(wood: 1, brick: 1, wool: 1, grain: 1);

        /// <summary>Cost of upgrading a settlement to a city: 2 grain, 3 ore.</summary>
        public static readonly ResourceBundle CityCost = new ResourceBundle(grain: 2, ore: 3);

        /// <summary>Cost of a development card: 1 wool, 1 grain, 1 ore.</summary>
        public static readonly ResourceBundle DevCardCost = new ResourceBundle(wool: 1, grain: 1, ore: 1);

        /// <summary>Amount of wood.</summary>
        public int Wood;

        /// <summary>Amount of brick.</summary>
        public int Brick;

        /// <summary>Amount of wool.</summary>
        public int Wool;

        /// <summary>Amount of grain.</summary>
        public int Grain;

        /// <summary>Amount of ore.</summary>
        public int Ore;

        /// <summary>
        /// Creates a bundle. Use named arguments for readability, e.g.
        /// <c>new ResourceBundle(grain: 2, ore: 3)</c>.
        /// </summary>
        public ResourceBundle(int wood = 0, int brick = 0, int wool = 0, int grain = 0, int ore = 0)
        {
            Wood = wood;
            Brick = brick;
            Wool = wool;
            Grain = grain;
            Ore = ore;
        }

        /// <summary>
        /// Reads or writes the amount of one resource, so code can loop over
        /// <see cref="AllTypes"/> instead of naming each field.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="type"/> is not a defined value.</exception>
        public int this[ResourceType type]
        {
            get => type switch
            {
                ResourceType.Wood => Wood,
                ResourceType.Brick => Brick,
                ResourceType.Wool => Wool,
                ResourceType.Grain => Grain,
                ResourceType.Ore => Ore,
                _ => throw UnknownType(type)
            };
            set
            {
                switch (type)
                {
                    case ResourceType.Wood: Wood = value; break;
                    case ResourceType.Brick: Brick = value; break;
                    case ResourceType.Wool: Wool = value; break;
                    case ResourceType.Grain: Grain = value; break;
                    case ResourceType.Ore: Ore = value; break;
                    default: throw UnknownType(type);
                }
            }
        }

        /// <summary>Sum of the five amounts (number of cards in a hand).</summary>
        public int Total => Wood + Brick + Wool + Grain + Ore;

        /// <summary>
        /// True when every amount is zero. Checked field by field (not via <see cref="Total"/>)
        /// so a bundle like "+1 wood, -1 ore" is not reported as empty.
        /// </summary>
        public bool IsEmpty => Wood == 0 && Brick == 0 && Wool == 0 && Grain == 0 && Ore == 0;

        /// <summary>
        /// True when this bundle has at least as much of every resource as <paramref name="other"/>.
        /// Typical use: <c>player.Resources.Contains(ResourceBundle.CityCost)</c> ("can I afford it?").
        /// </summary>
        public bool Contains(ResourceBundle other)
        {
            foreach (ResourceType type in AllTypes)
            {
                if (this[type] < other[type])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Creates a bundle with a single resource, e.g. <c>ResourceBundle.Of(ResourceType.Grain, 2)</c>.</summary>
        public static ResourceBundle Of(ResourceType type, int amount)
        {
            ResourceBundle bundle = Empty;
            bundle[type] = amount;
            return bundle;
        }

        /// <summary>Adds the amounts resource by resource.</summary>
        public static ResourceBundle operator +(ResourceBundle a, ResourceBundle b) =>
            new ResourceBundle(a.Wood + b.Wood, a.Brick + b.Brick, a.Wool + b.Wool, a.Grain + b.Grain, a.Ore + b.Ore);

        /// <summary>Subtracts the amounts resource by resource. The result may contain negative amounts.</summary>
        public static ResourceBundle operator -(ResourceBundle a, ResourceBundle b) =>
            new ResourceBundle(a.Wood - b.Wood, a.Brick - b.Brick, a.Wool - b.Wool, a.Grain - b.Grain, a.Ore - b.Ore);

        /// <summary>Two bundles are equal when all five amounts match.</summary>
        public static bool operator ==(ResourceBundle a, ResourceBundle b) => a.Equals(b);

        /// <summary>True when at least one of the five amounts differs.</summary>
        public static bool operator !=(ResourceBundle a, ResourceBundle b) => !a.Equals(b);

        /// <inheritdoc/>
        public bool Equals(ResourceBundle other) =>
            Wood == other.Wood && Brick == other.Brick && Wool == other.Wool && Grain == other.Grain && Ore == other.Ore;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is ResourceBundle other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Wood, Brick, Wool, Grain, Ore);

        /// <summary>
        /// Player-facing description listing only the non-zero amounts,
        /// e.g. "2 trigo, 1 mineral". Returns "nada" for an empty bundle.
        /// </summary>
        public override string ToString()
        {
            var parts = new List<string>();

            foreach (ResourceType type in AllTypes)
            {
                int amount = this[type];
                if (amount != 0)
                {
                    parts.Add($"{amount} {ResourceNames.ToSpanish(type)}");
                }
            }

            return parts.Count == 0 ? "nada" : string.Join(", ", parts);
        }

        private static ArgumentOutOfRangeException UnknownType(ResourceType type) =>
            new ArgumentOutOfRangeException(nameof(type), type, "Unknown resource type.");
    }
}
