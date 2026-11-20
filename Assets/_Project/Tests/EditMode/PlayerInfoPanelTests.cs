using System.Collections.Generic;
using System.Linq;
using Chatan.AI;
using Chatan.Core;
using Chatan.Dev;
using Chatan.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Chatan.Tests
{
    /// <summary>
    /// Tests <see cref="PlayerInfoPanel"/> with the real <c>PlayerInfoCard.prefab</c>, so a
    /// broken reference in the prefab also fails here.
    /// </summary>
    public class PlayerInfoPanelTests
    {
        private const string CardPrefabPath = "Assets/_Project/Prefabs/UI/PlayerInfoCard.prefab";

        private GameObject _root;
        private PlayerInfoPanel _panel;
        private DebugSettings _debug;
        private PersonalityProfile _profile;
        private GameState _state;

        [SetUp]
        public void SetUp()
        {
            var cardPrefab = AssetDatabase.LoadAssetAtPath<PlayerInfoCard>(CardPrefabPath);
            Assert.IsNotNull(cardPrefab, $"No card prefab at {CardPrefabPath}.");

            _debug = ScriptableObject.CreateInstance<DebugSettings>();
            _root = new GameObject("Panel", typeof(RectTransform));
            _panel = _root.AddComponent<PlayerInfoPanel>();

            var so = new SerializedObject(_panel);
            so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
            so.FindProperty("debugSettings").objectReferenceValue = _debug;
            so.FindProperty("humanPlayerId").intValue = FakeGameStateFactory.HumanId;
            so.ApplyModifiedPropertiesWithoutUndo();

            _profile = ScriptableObject.CreateInstance<PersonalityProfile>();
            _profile.displayName = "El Prudente";
            _state = FakeGameStateFactory.Create();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_debug);
            Object.DestroyImmediate(_profile);
        }

        private static string Text(PlayerInfoCard card, string childName) =>
            card.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == childName).text;

        private static GameObject Child(PlayerInfoCard card, string childName) =>
            card.GetComponentsInChildren<Transform>(true).First(t => t.name == childName).gameObject;

        [Test]
        public void Refresh_CreatesOneCardPerBot_AndNoneForTheHuman()
        {
            _panel.Refresh(_state);

            Assert.AreEqual(3, _panel.CardCount);
            Assert.IsNull(_panel.GetCard(FakeGameStateFactory.HumanId));
            Assert.AreEqual(2, _panel.GetCard(2).PlayerId);
        }

        [Test]
        public void Refresh_Twice_DoesNotDuplicateCards()
        {
            _panel.Refresh(_state);
            _panel.Refresh(_state);

            Assert.AreEqual(3, _panel.CardCount);
            Assert.AreEqual(3, _root.GetComponentsInChildren<PlayerInfoCard>(true).Length);
        }

        [Test]
        public void Card_ShowsPublicInfoOnly()
        {
            _state.Players[1].HiddenPoints = 2;

            _panel.Refresh(_state);
            PlayerInfoCard card = _panel.GetCard(1);

            Assert.AreEqual("Bot Azul", Text(card, "Name"));
            Assert.AreEqual("3 PV", Text(card, "Points"));
            Assert.AreEqual("7 cartas", Text(card, "Cards"));
            Assert.AreEqual(PlayerCardText.UnknownPersonality, Text(card, "Personality"));
        }

        [Test]
        public void Card_WithMoreThanSevenCards_ShowsTheWarning()
        {
            _panel.Refresh(_state);
            Assert.IsFalse(Child(_panel.GetCard(1), "TooManyCardsWarning").activeSelf);

            _state.Players[1].Resources.Ore += 1;
            _panel.Refresh(_state);
            Assert.IsTrue(Child(_panel.GetCard(1), "TooManyCardsWarning").activeSelf);
        }

        [Test]
        public void Badges_FollowLongestRoadAndLargestArmy()
        {
            _state.Players[2].HasLongestRoad = true;

            _panel.Refresh(_state);

            Assert.IsTrue(Child(_panel.GetCard(2), "LongestRoadBadge").activeSelf);
            Assert.IsFalse(Child(_panel.GetCard(2), "LargestArmyBadge").activeSelf);
        }

        [Test]
        public void OnlyTheCurrentBot_IsHighlighted()
        {
            _state.CurrentPlayerIndex = 2;

            _panel.Refresh(_state);

            Assert.IsFalse(_panel.GetCard(1).IsHighlighted);
            Assert.IsTrue(_panel.GetCard(2).IsHighlighted);
            Assert.IsFalse(_panel.GetCard(3).IsHighlighted);
        }

        [Test]
        public void Personality_IsShownOnlyWithTheDebugSwitch()
        {
            var assignment = new Dictionary<int, PersonalityProfile> { [1] = _profile };
            _panel.SetPersonalities(new BotPersonalityRegistry(assignment, seed: 1));

            _panel.Refresh(_state);
            Assert.AreEqual(PlayerCardText.UnknownPersonality, Text(_panel.GetCard(1), "Personality"));

            _debug.revealPersonalities = true;
            _panel.Refresh(_state);
            Assert.AreEqual("El Prudente", Text(_panel.GetCard(1), "Personality"));
        }

        [Test]
        public void ClickingACard_IsForwardedWithThePlayerId()
        {
            _panel.Refresh(_state);
            int clicked = -1;
            _panel.BotCardClicked += id => clicked = id;

            _panel.GetCard(3).OnPointerClick(null);

            Assert.AreEqual(3, clicked);
        }

        [Test]
        public void NewCard_StartsWithNeutralAffinity()
        {
            _panel.Refresh(_state);

            Image fill = Child(_panel.GetCard(1), "Fill").GetComponent<Image>();
            Assert.AreEqual(0.5f, fill.fillAmount, 1e-4f);
        }

        [Test]
        public void CommittedDebugSettings_RevealNothing()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DebugSettings>("Assets/_Project/Data/Settings/DebugSettings.asset");

            Assert.IsNotNull(settings);
            Assert.IsFalse(settings.revealPersonalities, "revealPersonalities must stay off in the repository.");
            Assert.IsFalse(settings.revealBotHands, "revealBotHands must stay off in the repository.");
        }
    }
}
