using System.Collections.Generic;
using Chatan.AI;
using NUnit.Framework;
using UnityEngine;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="RelationshipMemory"/>. Bot 1 remembers what player 0 does.</summary>
    public class RelationshipMemoryTests
    {
        private readonly List<PersonalityProfile> _profiles = new List<PersonalityProfile>();

        [TearDown]
        public void TearDown()
        {
            foreach (PersonalityProfile profile in _profiles)
            {
                Object.DestroyImmediate(profile);
            }

            _profiles.Clear();
        }

        private RelationshipMemory MemoryWithGrudge(float grudge)
        {
            var profile = ScriptableObject.CreateInstance<PersonalityProfile>();
            profile.grudge = grudge;
            _profiles.Add(profile);
            return new RelationshipMemory(1, profile);
        }

        [Test]
        public void StartsAtZeroWithEverybody()
        {
            var memory = MemoryWithGrudge(0.5f);

            Assert.AreEqual(0f, memory.Get(0));
            Assert.AreEqual(0f, memory.Get(3));
            Assert.AreEqual("te es indiferente", memory.DescribeFor(0));
        }

        [Test]
        public void Robbery_LowersAffinityButNeverBelowMinusOne()
        {
            var memory = MemoryWithGrudge(1f);

            memory.Record(0, RelationshipEventType.RobbedMe);
            Assert.Less(memory.Get(0), 0f);

            for (int i = 0; i < 10; i++)
            {
                memory.Record(0, RelationshipEventType.RobbedMe);
            }

            Assert.AreEqual(-1f, memory.Get(0));
        }

        [Test]
        public void Trades_NeverGoAbovePlusOne()
        {
            var memory = MemoryWithGrudge(0f);

            for (int i = 0; i < 20; i++)
            {
                memory.Record(0, RelationshipEventType.TradeCompleted);
            }

            Assert.AreEqual(1f, memory.Get(0));
            Assert.AreEqual("es tu aliado (lo último: cerrasteis un trato)", memory.DescribeFor(0));
        }

        [Test]
        public void MoreGrudge_RobberyHurtsMore()
        {
            var soft = MemoryWithGrudge(0f);
            var hard = MemoryWithGrudge(1f);

            soft.Record(0, RelationshipEventType.RobbedMe);
            hard.Record(0, RelationshipEventType.RobbedMe);

            Assert.Less(hard.Get(0), soft.Get(0));
            Assert.AreEqual(-0.35f * 0.6f, soft.Get(0), 1e-5f);
            Assert.AreEqual(-0.35f * 1.6f, hard.Get(0), 1e-5f);
        }

        [Test]
        public void MoreGrudge_HarderToWinOver()
        {
            var soft = MemoryWithGrudge(0f);
            var hard = MemoryWithGrudge(1f);

            soft.Record(0, RelationshipEventType.TradeCompleted);
            hard.Record(0, RelationshipEventType.TradeCompleted);

            Assert.Greater(soft.Get(0), hard.Get(0));
        }

        [Test]
        public void Decay_MovesTowardsZeroAndSlowerWithGrudge()
        {
            var soft = MemoryWithGrudge(0f);
            var hard = MemoryWithGrudge(1f);
            soft.Record(0, RelationshipEventType.RobbedMe);
            hard.Record(0, RelationshipEventType.RobbedMe);
            float softBefore = soft.Get(0);
            float hardBefore = hard.Get(0);

            soft.DecayOneRound();
            hard.DecayOneRound();

            Assert.Greater(soft.Get(0), softBefore, "closer to 0");
            Assert.Less(soft.Get(0), 0f, "but still negative");
            Assert.AreEqual(0.85f, soft.Get(0) / softBefore, 1e-4f);
            Assert.AreEqual(0.99f, hard.Get(0) / hardBefore, 1e-4f);
        }

        [Test]
        public void Decay_EventuallyForgets()
        {
            var memory = MemoryWithGrudge(0f);
            memory.Record(0, RelationshipEventType.TradeRejectedByThem);

            for (int i = 0; i < 50; i++)
            {
                memory.DecayOneRound();
            }

            Assert.AreEqual(0f, memory.Get(0));
        }

        [Test]
        public void RecordAboutItself_DoesNothing()
        {
            var memory = MemoryWithGrudge(0.5f);
            int calls = 0;
            memory.Changed += (_, __) => calls++;

            memory.Record(1, RelationshipEventType.RobbedMe);

            Assert.AreEqual(0f, memory.Get(1));
            Assert.AreEqual(0, calls);
            Assert.IsEmpty(memory.History);
        }

        [Test]
        public void Changed_ReportsTheNewValue()
        {
            var memory = MemoryWithGrudge(0.5f);
            int other = -1;
            float value = 0f;
            memory.Changed += (o, v) => { other = o; value = v; };

            memory.Record(2, RelationshipEventType.HelpedAgainstLeader);

            Assert.AreEqual(2, other);
            Assert.AreEqual(memory.Get(2), value);
            Assert.Greater(value, 0f);
        }

        [Test]
        public void Changed_AlsoFiresOnDecay()
        {
            var memory = MemoryWithGrudge(0.5f);
            memory.Record(0, RelationshipEventType.RobbedMe);
            float last = 0f;
            memory.Changed += (_, v) => last = v;

            memory.DecayOneRound();

            Assert.AreEqual(memory.Get(0), last);
        }

        [Test]
        public void WithoutPersonality_UsesTheDefaultGrudge()
        {
            var memory = new RelationshipMemory(1, null);

            memory.Record(0, RelationshipEventType.RobbedMe);

            float expected = RelationshipMemory.ScaledDelta(RelationshipEventType.RobbedMe, RelationshipMemory.DefaultGrudge);
            Assert.AreEqual(expected, memory.Get(0), 1e-5f);
        }

        [Test]
        public void DescribeFor_MentionsTheLastEvent()
        {
            var memory = MemoryWithGrudge(1f);

            memory.Record(0, RelationshipEventType.RobbedMe);

            Assert.AreEqual("te cae mal (lo último: te robó con el ladrón)", memory.DescribeFor(0));
        }

        [TestCase(-0.9f, "le odias")]
        [TestCase(-0.3f, "te cae mal")]
        [TestCase(0f, "te es indiferente")]
        [TestCase(0.4f, "te cae bien")]
        [TestCase(0.8f, "es tu aliado")]
        public void Feeling_MatchesTheAffinity(float affinity, string expected)
        {
            Assert.AreEqual(expected, RelationshipMemory.Feeling(affinity));
        }

        [Test]
        public void History_KeepsOneLinePerEvent()
        {
            var memory = MemoryWithGrudge(0.5f);

            memory.Record(0, RelationshipEventType.TradeCompleted);
            memory.Record(2, RelationshipEventType.RobbedMe);

            Assert.AreEqual(2, memory.History.Count);
            StringAssert.StartsWith("TradeCompleted con J0", memory.History[0]);
            StringAssert.StartsWith("RobbedMe con J2", memory.History[1]);
        }
    }
}
