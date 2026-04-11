using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PSA.Core;

namespace PSA.Gameplay.UI
{
    public class UIManager : MonoBehaviour, ISystem
    {
        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Slider timerSlider;
        [SerializeField] private Image sliderFillImage;

        [Header("End Game Panel")]
        [SerializeField] private GameObject endGamePanel;
        [SerializeField] private TextMeshProUGUI endGameTitleText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button nextLevelButton;

        [Header("References")]
        private TimerManager _timerManager;

        [Header("Controls")]
        private bool _isGameEnded;
        private bool _isPulsing;

        [Header("Data")]
        private Color _originalTimerColor;
        private Color _originalSliderColor;
        private Tween _pulseTween;

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (SystemLocator.TryGet(out EventManager eventManager))
            {
                eventManager.RemoveListener<LevelCompletedEvent>(OnLevelCompleted);
                eventManager.RemoveListener<LevelStartedEvent>(OnLevelStarted);
            }

            _pulseTween?.Kill();

            SystemLocator.Deregister<UIManager>();
        }

        private void Update()
        {
            UpdateTimerDisplay();
        }

        #endregion

        #region ISystem Implementation

        public void Initialize()
        {
            SystemLocator.Register(this);

            var eventManager = SystemLocator.Get<EventManager>();
            eventManager.AddListener<LevelCompletedEvent>(OnLevelCompleted);
            eventManager.AddListener<LevelStartedEvent>(OnLevelStarted);

            _timerManager = SystemLocator.Get<TimerManager>();

            _originalTimerColor = timerText.color;
            _originalSliderColor = sliderFillImage.color;

            endGamePanel.SetActive(false);
            _isGameEnded = false;
            _isPulsing = false;

            restartButton.onClick.AddListener(() => SystemLocator.Get<EventManager>().TriggerEvent(new RestartLevelEvent()));
            nextLevelButton.onClick.AddListener(() => SystemLocator.Get<EventManager>().TriggerEvent(new NextLevelEvent()));
        }

        #endregion

        #region Core Logic

        private void UpdateTimerDisplay()
        {
            if (_isGameEnded || !_timerManager.IsRunning) return;

            float time = _timerManager.CurrentTime;
            float startingTime = _timerManager.StartingTime;

            int minutes = Mathf.FloorToInt(time / 60F);
            int seconds = Mathf.FloorToInt(time - minutes * 60);

            timerText.text = $"{minutes:00}:{seconds:00}";

            timerSlider.value = time / startingTime;

            if (time <= 10f)
            {
                if (!_isPulsing)
                {
                    _isPulsing = true;

                    if (timerText.color != Color.red) timerText.color = Color.red;
                    if (sliderFillImage.color != Color.red) sliderFillImage.color = Color.red;

                    _pulseTween?.Kill();
                    _pulseTween = timerText.transform.DOScale(1.5f, 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
                }
            }
            else
            {
                if (_isPulsing) ResetTimerVisuals();
            }
        }

        private void ResetTimerVisuals()
        {
            _isPulsing = false;
            _pulseTween?.Kill();

            if (timerText)
            {
                if (timerText.color != _originalTimerColor) timerText.color = _originalTimerColor;
                if (timerText.transform.localScale != Vector3.one) timerText.transform.localScale = Vector3.one;
            }

            if (sliderFillImage.color != _originalSliderColor) sliderFillImage.color = _originalSliderColor;
        }

        #endregion

        #region Event Callbacks

        private void OnLevelStarted(LevelStartedEvent data)
        {
            _isGameEnded = false;
            endGamePanel.SetActive(false);

            ResetTimerVisuals();
        }

        private void OnLevelCompleted(LevelCompletedEvent data)
        {
            _isGameEnded = true;
            endGamePanel.SetActive(true);

            ResetTimerVisuals();

            if (data.isVictory)
            {
                endGameTitleText.text = "LEVEL COMPLETED!";
                endGameTitleText.color = Color.green;

                restartButton.gameObject.SetActive(false);
                nextLevelButton.gameObject.SetActive(true);
            }
            else
            {
                endGameTitleText.text = "TIME IS UP!";
                endGameTitleText.color = Color.red;

                restartButton.gameObject.SetActive(true);
                nextLevelButton.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}