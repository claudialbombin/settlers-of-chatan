using System.Collections.Generic;
using System.Linq;
using Chatan.AI;
using Chatan.Core;
using Chatan.Dev;
using Chatan.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Chatan.Tests
{
    /// <summary>
    /// Checks that <see cref="PlayerInfoPanel.BindRelationships"/> moves the affinity bar of
    /// the real card prefab.
    /// </summary>
    public class PlayerInfoPanelAffinityTests
    {
        private const string CardPrefabPath = "Assets/_Project/Prefabs/UI/PlayerInfoCard.prefab";

        private readonly List<PersonalityProfile> _profiles = new List<PersonalityProfile>();
        private GameObject _root;
        private PlayerInfoPanel _panel;
        private RelationshipBook _book;
        private GameState _state;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Panel", typeof(RectTransform));
            _panel = _root.AddComponent<PlayerInfoPanel>();
            var so = new SerializedObject(_panel);
            so.FindProperty("cardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerInfoCard>(CardPrefabPath);
            so.FindProperty("humanPlayerId").intValue = FakeGameStateFactory.HumanId;
            so.ApplyModifiedPropertiesWithoutUndo();

            var assignment = new Dictionary<int, PersonalityProfile>();
            for (int bot = 1; bot <= 3; bot++)
            {
                var profile = ScriptableObject.CreateInstance<PersonalityProfile>();
                _profiles.Add(profile);
                assignment[bot] = profile;
            }

            _book = new RelationshipBook(new BotPersonalityRegistry(assignment, 7));
            _state = FakeGameStateFactory.Create();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            foreach (PersonalityProfile profile in _profiles)
            {
                Object.DestroyImmediate(profile);
            }

            _profiles.Clear();
        }

        private float Fill(int botId) =>
            _panel.GetCard(botId).GetComponentsInChildren<Image>(true).First(i => i.name == "Fill").fillAmount;

        [Test]
        public void Bar_FollowsTheBotsFeelingAboutTheHuman()
        {
            _panel.Refresh(_state);
            _panel.BindRelationships(_book);
            Assert.AreEqual(0.5f, Fill(1), 1e-5f, "neutral");

            _book.For(1).Record(FakeGameStateFactory.HumanId, RelationshipEventType.RobbedMe);

            Assert.Less(Fill(1), 0.5f);
            Assert.AreEqual(0.5f, Fill(2), 1e-5f, "other bots do not change");
        }

        [Test]
        public void Bar_IgnoresFeelingsAboutOtherBots()
        {
            _panel.Refresh(_state);
            _panel.BindRelationships(_book);

            _book.For(1).Record(2, RelationshipEventType.RobbedMe);

            Assert.AreEqual(0.5f, Fill(1), 1e-5f);
        }

        [Test]
        public void CardsCreatedLater_StartAtTheCurrentValue()
        {
            _book.For(3).Record(FakeGameStateFactory.HumanId, RelationshipEventType.TradeCompleted);

            _panel.BindRelationships(_book);
            _panel.Refresh(_state);

            Assert.Greater(Fill(3), 0.5f);
        }

        [Test]
        public void BindNull_StopsFollowing()
        {
            _panel.Refresh(_state);
            _panel.BindRelationships(_book);
            _panel.BindRelationships(null);

            _book.For(1).Record(FakeGameStateFactory.HumanId, RelationshipEventType.RobbedMe);

            Assert.AreEqual(0.5f, Fill(1), 1e-5f);
        }
    }
}
