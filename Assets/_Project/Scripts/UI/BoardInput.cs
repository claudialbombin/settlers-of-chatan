using System;
using Chatan.Board;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Chatan.UI
{
    /// <summary>
    /// Turns mouse clicks into "the vertex 12 was clicked". Knows nothing about the rules.
    /// </summary>
    /// <remarks>
    /// Uses the new Input System (<c>Mouse.current</c>); the old <c>UnityEngine.Input</c>
    /// throws in this project. Clicks over a UI element are ignored, so pressing a button
    /// never clicks the board behind it.
    /// </remarks>
    public class BoardInput : MonoBehaviour
    {
        [Tooltip("Camera that looks at the board. Main camera if empty.")]
        [SerializeField] private Camera cam;

        [Tooltip("Only the BoardTargets layer.")]
        [SerializeField] private LayerMask targetsMask;

        [SerializeField] private float maxDistance = 200f;

        /// <summary>Left click on a board piece.</summary>
        public event Action<BoardClickTarget> TargetClicked;

        /// <summary>Right click anywhere: cancel the current mode.</summary>
        public event Action RightClicked;

        private void Awake()
        {
            if (cam == null)
            {
                cam = Camera.main;
            }
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || cam == null)
            {
                return;
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                RightClicked?.Invoke();
                return;
            }

            if (!mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, targetsMask))
            {
                var target = hit.collider.GetComponentInParent<BoardClickTarget>();
                if (target != null)
                {
                    TargetClicked?.Invoke(target);
                }
            }
        }
    }
}
