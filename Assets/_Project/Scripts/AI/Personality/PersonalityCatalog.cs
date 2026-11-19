using System.Collections.Generic;
using UnityEngine;

namespace Chatan.AI
{
    /// <summary>
    /// Asset that lists every personality available in the game.
    /// </summary>
    /// <remarks>
    /// The personality assigner receives this catalog instead of searching the project
    /// for profile assets, so adding a personality only means creating its asset and
    /// dragging it into <see cref="profiles"/>.
    /// </remarks>
    [CreateAssetMenu(menuName = "Chatan/Personality Catalog", fileName = "PersonalityCatalog")]
    public class PersonalityCatalog : ScriptableObject
    {
        /// <summary>The personalities of the game, one per <see cref="PersonalityId"/>.</summary>
        [Tooltip("The personalities of the game, one per PersonalityId.")]
        public List<PersonalityProfile> profiles = new List<PersonalityProfile>();

        /// <summary>Read-only view of <see cref="profiles"/>, for code that must not edit the list.</summary>
        public IReadOnlyList<PersonalityProfile> Profiles => profiles;

        /// <summary>
        /// The profile with the given identifier, or <c>null</c> if the catalog has none.
        /// Empty slots in the list are skipped.
        /// </summary>
        public PersonalityProfile Get(PersonalityId id) => profiles.Find(p => p != null && p.id == id);

        /// <summary>Whether the catalog contains a profile with the given identifier.</summary>
        public bool Contains(PersonalityId id) => Get(id) != null;

#if UNITY_EDITOR
        /// <summary>
        /// Called by the editor whenever the list changes. Warns about empty slots and
        /// repeated personalities; it never changes the list.
        /// </summary>
        private void OnValidate()
        {
            var seen = new HashSet<PersonalityId>();
            foreach (PersonalityProfile profile in profiles)
            {
                if (profile == null)
                {
                    Debug.LogWarning($"{name}: the catalog has an empty slot.", this);
                    continue;
                }

                if (!seen.Add(profile.id))
                {
                    Debug.LogWarning($"{name}: personality {profile.id} appears more than once.", this);
                }
            }
        }
#endif
    }
}
