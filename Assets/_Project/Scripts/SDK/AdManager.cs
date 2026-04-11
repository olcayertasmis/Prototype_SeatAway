using UnityEngine;
using PSA.Core;
using PSA.Gameplay;

namespace PSA.SDK
{
    /// <summary>
    /// STUB: Handles monetization SDKs
    /// </summary>
    public class AdManager : MonoBehaviour, ISystem
    {
        #region ISystem Implementation

        public void Initialize()
        {
            SystemLocator.Register(this);

            var eventManager = SystemLocator.Get<EventManager>();
            eventManager.AddListener<LevelCompletedEvent>(OnLevelCompleted);
        }

        #endregion

        #region Callbacks

        private void OnLevelCompleted(LevelCompletedEvent data)
        {
            // Fires at the end of a level (Win or Lose). 
            // In a real scenario, I would add a cooldown check here so players aren't spammed with ads every retry.
            ShowInterstitial();
        }

        #endregion

        #region Core Logic

        public void ShowInterstitial()
        {
            Debug.Log("<color=magenta>[AdManager] Showing Interstitial Ad...</color>");
        }

        #endregion
    }
}