using UnityEngine;

namespace Chatan.Board
{
    /// <summary>What a clickable board piece stands for.</summary>
    public enum BoardTargetKind
    {
        /// <summary>A corner, where settlements and cities go.</summary>
        Vertex,

        /// <summary>A side, where roads go.</summary>
        Edge,

        /// <summary>A hexagon, where the robber goes.</summary>
        Tile
    }

    /// <summary>
    /// Marks a vertex, edge or tile of the board as clickable and lets the UI light it up.
    /// </summary>
    /// <remarks>
    /// Needs a <c>Collider</c> on the same object (or a child) and the <c>BoardTargets</c>
    /// layer, so <c>BoardInput</c>'s raycast only hits these. Teresa's <c>BoardView</c>
    /// adds it to its prefabs; the sandbox creates them with <c>FakeBoardTargets</c>.
    /// </remarks>
    public class BoardClickTarget : MonoBehaviour
    {
        /// <summary>Name of the physics layer every target must be on.</summary>
        public const string LayerName = "BoardTargets";

        /// <summary>What this piece stands for.</summary>
        public BoardTargetKind kind;

        /// <summary>Id of the vertex, edge or tile in the board graph.</summary>
        public int id;

        [Tooltip("Ring or glow shown while the piece is a valid choice. Off by default.")]
        [SerializeField] private GameObject highlight;

        /// <summary>Whether the highlight is showing.</summary>
        public bool IsHighlighted => highlight != null && highlight.activeSelf;

        /// <summary>Sets kind, id and highlight from code (for boards built at runtime).</summary>
        public void Setup(BoardTargetKind targetKind, int targetId, GameObject highlightObject)
        {
            kind = targetKind;
            id = targetId;
            highlight = highlightObject;
            SetHighlighted(false);
        }

        /// <summary>Shows or hides the highlight.</summary>
        public void SetHighlighted(bool on)
        {
            if (highlight != null)
            {
                highlight.SetActive(on);
            }
        }
    }
}
