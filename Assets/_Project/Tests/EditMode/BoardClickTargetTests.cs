using Chatan.Board;
using NUnit.Framework;
using UnityEngine;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="BoardClickTarget"/>.</summary>
    public class BoardClickTargetTests
    {
        private GameObject _piece;
        private GameObject _glow;
        private BoardClickTarget _target;

        [SetUp]
        public void SetUp()
        {
            _piece = new GameObject("Vertex_3");
            _glow = new GameObject("Highlight");
            _glow.transform.SetParent(_piece.transform);
            _target = _piece.AddComponent<BoardClickTarget>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_piece);

        [Test]
        public void Setup_StoresKindAndIdAndHidesTheHighlight()
        {
            _target.Setup(BoardTargetKind.Vertex, 3, _glow);

            Assert.AreEqual(BoardTargetKind.Vertex, _target.kind);
            Assert.AreEqual(3, _target.id);
            Assert.IsFalse(_glow.activeSelf);
        }

        [Test]
        public void SetHighlighted_TogglesTheHighlight()
        {
            _target.Setup(BoardTargetKind.Edge, 1, _glow);

            _target.SetHighlighted(true);
            Assert.IsTrue(_target.IsHighlighted);

            _target.SetHighlighted(false);
            Assert.IsFalse(_target.IsHighlighted);
        }

        [Test]
        public void SetHighlighted_WithoutHighlightDoesNothing()
        {
            Assert.DoesNotThrow(() => _target.SetHighlighted(true));
            Assert.IsFalse(_target.IsHighlighted);
        }
    }
}
