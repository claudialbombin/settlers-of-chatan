using Chatan.AI;
using NUnit.Framework;
using UnityEngine;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="PersonalityCatalog"/>.</summary>
    public class PersonalityCatalogTests
    {
        private PersonalityCatalog _catalog;
        private PersonalityProfile _greedy;
        private PersonalityProfile _merchant;

        [SetUp]
        public void SetUp()
        {
            _greedy = ScriptableObject.CreateInstance<PersonalityProfile>();
            _greedy.id = PersonalityId.Greedy;

            _merchant = ScriptableObject.CreateInstance<PersonalityProfile>();
            _merchant.id = PersonalityId.Merchant;

            _catalog = ScriptableObject.CreateInstance<PersonalityCatalog>();
            _catalog.profiles.Add(_greedy);
            _catalog.profiles.Add(_merchant);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_catalog);
            Object.DestroyImmediate(_greedy);
            Object.DestroyImmediate(_merchant);
        }

        [Test]
        public void Get_ReturnsTheProfileWithThatId()
        {
            Assert.AreSame(_greedy, _catalog.Get(PersonalityId.Greedy));
            Assert.AreSame(_merchant, _catalog.Get(PersonalityId.Merchant));
        }

        [Test]
        public void Get_MissingId_ReturnsNull()
        {
            Assert.IsNull(_catalog.Get(PersonalityId.Trickster));
            Assert.IsFalse(_catalog.Contains(PersonalityId.Trickster));
        }

        [Test]
        public void Get_SkipsEmptySlots()
        {
            _catalog.profiles.Insert(0, null);

            Assert.AreSame(_merchant, _catalog.Get(PersonalityId.Merchant));
            Assert.IsTrue(_catalog.Contains(PersonalityId.Greedy));
        }

        [Test]
        public void Profiles_ReflectsTheList()
        {
            Assert.AreEqual(2, _catalog.Profiles.Count);
            Assert.AreSame(_greedy, _catalog.Profiles[0]);
        }
    }
}
