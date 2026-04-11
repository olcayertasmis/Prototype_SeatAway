using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using PSA.Core;
using PSA.Gameplay.Data;
using PSA.Gameplay.Grid;
using PSA.Gameplay.Seats;

namespace PSA.Gameplay.Passengers
{
    public class PassengerManager : MonoBehaviour, ISystem
    {
        [Header("Settings")]
        [SerializeField] private Passenger passengerPrefab;
        [SerializeField] private Transform passengerContainer;
        [SerializeField] private float queueSpacing;
        [SerializeField] private float passengerOffsetY;

        [Header("Data")]
        private Queue<ElementColor> _passengerQueue;
        private readonly List<Passenger> _waitingLine = new();
        private Cell _entranceCell;

        private int _totalPassengers;
        private int _seatedPassengers;
        private readonly List<Passenger> _activePassengers = new();

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (SystemLocator.TryGet(out EventManager eventManager))
            {
                eventManager.RemoveListener<GridUpdatedEvent>(OnGridUpdated);
                eventManager.RemoveListener<PassengerSeatedEvent>(OnPassengerSeated);
                eventManager.RemoveListener<LevelStartedEvent>(OnLevelStarted);
                eventManager.RemoveListener<GridReadyEvent>(OnGridReady);
                eventManager.RemoveListener<LevelCompletedEvent>(OnLevelEnd);
            }

            SystemLocator.Deregister<PassengerManager>();
        }

        #endregion

        #region ISystemImplementation

        public void Initialize()
        {
            SystemLocator.Register(this);

            var eventManager = SystemLocator.Get<EventManager>();
            eventManager.AddListener<GridUpdatedEvent>(OnGridUpdated);
            eventManager.AddListener<PassengerSeatedEvent>(OnPassengerSeated);
            eventManager.AddListener<LevelStartedEvent>(OnLevelStarted);
            eventManager.AddListener<GridReadyEvent>(OnGridReady);
            eventManager.AddListener<LevelCompletedEvent>(OnLevelEnd);
        }

        #endregion

        #region Core Logic

        private void BuildPassengerLine()
        {
            if (!_entranceCell) return;

            Vector3 spawnDirection = SystemLocator.Get<GridManager>().CurrentLevelData.QueueDirection;
            int index = 0;

            PoolManager poolManager = SystemLocator.Get<PoolManager>();

            while (_passengerQueue.Count > 0)
            {
                ElementColor color = _passengerQueue.Dequeue();
                //Passenger newPassenger = Instantiate(passengerPrefab, passengerContainer);

                Vector3 queuePosition = _entranceCell.transform.position + (spawnDirection * index * queueSpacing);
                queuePosition.y += passengerOffsetY;

                Passenger newPassenger = poolManager.Spawn<Passenger>(PoolType.Passenger, queuePosition, Quaternion.identity, passengerContainer);
                newPassenger.Initialize(_entranceCell, color);

                newPassenger.transform.position = queuePosition;

                _waitingLine.Add(newPassenger);
                _activePassengers.Add(newPassenger);
                index++;
            }

            CheckForAvailablePaths();
        }

        private void CheckForAvailablePaths()
        {
            if (_waitingLine.Count == 0) return;

            Passenger headPassenger = _waitingLine[0];

            Pathfinder pathfinder = SystemLocator.Get<Pathfinder>();

            foreach (Seat targetSeat in Seat.AllSeats)
            {
                if (!targetSeat.IsMovable || targetSeat.IsHeld || !targetSeat.HasEmptySlot || targetSeat.Color != headPassenger.Color) continue;

                List<Cell> availableSlots = targetSeat.GetAvailableSlots();
                if (availableSlots.Count == 0) continue;

                List<Cell> path = pathfinder.FindPath(_entranceCell.Coordinate, availableSlots);

                if (path != null && path.Count > 0)
                {
                    Cell reachedSlot = path[path.Count - 1];

                    targetSeat.ReserveSlot(reachedSlot);
                    headPassenger.MoveAlongPath(path, targetSeat, reachedSlot);

                    _waitingLine.RemoveAt(0);
                    UpdateLinePositions();
                    break;
                }
            }
        }

        private void UpdateLinePositions()
        {
            Vector3 spawnDirection = SystemLocator.Get<GridManager>().CurrentLevelData.QueueDirection;

            for (int i = 0; i < _waitingLine.Count; i++)
            {
                Vector3 nextQueuePosition = _entranceCell.transform.position + (spawnDirection * (i) * queueSpacing);
                nextQueuePosition.y += passengerOffsetY;

                _waitingLine[i].transform.DOMove(nextQueuePosition, 0.3f).SetEase(Ease.OutQuad);
            }
        }

        private void ClearPassengers()
        {
            _waitingLine.Clear();

            if (SystemLocator.TryGet(out PoolManager poolManager))
            {
                foreach (var passenger in _activePassengers)
                {
                    if (passenger.gameObject.activeSelf)
                    {
                        poolManager.Despawn(PoolType.Passenger, passenger.gameObject);
                    }
                }
            }

            _activePassengers.Clear();
        }

        #endregion

        #region Callbacks

        private void OnLevelStarted(LevelStartedEvent data)
        {
            ClearPassengers();

            _passengerQueue = new Queue<ElementColor>(data.levelData.PassengerQueue);
            _totalPassengers = data.levelData.PassengerQueue.Count;
            _seatedPassengers = 0;
            _entranceCell = null;
        }

        private void OnGridReady(GridReadyEvent data)
        {
            var gridManager = SystemLocator.Get<GridManager>();
            LevelData currentData = gridManager.CurrentLevelData;

            _entranceCell = gridManager.GetCellAt(currentData.EntranceCoordinate);

            if (!_entranceCell)
            {
                Debug.LogError("<color=red>[PassengerManager] Entrance cell could not be found! Check LevelData.</color>");
                return;
            }

            BuildPassengerLine();
        }

        private void OnPassengerSeated(PassengerSeatedEvent data)
        {
            _seatedPassengers++;

            if (_seatedPassengers >= _totalPassengers)
            {
                SystemLocator.Get<EventManager>().TriggerEvent(new AllPassengersSeatedEvent());
            }
            else
            {
                CheckForAvailablePaths();
            }
        }

        private void OnGridUpdated(GridUpdatedEvent data)
        {
            CheckForAvailablePaths();
        }

        private void OnLevelEnd(LevelCompletedEvent data)
        {
            foreach (var passenger in _activePassengers)
            {
                if (passenger && passenger.gameObject.activeSelf)
                {
                    passenger.StopMovement();
                }
            }
        }

        #endregion
    }
}