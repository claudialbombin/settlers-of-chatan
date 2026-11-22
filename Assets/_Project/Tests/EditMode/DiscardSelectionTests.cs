using Chatan.Core;
using Chatan.UI;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="DiscardSelection"/>: 9 cards in hand, 4 to discard.</summary>
    public class DiscardSelectionTests
    {
        private DiscardSelection _selection;

        [SetUp]
        public void SetUp() => _selection = new DiscardSelection(new ResourceBundle(2, 2, 2, 2, 1), 4);

        [Test]
        public void StartsEmptyAndCannotConfirm()
        {
            Assert.AreEqual(0, _selection.Chosen.Total);
            Assert.IsFalse(_selection.CanConfirm);
        }

        [Test]
        public void CannotTakeMoreThanYouHoldOfAType()
        {
            Assert.IsTrue(_selection.TryChange(ResourceType.Ore, +1));
            Assert.IsFalse(_selection.TryChange(ResourceType.Ore, +1));
            Assert.AreEqual(1, _selection.Chosen.Ore);
        }

        [Test]
        public void CannotGoBelowZero()
        {
            Assert.IsFalse(_selection.TryChange(ResourceType.Wood, -1));
            Assert.AreEqual(0, _selection.Chosen.Wood);
        }

        [Test]
        public void ConfirmOnlyAtTheExactAmount()
        {
            _selection.TryChange(ResourceType.Wood, +1);
            _selection.TryChange(ResourceType.Wood, +1);
            _selection.TryChange(ResourceType.Brick, +1);
            Assert.IsFalse(_selection.CanConfirm, "3 of 4");

            _selection.TryChange(ResourceType.Wool, +1);
            Assert.IsTrue(_selection.CanConfirm, "4 of 4");
        }

        [Test]
        public void CannotPickMoreThanRequired()
        {
            _selection.TryChange(ResourceType.Wood, +1);
            _selection.TryChange(ResourceType.Wood, +1);
            _selection.TryChange(ResourceType.Brick, +1);
            _selection.TryChange(ResourceType.Brick, +1);

            Assert.IsFalse(_selection.TryChange(ResourceType.Grain, +1), "a 5th card");
            Assert.AreEqual(4, _selection.Chosen.Total);
        }

        [Test]
        public void Texts_ShowTheProgress()
        {
            _selection.TryChange(ResourceType.Grain, +1);

            Assert.AreEqual("Ha salido un 7: descarta 4 cartas (llevas 1)", _selection.Title);
            Assert.AreEqual("1 / 2", _selection.AmountText(ResourceType.Grain));
        }
    }
}
