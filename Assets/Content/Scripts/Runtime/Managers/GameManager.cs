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

        public CanvasManager CanvasManager => GetComponent<CanvasManager>();
        public LoadingManager LoadingManager => GetComponent<LoadingManager>();
        public SceneManager SceneManager => GetComponent<SceneManager>();

        #endregion

        #region Game State

        private bool isPaused = false;

        public bool IsPaused => isPaused;

        public bool HasAnyJourneySave => JourneyManager.HasAnySave();

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

        public void LoadNextLevel()
        {
            if (JourneyManager.Instance == null)
            {
                if (showDebugLog) Debug.LogWarning("[GameManager] JourneyManager not found!");
                return;
            }

            int nextLevelNumber = JourneyManager.Instance.GetCurrentLevelNumber() + 1;
            LoadLevel(nextLevelNumber);
        }

        public void RetryLevel()
        {
            if (JourneyManager.Instance == null)
            {
                if (showDebugLog) Debug.LogWarning("[GameManager] JourneyManager not found!");
                return;
            }

            int currentLevelNumber = JourneyManager.Instance.GetCurrentLevelNumber();
            LoadLevel(currentLevelNumber);
        }

        public void LoadLevel(int levelNumber)
        {
            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Loading level {levelNumber}");

            Time.timeScale = 1f; // Resume time
            ResetGameState();
            
            if (JourneyManager.Instance != null)
            {
                JourneyManager.Instance.LoadStage(levelNumber);
            }
        }

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

        public void ExitGame()
        {
            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Exiting game...");

            Time.timeScale = 1f;

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Scene Management

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeFrameRate()
        {
            Application.targetFrameRate = 60;
        }

        #endregion

    }
}
