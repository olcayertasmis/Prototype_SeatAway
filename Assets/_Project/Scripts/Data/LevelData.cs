using System;
using System.Collections.Generic;
using UnityEngine;

namespace PSA.Gameplay.Data
{
    public enum CellType
    {
        Empty,
        Obstacle,
        //MovableSeat,
        //StaticSeat,
        Entrance
    }

    public enum ElementColor
    {
        Blue,
        Green,
        Red,
        Yellow,
        //Transparent
    }

    [Serializable]
    public struct CellData
    {
        public Vector2Int coordinates;
        public CellType cellType;
    }

    [Serializable]
    public struct SeatData
    {
        public Vector2Int rootCoordinate;
        public int width;
        public int height;
        public ElementColor color;
        public bool isMovable;
    }

    [CreateAssetMenu(fileName = "NewLevelData", menuName = "ScriptableObjects/LevelData")]
    public class LevelData : ScriptableObject
    {
        [Header("Level Info")]
        [SerializeField] private string levelName = "New Level";
        [SerializeField] private float timeLimit = 60f;

        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 5;
        [SerializeField] private int gridHeight = 5;

        [Header("Grid Cells")]
        [SerializeField] private List<CellData> gridCells = new();
        [SerializeField] private List<SeatData> seats = new();
        [SerializeField] private List<ElementColor> passengerQueue = new();

        [Header("Entrance Settings")]
        [SerializeField] private Vector2Int entranceCoordinate;
        [SerializeField] private Vector3 queueDirection = Vector3.right;

        #region Encapsulation

        public string LevelName => levelName;
        public float TimeLimit => timeLimit;
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public IReadOnlyList<CellData> GridCells => gridCells;
        public IReadOnlyList<SeatData> Seats => seats;
        public IReadOnlyList<ElementColor> PassengerQueue => passengerQueue;
        public Vector2Int EntranceCoordinate => entranceCoordinate;
        public Vector3 QueueDirection => queueDirection;

        #endregion
    }
}