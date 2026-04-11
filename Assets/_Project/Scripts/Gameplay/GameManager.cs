using System.Collections.Generic;
using PSA.Core;
using PSA.Gameplay.Data;
using UnityEngine;

namespace PSA.Gameplay
{
    public class GameManager : MonoBehaviour, ISystem
    {
        [Header("Levels")]
        [SerializeField] private List<LevelData> levels;

        [Header("Data & Controls")]
        private int _currentLevelIndex;

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (SystemLocator.TryGet(out EventManager eventManager))
            {
                eventManager.RemoveListener<RestartLevelEvent>(OnRestartLevel);
                eventManager.RemoveListener<NextLevelEvent>(OnNextLevel);
                eventManager.RemoveListener<AllPassengersSeatedEvent>(OnAllPassengersSeated);
                eventManager.RemoveListener<TimeUpEvent>(OnTimeUp);
            }

            SystemLocator.Deregister<GameManager>();
        }

        #endregion

        #region ISystem Implementation

        public void Initialize()
        {
            SystemLocator.Register(this);

            var eventManager = SystemLocator.Get<EventManager>();
            eventManager.AddListener<RestartLevelEvent>(OnRestartLevel);
            eventManager.AddListener<NextLevelEvent>(OnNextLevel);
            eventManager.AddListener<AllPassengersSeatedEvent>(OnAllPassengersSeated);
            eventManager.AddListener<TimeUpEvent>(OnTimeUp);
        }

        #endregion

        #region Core Logic

        public void StartGame()
        {
            PublishLevelData();
        }

        private void PublishLevelData()
        {
            if (levels.Count == 0) return;
            LevelData data = levels[_currentLevelIndex];

            SystemLocator.Get<EventManager>().TriggerEvent(new LevelStartedEvent { levelData = data });
        }

        #endregion

        #region Event Callbacks

        private void OnRestartLevel(RestartLevelEvent data)
        {
            PublishLevelData();
        }

        private void OnNextLevel(NextLevelEvent data)
        {
            _currentLevelIndex++;
            if (_currentLevelIndex >= levels.Count)
            {
                Debug.Log("<color=cyan>ALL LEVELS COMPLETED!</color>");
                _currentLevelIndex = 0;
            }

            PublishLevelData();
        }

        private void OnAllPassengersSeated(AllPassengersSeatedEvent data)
        {
            SystemLocator.Get<EventManager>().TriggerEvent(new LevelCompletedEvent { isVictory = true });
            Debug.Log("<color=yellow>LEVEL COMPLETED! VICTORY!</color>");
        }

        private void OnTimeUp(TimeUpEvent data)
        {
            SystemLocator.Get<EventManager>().TriggerEvent(new LevelCompletedEvent { isVictory = false });
            Debug.Log("<color=red>GAME OVER! TIME UP!</color>");
        }

        #endregion
    }
}