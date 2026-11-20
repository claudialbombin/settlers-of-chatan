using System.IO;
using Chatan.Core;
using Chatan.Dev;
using Chatan.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chatan.EditorTools
{
    /// <summary>
    /// Builds the HUD prefabs and the HUD sandbox scene from code, so the layout is
    /// reproducible and every Inspector reference is wired without dragging by hand.
    /// </summary>
    /// <remarks>
    /// Menu <c>Chatan → UI</c>. Run "Build HUD Prefabs" after changing this layout (it also
    /// builds the bot info card, see <see cref="PlayerInfoCardBuilder"/>), then
    /// "Set Up HUD Sandbox". Both can be run again: they overwrite the prefabs and do not
    /// duplicate objects in the scene. Layout is in pixels of a 1920×1080 reference screen.
    /// </remarks>
    public static class HudPrefabBuilder
    {
        internal const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string IconFolder = "Assets/_Project/Art/UI/Icons";
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string SlotPrefabPath = PrefabFolder + "/ResourceSlot.prefab";
        private const string HudPrefabPath = PrefabFolder + "/HUD.prefab";
        private const string SandboxScenePath = SceneFolder + "/Sandbox_Claudia.unity";

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        internal static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color ButtonColor = new Color(0.95f, 0.85f, 0.55f);
        internal static readonly Color DarkText = new Color(0.12f, 0.12f, 0.12f);

        private static Scene _buildScene;

        [MenuItem("Chatan/UI/Build HUD Prefabs")]
        public static void BuildPrefabs()
        {
            if (!TmpIsReady())
            {
                return;
            }

            ConfigureIcons();
            RunInBuildScene(() =>
            {
                GameObject slotPrefab = BuildSlotPrefab();
                BuildHudPrefab(slotPrefab);
                PlayerInfoCardBuilder.BuildCardPrefab();
            });
            Debug.Log($"[HUD] Prefabs saved in {PrefabFolder}.");
        }

        [MenuItem("Chatan/UI/Build Player Info Card Prefab")]
        public static void BuildInfoCardPrefab()
        {
            if (!TmpIsReady())
            {
                return;
            }

            RunInBuildScene(PlayerInfoCardBuilder.BuildCardPrefab);
            Debug.Log($"[HUD] Player info card saved in {PrefabFolder}.");
        }

        // Builds in a hidden preview scene so the scene open in the editor is not touched
        // (and not marked as modified).
        private static void RunInBuildScene(System.Action build)
        {
            EnsureFolder(PrefabFolder);
            _buildScene = EditorSceneManager.NewPreviewScene();
            try
            {
                build();
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(_buildScene);
                _buildScene = default;
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem("Chatan/UI/Set Up HUD Sandbox")]
        public static void SetUpSandbox()
        {
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            if (hudPrefab == null)
            {
                BuildPrefabs();
                hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
                if (hudPrefab == null)
                {
                    return;
                }
            }
            else if (!PlayerInfoCardBuilder.CardPrefabExists)
            {
                // Only the card is missing: do not rebuild the HUD, which scenes already use.
                BuildInfoCardPrefab();
                if (!PlayerInfoCardBuilder.CardPrefabExists)
                {
                    return;
                }
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder(SceneFolder);
            var scene = File.Exists(SandboxScenePath)
                ? EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Canvas canvas = FindOrCreateCanvas();
            EnsureEventSystem();

            if (Object.FindAnyObjectByType<HUDController>() == null)
            {
                var hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, canvas.transform);
                Stretch((RectTransform)hud.transform);
            }

            PlayerInfoPanel infoPanel = PlayerInfoCardBuilder.EnsurePanel(canvas);

            var driver = Object.FindAnyObjectByType<HudSandboxDriver>();
            if (driver == null)
            {
                driver = new GameObject("SandboxDriver").AddComponent<HudSandboxDriver>();
            }

            PlayerInfoCardBuilder.WireDriver(driver, infoPanel);

            EditorSceneManager.SaveScene(scene, SandboxScenePath);
            Debug.Log($"[HUD] Sandbox ready: {SandboxScenePath}. Press Play.");
        }

        // ---------------------------------------------------------------- prefabs

        private static GameObject BuildSlotPrefab()
        {
            GameObject slot = NewUi("ResourceSlot", null);
            SetRect(slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 170f));
            var layout = slot.AddComponent<LayoutElement>();
            layout.preferredWidth = 130f;
            layout.preferredHeight = 170f;

            GameObject icon = NewUi("Icon", slot.transform);
            SetRect(icon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(96f, 96f));
            var iconImage = icon.AddComponent<Image>();
            iconImage.preserveAspect = true;

            TMP_Text amount = NewText("Amount", slot.transform, "0", 44f, Color.white, FontStyles.Bold);
            SetRect(amount.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(120f, 50f));

            TMP_Text label = NewText("Name", slot.transform, "recurso", 20f, Color.white, FontStyles.Normal);
            SetRect(label.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(120f, 26f));

            var view = slot.AddComponent<ResourceSlotView>();
            var so = new SerializedObject(view);
            so.FindProperty("icon").objectReferenceValue = iconImage;
            so.FindProperty("amountText").objectReferenceValue = amount;
            so.FindProperty("nameText").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(slot, SlotPrefabPath);
            Object.DestroyImmediate(slot);
            return prefab;
        }

        private static void BuildHudPrefab(GameObject slotPrefab)
        {
            GameObject hud = NewUi("HUD", null);
            Stretch((RectTransform)hud.transform);

            ResourcePanel resourcePanel = BuildResourcePanel(hud.transform, slotPrefab);
            DiceView diceView = BuildDice(hud.transform);
            Button rollButton = NewButton("RollButton", hud.transform, "Tirar dados");
            SetRect(rollButton.gameObject, Vector2.one, Vector2.one, new Vector2(-24f, -236f), new Vector2(300f, 70f));
            TurnIndicatorView turn = BuildTurnIndicator(hud.transform);

            TMP_Text points = NewText("Points", hud.transform, "0 PV", 40f, Color.white, FontStyles.Bold);
            points.alignment = TextAlignmentOptions.Right;
            SetRect(points.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 108f), new Vector2(340f, 60f));

            Button endTurnButton = NewButton("EndTurnButton", hud.transform, "Terminar turno");
            SetRect(endTurnButton.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(340f, 70f));

            var controller = hud.AddComponent<HUDController>();
            var so = new SerializedObject(controller);
            so.FindProperty("resourcePanel").objectReferenceValue = resourcePanel;
            so.FindProperty("diceView").objectReferenceValue = diceView;
            so.FindProperty("turnIndicator").objectReferenceValue = turn;
            so.FindProperty("pointsText").objectReferenceValue = points;
            so.FindProperty("rollButton").objectReferenceValue = rollButton;
            so.FindProperty("endTurnButton").objectReferenceValue = endTurnButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(hud, HudPrefabPath);
            Object.DestroyImmediate(hud);
        }

        private static ResourcePanel BuildResourcePanel(Transform parent, GameObject slotPrefab)
        {
            GameObject panel = NewUi("ResourcePanel", parent);
            SetRect(panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(760f, 200f));
            panel.AddComponent<Image>().color = PanelColor;

            var row = panel.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(16, 16, 15, 15);
            row.spacing = 12f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var slots = new ResourceSlotView[ResourceBundle.AllTypes.Count];
            for (int i = 0; i < slots.Length; i++)
            {
                ResourceType type = ResourceBundle.AllTypes[i];
                var slotObject = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, panel.transform);
                slotObject.name = $"Slot_{type}";

                var view = slotObject.GetComponent<ResourceSlotView>();
                view.Icon.sprite = LoadIcon(type);

                var so = new SerializedObject(view);
                so.FindProperty("type").enumValueIndex = (int)type;
                so.ApplyModifiedPropertiesWithoutUndo();

                // The prefab's placeholder text is replaced in Awake; show the real name in edit mode too.
                slotObject.transform.Find("Name").GetComponent<TMP_Text>().text = ResourceNames.ToSpanish(type);
                slots[i] = view;
            }

            var resourcePanel = panel.AddComponent<ResourcePanel>();
            var panelSo = new SerializedObject(resourcePanel);
            SerializedProperty array = panelSo.FindProperty("slots");
            array.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }

            panelSo.ApplyModifiedPropertiesWithoutUndo();
            return resourcePanel;
        }

        private static DiceView BuildDice(Transform parent)
        {
            GameObject dice = NewUi("DiceView", parent);
            SetRect(dice, Vector2.one, Vector2.one, new Vector2(-24f, -24f), new Vector2(300f, 196f));
            dice.AddComponent<Image>().color = PanelColor;

            TMP_Text dieA = NewDie("DieA", dice.transform, new Vector2(-70f, -16f));
            TMP_Text dieB = NewDie("DieB", dice.transform, new Vector2(70f, -16f));

            TMP_Text total = NewText("Total", dice.transform, "", 40f, Color.white, FontStyles.Bold);
            SetRect(total.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(260f, 50f));

            var view = dice.AddComponent<DiceView>();
            var so = new SerializedObject(view);
            so.FindProperty("dieA").objectReferenceValue = dieA;
            so.FindProperty("dieB").objectReferenceValue = dieB;
            so.FindProperty("total").objectReferenceValue = total;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static TMP_Text NewDie(string name, Transform parent, Vector2 position)
        {
            GameObject box = NewUi(name + "Box", parent);
            SetRect(box, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, new Vector2(110f, 110f));
            box.AddComponent<Image>().color = Color.white;

            TMP_Text text = NewText(name, box.transform, HudText.NoDie, 72f, DarkText, FontStyles.Bold);
            Stretch((RectTransform)text.transform);
            return text;
        }

        private static TurnIndicatorView BuildTurnIndicator(Transform parent)
        {
            GameObject turn = NewUi("TurnIndicator", parent);
            SetRect(turn, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(680f, 120f));
            turn.AddComponent<Image>().color = PanelColor;

            GameObject stripe = NewUi("ColorStripe", turn.transform);
            var stripeRect = (RectTransform)stripe.transform;
            stripeRect.anchorMin = Vector2.zero;
            stripeRect.anchorMax = new Vector2(0f, 1f);
            stripeRect.pivot = new Vector2(0f, 0.5f);
            stripeRect.anchoredPosition = Vector2.zero;
            stripeRect.sizeDelta = new Vector2(18f, 0f);
            var stripeImage = stripe.AddComponent<Image>();

            TMP_Text playerName = NewText("PlayerName", turn.transform, "Tu turno", 44f, Color.white, FontStyles.Bold);
            SetRect(playerName.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(9f, -8f), new Vector2(640f, 56f));

            TMP_Text phase = NewText("Phase", turn.transform, HudText.Phase(GamePhase.Roll), 28f, Color.white, FontStyles.Normal);
            SetRect(phase.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(9f, 10f), new Vector2(640f, 44f));

            var view = turn.AddComponent<TurnIndicatorView>();
            var so = new SerializedObject(view);
            so.FindProperty("playerName").objectReferenceValue = playerName;
            so.FindProperty("phaseText").objectReferenceValue = phase;
            so.FindProperty("colorStripe").objectReferenceValue = stripeImage;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // ---------------------------------------------------------------- scene

        private static Canvas FindOrCreateCanvas()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }

            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        // The project uses the new Input System only: the old StandaloneInputModule
        // would leave the buttons dead.
        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            }

            var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                Object.DestroyImmediate(oldModule);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        // ---------------------------------------------------------------- helpers

        internal static bool TmpIsReady()
        {
            // TMP_Settings throws if its asset does not exist yet, so look for the asset first.
            if (Resources.Load<TMP_Settings>("TMP Settings") != null && TMP_Settings.defaultFontAsset != null)
            {
                return true;
            }

            EditorUtility.DisplayDialog(
                "TextMeshPro",
                "Import the TMP Essential Resources first (Window → TextMeshPro → Import TMP Essential Resources), then run this again.",
                "OK");
            return false;
        }

        private static void ConfigureIcons()
        {
            foreach (ResourceType type in ResourceBundle.AllTypes)
            {
                string path = IconPath(type);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer
                    && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }
        }

        private static Sprite LoadIcon(ResourceType type)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath(type));
            if (sprite == null)
            {
                Debug.LogWarning($"[HUD] No sprite at {IconPath(type)}.");
            }

            return sprite;
        }

        private static string IconPath(ResourceType type) => $"{IconFolder}/{type.ToString().ToLowerInvariant()}.png";

        internal static GameObject NewUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            else if (_buildScene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(go, _buildScene);
            }

            return go;
        }

        internal static TMP_Text NewText(string name, Transform parent, string text, float size, Color color, FontStyles style)
        {
            GameObject go = NewUi(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button NewButton(string name, Transform parent, string label)
        {
            GameObject go = NewUi(name, parent);
            var image = go.AddComponent<Image>();
            image.color = ButtonColor;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text text = NewText("Label", go.transform, label, 34f, DarkText, FontStyles.Bold);
            Stretch((RectTransform)text.transform);
            return button;
        }

        // Anchors and pivot at the same point, so the block stays glued to that corner
        // whatever the screen size.
        internal static void SetRect(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
