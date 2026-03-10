using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LittleHeroJourney.UI;
using Cinemachine;

namespace LittleHeroJourney
{
    public class GameplayManager : MonoBehaviour, ISceneLoadProgress
    {
        #region Fields
        [Header("Runtime References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CinemachineFreeLook currentCamera;
        [SerializeField] private PlayerMovementController playerController;
        [SerializeField] private List<EncounterZone> encounterZones = new List<EncounterZone>();

        [Header("Settings")]
        [Tooltip("Delay before starting gameplay after loading screen is gone")]
        [SerializeField] private float startDelay = 0.5f;
        [SerializeField] private string gameplayCanvasId = "Gameplay";
        [SerializeField] private string pauseCanvasId = "Pause";
        [SerializeField] private string winCanvasId = "Win";
        [SerializeField] private string loseCanvasId = "Lose";

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = true;

        private bool isLevelEnded = false;
        private bool isInputActive = false;
        private bool isPaused = false;

        public static GameplayManager Instance { get; private set; }
        public bool IsInputActive => isInputActive;
        public bool IsPaused => isPaused;
        public Camera MainCamera => mainCamera;
        public CinemachineFreeLook CurrentCamera => currentCamera;
        public PlayerMovementController PlayerController => playerController;
        public List<EncounterZone> EncounterZones => encounterZones;

        public static event System.Action<CinemachineFreeLook> OnCameraInitialized;
        public static event System.Action<PlayerMovementController> OnPlayerInitialized;
        public static event System.Action<List<EncounterZone>> OnEncounterZonesInitialized;

        #endregion
        
        private bool _sceneReady = false;
        public string SceneId => "Gameplay";
        public bool IsReady => _sceneReady;
        public float Progress => _sceneReady ? 1f : 0f;
        public event System.Action OnReady;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) 
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            SetInputActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            FindReferences();
        }

        private void OnEnable()
        {
            if (playerController != null)
            {
                var health = playerController.GetComponent<Health>();
                if (health != null) health.OnDeath += HandlePlayerDeath;
            }

            LoadingManager.OnLoadingFinished += HandleLoadingFinished;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
            EventBus.Subscribe<GamePausedEvent>(HandleGamePausedEvent);
            EventBus.Subscribe<GameResumedEvent>(HandleGameResumedEvent);
            EventBus.Subscribe<GameOverEvent>(HandleGameOverEvent);
            EventBus.Subscribe<GameWinEvent>(HandleGameWinEvent);
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                var health = playerController.GetComponent<Health>();
                if (health != null) health.OnDeath -= HandlePlayerDeath;
            }

            if (encounterZones != null)
            {
                foreach (var zone in encounterZones)
                {
                    if (zone != null)
                    {
                        zone.OnEncounterCompleted -= CheckAllEncountersCompleted;
                    }
                }
            }

            LoadingManager.OnLoadingFinished -= HandleLoadingFinished;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
            EventBus.Unsubscribe<GamePausedEvent>(HandleGamePausedEvent);
            EventBus.Unsubscribe<GameResumedEvent>(HandleGameResumedEvent);
            EventBus.Unsubscribe<GameOverEvent>(HandleGameOverEvent);
            EventBus.Unsubscribe<GameWinEvent>(HandleGameWinEvent);
        }

        #endregion

