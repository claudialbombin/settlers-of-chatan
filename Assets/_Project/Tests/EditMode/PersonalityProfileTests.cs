using Chatan.AI;
using Chatan.Core;
using NUnit.Framework;
using UnityEngine;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="PersonalityProfile"/>.</summary>
    public class PersonalityProfileTests
    {
        private const float Tolerance = 1e-4f;

        private PersonalityProfile _profile;

        // ScriptableObjects are engine objects: create one per test and destroy it after.
        [SetUp]
        public void SetUp() => _profile = ScriptableObject.CreateInstance<PersonalityProfile>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_profile);

        [Test]
        public void PreferenceFor_ReturnsTheMatchingField()
        {
            _profile.woodPreference = 0.6f;
            _profile.brickPreference = 0.7f;
            _profile.woolPreference = 1.2f;
            _profile.grainPreference = 1.5f;
            _profile.orePreference = 1.8f;

            Assert.AreEqual(0.6f, _profile.PreferenceFor(ResourceType.Wood), Tolerance);
            Assert.AreEqual(0.7f, _profile.PreferenceFor(ResourceType.Brick), Tolerance);
            Assert.AreEqual(1.2f, _profile.PreferenceFor(ResourceType.Wool), Tolerance);
            Assert.AreEqual(1.5f, _profile.PreferenceFor(ResourceType.Grain), Tolerance);
            Assert.AreEqual(1.8f, _profile.PreferenceFor(ResourceType.Ore), Tolerance);
        }

        [Test]
        public void NewProfile_HasNeutralWeightsAndPreferences()
        {
            foreach (ResourceType type in ResourceBundle.AllTypes)
            {
                Assert.AreEqual(PersonalityProfile.NeutralMultiplier, _profile.PreferenceFor(type), Tolerance);
            }

            Assert.AreEqual(PersonalityProfile.NeutralMultiplier, _profile.cityWeight, Tolerance);
        }

        [Test]
        public void PromptText_WithoutFile_UsesTheDefaultText()
        {
            _profile.displayName = "Prueba";
            _profile.shortDescription = "Le encanta el trigo.";

            StringAssert.Contains("Prueba", _profile.PromptText);
            StringAssert.Contains("Le encanta el trigo.", _profile.PromptText);
        }

        [Test]
        public void PromptText_WithFile_UsesTheFileContent()
        {
            var prompt = new TextAsset("Eres un comerciante astuto.");
            _profile.promptFile = prompt;

            Assert.AreEqual("Eres un comerciante astuto.", _profile.PromptText);

            Object.DestroyImmediate(prompt);
        }
    }
}
