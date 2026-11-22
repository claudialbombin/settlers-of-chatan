using System.Collections.Generic;
using Chatan.Board;
using UnityEngine;

namespace Chatan.Dev
{
    /// <summary>
    /// Clickable placeholder board for <c>Sandbox_Claudia</c>, until Teresa's
    /// <c>BoardView</c> exists: a row of tiles (flat cylinders), a row of edges (capsules)
    /// and a row of vertices (spheres), each with a <see cref="BoardClickTarget"/>.
    /// </summary>
    /// <remarks>
    /// Built in <c>Awake</c> in front of the main camera, on the <c>BoardTargets</c> layer.
    /// The highlight is a bigger yellow copy of the piece, without collider.
    /// </remarks>
    public class FakeBoardTargets : MonoBehaviour
    {
        [SerializeField] private int tileCount = 5;
        [SerializeField] private int edgeCount = 6;
        [SerializeField] private int vertexCount = 7;
        [SerializeField] private float spacing = 1.6f;

        private static readonly Color TileColor = new Color(0.85f, 0.75f, 0.5f);
        private static readonly Color EdgeColor = new Color(0.55f, 0.35f, 0.2f);
        private static readonly Color VertexColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color HighlightColor = new Color(1f, 0.85f, 0.1f);

        private readonly List<BoardClickTarget> _targets = new List<BoardClickTarget>();

        /// <summary>Number of tiles built.</summary>
        public int TileCount => tileCount;

        /// <summary>Number of edges built.</summary>
        public int EdgeCount => edgeCount;

        /// <summary>Number of vertices built.</summary>
        public int VertexCount => vertexCount;

        /// <summary>Every piece built, in creation order.</summary>
        public IReadOnlyList<BoardClickTarget> Targets => _targets;

        private void Awake()
        {
            int layer = LayerMask.NameToLayer(BoardClickTarget.LayerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[FakeBoard] Layer {BoardClickTarget.LayerName} is missing; using Default.", this);
                layer = 0;
            }

            BuildRow(BoardTargetKind.Tile, PrimitiveType.Cylinder, tileCount, 2.4f, new Vector3(1.2f, 0.05f, 1.2f), Quaternion.Euler(90f, 0f, 0f), TileColor, layer);
            BuildRow(BoardTargetKind.Edge, PrimitiveType.Capsule, edgeCount, 0.8f, new Vector3(0.25f, 0.5f, 0.25f), Quaternion.Euler(0f, 0f, 90f), EdgeColor, layer);
            BuildRow(BoardTargetKind.Vertex, PrimitiveType.Sphere, vertexCount, -0.6f, Vector3.one * 0.45f, Quaternion.identity, VertexColor, layer);
        }

        /// <summary>The piece of that kind and id, or null.</summary>
        public BoardClickTarget Get(BoardTargetKind kind, int id) =>
            _targets.Find(t => t.kind == kind && t.id == id);

        private void BuildRow(BoardTargetKind kind, PrimitiveType shape, int count, float y, Vector3 scale, Quaternion rotation, Color color, int layer)
        {
            float startX = -(count - 1) * spacing / 2f;
            for (int i = 0; i < count; i++)
            {
                GameObject piece = GameObject.CreatePrimitive(shape);
                piece.name = $"{kind}_{i}";
                piece.layer = layer;
                piece.transform.SetParent(transform, false);
                piece.transform.localPosition = new Vector3(startX + i * spacing, y, 0f);
                piece.transform.localRotation = rotation;
                piece.transform.localScale = scale;
                piece.GetComponent<Renderer>().material.color = color;

                GameObject glow = GameObject.CreatePrimitive(shape);
                glow.name = "Highlight";
                Destroy(glow.GetComponent<Collider>());
                glow.layer = layer;
                glow.transform.SetParent(piece.transform, false);
                glow.transform.localScale = Vector3.one * 1.25f;
                glow.GetComponent<Renderer>().material.color = HighlightColor;

                var target = piece.AddComponent<BoardClickTarget>();
                target.Setup(kind, i, glow);
                _targets.Add(target);
            }
        }
    }
}
