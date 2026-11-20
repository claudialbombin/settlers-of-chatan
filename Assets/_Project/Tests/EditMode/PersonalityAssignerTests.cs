using System;
using System.Collections.Generic;
using System.Linq;
using Chatan.AI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="PersonalityAssigner"/>.</summary>
    public class PersonalityAssignerTests
    {
        private static readonly int[] Bots = { 1, 2, 3 };

        private List<PersonalityProfile> _catalog;

        [SetUp]
        public void SetUp() => _catalog = MakeCatalog(10);

        [TearDown]
        public void TearDown()
        {
            foreach (PersonalityProfile profile in _catalog)
            {
                Object.DestroyImmediate(profile);
            }
        }

        private static List<PersonalityProfile> MakeCatalog(int count)
        {
            var list = new List<PersonalityProfile>();
            for (int i = 0; i < count; i++)
            {
                var profile = ScriptableObject.CreateInstance<PersonalityProfile>();
                profile.id = (PersonalityId)i;
                list.Add(profile);
            }

            return list;
        }

        [Test]
        public void SameSeed_GivesTheSameAssignment()
        {
            var a = PersonalityAssigner.Assign(Bots, _catalog, seed: 123);
            var b = PersonalityAssigner.Assign(Bots, _catalog, seed: 123);

            foreach (int bot in Bots)
            {
                Assert.AreSame(a[bot], b[bot]);
            }
        }

        [Test]
        public void EveryBot_GetsAPersonality()
        {
            var result = PersonalityAssigner.Assign(Bots, _catalog, seed: 7);

            CollectionAssert.AreEquivalent(Bots, result.Keys);
            Assert.IsTrue(result.Values.All(p => p != null));
        }

        [Test]
        public void WithoutRepeats_BotsNeverShareAPersonality()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var result = PersonalityAssigner.Assign(Bots, _catalog, seed);
                Assert.AreEqual(Bots.Length, result.Values.Distinct().Count(), $"Repeated personality with seed {seed}.");
            }
        }

        [Test]
        public void EveryPersonality_ComesOutSometimes()
        {
            var seen = new HashSet<PersonalityId>();
            for (int seed = 0; seed < 500; seed++)
            {
                foreach (PersonalityProfile profile in PersonalityAssigner.Assign(Bots, _catalog, seed).Values)
                {
                    seen.Add(profile.id);
                }
            }

            Assert.AreEqual(_catalog.Count, seen.Count);
        }

        [Test]
        public void DifferentSeeds_GiveDifferentAssignments()
        {
            var combinations = new HashSet<string>();
            for (int seed = 0; seed < 5; seed++)
            {
                var result = PersonalityAssigner.Assign(Bots, _catalog, seed);
                combinations.Add(string.Join(",", Bots.Select(b => result[b].id)));
            }

            Assert.Greater(combinations.Count, 1);
        }

        [Test]
        public void Assign_DoesNotReorderTheCatalog()
        {
            var before = _catalog.ToList();

            PersonalityAssigner.Assign(Bots, _catalog, seed: 99);

            CollectionAssert.AreEqual(before, _catalog);
        }

        [Test]
        public void Assign_IgnoresEmptySlots()
        {
            var withHoles = new List<PersonalityProfile> { null, _catalog[0], null, _catalog[1], _catalog[2] };

            var result = PersonalityAssigner.Assign(Bots, withHoles, seed: 3);

            Assert.IsTrue(result.Values.All(p => p != null));
        }

        [Test]
        public void SmallCatalog_WithoutRepeats_Throws()
        {
            Assert.Throws<ArgumentException>(() => PersonalityAssigner.Assign(Bots, _catalog.Take(2).ToList(), 0));
        }

        [Test]
        public void SmallCatalog_WithRepeats_StillAssignsEveryBot()
        {
            var result = PersonalityAssigner.Assign(Bots, _catalog.Take(2).ToList(), 0, allowRepeats: true);

            Assert.AreEqual(Bots.Length, result.Count);
        }

        [Test]
        public void EmptyCatalog_Throws()
        {
            Assert.Throws<ArgumentException>(() => PersonalityAssigner.Assign(Bots, new List<PersonalityProfile>(), 0));
            Assert.Throws<ArgumentException>(() => PersonalityAssigner.Assign(Bots, null, 0));
        }

        [Test]
        public void RepeatedBotIds_Throw()
        {
            Assert.Throws<ArgumentException>(() => PersonalityAssigner.Assign(new[] { 1, 1, 2 }, _catalog, 0));
        }

        [Test]
        public void NoBots_GivesAnEmptyAssignment()
        {
            Assert.AreEqual(0, PersonalityAssigner.Assign(new int[0], _catalog, 0).Count);
            Assert.AreEqual(0, PersonalityAssigner.Assign(null, _catalog, 0).Count);
        }

        [Test]
        public void RealCatalog_SameSeedSameBots()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PersonalityCatalog>(
                "Assets/_Project/Data/Personalities/PersonalityCatalog.asset");
            Assert.IsNotNull(catalog);

            var a = PersonalityAssigner.Assign(Bots, catalog.Profiles, seed: 2026);
            var b = PersonalityAssigner.Assign(Bots, catalog.Profiles, seed: 2026);

            Assert.AreEqual(new BotPersonalityRegistry(a, 2026).ToString(), new BotPersonalityRegistry(b, 2026).ToString());
            Assert.AreEqual(Bots.Length, a.Values.Distinct().Count());
        }
    }
}
