using PSA.Gameplay.Data;
using UnityEngine;

namespace PSA.Gameplay.Grid
{
    public class Cell : MonoBehaviour
    {
        [Header("Cell Visuals")]
        [SerializeField] private MeshRenderer meshRenderer;

        [Header("Cell Colors")]
        [SerializeField] private Color firstCellColor = new Color(0.8f, 0.8f, 0.8f); // lightGrayColor
        [SerializeField] private Color secondCellColor = new Color(0.6f, 0.6f, 0.6f); // darkGrayColor
        [SerializeField] private Color entranceColor = Color.white;

        private Vector2Int _coordinate;
        private CellType _cellType;
        private bool _isOccupied;

        #region Encapsulation

        public Vector2Int Coordinate => _coordinate;
        public CellType CellType => _cellType;
        public bool IsOccupied => _isOccupied;

        #endregion

        public void Initialize(Vector2Int coordinate, CellType type)
        {
            _coordinate = coordinate;
            _cellType = type;
            _isOccupied = type == CellType.Obstacle;

            UpdateVisuals();
        }

        public void SetOccupiedStatus(bool status)
        {
            _isOccupied = status;
        }

        private void UpdateVisuals()
        {
            if (!meshRenderer) return;

            if (_cellType == CellType.Entrance)
            {
                meshRenderer.material.color = entranceColor;
            }
            else
            {
                bool isLight = (_coordinate.x + _coordinate.y) % 2 == 0;
                meshRenderer.material.color = isLight ? firstCellColor : secondCellColor;
            }
        }
    }
}