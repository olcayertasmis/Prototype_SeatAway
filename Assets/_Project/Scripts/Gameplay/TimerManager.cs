using UnityEngine;
using PSA.Core;

namespace PSA.Gameplay
{
    public class TimerManager : MonoBehaviour, ISystem
    {
        [Header("Data")]
        private float _currentTime;
        private float _startingTime;

        [Header("Control")]
        private bool _isRunning;

        #region Encapsulation

        public float CurrentTime => _currentTime;
        public bool IsRunning => _isRunning;
        public float StartingTime => _startingTime;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (SystemLocator.TryGet(out EventManager eventManager))
            {
                eventManager.RemoveListener<LevelStartedEvent>(OnLevelStarted);
            }

            SystemLocator.Deregister<TimerManager>();
        }

        private void Update()
        {
            TickTimer();
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

        private void StartTimer(float duration)
        {
            _startingTime = duration;
            _currentTime = duration;
            _isRunning = true;
        }

        public void StopTimer()
        {
            _isRunning = false;
        }

        private void TickTimer()
        {
            if (!_isRunning) return;

            _currentTime -= Time.deltaTime;
            if (_currentTime <= 0)
            {
                _currentTime = 0;
                _isRunning = false;
                SystemLocator.Get<EventManager>().TriggerEvent(new TimeUpEvent());
            }
        }

        #endregion

        #region Callbacks

        private void OnLevelStarted(LevelStartedEvent data)
        {
            StartTimer(data.levelData.TimeLimit);
        }

        #endregion
    }
}