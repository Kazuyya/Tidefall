using System;
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
        [Tooltip("Leave empty to auto-find from active scene.")]
        [SerializeField] private Camera mainCamera;
        [Tooltip("Leave empty to auto-find from active scene.")]
        [SerializeField] private CinemachineFreeLook currentCamera;
        [Tooltip("Leave empty to auto-find from active scene (refreshed again after encounter start).")]
        [SerializeField] private PlayerMovementController playerController;

        [Header("Settings")]
        [Tooltip("Delay before starting gameplay after loading screen is gone")]
        [SerializeField] private float startDelay = 0.5f;
        [Tooltip("Delay after binding camera to player before signalling player ready (lets Cinemachine settle when story is skipped).")]
        [SerializeField] private float cameraSettleDelay = 0.8f;
        [Tooltip("Layer name of a child under player to use as Cinemachine Follow/LookAt (e.g. LookAtPlayer). If none found, uses player root.")]
        [SerializeField] private string cinemachineTargetLayerName = "LookAtPlayer";
        [SerializeField] private string gameplayCanvasId = "Gameplay";
        [SerializeField] private string pauseCanvasId = "Pause";
        [SerializeField] private string winCanvasId = "Win";
        [SerializeField] private string loseCanvasId = "Lose";

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = true;

        private bool isLevelEnded = false;
        private bool isInputActive = false;
        private bool isPaused = false;

        private Action _pauseHandler, _resumeHandler, _lockHandler;
        private Action _nextLevelHandler, _retryHandler, _gameOverHandler, _gameWinHandler, _encounterStartedHandler, _storySequenceCompletedHandler, _encounterZoneCompletedHandler, _playerSpawnedForEncounterHandler;
        private Action<string> _loadingFinishedHandler;

        public static GameplayManager Instance { get; private set; }
        public bool IsInputActive => isInputActive;
        public bool IsPaused => isPaused;
        public Camera MainCamera => mainCamera;
        public CinemachineFreeLook CurrentCamera => currentCamera;
        public PlayerMovementController PlayerController => playerController;
        public List<StoryEncounterSpawner> EncounterZones => _encounterZones;

        private List<StoryEncounterSpawner> _encounterZones = new List<StoryEncounterSpawner>();

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

            _loadingFinishedHandler = HandleLoadingFinished;
            GameEventSystem.SubscribeActionWithPayload("LoadingFinished", _loadingFinishedHandler);
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;

            _pauseHandler = PauseGame;
            _resumeHandler = ResumeGame;
            _lockHandler = HandleLockAction;
            _nextLevelHandler = LoadNextStage;
            _retryHandler = RetryStage;
            _gameOverHandler = () => { if (GameManager.Instance?.CanvasManager != null && !string.IsNullOrEmpty(loseCanvasId)) GameManager.Instance.CanvasManager.SwitchCanvas(loseCanvasId); };
            _gameWinHandler = () => { if (GameManager.Instance?.CanvasManager != null && !string.IsNullOrEmpty(winCanvasId)) GameManager.Instance.CanvasManager.SwitchCanvas(winCanvasId); };

            GameEventSystem.SubscribeAction("Pause", _pauseHandler);
            GameEventSystem.SubscribeAction("Resume", _resumeHandler);
            GameEventSystem.SubscribeAction("Lock", _lockHandler);
            GameEventSystem.SubscribeAction("NextLevel", _nextLevelHandler);
            GameEventSystem.SubscribeAction("Retry", _retryHandler);
            GameEventSystem.SubscribeAction("GameOver", _gameOverHandler);
            GameEventSystem.SubscribeAction("GameWin", _gameWinHandler);
            _encounterStartedHandler = OnEncounterStartedEvent;
            GameEventSystem.SubscribeAction("EncounterStarted", _encounterStartedHandler);
            _encounterZoneCompletedHandler = CheckAllEncountersCompleted;
            _storySequenceCompletedHandler = OnStorySequenceCompleted;
            GameEventSystem.SubscribeAction("StorySequenceCompleted", _storySequenceCompletedHandler);
            _playerSpawnedForEncounterHandler = OnPlayerSpawnedForEncounter;
            GameEventSystem.SubscribeAction("PlayerSpawnedForEncounter", _playerSpawnedForEncounterHandler);
            GameEventSystem.SubscribeAction("EncounterZoneCompleted", _encounterZoneCompletedHandler);
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                var health = playerController.GetComponent<Health>();
                if (health != null) health.OnDeath -= HandlePlayerDeath;
            }

            if (_loadingFinishedHandler != null)
                GameEventSystem.UnsubscribeActionWithPayload("LoadingFinished", _loadingFinishedHandler);
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;

            GameEventSystem.UnsubscribeAction("Pause", _pauseHandler);
            GameEventSystem.UnsubscribeAction("Resume", _resumeHandler);
            GameEventSystem.UnsubscribeAction("Lock", _lockHandler);
            GameEventSystem.UnsubscribeAction("NextLevel", _nextLevelHandler);
            GameEventSystem.UnsubscribeAction("Retry", _retryHandler);
            GameEventSystem.UnsubscribeAction("GameOver", _gameOverHandler);
            GameEventSystem.UnsubscribeAction("GameWin", _gameWinHandler);
            if (_encounterStartedHandler != null)
                GameEventSystem.UnsubscribeAction("EncounterStarted", _encounterStartedHandler);
            if (_storySequenceCompletedHandler != null)
                GameEventSystem.UnsubscribeAction("StorySequenceCompleted", _storySequenceCompletedHandler);
            if (_playerSpawnedForEncounterHandler != null)
                GameEventSystem.UnsubscribeAction("PlayerSpawnedForEncounter", _playerSpawnedForEncounterHandler);
            GameEventSystem.UnsubscribeAction("EncounterZoneCompleted", _encounterZoneCompletedHandler);
        }

        private void OnStorySequenceCompleted()
        {
            FindReferences();
            if (GameManager.Instance?.CanvasManager != null && !string.IsNullOrEmpty(gameplayCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(gameplayCanvasId);
            SetInputActive(true);
            if (showDebugLog) Debug.Log("[GameplayManager] Story sequence completed -> init: gameplay canvas and input active.");
        }

        private void OnPlayerSpawnedForEncounter()
        {
            FindReferences();
            if (showDebugLog) Debug.Log("[GameplayManager] PlayerSpawnedForEncounter: refs refreshed, camera bound. Waiting camera settle before player ready.");
            StartCoroutine(CameraSettleThenPlayerReadyRoutine());
        }

        private IEnumerator CameraSettleThenPlayerReadyRoutine()
        {
            if (cameraSettleDelay > 0f)
                yield return new WaitForSeconds(cameraSettleDelay);
            GameEventSystem.Publish(new UIActionEvent("PlayerReadyForEncounter"));
            if (showDebugLog) Debug.Log("[GameplayManager] PlayerReadyForEncounter published (camera settle done).");
        }

        private void OnEncounterStartedEvent()
        {
            FindReferences();
            if (showDebugLog) Debug.Log("[GameplayManager] Refreshed references after EncounterStarted.");
        }

        private void HandleLockAction()
        {
            var lockCam = FindObjectOfType<TargetLockCameraController>();
            if (lockCam != null) lockCam.ToggleLockTarget();
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
            if (loadedSceneName == "MainMenu" || loadedSceneName == "Loading") return;

            if (JourneyManager.Instance != null)
            {
                var journey = JourneyManager.Instance.GetCurrentJourney();
                if (journey?.StartStorySequence != null && journey.StartStorySequence.StepCount > 0)
                {
                    if (showDebugLog) Debug.Log("[GameplayManager] Start story present — skipping StartLevelSequence; wait for EncounterStarted.");
                    return;
                }
            }

            StartCoroutine(StartLevelSequence());
        }

        private void FindReferences()
        {
            FindMainCamera();
            FindCinemachineCamera();
            FindPlayer();
            FindEncounterZones();
            if (showDebugLog) Debug.Log("[GameplayManager] FindReferences completed.");
            _sceneReady = true;
            GameEventSystem.Publish(new UIActionEvent("GameplayReady"));
            OnReady?.Invoke();
        }

        private void FindMainCamera()
        {
            var found = Camera.main;
            if (found == null)
            {
                var camObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (camObj != null) found = camObj.GetComponent<Camera>();
            }
            if (found != null) mainCamera = found;
            if (mainCamera == null && showDebugLog) Debug.LogWarning("[GameplayManager] Main Camera NOT found in this scene.");
        }

        private void FindCinemachineCamera()
        {
            var found = FindObjectOfType<CinemachineFreeLook>();
            if (found != null)
            {
                currentCamera = found;
                if (showDebugLog) Debug.Log("[GameplayManager] Found Cinemachine Camera. Broadcasting event.");
                GameEventSystem.Publish(new UIActionEvent("CameraInitialized"));
            }
            else
            {
                currentCamera = null;
                if (showDebugLog) Debug.LogWarning("[GameplayManager] Cinemachine Camera NOT found in this scene.");
            }
        }

        private void FindPlayer()
        {
            if (playerController != null)
            {
                var health = playerController.GetComponent<Health>();
                if (health != null) health.OnDeath -= HandlePlayerDeath;
            }
            playerController = FindObjectOfType<PlayerMovementController>();
            if (playerController != null)
            {
                if (showDebugLog) Debug.Log("[GameplayManager] Found Player Controller. Broadcasting event.");
                BindCinemachineToPlayer();
                GameEventSystem.Publish(new UIActionEvent("PlayerInitialized"));
                var health = playerController.GetComponent<Health>();
                if (health != null) health.OnDeath += HandlePlayerDeath;
            }
            else if (showDebugLog) Debug.LogWarning("[GameplayManager] Player Controller NOT found in this scene.");
        }

        private void BindCinemachineToPlayer()
        {
            if (currentCamera == null || playerController == null) return;
            Transform target = GetCinemachineTargetFromPlayer(playerController.transform);
            currentCamera.Follow = target;
            currentCamera.LookAt = target;
            float orbitY = target.eulerAngles.y;
            currentCamera.m_XAxis.Value = orbitY;
            currentCamera.m_YAxis.Value = 0.5f;
            if (showDebugLog) Debug.Log($"[GameplayManager] Cinemachine Follow/LookAt bound; orbit set to player forward (X={orbitY:F0}°, Y=0.5).");
        }

        private Transform GetCinemachineTargetFromPlayer(Transform playerRoot)
        {
            if (playerRoot == null) return null;
            if (string.IsNullOrEmpty(cinemachineTargetLayerName)) return playerRoot;
            int layer = LayerMask.NameToLayer(cinemachineTargetLayerName);
            if (layer < 0) return playerRoot;
            Transform[] children = playerRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].gameObject.layer == layer)
                    return children[i];
            }
            return playerRoot;
        }

        private void FindEncounterZones()
        {
            _encounterZones.Clear();
            var zones = FindObjectsOfType<StoryEncounterSpawner>();
            if (zones != null && zones.Length > 0)
            {
                _encounterZones.AddRange(zones);
                if (showDebugLog) Debug.Log($"[GameplayManager] Found {_encounterZones.Count} Encounter spawners. Broadcasting event.");
                GameEventSystem.Publish(new UIActionEvent("EncounterZonesInitialized"));
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
            if (_encounterZones == null || _encounterZones.Count == 0)
                return;

            bool allCompleted = true;
            foreach (var zone in _encounterZones)
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
            GameEventSystem.Publish(new UIActionEvent("GameOver"));
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
            GameEventSystem.Publish(new UIActionEvent("GameWin"));
        }

        #endregion

        #region Pause / Resume (gameplay; time scale via GameManager)

        public void PauseGame()
        {
            if (isPaused) return;
            isPaused = true;
            if (GameManager.Instance != null) GameManager.Instance.SetTimeScale(0f);
            if (GameManager.Instance?.CanvasManager != null && !string.IsNullOrEmpty(pauseCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(pauseCanvasId);
            if (showDebugLog) Debug.Log("[GameplayManager] Game paused");
        }

        public void ResumeGame()
        {
            if (!isPaused) return;
            isPaused = false;
            if (GameManager.Instance != null) GameManager.Instance.SetTimeScale(1f);
            if (GameManager.Instance?.CanvasManager != null && !string.IsNullOrEmpty(gameplayCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(gameplayCanvasId);
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
