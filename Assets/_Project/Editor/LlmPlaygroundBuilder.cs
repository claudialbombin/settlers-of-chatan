using Chatan.AI;
using Chatan.Dev;
using Chatan.LLM;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using H = Chatan.EditorTools.HudPrefabBuilder;

namespace Chatan.EditorTools
{
    /// <summary>
    /// Creates the <see cref="LlmSettings"/> asset and the <c>Sandbox_LLM</c> scene with the
    /// <see cref="LlmPlayground"/>.
    /// </summary>
    /// <remarks>
    /// Menu <c>Chatan → LLM</c>. The settings asset is only created when missing, always
    /// with <c>useMock</c> on. The scene is rebuilt from scratch every time.
    /// </remarks>
    public static class LlmPlaygroundBuilder
    {
        /// <summary>Where the shared settings asset lives.</summary>
        public const string SettingsPath = "Assets/_Project/Data/Settings/LlmSettings.asset";

        /// <summary>Where the playground scene lives.</summary>
        public const string ScenePath = "Assets/_Project/Scenes/Sandbox_LLM.unity";

        private const string DefaultPersonalityPath = "Assets/_Project/Data/Personalities/Personality_Aggressive.asset";

        private static readonly Color Background = new Color(0.12f, 0.12f, 0.15f, 1f);
        private static readonly Color FieldColor = new Color(0.95f, 0.95f, 0.95f, 1f);

        [MenuItem("Chatan/LLM/Create LLM Settings")]
        public static LlmSettings CreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LlmSettings>(SettingsPath);
            if (settings != null)
            {
                return settings;
            }

            H.EnsureFolder("Assets/_Project/Data/Settings");
            settings = ScriptableObject.CreateInstance<LlmSettings>();
            settings.useMock = true;
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LLM] Settings created at {SettingsPath} (useMock = true).");
            return settings;
        }

        [MenuItem("Chatan/LLM/Build LLM Playground")]
        public static void BuildPlayground()
        {
            if (!H.TmpIsReady() || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            LlmSettings settings = CreateSettings();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Canvas canvas = H.FindOrCreateCanvas();
            H.EnsureEventSystem();

            GameObject panel = H.NewUi("Panel", canvas.transform);
            H.Stretch((RectTransform)panel.transform);
            panel.AddComponent<Image>().color = Background;

            TMP_Text title = H.NewText("Title", panel.transform, "LLM Playground", 44f, Color.white, FontStyles.Bold);
            Place(title.gameObject, new Vector2(0f, 440f), new Vector2(1200f, 70f));

            var resources = new TMP_DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd")
            };

            GameObject inputGo = TMP_DefaultControls.CreateInputField(resources);
            inputGo.name = "Input";
            inputGo.transform.SetParent(panel.transform, false);
            Place(inputGo, new Vector2(0f, 320f), new Vector2(1200f, 80f));
            var input = inputGo.GetComponent<TMP_InputField>();
            input.text = "te doy 2 trigo por 1 mineral";
            input.pointSize = 30f;
            inputGo.GetComponent<Image>().color = FieldColor;

            TMP_Dropdown mode = NewDropdown("Mode", panel.transform, resources, new Vector2(-400f, 210f));
            TMP_Dropdown decision = NewDropdown("Decision", panel.transform, resources, new Vector2(0f, 210f));

            Button send = H.NewButton("Send", panel.transform, "Enviar");
            Place(send.gameObject, new Vector2(450f, 210f), new Vector2(300f, 80f));

            TMP_Text output = H.NewText("Output", panel.transform, "Pulsa Enviar.", 30f, Color.white, FontStyles.Normal);
            output.alignment = TextAlignmentOptions.TopLeft;
            output.textWrappingMode = TextWrappingModes.Normal;
            output.richText = true;
            Place(output.gameObject, new Vector2(0f, -120f), new Vector2(1200f, 560f));

            var playground = new GameObject("LlmPlayground").AddComponent<LlmPlayground>();
            var so = new SerializedObject(playground);
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("personality").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PersonalityProfile>(DefaultPersonalityPath);
            so.FindProperty("input").objectReferenceValue = input;
            so.FindProperty("mode").objectReferenceValue = mode;
            so.FindProperty("decision").objectReferenceValue = decision;
            so.FindProperty("send").objectReferenceValue = send;
            so.FindProperty("output").objectReferenceValue = output;
            so.ApplyModifiedPropertiesWithoutUndo();

            H.EnsureFolder("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[LLM] Playground saved at {ScenePath}.");
        }

        private static TMP_Dropdown NewDropdown(string name, Transform parent, TMP_DefaultControls.Resources resources, Vector2 position)
        {
            GameObject go = TMP_DefaultControls.CreateDropdown(resources);
            go.name = name;
            go.transform.SetParent(parent, false);
            Place(go, position, new Vector2(360f, 80f));
            var dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.captionText.fontSize = 30f;
            dropdown.itemText.fontSize = 28f;
            return dropdown;
        }

        private static void Place(GameObject go, Vector2 position, Vector2 size) =>
            H.SetRect(go, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
    }
}
