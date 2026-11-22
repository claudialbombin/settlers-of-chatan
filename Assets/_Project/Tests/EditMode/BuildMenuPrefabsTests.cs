using System.Linq;
using Chatan.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Chatan.Tests
{
    /// <summary>
    /// Opens the committed prefabs of the build menu and dialogs and checks every
    /// Inspector reference is wired, so a broken prefab fails here and not in Play mode.
    /// </summary>
    public class BuildMenuPrefabsTests
    {
        private const string Folder = "Assets/_Project/Prefabs/UI/";

        private static T Load<T>(string file) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + file);
            Assert.IsNotNull(prefab, $"No prefab at {Folder + file}.");
            var component = prefab.GetComponent<T>();
            Assert.IsNotNull(component, $"{file} has no {typeof(T).Name}.");
            return component;
        }

        private static void AssertWired(Object component)
        {
            var so = new SerializedObject(component);
            SerializedProperty property = so.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.name != "m_Script")
                {
                    Assert.IsNotNull(property.objectReferenceValue, $"{component.name}: {property.propertyPath} is empty.");
                }
            }
        }

        [Test]
        public void BuildMenu_HasOneButtonPerOption()
        {
            BuildMenu menu = Load<BuildMenu>("BuildMenu.prefab");
            BuildButton[] buttons = menu.GetComponentsInChildren<BuildButton>(true);

            CollectionAssert.AreEquivalent(
                new[] { BuildKind.Road, BuildKind.Settlement, BuildKind.City, BuildKind.DevCard },
                buttons.Select(b => b.Kind).ToArray());
            AssertWired(menu);
            foreach (BuildButton button in buttons)
            {
                AssertWired(button);
            }
        }

        [Test]
        public void BuildButtons_HaveAnIcon()
        {
            foreach (BuildButton button in Load<BuildMenu>("BuildMenu.prefab").GetComponentsInChildren<BuildButton>(true))
            {
                Image icon = button.transform.Find("Icon").GetComponent<Image>();
                Assert.IsNotNull(icon.sprite, $"{button.name} has no icon.");
            }
        }

        [Test]
        public void DiscardDialog_IsWiredAndHidden()
        {
            DiscardDialog dialog = Load<DiscardDialog>("DiscardDialog.prefab");

            AssertWired(dialog);
            Assert.IsFalse(dialog.gameObject.activeSelf);
        }

        [Test]
        public void RobberDialog_IsWiredAndHidden()
        {
            RobberVictimDialog dialog = Load<RobberVictimDialog>("RobberVictimDialog.prefab");

            AssertWired(dialog);
            Assert.IsFalse(dialog.gameObject.activeSelf);
        }
    }
}
