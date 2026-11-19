using System;
using System.Collections.Generic;
using System.Linq;
using Chatan.AI;
using NUnit.Framework;
using UnityEditor;

namespace Chatan.Tests
{
    /// <summary>
    /// Checks the personality assets shipped in <c>Data/Personalities</c>: the catalog must
    /// list the ten personalities, each one complete and different from the others.
    /// </summary>
    public class PersonalityAssetsTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Personalities/PersonalityCatalog.asset";

        private PersonalityCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<PersonalityCatalog>(CatalogPath);
            Assert.IsNotNull(_catalog, $"No catalog found at {CatalogPath}.");
        }

        [Test]
        public void Catalog_HasOneProfilePerPersonalityId()
        {
            Assert.IsTrue(_catalog.Profiles.All(p => p != null), "The catalog has empty slots.");

            var ids = _catalog.Profiles.Select(p => p.id).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
            CollectionAssert.AreEquivalent(Enum.GetValues(typeof(PersonalityId)), ids);
        }

        [Test]
        public void EveryProfile_HasNameDescriptionAndPrompt()
        {
            foreach (PersonalityProfile profile in _catalog.Profiles)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(profile.displayName), $"{profile.name}: no displayName.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(profile.shortDescription), $"{profile.name}: no shortDescription.");
                Assert.IsNotNull(profile.promptFile, $"{profile.name}: no promptFile.");
                StringAssert.StartsWith($"Eres {profile.displayName}", profile.PromptText, $"{profile.name}: prompt of another character?");
            }
        }

        [Test]
        public void NoTwoProfiles_ShareTheSameValues()
        {
            var seen = new HashSet<string>();
            foreach (PersonalityProfile profile in _catalog.Profiles)
            {
                string key = string.Join("|",
                    profile.aggression, profile.greed, profile.cooperation, profile.grudge,
                    profile.bluffing, profile.riskTolerance, profile.randomness,
                    profile.roadWeight, profile.settlementWeight, profile.cityWeight,
                    profile.devCardWeight, profile.portWeight);
                Assert.IsTrue(seen.Add(key), $"{profile.name} has the same values as another personality.");
            }
        }
    }
}
