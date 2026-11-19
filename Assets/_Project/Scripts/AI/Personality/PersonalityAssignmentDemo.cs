using UnityEngine;

namespace Chatan.AI
{
    /// <summary>
    /// Sandbox helper: draws the personalities of the bots on <c>Start</c> and logs them
    /// with the seed, the same way the game bootstrap will.
    /// </summary>
    /// <remarks>
    /// Add it to any GameObject in <c>Sandbox_Claudia.unity</c> and drag the catalog in.
    /// Use <c>Reroll</c> in the component's context menu to draw again with a new seed.
    /// Remove it once the game bootstrap does the assignment.
    /// </remarks>
    public class PersonalityAssignmentDemo : MonoBehaviour
    {
        /// <summary>Catalog to draw from (Data/Personalities/PersonalityCatalog.asset).</summary>
        [Tooltip("Catalog to draw from (Data/Personalities/PersonalityCatalog.asset).")]
        public PersonalityCatalog catalog;

        /// <summary>Player ids of the bots.</summary>
        [Tooltip("Player ids of the bots.")]
        public int[] botIds = { 1, 2, 3 };

        /// <summary>If enabled, always uses <see cref="fixedSeed"/> to reproduce a game.</summary>
        [Tooltip("If enabled, always uses fixedSeed to reproduce a game.")]
        public bool useFixedSeed;

        /// <summary>Seed used when <see cref="useFixedSeed"/> is enabled.</summary>
        public int fixedSeed = 12345;

        /// <summary>The last assignment, for other sandbox scripts.</summary>
        public BotPersonalityRegistry Registry { get; private set; }

        private void Start() => Assign(useFixedSeed ? fixedSeed : System.Environment.TickCount);

        [ContextMenu("Reroll")]
        private void Reroll() => Assign(System.Environment.TickCount);

        private void Assign(int seed)
        {
            if (catalog == null)
            {
                Debug.LogWarning($"{name}: no personality catalog assigned.", this);
                return;
            }

            var assignment = PersonalityAssigner.Assign(botIds, catalog.Profiles, seed);
            Registry = new BotPersonalityRegistry(assignment, seed);
            Debug.Log($"[Personalities] {Registry}", this);
        }
    }
}
