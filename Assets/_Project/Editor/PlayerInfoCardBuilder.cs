using Chatan.AI;
using Chatan.Dev;
using Chatan.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using H = Chatan.EditorTools.HudPrefabBuilder;

namespace Chatan.EditorTools
{
    /// <summary>
    /// Builds the bot info card prefab and adds the info panel to the sandbox. Called by
    /// the <c>Chatan → UI</c> menu items of <see cref="HudPrefabBuilder"/>.
    /// </summary>
    /// <remarks>Layout in pixels of the 1920×1080 reference screen; the card is 340×150.</remarks>
    internal static class PlayerInfoCardBuilder
    {
        private const string CardPrefabPath = H.PrefabFolder + "/PlayerInfoCard.prefab";
        private const string DebugSettingsPath = "Assets/_Project/Data/Settings/DebugSettings.asset";
        private const string CatalogPath = "Assets/_Project/Data/Personalities/PersonalityCatalog.asset";
        private const string UnknownPortraitPath = "Assets/_Project/Art/UI/unknown_portrait.png";
        private const string BarSpritePath = "Assets/_Project/Art/UI/bar.png";

        private static readonly Vector2 CardSize = new Vector2(340f, 150f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Color BadgeColor = new Color(0.95f, 0.85f, 0.55f);
        private static readonly Color WarningColor = new Color(0.85f, 0.15f, 0.15f);
        private static readonly Color BarBackground = new Color(1f, 1f, 1f, 0.15f);

        /// <summary>Whether the card prefab has been built.</summary>
        public static bool CardPrefabExists => AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath) != null;

        /// <summary>Builds <c>PlayerInfoCard.prefab</c>. Must run inside HudPrefabBuilder's build scene.</summary>
        public static void BuildCardPrefab()
        {
            Sprite bar = LoadSprite(BarSpritePath);

            GameObject card = H.NewUi("PlayerInfoCard", null);
            H.SetRect(card, TopLeft, TopLeft, Vector2.zero, CardSize);
            var layout = card.AddComponent<LayoutElement>();
            layout.preferredWidth = CardSize.x;
            layout.preferredHeight = CardSize.y;

            // The background receives the click, so it must stay a raycast target.
            var background = card.AddComponent<Image>();
            background.color = H.PanelColor;
            background.raycastTarget = true;

            GameObject stripe = H.NewUi("ColorStripe", card.transform);
            var stripeRect = (RectTransform)stripe.transform;
            stripeRect.anchorMin = Vector2.zero;
            stripeRect.anchorMax = new Vector2(0f, 1f);
            stripeRect.pivot = new Vector2(0f, 0.5f);
            stripeRect.anchoredPosition = Vector2.zero;
            stripeRect.sizeDelta = new Vector2(14f, 0f);
            var stripeImage = stripe.AddComponent<Image>();
            stripeImage.raycastTarget = false;

            GameObject portraitObject = H.NewUi("Portrait", card.transform);
            H.SetRect(portraitObject, TopLeft, TopLeft, new Vector2(24f, -10f), new Vector2(72f, 72f));
            var portrait = portraitObject.AddComponent<Image>();
            portrait.sprite = LoadSprite(UnknownPortraitPath);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;

            GameObject roadBadge = NewBadge("LongestRoadBadge", card.transform, "Carretera", new Vector2(16f, -90f));
            GameObject armyBadge = NewBadge("LargestArmyBadge", card.transform, "Ejército", new Vector2(16f, -118f));

            TMP_Text nameText = NewLabel("Name", card.transform, "Bot", 30f, FontStyles.Bold, new Vector2(108f, -8f), new Vector2(190f, 40f));
            TMP_Text personality = NewLabel("Personality", card.transform, PlayerCardText.UnknownPersonality, 22f, FontStyles.Italic, new Vector2(108f, -46f), new Vector2(220f, 30f));
            TMP_Text points = NewLabel("Points", card.transform, "0 PV", 22f, FontStyles.Bold, new Vector2(108f, -80f), new Vector2(90f, 28f));
            TMP_Text cards = NewLabel("Cards", card.transform, "0 cartas", 20f, FontStyles.Normal, new Vector2(200f, -80f), new Vector2(132f, 28f));
            TMP_Text devCards = NewLabel("DevCards", card.transform, "0 desarrollo", 20f, FontStyles.Normal, new Vector2(108f, -108f), new Vector2(110f, 28f));
            TMP_Text knights = NewLabel("Knights", card.transform, "0 caballeros", 20f, FontStyles.Normal, new Vector2(220f, -108f), new Vector2(112f, 28f));

            GameObject warning = H.NewUi("TooManyCardsWarning", card.transform);
            H.SetRect(warning, Vector2.one, Vector2.one, new Vector2(-8f, -8f), new Vector2(30f, 30f));
            var warningImage = warning.AddComponent<Image>();
            warningImage.color = WarningColor;
            warningImage.raycastTarget = false;
            TMP_Text mark = H.NewText("Mark", warning.transform, "!", 24f, Color.white, FontStyles.Bold);
            H.Stretch((RectTransform)mark.transform);
            warning.SetActive(false);

            Image affinityFill = NewAffinityBar(card.transform, bar);

            var view = card.AddComponent<PlayerInfoCard>();
            var so = new SerializedObject(view);
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("colorStripe").objectReferenceValue = stripeImage;
            so.FindProperty("portrait").objectReferenceValue = portrait;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("pointsText").objectReferenceValue = points;
            so.FindProperty("cardsText").objectReferenceValue = cards;
            so.FindProperty("devCardsText").objectReferenceValue = devCards;
            so.FindProperty("knightsText").objectReferenceValue = knights;
            so.FindProperty("longestRoadBadge").objectReferenceValue = roadBadge;
            so.FindProperty("largestArmyBadge").objectReferenceValue = armyBadge;
            so.FindProperty("personalityText").objectReferenceValue = personality;
            so.FindProperty("affinityBar").objectReferenceValue = affinityFill;
            so.FindProperty("tooManyCardsWarning").objectReferenceValue = warning;
            so.FindProperty("unknownPortrait").objectReferenceValue = portrait.sprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(card, CardPrefabPath);
            Object.DestroyImmediate(card);
        }

