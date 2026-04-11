using UnityEngine;
using PSA.Core;
using PSA.Gameplay;

namespace PSA.SDK
{
    /// <summary>
    /// STUB: Handles sending telemetry and progression data to the analytics provider
    /// </summary>
    public class AnalyticsManager : MonoBehaviour, ISystem
    {
        #region ISystem Implementation

        public void Initialize()
        {
            SystemLocator.Register(this);

            var eventManager = SystemLocator.Get<EventManager>();
            eventManager.AddListener<LevelStartedEvent>(OnLevelStarted);
            eventManager.AddListener<LevelCompletedEvent>(OnLevelCompleted);
        }

        #endregion

        #region Callbacks

        private void OnLevelStarted(LevelStartedEvent data)
        {
            // Fires when a level begins. Used to track drop-off rates and funnel progression.
            LogEvent($"level_start_{data.levelData.LevelName}");
        }

        private void OnLevelCompleted(LevelCompletedEvent data)
        {
            // Fires when a level ends. Differentiates between win and fail states to balance level difficulty.
            string status = data.isVictory ? "win" : "fail";
            LogEvent($"level_complete_{status}");
        }

        #endregion

        #region Core Logic

        public void LogEvent(string eventName)
        {
            // Real Analytics SDK call here
            Debug.Log($"<color=cyan>[Analytics] Event Logged: {eventName}</color>");
        }

        #endregion
    }
}