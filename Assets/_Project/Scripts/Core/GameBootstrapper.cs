using System.Collections;
using System.Collections.Generic;
using PSA.Gameplay;
using PSA.Gameplay.Grid;
using PSA.Gameplay.Passengers;
using PSA.Gameplay.UI;
using PSA.SDK;
using UnityEngine;

namespace PSA.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private EventManager eventManager;
        [SerializeField] private PoolManager poolManager;
        [SerializeField] private TimerManager timerManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Pathfinder pathfinder;
        [SerializeField] private PassengerManager passengerManager;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AnalyticsManager analyticsManager;
        [SerializeField] private AdManager adManager;

        #region Unity Lifecycle

        private void Awake()
        {
            List<ISystem> systems = new()
            {
                eventManager, poolManager, timerManager, gameManager, pathfinder, passengerManager, gridManager, uiManager, analyticsManager, adManager
            };

            foreach (var system in systems)
            {
                if (system != null) InitSystem(system);
            }

            //SystemLocator.Get<GameManager>().StartGame();

            /*InitSystem(eventManager);
            InitSystem(gameManager);
            InitSystem(gridManager);
            InitSystem(pathfinder);
            InitSystem(passengerManager);*/
        }

        private IEnumerator Start()
        {
            yield return null;

            SystemLocator.Get<GameManager>().StartGame();
        }

        #endregion

        #region Initialization

        private void InitSystem(ISystem system)
        {
            if (system == null)
            {
                Debug.Log("system is null, not registered");
                return;
            }

            system.Initialize();

            Debug.Log($"<color=#00FF00>[{system.GetType().Name}]</color> Initialized successfully.");
        }

        #endregion
    }
}