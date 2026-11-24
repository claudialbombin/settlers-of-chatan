using System.Collections.Generic;
using Chatan.AI;
using Chatan.Core;
using Chatan.Dev;
using NUnit.Framework;
using UnityEngine;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="RelationshipBook"/> with bots 1, 2 and 3.</summary>
    public class RelationshipBookTests
    {
        private readonly List<PersonalityProfile> _profiles = new List<PersonalityProfile>();
        private RelationshipBook _book;
        private GameState _state;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            var assignment = new Dictionary<int, PersonalityProfile>();
            for (int bot = 1; bot <= 3; bot++)
            {
                var profile = ScriptableObject.CreateInstance<PersonalityProfile>();
                profile.grudge = 0.5f;
                _profiles.Add(profile);
                assignment[bot] = profile;
            }

            _book = new RelationshipBook(new BotPersonalityRegistry(assignment, 42));
            _state = FakeGameStateFactory.Create();
        }

        [TearDown]
        public void TearDown()
        {
            _book.Unsubscribe();
            GameEvents.ClearAll();
            foreach (PersonalityProfile profile in _profiles)
            {
                Object.DestroyImmediate(profile);
            }

            _profiles.Clear();
        }

        [Test]
        public void HasOneMemoryPerBot()
        {
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, _book.BotIds);
            Assert.AreEqual(2, _book.For(2).OwnerId);
            Assert.IsNull(_book.For(0), "the human has no memory");
        }

        [Test]
        public void Robbery_IsRememberedByTheVictimOnly()
        {
            _book.Subscribe();

            GameEvents.RaisePlayerRobbed(0, 1);

            Assert.Less(_book.For(1).Get(0), 0f);
            Assert.AreEqual(0f, _book.For(2).Get(0));
        }

        [Test]
        public void WithoutSubscribe_EventsAreIgnored()
        {
            GameEvents.RaisePlayerRobbed(0, 1);

            Assert.AreEqual(0f, _book.For(1).Get(0));
        }

        [Test]
        public void SubscribeTwice_CountsEventsOnce()
        {
            _book.Subscribe();
            _book.Subscribe();

            GameEvents.RaisePlayerRobbed(0, 1);

            Assert.AreEqual(1, _book.For(1).History.Count);
        }

        [Test]
        public void Decay_OnlyWhenTheRoundStartsAgain()
        {
            _book.Subscribe();
            GameEvents.RaisePlayerRobbed(0, 1);
            float before = _book.For(1).Get(0);

            GameEvents.RaiseTurnStarted(_state.Players[2]);
            Assert.AreEqual(before, _book.For(1).Get(0), "a bot turn does not decay");

            GameEvents.RaiseTurnStarted(_state.Players[0]);
            Assert.Greater(_book.For(1).Get(0), before, "back to seat 0 decays");
        }

        [Test]
        public void TradeCompleted_BothSidesLikeEachOtherMore()
        {
            _book.OnTradeCompleted(1, 2);

            Assert.Greater(_book.For(1).Get(2), 0f);
            Assert.Greater(_book.For(2).Get(1), 0f);
        }

        [Test]
        public void TradeWithTheHuman_OnlyTheBotRemembers()
        {
            Assert.DoesNotThrow(() => _book.OnTradeCompleted(0, 3));

            Assert.Greater(_book.For(3).Get(0), 0f);
        }

        [Test]
        public void OfferRejected_LowersTheBotsAffinity()
        {
            _book.OnOfferRejected(2, 0);

            Assert.Less(_book.For(2).Get(0), 0f);
        }
    }
}
