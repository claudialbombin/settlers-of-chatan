using System.Collections.Generic;
using Chatan.AI;
using NUnit.Framework;
using UnityEngine;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="BotPersonalityRegistry"/>.</summary>
    public class BotPersonalityRegistryTests
    {
        private PersonalityProfile _greedy;
        private PersonalityProfile _cautious;
        private Dictionary<int, PersonalityProfile> _assignment;

        [SetUp]
        public void SetUp()
        {
            _greedy = ScriptableObject.CreateInstance<PersonalityProfile>();
            _greedy.id = PersonalityId.Greedy;
            _cautious = ScriptableObject.CreateInstance<PersonalityProfile>();
            _cautious.id = PersonalityId.Cautious;

            _assignment = new Dictionary<int, PersonalityProfile> { [2] = _cautious, [1] = _greedy };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_greedy);
            Object.DestroyImmediate(_cautious);
        }

        [Test]
        public void Get_ReturnsTheBotPersonality_OrNullForTheHuman()
        {
            var registry = new BotPersonalityRegistry(_assignment, seed: 5);

            Assert.AreSame(_greedy, registry.Get(1));
            Assert.AreSame(_cautious, registry.Get(2));
            Assert.IsNull(registry.Get(0));
            Assert.IsFalse(registry.TryGet(0, out _));
        }

        [Test]
        public void Registry_CopiesTheAssignment()
        {
            var registry = new BotPersonalityRegistry(_assignment, seed: 5);

            _assignment[3] = _greedy;

            Assert.AreEqual(2, registry.Count);
            Assert.IsNull(registry.Get(3));
        }

        [Test]
        public void ToString_ShowsTheSeedAndEachBotInOrder()
        {
            var registry = new BotPersonalityRegistry(_assignment, seed: 42);

            Assert.AreEqual("Seed 42: P1=Greedy P2=Cautious", registry.ToString());
        }

        [Test]
        public void NullAssignment_GivesAnEmptyRegistry()
        {
            var registry = new BotPersonalityRegistry(null, seed: 1);

            Assert.AreEqual(0, registry.Count);
            Assert.AreEqual("Seed 1:", registry.ToString());
        }
    }
}
