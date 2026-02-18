using System;
using System.Collections.Generic;
using Common.Shapes;
using Common.UI;
using UnityEngine;
using Common.Interfaces;
using Match3.Core.Structs;

namespace Common
{
    public class BlockBlastInputManager : MonoBehaviour
    {
        [SerializeField] private UnityGameBoardRenderer _boardRenderer;
        [SerializeField] private Camera _camera;

        public void Setup(UnityGameBoardRenderer renderer, Camera camera)
        {
            _boardRenderer = renderer;
            _camera = camera;
        }

        private ShapeSlot _draggingSlot;
        private readonly HashSet<ShapeSlot> _registeredSlots = new HashSet<ShapeSlot>();

        public event Action<ShapeSlot, ShapeData, GridPosition, Vector3> OnShapeDropped;
        public event Action<GridPosition, ShapeData, bool, Vector3> OnShapeHover;

        public void RegisterSlot(ShapeSlot slot)
        {
            if (_registeredSlots.Contains(slot)) return;
            
            _registeredSlots.Add(slot);
            slot.OnDragStarted += OnSlotDragStarted;
            slot.OnDragUpdated += OnSlotDragUpdated;
            slot.OnDragEnded += OnSlotDragEnded;
        }

        /// <summary>
        /// Convert screen position to world position at z=0 plane.
        /// </summary>
        private Vector3 ScreenToWorld(Vector2 screenPos)
        {
            float camDist = Mathf.Abs(_camera.transform.position.z);
            Vector3 worldPos = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, camDist));
            worldPos.z = 0;
            return worldPos;
        }

        private void OnSlotDragStarted(ShapeSlot slot, UnityEngine.EventSystems.PointerEventData data)
        {
            if (_draggingSlot != null) return;
            _draggingSlot = slot;

            // Code removed

        }

        private void OnSlotDragUpdated(ShapeSlot slot, UnityEngine.EventSystems.PointerEventData data)
        {
            if (_draggingSlot == null) return;

            Vector3 worldPos = ScreenToWorld(data.position);

            // REVERT: Logic uses raw finger position. Visuals (ShapeSlot) have their own offset.
            // worldPos.y += 3.0f; // REMOVED

            var rawGridPos = _boardRenderer.GetRawGridPosition(worldPos);
            bool isPointerOnBoard = _boardRenderer.IsPointerOnBoard(worldPos, out var clampedGridPos);
            
            OnShapeHover?.Invoke(rawGridPos, slot.CurrentShape, isPointerOnBoard, worldPos);
        }

        private void OnSlotDragEnded(ShapeSlot slot, UnityEngine.EventSystems.PointerEventData data)
        {
            if (_draggingSlot == null) return;

            Vector3 worldPos = ScreenToWorld(data.position);
            bool onBoard = _boardRenderer.IsPointerOnBoard(worldPos, out var gridPos);
            
            if (onBoard)
            {
                OnShapeDropped?.Invoke(slot, slot.CurrentShape, gridPos, worldPos);
            }
            
            _draggingSlot = null;
            OnShapeHover?.Invoke(GridPosition.Zero, null, false, Vector3.zero);
        }
    }
}
