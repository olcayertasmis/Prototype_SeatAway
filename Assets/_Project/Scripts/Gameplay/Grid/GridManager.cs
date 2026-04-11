using System.Collections.Generic;
using PSA.Gameplay.Data;
using UnityEngine;
using PSA.Core;
using PSA.Gameplay.Seats;

namespace PSA.Gameplay.Grid
{
    public class GridManager : MonoBehaviour, ISystem
    {
        [Header("Prefabs")]
        [SerializeField] private Cell cellPrefab;
        [SerializeField] private Seat seatPrefab;
        [SerializeField] private GameObject obstaclePrefab;

        [Header("Containers")]
        [SerializeField] private Transform gridContainer;
        [SerializeField] private Transform seatContainer;

        [Header("Grid Setup")]
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float cellSpacing = 0.1f;

        [Header("Grid Data")]
        private LevelData _currentLevelData;
        private readonly Dictionary<Vector2Int, Cell> _gridCells = new();

        #region Encapsulation

        public LevelData CurrentLevelData => _currentLevelData;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (SystemLocator.TryGet(out EventManager eventManager))
            {
                eventManager.RemoveListener<LevelStartedEvent>(OnLevelStarted);
            }

            SystemLocator.Deregister<GridManager>();
        }

        #endregion

        #region ISystem Implementation

        public void Initialize()
        {
            SystemLocator.Register(this);

            var eventManager = SystemLocator.Get<EventManager>();
            eventManager.AddListener<LevelStartedEvent>(OnLevelStarted);
        }

        #endregion

        #region Core Logic

        private void GenerateGrid()
        {
            ClearGrid();
            Seat.AllSeats.Clear();

            float totalWidth = _currentLevelData.GridWidth * (cellSize + cellSpacing);
            float totalHeight = _currentLevelData.GridHeight * (cellSize + cellSpacing);
            Vector3 startOffset = new Vector3(-totalWidth / 2f + (cellSize / 2f), 0, -totalHeight / 2f + (cellSize / 2f));

            foreach (CellData data in _currentLevelData.GridCells)
            {
                Vector3 spawnPosition = startOffset + new Vector3(data.coordinates.x * (cellSize + cellSpacing), 0f, data.coordinates.y * (cellSize + cellSpacing));

                Cell spawnedCell = Instantiate(cellPrefab, spawnPosition, Quaternion.identity, gridContainer);
                spawnedCell.name = $"Cell_{data.coordinates.x}_{data.coordinates.y}";

                spawnedCell.Initialize(data.coordinates, data.cellType);
                _gridCells.Add(data.coordinates, spawnedCell);

                if (data.cellType == CellType.Obstacle && obstaclePrefab)
                {
                    Instantiate(obstaclePrefab, spawnPosition, Quaternion.identity, gridContainer);
                }
            }

            foreach (SeatData seatData in _currentLevelData.Seats)
            {
                SpawnSeat(seatData);
            }

            Vector2Int entCoord = _currentLevelData.EntranceCoordinate;
            Vector3 entPos = startOffset + new Vector3(entCoord.x * (cellSize + cellSpacing), 0f, entCoord.y * (cellSize + cellSpacing));

            if (!_gridCells.ContainsKey(entCoord))
            {
                Cell entranceCell = Instantiate(cellPrefab, entPos, Quaternion.identity, gridContainer);
                entranceCell.name = $"Cell_Entrance_{entCoord.x}_{entCoord.y}";
                entranceCell.Initialize(entCoord, CellType.Entrance);
                _gridCells.Add(entCoord, entranceCell);
            }
            else
            {
                _gridCells[entCoord].Initialize(entCoord, CellType.Entrance);
            }

            SystemLocator.Get<EventManager>().TriggerEvent(new GridReadyEvent());
        }

        private void ClearGrid()
        {
            foreach (var cell in _gridCells.Values)
            {
                if (cell) Destroy(cell.gameObject);
            }

            _gridCells.Clear();

            foreach (Transform child in seatContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void SpawnSeat(SeatData seatData)
        {
            List<Cell> targetCells = GetCellsInRect(seatData.rootCoordinate, seatData.width, seatData.height);
            if (targetCells.Count == 0) return;

            Seat spawnedSeat = Instantiate(seatPrefab, seatContainer);
            spawnedSeat.Initialize(seatData);
            spawnedSeat.name = spawnedSeat.IsMovable ? $"Seat_{seatData.color}_{seatData.width}x{seatData.height}" : $"Seat_{seatData.width}x{seatData.height}_Static";
            spawnedSeat.PlaceOnCellsInstant(targetCells);
        }

        public Cell GetCellAt(Vector2Int coordinate)
        {
            return _gridCells.GetValueOrDefault(coordinate);
        }

        public List<Cell> GetCellsInRect(Vector2Int rootPos, int width, int height)
        {
            List<Cell> cells = new();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Cell c = GetCellAt(new Vector2Int(rootPos.x + x, rootPos.y + y));
                    if (c) cells.Add(c);
                }
            }

            return cells;
        }

        public bool IsCellWalkable(Vector2Int coordinate)
        {
            Cell cell = GetCellAt(coordinate);
            if (!cell) return false;

            return !cell.IsOccupied;
        }

        public Cell CalculateRootCellForSeat(Vector3 seatCenter, int width, int height)
        {
            float step = cellSize + cellSpacing;

            Vector3 rootEstimate = seatCenter - new Vector3((width - 1) * step / 2f, 0, (height - 1) * step / 2f);

            Cell closestCell = null;
            float minDistance = float.MaxValue;

            foreach (var cell in _gridCells.Values)
            {
                float dist = Vector3.Distance(rootEstimate, cell.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestCell = cell;
                }
            }

            return closestCell;
        }

        public Vector3 ClampPositionToGridBounds(Vector3 targetPos, int seatWidth, int seatHeight)
        {
            if (!_currentLevelData) return targetPos;

            float step = cellSize + cellSpacing;
            float totalWidth = _currentLevelData.GridWidth * step;
            float totalHeight = _currentLevelData.GridHeight * step;

            Vector3 startOffset = new Vector3(-totalWidth / 2f + (cellSize / 2f), 0, -totalHeight / 2f + (cellSize / 2f));

            float minX = startOffset.x + (seatWidth - 1) * step / 2f;
            float minZ = startOffset.z + (seatHeight - 1) * step / 2f;

            int maxRootX = _currentLevelData.GridWidth - seatWidth;
            int maxRootZ = _currentLevelData.GridHeight - seatHeight;

            float maxX = startOffset.x + maxRootX * step + (seatWidth - 1) * step / 2f;
            float maxZ = startOffset.z + maxRootZ * step + (seatHeight - 1) * step / 2f;

            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.z = Mathf.Clamp(targetPos.z, minZ, maxZ);

            return targetPos;
        }

        #endregion

        #region Callbacks

        private void OnLevelStarted(LevelStartedEvent data)
        {
            _currentLevelData = data.levelData;
            GenerateGrid();
        }

        #endregion
    }
}