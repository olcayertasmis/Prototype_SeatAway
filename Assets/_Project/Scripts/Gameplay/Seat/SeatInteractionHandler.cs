using System.Collections.Generic;
using System.Linq;
using PSA.Core;
using UnityEngine;
using PSA.Gameplay.Grid;
using UnityEngine.InputSystem;

namespace PSA.Gameplay.Seats
{
    public class SeatInteractionHandler : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private LayerMask seatLayer;
        [SerializeField] private float dragHeightOffset = 1f;
        //[SerializeField] private LayerMask gridLayer;

        [Header("Data")]
        private Seat _selectedSeat;
        private Vector3 _dragOffset;
        private Vector3 _dragBoxExtents;

        [Header("References")]
        private Camera _mainCamera;
        private GridManager _gridManager;

        [Header("Controls")]
        private bool _isDragging;
        private bool _isGameActive;

        private void OnEnable()
        {
            var eventManager = SystemLocator.Get<EventManager>();
            if (eventManager)
            {
                eventManager.AddListener<LevelStartedEvent>(OnLevelStarted);
                eventManager.AddListener<LevelCompletedEvent>(OnLevelCompleted);
            }
        }

        private void OnDisable()
        {
            if (SystemLocator.TryGet(out EventManager eventManager))
            {
                eventManager.RemoveListener<LevelStartedEvent>(OnLevelStarted);
                eventManager.RemoveListener<LevelCompletedEvent>(OnLevelCompleted);
            }
        }

        private void Start()
        {
            _mainCamera = Camera.main;

            _gridManager = SystemLocator.Get<GridManager>();
        }

        private void Update()
        {
            if (!_isGameActive) return;
            HandleInput();
        }

        private void HandleInput()
        {
            if (Mouse.current == null) return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryPickUpSeat();
            }
            else if (Mouse.current.leftButton.isPressed && _isDragging)
            {
                DragSeat();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame && _isDragging)
            {
                ReleaseSeat();
            }
        }

        private void TryPickUpSeat()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, seatLayer))
            {
                if (hit.collider.TryGetComponent(out Seat seat))
                {
                    if (!seat.IsMovable || seat.IsBlockedByIncomingPassenger) return;

                    _selectedSeat = seat;
                    _isDragging = true;

                    Vector3 groundHit = hit.point;
                    groundHit.y = _selectedSeat.transform.position.y;
                    _dragOffset = _selectedSeat.transform.position - groundHit;

                    BoxCollider seatCollider = _selectedSeat.SeatCollider;
                    _dragBoxExtents = Vector3.Scale(seatCollider.size, _selectedSeat.transform.lossyScale) * 0.45f;
                    _dragBoxExtents.y = 0.1f;

                    _selectedSeat.PickUp();
                }
            }
        }

        private void DragSeat()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                Vector3 currentGroundPos = _selectedSeat.transform.position;
                currentGroundPos.y = 0f;

                Vector3 intendedGroundPos = hitPoint + _dragOffset;
                intendedGroundPos.y = 0f;

                intendedGroundPos = _gridManager.ClampPositionToGridBounds(intendedGroundPos, _selectedSeat.Width, _selectedSeat.Height);

                Vector3 direction = (intendedGroundPos - currentGroundPos).normalized;
                float distance = Vector3.Distance(currentGroundPos, intendedGroundPos);

                if (distance > 0.01f)
                {
                    BoxCollider seatCollider = _selectedSeat.SeatCollider;
                    seatCollider.enabled = false;

                    if (Physics.BoxCast(currentGroundPos, _dragBoxExtents, direction, out RaycastHit blockHit, _selectedSeat.transform.rotation, distance, seatLayer))
                    {
                        distance = Mathf.Max(0, blockHit.distance - 0.05f);
                        intendedGroundPos = currentGroundPos + (direction * distance);
                    }

                    seatCollider.enabled = true;
                }

                Vector3 finalTarget = intendedGroundPos;
                finalTarget.y = hitPoint.y + dragHeightOffset;

                _selectedSeat.transform.position = Vector3.Lerp(_selectedSeat.transform.position, finalTarget, Time.deltaTime * 20f);
            }
        }

        private void ReleaseSeat()
        {
            _isDragging = false;
            _selectedSeat.Drop();

            Cell rootCell = _gridManager.CalculateRootCellForSeat(_selectedSeat.transform.position, _selectedSeat.Width, _selectedSeat.Height);
            if (rootCell)
            {
                List<Cell> targetCells = _gridManager.GetCellsInRect(rootCell.Coordinate, _selectedSeat.Width, _selectedSeat.Height);

                if (targetCells.Count == (_selectedSeat.Width * _selectedSeat.Height))
                {
                    bool canPlace = true;
                    foreach (var c in targetCells)
                    {
                        if (c.CellType == Data.CellType.Obstacle || c.CellType == Data.CellType.Entrance) canPlace = false;
                        if (c.IsOccupied && !_selectedSeat.OccupiedCells.Contains(c)) canPlace = false;
                    }

                    if (canPlace)
                    {
                        _selectedSeat.SnapToCells(targetCells);
                        SystemLocator.Get<EventManager>().TriggerEvent(new GridUpdatedEvent());
                        _selectedSeat = null;
                        return;
                    }
                }
            }

            _selectedSeat.SnapToCells(new List<Cell>(_selectedSeat.OccupiedCells));
            _selectedSeat = null;
        }

        #region Callbacks

        private void OnLevelStarted(LevelStartedEvent data)
        {
            _isGameActive = true;
        }

        private void OnLevelCompleted(LevelCompletedEvent data)
        {
            _isGameActive = false;

            if (_isDragging && _selectedSeat)
            {
                _isDragging = false;
                _selectedSeat.Drop();
                _selectedSeat.SnapToCells(new List<Cell>(_selectedSeat.OccupiedCells));
                _selectedSeat = null;
            }
        }

        #endregion
    }
}