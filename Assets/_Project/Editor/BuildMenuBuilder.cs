using System.IO;
using Chatan.AI;
using Chatan.Board;
using Chatan.Core;
using Chatan.Dev;
using Chatan.Players;
using Chatan.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using H = Chatan.EditorTools.HudPrefabBuilder;

namespace Chatan.EditorTools
{
    /// <summary>
    /// Builds the build menu and the two dialogs (discard, robber victim), and adds the
    /// human controller with a fake board to <c>Sandbox_Claudia</c>.
    /// </summary>
    /// <remarks>
    /// Menu <c>Chatan → UI</c>. "Build Build Menu Prefabs" overwrites the three prefabs;
    /// "Set Up Build Sandbox" can be run again without duplicating objects. Layout in
    /// pixels of the 1920×1080 reference screen.
    /// </remarks>
    public static class BuildMenuBuilder
    {
        private const string BuildMenuPath = H.PrefabFolder + "/BuildMenu.prefab";
        private const string DiscardDialogPath = H.PrefabFolder + "/DiscardDialog.prefab";
        private const string RobberDialogPath = H.PrefabFolder + "/RobberVictimDialog.prefab";
        private const string BuildIconFolder = "Assets/_Project/Art/UI/Build";
        private const string ResourceIconFolder = "Assets/_Project/Art/UI/Icons";
        private const string CatalogPath = "Assets/_Project/Data/Personalities/PersonalityCatalog.asset";

        private static readonly Vector2 BottomLeft = Vector2.zero;
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 ButtonSize = new Vector2(156f, 128f);
        private static readonly Color Overlay = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color DialogColor = new Color(0.16f, 0.16f, 0.2f, 0.97f);
        private static readonly Color TooltipColor = new Color(0f, 0f, 0f, 0.85f);

        [MenuItem("Chatan/UI/Build Build Menu Prefabs")]
        public static void BuildPrefabs()
        {
            if (!H.TmpIsReady())
            {
                return;
            }

            H.RunInBuildScene(() =>
            {
                BuildMenuPrefab();
                BuildDiscardDialog();
                BuildRobberDialog();
            });
            Debug.Log($"[Build] Prefabs saved in {H.PrefabFolder}.");
        }

