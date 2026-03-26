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
        private Camera mainCamera;
        [Tooltip("Leave empty to auto-find from active scene.")]
        private CinemachineFreeLook currentCamera;
        [Tooltip("Leave empty to auto-find from active scene (refreshed again after encounter start).")]
        private PlayerMovementController playerController;

        [Header("Settings")]
        [Tooltip("Delay before starting gameplay after loading screen is gone")]
        [SerializeField] private float startDelay = 0.5f;
        [Tooltip("Delay after binding camera to player before signalling player ready (lets Cinemachine settle when story is skipped).")]
        [SerializeField] private float cameraSettleDelay = 0.8f;
        [Tooltip("Layer name of a child under player to use as Cinemachine Follow/LookAt (e.g. LookAtPlayer). If none found, uses player root.")]
        [SerializeField] private string cinemachineTargetLayerName = "LookAtPlayer";
        [SerializeField] private string gameplayCanvasId = "Gameplay";
        [SerializeField] private string pauseCanvasId = "Pause";
        [SerializeField] private string transitionCanvasId = "TransitionCanvas";
        [Tooltip("Delay after player reset to spawn before playing transition OUT (retry).")]
        [SerializeField] private float retrySpawnToOutDelay = 1f;
        [Tooltip("Only run FindReferences when this scene loads (e.g. Map). Prevents camera snap when Loading/MainMenu load during exit.")]
        [SerializeField] private string gameplaySceneName = "Map";

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = true;

        private bool isLevelEnded = false;
        private bool isInputActive = false;
        private bool isPaused = false;

        private Action _pauseHandler, _resumeHandler, _lockHandler;
        private Action _nextLevelHandler, _retryHandler, _encounterStartedHandler, _storySequenceCompletedHandler, _encounterZoneCompletedHandler, _playerSpawnedForEncounterHandler, _endStorySequenceCompletedHandler;
        private Action<string> _loadingFinishedHandler;
        private Action<string> _canvasOpenCompletePayloadHandler;
        private Action<string> _canvasCloseCompletePayloadHandler;
        private bool _isRetryTransitionActive;
        private StoryEncounterSpawner _retrySpawner;

        public static GameplayManager Instance { get; private set; }
        public bool IsInputActive => isInputActive;
        public bool IsPaused => isPaused;
        public Camera MainCamera => mainCamera;
        public CinemachineFreeLook CurrentCamera => currentCamera;
        public PlayerMovementController PlayerController => playerController;
        public List<StoryEncounterSpawner> EncounterZones => _encounterZones;

        private List<StoryEncounterSpawner> _encounterZones = new List<StoryEncounterSpawner>();
        private StoryEncounterSpawner _activeJourneyEncounterZone;

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

            GameEventSystem.SubscribeAction("Pause", _pauseHandler);
            GameEventSystem.SubscribeAction("Resume", _resumeHandler);
            GameEventSystem.SubscribeAction("Lock", _lockHandler);
            GameEventSystem.SubscribeAction("NextLevel", _nextLevelHandler);
            GameEventSystem.SubscribeAction("Retry", _retryHandler);
            _encounterStartedHandler = OnEncounterStartedEvent;
            GameEventSystem.SubscribeAction("EncounterStarted", _encounterStartedHandler);
            _encounterZoneCompletedHandler = CheckAllEncountersCompleted;
            _storySequenceCompletedHandler = OnStorySequenceCompleted;
            GameEventSystem.SubscribeAction("StorySequenceCompleted", _storySequenceCompletedHandler);
            _endStorySequenceCompletedHandler = OnEndStorySequenceCompleted;
            GameEventSystem.SubscribeAction("EndStorySequenceCompleted", _endStorySequenceCompletedHandler);
            _playerSpawnedForEncounterHandler = OnPlayerSpawnedForEncounter;
            GameEventSystem.SubscribeAction("PlayerSpawnedForEncounter", _playerSpawnedForEncounterHandler);
            GameEventSystem.SubscribeAction("EncounterZoneCompleted", _encounterZoneCompletedHandler);
            _canvasOpenCompletePayloadHandler = HandleCanvasOpenCompleteByPayload;
            _canvasCloseCompletePayloadHandler = HandleCanvasCloseCompleteByPayload;
            GameEventSystem.SubscribeActionWithPayload("CanvasOpenComplete", _canvasOpenCompletePayloadHandler);
            GameEventSystem.SubscribeActionWithPayload("CanvasCloseComplete", _canvasCloseCompletePayloadHandler);
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
            if (_encounterStartedHandler != null)
                GameEventSystem.UnsubscribeAction("EncounterStarted", _encounterStartedHandler);
            if (_storySequenceCompletedHandler != null)
                GameEventSystem.UnsubscribeAction("StorySequenceCompleted", _storySequenceCompletedHandler);
            if (_endStorySequenceCompletedHandler != null)
                GameEventSystem.UnsubscribeAction("EndStorySequenceCompleted", _endStorySequenceCompletedHandler);
            if (_playerSpawnedForEncounterHandler != null)
                GameEventSystem.UnsubscribeAction("PlayerSpawnedForEncounter", _playerSpawnedForEncounterHandler);
            GameEventSystem.UnsubscribeAction("EncounterZoneCompleted", _encounterZoneCompletedHandler);
            if (_canvasOpenCompletePayloadHandler != null)
                GameEventSystem.UnsubscribeActionWithPayload("CanvasOpenComplete", _canvasOpenCompletePayloadHandler);
            if (_canvasCloseCompletePayloadHandler != null)
                GameEventSystem.UnsubscribeActionWithPayload("CanvasCloseComplete", _canvasCloseCompletePayloadHandler);
        }

        private void OnStorySequenceCompleted()
        {
            FindReferences();
            if (GameManager.Instance?.CanvasManager != null && !string.IsNullOrEmpty(gameplayCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(gameplayCanvasId);
            SetInputActive(true);
            if (showDebugLog) Debug.Log("[GameplayManager] Story sequence completed -> init: gameplay canvas and input active.");
        }

        private void OnEndStorySequenceCompleted()
        {
            if (GameManager.Instance?.CanvasManager != null && !string.IsNullOrEmpty(gameplayCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(gameplayCanvasId);
            SetInputActive(true);
            if (showDebugLog) Debug.Log("[GameplayManager] End story sequence completed -> gameplay canvas and input active.");
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
            if (!string.IsNullOrEmpty(gameplaySceneName) && !string.Equals(scene.name, gameplaySceneName, StringComparison.OrdinalIgnoreCase))
                return;
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

        private Coroutine _forceOrbitCoroutine;

        private void BindCinemachineToPlayer()
        {
            if (currentCamera == null || playerController == null) return;
            Transform target = GetCinemachineTargetFromPlayer(playerController.transform);
            currentCamera.Follow = target;
            currentCamera.LookAt = target;
            ForceDefaultOrbit();
            if (_forceOrbitCoroutine != null) StopCoroutine(_forceOrbitCoroutine);
            _forceOrbitCoroutine = StartCoroutine(ForceDefaultOrbitAfterFrame());
            if (showDebugLog) Debug.Log($"[GameplayManager] Cinemachine Follow/LookAt bound; orbit behind player (X=0, Y=0.5).");
        }

        private void ForceDefaultOrbit()
        {
            if (currentCamera == null) return;
            currentCamera.m_XAxis.Value = 0f;
            currentCamera.m_YAxis.Value = 0.5f;
            currentCamera.PreviousStateIsValid = false;
        }

        private IEnumerator ForceDefaultOrbitAfterFrame()
        {
            yield return null;
            ForceDefaultOrbit();
            yield return null;
            ForceDefaultOrbit();
            _forceOrbitCoroutine = null;
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
            _activeJourneyEncounterZone = null;

            string requiredEncounterId = string.Empty;
            if (JourneyManager.Instance != null)
            {
                var journey = JourneyManager.Instance.GetCurrentJourney();
                if (journey != null)
                    requiredEncounterId = journey.StartEncounterId;
            }

            var zones = FindObjectsOfType<StoryEncounterSpawner>();
            if (zones == null || zones.Length == 0)
            {
                if (showDebugLog) Debug.LogWarning("[GameplayManager] No encounter spawner found in this scene.");
                return;
            }

            if (string.IsNullOrEmpty(requiredEncounterId))
            {
                if (showDebugLog) Debug.LogWarning("[GameplayManager] Current journey has empty StartEncounterId; no encounter zone will be tracked.");
                return;
            }

            for (int i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                if (zone == null) continue;
                if (!string.Equals(zone.EncounterId, requiredEncounterId, StringComparison.Ordinal)) continue;
                _activeJourneyEncounterZone = zone;
                _encounterZones.Add(zone);
                break;
            }

            if (_activeJourneyEncounterZone != null)
            {
                if (showDebugLog) Debug.Log($"[GameplayManager] Tracking journey encounter zone id '{requiredEncounterId}'. Other zones ignored.");
                GameEventSystem.Publish(new UIActionEvent("EncounterZonesInitialized"));
            }
            else if (showDebugLog)
            {
                Debug.LogWarning($"[GameplayManager] No encounter spawner matched Journey StartEncounterId '{requiredEncounterId}'.");
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
            if (_activeJourneyEncounterZone == null)
                return;

            if (_activeJourneyEncounterZone.IsCompleted)
            {
                if (showDebugLog) Debug.Log("[GameplayManager] Journey encounter zone completed -> Triggering Win!");
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
            if (showDebugLog) Debug.Log("[GameplayManager] Combat finished -> notifying JourneyManager (end story then save).");
            isLevelEnded = true;
            SetInputActive(false);
            GameEventSystem.Publish(new UIActionEvent("CombatFinished"));
        }

        #endregion

        #region Pause / Resume (UI only; game does not actually pause — no time scale)

        public void PauseGame()
        {
            if (isPaused) return;
            isPaused = true;
            if (GameManager.Instance?.CanvasManager != null && !string.IsNullOrEmpty(pauseCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(pauseCanvasId);
            if (showDebugLog) Debug.Log("[GameplayManager] Pause menu opened (canvas only; game keeps running).");
        }

        public void ResumeGame()
        {
            if (!isPaused) return;
            isPaused = false;
            if (GameManager.Instance?.CanvasManager != null && !string.IsNullOrEmpty(gameplayCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(gameplayCanvasId);
            if (showDebugLog) Debug.Log("[GameplayManager] Pause menu closed.");
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
            var spawner = JourneyManager.Instance.GetStartEncounterSpawner();
            if (spawner == null) { if (showDebugLog) Debug.LogWarning("[GameplayManager] Retry: no encounter spawner, falling back to LoadStage."); LoadStage(JourneyManager.Instance.GetCurrentLevelNumber()); return; }
            isPaused = false;
            _retrySpawner = spawner;
            _isRetryTransitionActive = true;
            SetInputActive(false);
            if (GameManager.Instance?.CanvasManager != null && !string.IsNullOrEmpty(transitionCanvasId))
            {
                GameManager.Instance.CanvasManager.SwitchCanvas(transitionCanvasId);
                if (showDebugLog) Debug.Log("[GameplayManager] Retry: opened TransitionCanvas, waiting for IN to finish.");
            }
            else
            {
                ApplyRetryNoTransitionCanvas();
            }
        }

        private void HandleCanvasOpenCompleteByPayload(string canvasId)
        {
            if (!_isRetryTransitionActive || _retrySpawner == null || string.IsNullOrEmpty(transitionCanvasId)) return;
            if (!string.Equals(canvasId, transitionCanvasId, StringComparison.OrdinalIgnoreCase)) return;
            PerformGameplayReset(_retrySpawner);
            StartCoroutine(DeferredSwitchToGameplayAfterRetryReset());
        }

        private IEnumerator DeferredSwitchToGameplayAfterRetryReset()
        {
            yield return null;
            if (retrySpawnToOutDelay > 0f)
                yield return new WaitForSeconds(retrySpawnToOutDelay);
            if (GameManager.Instance?.CanvasManager != null && !string.IsNullOrEmpty(gameplayCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(gameplayCanvasId);
            if (showDebugLog) Debug.Log("[GameplayManager] Retry: IN done → reset → delay → transition OUT started.");
        }

        private void HandleCanvasCloseCompleteByPayload(string canvasId)
        {
            if (!_isRetryTransitionActive || string.IsNullOrEmpty(transitionCanvasId)) return;
            if (!string.Equals(canvasId, transitionCanvasId, StringComparison.OrdinalIgnoreCase)) return;
            _isRetryTransitionActive = false;
            GameEventSystem.Publish(new UIActionEvent("RetryTransitionOutComplete"));
            if (_retrySpawner != null)
            {
                _retrySpawner.StartEncounter();
                GameEventSystem.Publish(new UIActionEvent("EncounterStarted"));
            }
            SetInputActive(true);
            _retrySpawner = null;
            if (showDebugLog) Debug.Log("[GameplayManager] Retry: transition out complete, encounter started, input active.");
        }

        public void PerformGameplayReset(StoryEncounterSpawner spawner)
        {
            if (spawner != null)
                spawner.ResetEncounter();
            if (spawner != null)
            {
                var player = FindObjectOfType<PlayerMovementController>();
                if (player != null && spawner.PlayerSpawnPoint != null)
                {
                    var t = spawner.PlayerSpawnPoint;
                    player.ResetToSpawnPoint(t.position, t.rotation);
                    player.SetEncounterSpawner(spawner);
                    var health = player.GetComponent<Health>();
                    if (health != null) health.Revive(1f);
                }
                else if (player == null && spawner.PlayerPrefab != null && spawner.PlayerSpawnPoint != null)
                {
                    var t = spawner.PlayerSpawnPoint;
                    Instantiate(spawner.PlayerPrefab, t.position, t.rotation);
                }
            }
            FindReferences();
            isLevelEnded = false;
            isPaused = false;
            GameEventSystem.Publish(new UIActionEvent("GameplayReset"));
            if (showDebugLog) Debug.Log("[GameplayManager] PerformGameplayReset done. Event GameplayReset published.");
        }

        private void ApplyRetryNoTransitionCanvas()
        {
            if (_retrySpawner == null) return;
            var spawner = _retrySpawner;
            _isRetryTransitionActive = false;
            PerformGameplayReset(spawner);
            if (GameManager.Instance?.CanvasManager != null && !string.IsNullOrEmpty(gameplayCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(gameplayCanvasId);
            GameEventSystem.Publish(new UIActionEvent("RetryTransitionOutComplete"));
            spawner.StartEncounter();
            GameEventSystem.Publish(new UIActionEvent("EncounterStarted"));
            SetInputActive(true);
            _retrySpawner = null;
            if (showDebugLog) Debug.Log("[GameplayManager] Retry: no TransitionCanvas, reset + encounter started directly.");
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
