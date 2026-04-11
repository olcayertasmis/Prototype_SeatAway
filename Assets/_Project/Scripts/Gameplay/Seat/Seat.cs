using System.Collections.Generic;
using UnityEngine;
using PSA.Gameplay.Grid;
using DG.Tweening;
using PSA.Gameplay.Data;

namespace PSA.Gameplay.Seats
{
    public class Seat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material staticMaterial;
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private Transform scaleRoot;
        [SerializeField] private BoxCollider boxCollider;

        [Header("Snap Settings")]
        [SerializeField] private float snapDuration;
        [SerializeField] private float jumpPower;

        [Header("Data")]
        public static readonly List<Seat> AllSeats = new();
        private List<Cell> _occupiedCells = new();
        private SeatData _seatData;
        private Vector3 _baseColliderSize;
        private Vector3 _baseColliderCenter;
        private Vector3 _baseScale;
        private GameObject _ghostInstance;

        [Header("Passenger Tracking")]
        private bool[] _slotOccupancy;
        private HashSet<int> _reservedLocalSlots = new();

        [Header("Control")]
        private bool _isHeld;

        private static MaterialPropertyBlock _mpb;

        #region Encapsulation

        public IReadOnlyList<Cell> OccupiedCells => _occupiedCells;
        public bool IsMovable => _seatData.isMovable;
        public ElementColor Color => _seatData.color;
        public int Width => _seatData.width;
        public int Height => _seatData.height;
        public bool IsHeld => _isHeld;
        public BoxCollider SeatCollider => boxCollider;

        #endregion

        #region Helpers

        private int TotalPassengers => GetSeatedCount() + _reservedLocalSlots.Count;
        public bool HasAnyPassenger => TotalPassengers > 0;
        public bool HasEmptySlot => TotalPassengers < (_seatData.width * _seatData.height);
        public bool IsBlockedByIncomingPassenger => _reservedLocalSlots.Count > 0;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            AllSeats.Add(this);
        }

        private void OnDisable()
        {
            AllSeats.Remove(this);
        }

        private void Awake()
        {
            _baseScale = transform.localScale;

            _baseColliderSize = boxCollider.size;
            _baseColliderCenter = boxCollider.center;

            if (_mpb == null) _mpb = new MaterialPropertyBlock();
        }

        #endregion

        #region ISystem Implementation

        public void Initialize(SeatData data)
        {
            _seatData = data;
            _isHeld = false;
            _reservedLocalSlots.Clear();

            _slotOccupancy = new bool[_seatData.width * _seatData.height];

            if (!_seatData.isMovable)
            {
                meshRenderer.material = staticMaterial;
                return;
            }

            Color matColor = GetSeatColorValue(_seatData.color);

            meshRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_Color", matColor);
            _mpb.SetColor("_BaseColor", matColor);
            meshRenderer.SetPropertyBlock(_mpb);

            scaleRoot.localScale = new Vector3(_seatData.width, 1, _seatData.height);

            boxCollider.size = new Vector3(_baseColliderSize.x * _seatData.width, _baseColliderSize.y, _baseColliderSize.z * _seatData.height);
            boxCollider.center = _baseColliderCenter;
        }

        #endregion

        #region Core Logic

        private Color GetSeatColorValue(ElementColor elementColor)
        {
            return elementColor switch
            {
                ElementColor.Blue => UnityEngine.Color.blue,
                ElementColor.Green => UnityEngine.Color.green,
                ElementColor.Red => UnityEngine.Color.red,
                ElementColor.Yellow => UnityEngine.Color.yellow,
                _ => UnityEngine.Color.white
            };
        }

        public void PlaceOnCellsInstant(List<Cell> targetCells)
        {
            FreeCurrentCells();
            _occupiedCells = targetCells;
            OccupyCurrentCells();
            transform.position = CalculateCenter();
        }

        public void SnapToCells(List<Cell> targetCells)
        {
            if (targetCells == null || targetCells.Count == 0) return;

            FreeCurrentCells();
            _occupiedCells = targetCells;
            OccupyCurrentCells();

            transform.DOJump(CalculateCenter(), jumpPower, 1, snapDuration).SetEase(Ease.OutQuad);
        }

        private void FreeCurrentCells()
        {
            foreach (var cell in _occupiedCells)
            {
                cell.SetOccupiedStatus(false);
            }
        }

        private void OccupyCurrentCells()
        {
            foreach (var cell in _occupiedCells)
            {
                cell.SetOccupiedStatus(true);
            }
        }

        private Vector3 CalculateCenter()
        {
            if (_occupiedCells.Count == 0) return transform.position;
            Vector3 center = Vector3.zero;

            foreach (var cell in _occupiedCells)
            {
                center += cell.transform.position;
            }

            return center / _occupiedCells.Count;
        }

        private int GetSeatedCount()
        {
            int count = 0;
            if (_slotOccupancy != null)
            {
                foreach (bool isOccupied in _slotOccupancy)
                {
                    if (isOccupied) count++;
                }
            }

            return count;
        }

        public void PickUp()
        {
            if (!IsMovable || IsBlockedByIncomingPassenger) return;

            _isHeld = true;
            CreateGhost();
            transform.DOScale(_baseScale * 1.2f, 0.15f);
        }

        public void Drop()
        {
            if (!IsMovable) return;

            _isHeld = false;
            DestroyGhost();
            transform.DOScale(_baseScale, 0.15f);
        }

        private void CreateGhost()
        {
            if (_ghostInstance) return;

            _ghostInstance = Instantiate(scaleRoot.gameObject, transform.position, transform.rotation);
            _ghostInstance.transform.localScale = Vector3.Scale(_baseScale, scaleRoot.localScale);

            if (_ghostInstance.TryGetComponent(out MeshRenderer ghostRenderer))
            {
                ghostRenderer.sharedMaterial = ghostMaterial;

                Color ghostColor = GetSeatColorValue(_seatData.color);
                ghostColor.a = ghostMaterial.color.a;

                ghostRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor("_Color", ghostColor);
                _mpb.SetColor("_BaseColor", ghostColor);
                ghostRenderer.SetPropertyBlock(_mpb);
            }
        }

        private void DestroyGhost()
        {
            if (!_ghostInstance) return;

            Destroy(_ghostInstance);
            _ghostInstance = null;
        }

        public List<Cell> GetAvailableSlots()
        {
            List<Cell> available = new List<Cell>();
            for (int i = 0; i < _occupiedCells.Count; i++)
            {
                if (!_slotOccupancy[i] && !_reservedLocalSlots.Contains(i))
                {
                    available.Add(_occupiedCells[i]);
                }
            }

            return available;
        }

        public void ReserveSlot(Cell slotCell)
        {
            int index = _occupiedCells.IndexOf(slotCell);
            if (index != -1)
            {
                _reservedLocalSlots.Add(index);
            }
        }

        public void SeatPassengerAtSlot(Cell slotCell)
        {
            int index = _occupiedCells.IndexOf(slotCell);

            if (index != -1)
            {
                _reservedLocalSlots.Remove(index);
                _slotOccupancy[index] = true;
            }
        }

        #endregion
    }
}