        [MenuItem("Chatan/UI/Set Up Build Sandbox")]
        public static void SetUpSandbox()
        {
            if (!File.Exists(H.SandboxScenePath))
            {
                H.SetUpSandbox();
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(BuildMenuPath) == null
                || AssetDatabase.LoadAssetAtPath<GameObject>(DiscardDialogPath) == null
                || AssetDatabase.LoadAssetAtPath<GameObject>(RobberDialogPath) == null)
            {
                BuildPrefabs();
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(H.SandboxScenePath, OpenSceneMode.Single);
            Canvas canvas = H.FindOrCreateCanvas();
            H.EnsureEventSystem();

            EnsureInstance<BuildMenu>(BuildMenuPath, canvas.transform);
            DiscardDialog discard = EnsureInstance<DiscardDialog>(DiscardDialogPath, canvas.transform);
            RobberVictimDialog robber = EnsureInstance<RobberVictimDialog>(RobberDialogPath, canvas.transform);

            BoardInput input = FindOrCreate<BoardInput>("BoardInput");
            var inputSo = new SerializedObject(input);
            inputSo.FindProperty("cam").objectReferenceValue = Camera.main;
            inputSo.FindProperty("targetsMask").intValue = LayerMask.GetMask(BoardClickTarget.LayerName);
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            FakeBoardTargets board = FindOrCreate<FakeBoardTargets>("FakeBoard");
            board.transform.position = new Vector3(0f, 0.6f, 0f);

            HumanPlayerController human = FindOrCreate<HumanPlayerController>("Human");
            var humanSo = new SerializedObject(human);
            humanSo.FindProperty("playerId").intValue = FakeGameStateFactory.HumanId;
            humanSo.FindProperty("boardInput").objectReferenceValue = input;
            humanSo.FindProperty("discardDialog").objectReferenceValue = discard;
            humanSo.FindProperty("robberDialog").objectReferenceValue = robber;
            humanSo.ApplyModifiedPropertiesWithoutUndo();

            BuildSandboxDriver driver = FindOrCreate<BuildSandboxDriver>("BuildSandboxDriver");
            var driverSo = new SerializedObject(driver);
            driverSo.FindProperty("human").objectReferenceValue = human;
            driverSo.FindProperty("board").objectReferenceValue = board;
            driverSo.FindProperty("catalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PersonalityCatalog>(CatalogPath);
            driverSo.FindProperty("infoPanel").objectReferenceValue = Object.FindAnyObjectByType<PlayerInfoPanel>();
            driverSo.ApplyModifiedPropertiesWithoutUndo();

            // Only one fake turn manager may answer the HUD buttons.
            var hudDriver = Object.FindAnyObjectByType<HudSandboxDriver>(FindObjectsInactive.Include);
            if (hudDriver != null)
            {
                hudDriver.enabled = false;
            }

            EditorSceneManager.SaveScene(scene, H.SandboxScenePath);
            Debug.Log($"[Build] Build sandbox ready in {H.SandboxScenePath}. Press Play.");
        }

        // ---------------------------------------------------------------- build menu

        private static void BuildMenuPrefab()
        {
            GameObject menu = H.NewUi("BuildMenu", null);
            H.SetRect(menu, BottomLeft, BottomLeft, new Vector2(24f, 24f), new Vector2(2f * ButtonSize.x + 36f, 2f * ButtonSize.y + 36f));
            menu.AddComponent<Image>().color = H.PanelColor;

            var grid = menu.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.spacing = new Vector2(12f, 12f);
            grid.cellSize = ButtonSize;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            var buttons = new[]
            {
                NewBuildButton(menu.transform, BuildKind.Road),
                NewBuildButton(menu.transform, BuildKind.Settlement),
                NewBuildButton(menu.transform, BuildKind.City),
                NewBuildButton(menu.transform, BuildKind.DevCard),
            };

            var view = menu.AddComponent<BuildMenu>();
            var so = new SerializedObject(view);
            so.FindProperty("humanPlayerId").intValue = FakeGameStateFactory.HumanId;
            SerializedProperty array = so.FindProperty("buttons");
            array.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(menu, BuildMenuPath);
            Object.DestroyImmediate(menu);
        }

        private static BuildButton NewBuildButton(Transform parent, BuildKind kind)
        {
            GameObject go = H.NewUi($"{kind}Button", parent);
            var image = go.AddComponent<Image>();
            image.color = H.ButtonColor;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
            button.colors = colors;

            GameObject icon = H.NewUi("Icon", go.transform);
            H.SetRect(icon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(72f, 72f));
            var iconImage = icon.AddComponent<Image>();
            iconImage.sprite = LoadSprite($"{BuildIconFolder}/{kind.ToString().ToLowerInvariant()}.png");
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            TMP_Text label = H.NewText("Label", go.transform, BuildChoices.Label(kind), 20f, H.DarkText, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.Normal;
            H.SetRect(label.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(148f, 42f));

            // Above the button; not a raycast target, or hovering it would hide it again.
            GameObject tooltip = H.NewUi("CostTooltip", go.transform);
            H.SetRect(tooltip, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(-ButtonSize.x / 2f, 8f), new Vector2(330f, 44f));
            var tooltipImage = tooltip.AddComponent<Image>();
            tooltipImage.color = TooltipColor;
            tooltipImage.raycastTarget = false;
            TMP_Text cost = H.NewText("Cost", tooltip.transform, BuildChoices.CostText(kind), 20f, Color.white, FontStyles.Normal);
            H.Stretch((RectTransform)cost.transform);
            tooltip.SetActive(false);

            var view = go.AddComponent<BuildButton>();
            var so = new SerializedObject(view);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("costTooltip").objectReferenceValue = tooltip;
            so.FindProperty("costText").objectReferenceValue = cost;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // ---------------------------------------------------------------- dialogs

        private static void BuildDiscardDialog()
        {
            GameObject root = NewOverlay("DiscardDialog");
            GameObject box = NewBox(root.transform, new Vector2(760f, 560f));

            TMP_Text title = H.NewText("Title", box.transform, "Ha salido un 7", 32f, Color.white, FontStyles.Bold);
            H.SetRect(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(720f, 50f));

            int count = ResourceBundle.AllTypes.Count;
            var amounts = new TMP_Text[count];
            var minus = new Button[count];
            var plus = new Button[count];
            for (int i = 0; i < count; i++)
            {
                ResourceType type = ResourceBundle.AllTypes[i];
                GameObject row = H.NewUi($"Row_{type}", box.transform);
                H.SetRect(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f - i * 72f), new Vector2(620f, 64f));

                GameObject icon = H.NewUi("Icon", row.transform);
                H.SetRect(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(56f, 56f));
                var iconImage = icon.AddComponent<Image>();
                iconImage.sprite = LoadSprite($"{ResourceIconFolder}/{type.ToString().ToLowerInvariant()}.png");
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                TMP_Text name = H.NewText("Name", row.transform, ResourceNames.ToSpanish(type), 28f, Color.white, FontStyles.Normal);
                name.alignment = TextAlignmentOptions.Left;
                H.SetRect(name.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(72f, 0f), new Vector2(200f, 56f));

                minus[i] = H.NewButton("Minus", row.transform, "−");
                H.SetRect(minus[i].gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-236f, 0f), new Vector2(64f, 56f));

                amounts[i] = H.NewText("Amount", row.transform, "0 / 0", 28f, Color.white, FontStyles.Bold);
                H.SetRect(amounts[i].gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-84f, 0f), new Vector2(140f, 56f));

                plus[i] = H.NewButton("Plus", row.transform, "+");
                H.SetRect(plus[i].gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(64f, 56f));
            }

            Button confirm = H.NewButton("ConfirmButton", box.transform, "Confirmar");
            H.SetRect(confirm.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(300f, 64f));

            var view = root.AddComponent<DiscardDialog>();
            var so = new SerializedObject(view);
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("confirmButton").objectReferenceValue = confirm;
            SetArray(so.FindProperty("amountTexts"), amounts);
            SetArray(so.FindProperty("minusButtons"), minus);
            SetArray(so.FindProperty("plusButtons"), plus);
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, DiscardDialogPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildRobberDialog()
        {
            GameObject root = NewOverlay("RobberVictimDialog");
            GameObject box = NewBox(root.transform, new Vector2(560f, 380f));

            TMP_Text title = H.NewText("Title", box.transform, "¿A quién le robas una carta?", 30f, Color.white, FontStyles.Bold);
            H.SetRect(title.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(520f, 50f));

            GameObject options = H.NewUi("Options", box.transform);
            H.SetRect(options, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(420f, 260f));
            var column = options.AddComponent<VerticalLayoutGroup>();
            column.spacing = 12f;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = false;
            column.childControlHeight = false;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;

            Button template = H.NewButton("OptionTemplate", options.transform, "Jugador");
            H.SetRect(template.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(400f, 64f));
            template.gameObject.SetActive(false);

            var view = root.AddComponent<RobberVictimDialog>();
            var so = new SerializedObject(view);
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("optionsContainer").objectReferenceValue = options.transform;
            so.FindProperty("optionTemplate").objectReferenceValue = template;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, RobberDialogPath);
            Object.DestroyImmediate(root);
        }

        // Full-screen dark layer: it also swallows clicks, so nothing behind can be pressed.
        private static GameObject NewOverlay(string name)
        {
            GameObject root = H.NewUi(name, null);
            H.Stretch((RectTransform)root.transform);
            var dim = root.AddComponent<Image>();
            dim.color = Overlay;
            dim.raycastTarget = true;
            return root;
        }

        private static GameObject NewBox(Transform parent, Vector2 size)
        {
            GameObject box = H.NewUi("Box", parent);
            H.SetRect(box, Center, Center, Vector2.zero, size);
            box.AddComponent<Image>().color = DialogColor;
            return box;
        }

        // ---------------------------------------------------------------- scene helpers

        private static T EnsureInstance<T>(string prefabPath, Transform parent) where T : Component
        {
            var existing = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.SetAsLastSibling();
            return instance.GetComponent<T>();
        }

        private static T FindOrCreate<T>(string name) where T : Component
        {
            var existing = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            return existing != null ? existing : new GameObject(name).AddComponent<T>();
        }

        private static void SetArray(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static Sprite LoadSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer
                && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
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
                Debug.LogWarning($"[Build] No sprite at {path}.");
            }

            return sprite;
        }
    }
}