        #region Level Flow

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindReferences();
        }

        private void HandleLoadingFinished(string loadedSceneName)
        {
            if (string.IsNullOrEmpty(loadedSceneName)) return;
            if (loadedSceneName != "MainMenu" && loadedSceneName != "Loading")
            {
                StartCoroutine(StartLevelSequence());
            }
        }

        private void HandleGamePausedEvent(GamePausedEvent _)
        {
            if (GameManager.Instance != null && GameManager.Instance.CanvasManager != null && !string.IsNullOrEmpty(pauseCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(pauseCanvasId);
        }

        private void HandleGameResumedEvent(GameResumedEvent _)
        {
            if (GameManager.Instance != null && GameManager.Instance.CanvasManager != null && !string.IsNullOrEmpty(gameplayCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(gameplayCanvasId);
        }

        private void HandleGameOverEvent(GameOverEvent _)
        {
            if (GameManager.Instance != null && GameManager.Instance.CanvasManager != null && !string.IsNullOrEmpty(loseCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(loseCanvasId);
        }

        private void HandleGameWinEvent(GameWinEvent _)
        {
            if (GameManager.Instance != null && GameManager.Instance.CanvasManager != null && !string.IsNullOrEmpty(winCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(winCanvasId);
        }

        private void FindReferences()
        {
            FindMainCamera();
            FindCinemachineCamera();
            FindPlayer();
            FindEncounterZones();
            if (showDebugLog) Debug.Log("[GameplayManager] FindReferences completed.");
            _sceneReady = true;
            OnReady?.Invoke();
        }

        private void FindMainCamera()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null)
            {
                if (showDebugLog) Debug.LogWarning("[GameplayManager] Main Camera NOT found via Camera.main.");
                var camObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (camObj != null) mainCamera = camObj.GetComponent<Camera>();
            }
        }

        private void FindCinemachineCamera()
        {
            if (currentCamera == null) currentCamera = FindObjectOfType<CinemachineFreeLook>();
            if (currentCamera != null)
            {
                if (showDebugLog) Debug.Log("[GameplayManager] Found Cinemachine Camera. Broadcasting event.");
                OnCameraInitialized?.Invoke(currentCamera);
            }
            else if (showDebugLog) Debug.LogWarning("[GameplayManager] Cinemachine Camera NOT found in this scene.");
        }

        private void FindPlayer()
        {
            if (playerController == null) playerController = FindObjectOfType<PlayerMovementController>();
            if (playerController != null)
            {
                if (showDebugLog) Debug.Log("[GameplayManager] Found Player Controller. Broadcasting event.");
                OnPlayerInitialized?.Invoke(playerController);
                var health = playerController.GetComponent<Health>();
                if (health != null)
                {
                    health.OnDeath -= HandlePlayerDeath;
                    health.OnDeath += HandlePlayerDeath;
                    InitializePlayerHealthBar(health);
                }
            }
            else if (showDebugLog) Debug.LogWarning("[GameplayManager] Player Controller NOT found in this scene.");
        }

        private void FindEncounterZones()
        {
            if (encounterZones == null || encounterZones.Count == 0)
            {
                var zones = FindObjectsOfType<EncounterZone>();
                if (zones != null && zones.Length > 0) encounterZones = new List<EncounterZone>(zones);
            }
            if (encounterZones != null && encounterZones.Count > 0)
            {
                if (showDebugLog) Debug.Log($"[GameplayManager] Found {encounterZones.Count} Encounter Zones. Broadcasting event.");
                OnEncounterZonesInitialized?.Invoke(encounterZones);
                foreach (var zone in encounterZones)
                {
                    if (zone != null)
                    {
                        zone.OnEncounterCompleted -= CheckAllEncountersCompleted;
                        zone.OnEncounterCompleted += CheckAllEncountersCompleted;
                    }
                }
            }
        }

        private void InitializePlayerHealthBar(Health playerHealth)
        {
            if (playerHealth == null) return;
            
            var gameplayCanvases = FindObjectsOfType<GameCanvas>(true);
            GameCanvas gameplayCanvas = null;
            
            foreach (var canvas in gameplayCanvases)
            {
                if (canvas.ID == "Gameplay")
                {
                    gameplayCanvas = canvas;
                    break;
                }
            }
            
            if (gameplayCanvas == null)
            {
                if (showDebugLog) Debug.LogWarning("[GameplayManager] Gameplay Canvas NOT found for player health bar setup");
                return;
            }
            
            var characterBarsConnector = gameplayCanvas.GetComponentInChildren<UI.CharacterBarsConnector>(true);
            
            if (characterBarsConnector != null)
            {
                characterBarsConnector.InitializeForTargets(playerHealth, playerHealth.GetComponent<StunManager>());
                if (showDebugLog) Debug.Log("[GameplayManager] Player health bar initialized successfully");
            }
            else
            {
                if (showDebugLog) Debug.LogWarning("[GameplayManager] CharacterBarsConnector NOT found in Gameplay Canvas");
            }
        }

        private IEnumerator StartLevelSequence()
        {
            if (showDebugLog) Debug.Log("[GameplayManager] Loading Finished -> Starting Level Sequence...");

            isLevelEnded = false;
            SetInputActive(false);

            yield return null;

            if (GameManager.Instance != null && GameManager.Instance.CanvasManager != null)
            {
                if (!string.IsNullOrEmpty(gameplayCanvasId))
                    GameManager.Instance.CanvasManager.SwitchCanvas(gameplayCanvasId); 
            }

            yield return new WaitForSeconds(startDelay);

            SetInputActive(true);
            
            if (showDebugLog) Debug.Log("[GameplayManager] Level Started. Input Active.");
        }

        private void CheckAllEncountersCompleted()
        {
            if (encounterZones == null || encounterZones.Count == 0)
                return;

            bool allCompleted = true;
            foreach (var zone in encounterZones)
            {
                if (zone == null || !zone.IsCompleted)
                {
                    allCompleted = false;
                    break;
                }
            }

            if (allCompleted)
            {
                if (showDebugLog) Debug.Log("[GameplayManager] All encounter zones completed -> Triggering Win!");
                TriggerLevelWin();
            }
        }

        public void TriggerGameOver()
        {
            if (isLevelEnded) return;
            if (showDebugLog) Debug.Log("[GameplayManager] Player Died -> Trigger Game Over");
            isLevelEnded = true;
            SetInputActive(false);
            EventBus.Publish(new GameOverEvent());
        }

        public void TriggerLevelWin()
        {
            if (isLevelEnded) return;
            if (showDebugLog) Debug.Log("[GameplayManager] Objective Reached -> Trigger Win");
            isLevelEnded = true;
            SetInputActive(false);
            if (JourneyManager.Instance != null)
            {
                int currentLevel = JourneyManager.Instance.GetCurrentLevelNumber();
                JourneyManager.Instance.CompleteLevel(currentLevel);
            }
            EventBus.Publish(new GameWinEvent());
        }

        #endregion

        #region Pause / Resume (gameplay; time scale via GameManager)

        public void PauseGame()
        {
            if (isPaused) return;
            isPaused = true;
            if (GameManager.Instance != null) GameManager.Instance.SetTimeScale(0f);
            EventBus.Publish(new GamePausedEvent());
            if (showDebugLog) Debug.Log("[GameplayManager] Game paused");
        }

        public void ResumeGame()
        {
            if (!isPaused) return;
            isPaused = false;
            if (GameManager.Instance != null) GameManager.Instance.SetTimeScale(1f);
            EventBus.Publish(new GameResumedEvent());
            if (showDebugLog) Debug.Log("[GameplayManager] Game resumed");
        }

        public void TogglePause()
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        #endregion

        #region Stage loading (gameplay; JourneyManager + GameManager reset)

        public void LoadNextStage()
        {
            if (JourneyManager.Instance == null) return;
            int next = JourneyManager.Instance.GetCurrentLevelNumber() + 1;
            LoadStage(next);
        }

        public void RetryStage()
        {
            if (JourneyManager.Instance == null) return;
            LoadStage(JourneyManager.Instance.GetCurrentLevelNumber());
        }

        public void LoadStage(int stageNumber)
        {
            if (showDebugLog) Debug.Log("[GameplayManager] Loading stage " + stageNumber);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetTimeScale(1f);
                GameManager.Instance.ResetGameState();
            }
            if (JourneyManager.Instance != null)
                JourneyManager.Instance.LoadStage(stageNumber);
        }

        #endregion

        #region Logic

        public void SetInputActive(bool active)
        {
            isInputActive = active;
        }

        private void HandlePlayerDeath()
        {
            TriggerGameOver();
        }

        #endregion
    }
}
