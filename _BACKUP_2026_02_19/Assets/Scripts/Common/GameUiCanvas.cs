using System;
using UnityEngine;
using UnityEngine.UI;
using Common.Interfaces;
using Match3.App;
using Match3.App.Interfaces;

namespace Common
{
    public class GameUiCanvas : MonoBehaviour, IGameUiCanvas
    {
        public static GameUiCanvas Instance { get; private set; }

        // IGameUiCanvas Implementation (Dummy)
        public int SelectedIconsSetIndex => 0;
        public int SelectedFillStrategyIndex => 0;
        public event EventHandler StartGameClick;
        public event EventHandler<int> StrategyChanged;
        public void ShowMessage(string message) { Debug.Log($"UI Message: {message}"); }
        public void RegisterAchievedGoal(LevelGoal<IUnityGridSlot> achievedGoal) { }

        [Header("Panels")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _gameHudPanel;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private GameObject _settingsPanel;

        [Header("Buttons")]
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _homeButton; // In Game Over
        [SerializeField] private Button _settingsButton; // Main Menu
        [SerializeField] private Button _closeSettingsButton;

        public event Action OnStartGame;
        public event Action OnRestartGame;
        public event Action OnGoHome;

        private void Awake()
        {
            Instance = this;
            
            // Listeners
            if (_startGameButton) _startGameButton.onClick.AddListener(() => OnStartGame?.Invoke());
            if (_restartButton)   _restartButton.onClick.AddListener(() => OnRestartGame?.Invoke());
            if (_homeButton)      _homeButton.onClick.AddListener(() => OnGoHome?.Invoke());
            
            if (_settingsButton)      _settingsButton.onClick.AddListener(() => ShowSettings(true));
            if (_closeSettingsButton) _closeSettingsButton.onClick.AddListener(() => ShowSettings(false));
        }

        public void ShowMainMenu()
        {
            _mainMenuPanel.SetActive(true);
            _gameHudPanel.SetActive(false);
            _gameOverPanel.SetActive(false);
            _settingsPanel.SetActive(false);
        }

        public void ShowGame()
        {
            _mainMenuPanel.SetActive(false);
            _gameHudPanel.SetActive(true);
            _gameOverPanel.SetActive(false);
            _settingsPanel.SetActive(false);
        }

        public void ShowGameOver()
        {
            _gameOverPanel.SetActive(true);
            _settingsPanel.SetActive(false);
        }

        [Header("Settings")]
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Toggle _vibrationToggle;

        private void Start()
        {
            // Init Toggles from Audio Manager
             if (Common.Audio.AudioManager.Instance != null)
             {
                 if (_sfxToggle) _sfxToggle.isOn = Common.Audio.AudioManager.Instance.IsSfxEnabled;
                 if (_vibrationToggle) _vibrationToggle.isOn = Common.Audio.AudioManager.Instance.IsVibEnabled;
             }

             if (_sfxToggle) _sfxToggle.onValueChanged.AddListener(val => Common.Audio.AudioManager.Instance?.ToggleSfx(val));
             if (_vibrationToggle) _vibrationToggle.onValueChanged.AddListener(val => Common.Audio.AudioManager.Instance?.ToggleVibration(val));
        }

        public void ShowSettings(bool show)
        {
            _settingsPanel.SetActive(show);
        }
    }
}