        /// <summary>
        /// Adds the info panel to the top-left corner of the canvas (once) and wires the
        /// card prefab and the debug settings.
        /// </summary>
        public static PlayerInfoPanel EnsurePanel(Canvas canvas)
        {
            var panel = Object.FindAnyObjectByType<PlayerInfoPanel>();
            if (panel != null)
            {
                return panel;
            }

            GameObject root = H.NewUi("PlayerInfoPanel", canvas.transform);
            H.SetRect(root, TopLeft, TopLeft, new Vector2(24f, -24f), new Vector2(CardSize.x, 0f));

            GameObject container = H.NewUi("Container", root.transform);
            H.SetRect(container, TopLeft, TopLeft, Vector2.zero, new Vector2(CardSize.x, 0f));
            var column = container.AddComponent<VerticalLayoutGroup>();
            column.spacing = 12f;
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = false;
            column.childControlHeight = false;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;
            var fitter = container.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            panel = root.AddComponent<PlayerInfoPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("cardPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath).GetComponent<PlayerInfoCard>();
            so.FindProperty("container").objectReferenceValue = container.transform;
            so.FindProperty("debugSettings").objectReferenceValue = LoadOrCreateDebugSettings();
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        /// <summary>Gives the sandbox driver the personality catalog and the info panel, if it has none.</summary>
        public static void WireDriver(HudSandboxDriver driver, PlayerInfoPanel panel)
        {
            var so = new SerializedObject(driver);
            SerializedProperty catalog = so.FindProperty("catalog");
            if (catalog.objectReferenceValue == null)
            {
                catalog.objectReferenceValue = AssetDatabase.LoadAssetAtPath<PersonalityCatalog>(CatalogPath);
            }

            SerializedProperty infoPanel = so.FindProperty("infoPanel");
            if (infoPanel.objectReferenceValue == null)
            {
                infoPanel.objectReferenceValue = panel;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Both switches stay off: this asset is committed and must not reveal secrets.
        private static DebugSettings LoadOrCreateDebugSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DebugSettings>(DebugSettingsPath);
            if (settings != null)
            {
                return settings;
            }

            H.EnsureFolder("Assets/_Project/Data/Settings");
            settings = ScriptableObject.CreateInstance<DebugSettings>();
            AssetDatabase.CreateAsset(settings, DebugSettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static TMP_Text NewLabel(string name, Transform parent, string text, float size, FontStyles style, Vector2 position, Vector2 boxSize)
        {
            TMP_Text label = H.NewText(name, parent, text, size, Color.white, style);
            label.alignment = TextAlignmentOptions.Left;
            label.overflowMode = TextOverflowModes.Ellipsis;
            H.SetRect(label.gameObject, TopLeft, TopLeft, position, boxSize);
            return label;
        }

        private static GameObject NewBadge(string name, Transform parent, string text, Vector2 position)
        {
            GameObject badge = H.NewUi(name, parent);
            H.SetRect(badge, TopLeft, TopLeft, position, new Vector2(84f, 22f));
            var image = badge.AddComponent<Image>();
            image.color = BadgeColor;
            image.raycastTarget = false;

            TMP_Text label = H.NewText("Label", badge.transform, text, 15f, H.DarkText, FontStyles.Bold);
            H.Stretch((RectTransform)label.transform);
            badge.SetActive(false);
            return badge;
        }

        private static Image NewAffinityBar(Transform parent, Sprite sprite)
        {
            GameObject back = H.NewUi("AffinityBar", parent);
            H.SetRect(back, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(108f, 8f), new Vector2(222f, 8f));
            var backImage = back.AddComponent<Image>();
            backImage.sprite = sprite;
            backImage.color = BarBackground;
            backImage.raycastTarget = false;

            GameObject fill = H.NewUi("Fill", back.transform);
            H.Stretch((RectTransform)fill.transform);
            var fillImage = fill.AddComponent<Image>();
            fillImage.sprite = sprite;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0.5f;
            fillImage.raycastTarget = false;
            return fillImage;
        }

        private static Sprite LoadSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[HUD] No sprite at {path}.");
            }

            return sprite;
        }
    }
}
