using System;
using UnityEngine;

namespace LittleHeroJourney
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(CanvasManager))]
    [RequireComponent(typeof(LoadingManager))]
    [RequireComponent(typeof(SceneManager))]
    public class GameManager : MonoBehaviour
    {
        #region Singleton

        public static GameManager Instance { get; private set; }

        #endregion

        #region Manager References

        // CanvasManager is a required component on this same GameObject
        public CanvasManager CanvasManager => GetComponent<CanvasManager>();
        public LoadingManager LoadingManager => GetComponent<LoadingManager>();
        public SceneManager SceneManager => GetComponent<SceneManager>();

        #endregion

        #region Game State

        private bool isPaused = false;

        public bool IsPaused => isPaused;

        #endregion

        #region Debug

        [Header("Debug")]
        public bool showDebugLog = false;

        #endregion

        #region Events

        public delegate void GameStateChanged();
        public static event GameStateChanged OnGamePaused;
        public static event GameStateChanged OnGameResumed;
        public static event GameStateChanged OnGameOver;
        public static event GameStateChanged OnGameWin;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton pattern with proper cleanup
            if (Instance != null && Instance != this)
            {
                if (showDebugLog)
                    Debug.Log($"[{GetType().Name}] Destroying duplicate GameManager instance");

                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Initialized as singleton. CanvasManager is on same GameObject.");
        }

        private void Start()
        {
            if (SceneManager != null)
            {
                SceneManager.StartFlow();
            }
        }

        #endregion

        #region Game State Management

        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (isPaused)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Game is already paused!");
                return;
            }

            isPaused = true;
            Time.timeScale = 0f;

            OnGamePaused?.Invoke();

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Game paused");
        }

        public void ResumeGame()
        {
            if (!isPaused)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Game is not paused!");
                return;
            }

            isPaused = false;
            Time.timeScale = 1f;

            OnGameResumed?.Invoke();

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Game resumed");
        }

        public void TogglePause()
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        public void TriggerGameOver()
        {
            OnGameOver?.Invoke();

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Game over event triggered");
        }

        public void TriggerGameWin()
        {
            OnGameWin?.Invoke();

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Game win event triggered");
        }

        /// <summary>
        /// Load next level dari level saat ini
        /// </summary>
        public void LoadNextLevel()
        {
            if (LevelManager.Instance == null)
            {
                if (showDebugLog) Debug.LogWarning("[GameManager] LevelManager not found!");
                return;
            }

            int nextLevelNumber = LevelManager.Instance.GetCurrentLevelNumber() + 1;
            LoadLevel(nextLevelNumber);
        }

        /// <summary>
        /// Retry level saat ini
        /// </summary>
        public void RetryLevel()
        {
            if (LevelManager.Instance == null)
            {
                if (showDebugLog) Debug.LogWarning("[GameManager] LevelManager not found!");
                return;
            }

            int currentLevelNumber = LevelManager.Instance.GetCurrentLevelNumber();
            LoadLevel(currentLevelNumber);
        }

        /// <summary>
        /// Load level berdasarkan number
        /// </summary>
        public void LoadLevel(int levelNumber)
        {
            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Loading level {levelNumber}");

            Time.timeScale = 1f; // Resume time
            ResetGameState();
            
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LoadLevel(levelNumber);
            }
        }

        /// <summary>
        /// Kembali ke main menu
        /// </summary>
        public void ReturnToMainMenu()
        {
            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Returning to Main Menu");

            Time.timeScale = 1f; // Resume time
            ResetGameState();
            if (SceneManager != null)
            {
                SceneManager.ReturnToMainMenu();
            }
        }

        public void ResetGameState()
        {
            isPaused = false;
            Time.timeScale = 1f;

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Game state reset");
        }

        #endregion

        #region Player Management

        #endregion

        #region Application Control

        /// <summary>
        /// Exit the game application
        /// </summary>
        public void ExitGame()
        {
            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Exiting game...");

            Time.timeScale = 1f; // Reset time scale

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Scene Management
        // SceneManager handles transitions

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void sawit()
        {
            Application.targetFrameRate = 60;
        }

        #endregion

    }
}
