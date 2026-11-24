using Chatan.Core;
using Chatan.Dev;
using Chatan.Negotiation;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Unit tests for the placeholder <see cref="SimpleResourceValuator"/>.</summary>
    public class SimpleResourceValuatorTests
    {
        private readonly SimpleResourceValuator _valuator = new SimpleResourceValuator();
        private GameState _state;

        [SetUp]
        public void SetUp() => _state = FakeGameStateFactory.Create();

        [Test]
        public void MissingResource_IsWorthMore()
        {
            // Bot Azul (1) has no wool.
            Assert.AreEqual(0.8f * 1.4f, _valuator.ValueOf(ResourceType.Wool, 1, _state), 1e-5f);
        }

        [Test]
        public void PlentifulResource_IsWorthLess()
        {
            // Bot Verde (2) has 4 grain.
            Assert.AreEqual(1.1f * 0.7f, _valuator.ValueOf(ResourceType.Grain, 2, _state), 1e-5f);
        }

        [Test]
        public void NormalAmount_KeepsTheBaseValue()
        {
            // Bot Azul (1) has 3 ore.
            Assert.AreEqual(1.2f, _valuator.ValueOf(ResourceType.Ore, 1, _state), 1e-5f);
        }

        [Test]
        public void UnknownPlayer_UsesTheBaseValue()
        {
            Assert.AreEqual(1f, _valuator.ValueOf(ResourceType.Brick, 9, _state), 1e-5f);
        }
    }
}
