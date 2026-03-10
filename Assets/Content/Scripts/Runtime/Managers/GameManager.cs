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

        public bool HasAnyJourneySave => JourneyManager.HasAnySave();

        #endregion

        #region Debug

        [Header("Debug")]
        public bool showDebugLog = false;

        #endregion

        private Action _exitGameHandler;
        private Action _saveGameHandler;

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

        private void OnEnable()
        {
            _exitGameHandler = ExitGame;
            _saveGameHandler = SaveGame;
            GameEventSystem.SubscribeAction("ExitGame", _exitGameHandler);
            GameEventSystem.SubscribeAction("Save", _saveGameHandler);
        }

        private void OnDisable()
        {
            GameEventSystem.UnsubscribeAction("ExitGame", _exitGameHandler);
            GameEventSystem.UnsubscribeAction("Save", _saveGameHandler);
        }

        #endregion

        #region Game State (time scale, reset only; gameplay = GameplayManager)

        public void ResetGameState()
        {
            Time.timeScale = 1f;
            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Game state reset");
        }

        #endregion

        #region Application Control (exit, save, time scale)

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

        public void SaveGame()
        {
            if (JourneyManager.Instance != null)
                JourneyManager.Instance.SaveProgress();
            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Game saved.");
        }

        public void SetTimeScale(float scale)
        {
            Time.timeScale = Mathf.Clamp(scale, 0f, float.MaxValue);
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
