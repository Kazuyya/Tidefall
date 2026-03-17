using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LittleHeroJourney
{
    public class JourneyManager : MonoBehaviour
    {
        public const string CurrentSaveKey = "GameState";

        public static JourneyManager Instance { get; private set; }

        [Header("Journey Data")]
        [SerializeField] private string storyCanvasId = "";
        [SerializeField] private JourneysDataSO journeysData;
        [SerializeField] private string journeySelectorCanvasId = "";

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;

        private int currentLevelNumber = 0;

        [Serializable]
        public class LevelState
        {
            public int levelNumber;
            public bool isUnlocked;
            public int bestScore;
            public bool isCompleted;
        }

        private List<LevelState> levelStates = new List<LevelState>();
        private int currentPlayerHealth = 100;

        private Action _playHandler;
        private Action _pendingAfterFade;
        private Action _fadeCompleteHandler;
        private Action _storyLastStepStartedHandler;
        private Action _storyLastStepCompletedHandler;
        private Action _playerReadyForEncounterHandler;
        private StorySequenceDisplay _currentStoryDisplay;
        private static int _pendingStoryStageNumber;
        private bool _isStartStoryPlaying;
        private bool _playerReadyForEncounter;
        private bool _lastStepCompleted;
        private StoryEncounterSpawner _pendingEncounterSpawner;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void OnEnable()
        {
            _playHandler = HandlePlay;
            GameEventSystem.SubscribeAction("Play", _playHandler);
            _fadeCompleteHandler = OnStartJourneyFadeComplete;
            GameEventSystem.SubscribeAction("StartJourneyFadeComplete", _fadeCompleteHandler);
            _storyLastStepStartedHandler = OnStoryLastStepStarted;
            GameEventSystem.SubscribeAction("StoryLastStepStarted", _storyLastStepStartedHandler);
            _storyLastStepCompletedHandler = OnStoryLastStepCompleted;
            GameEventSystem.SubscribeAction("StoryLastStepCompleted", _storyLastStepCompletedHandler);
            _playerReadyForEncounterHandler = OnPlayerReadyForEncounter;
            GameEventSystem.SubscribeAction("PlayerReadyForEncounter", _playerReadyForEncounterHandler);
        }

        private void HandlePlay()
        {
            if (HasProgress())
                OpenJourneySelector();
            else
                RequestNewJourney();
        }

        public void RequestNewJourney()
        {
            _pendingAfterFade = NewJourney;
            GameEventSystem.Publish(new UIActionEvent("StartJourney"));
        }

        public void RequestLoadStage(int stageNumber)
        {
            int n = stageNumber;
            _pendingAfterFade = () => LoadStage(n);
            GameEventSystem.Publish(new UIActionEvent("StartJourney"));
        }

        private void OnStartJourneyFadeComplete()
        {
            _pendingAfterFade?.Invoke();
            _pendingAfterFade = null;
        }

        private bool HasProgress()
        {
            if (!HasAnySave()) return false;
            return GetFirstUncompletedStageNumber() > 1;
        }

        private void OpenJourneySelector()
        {
            if (GameManager.Instance?.CanvasManager != null && !string.IsNullOrEmpty(journeySelectorCanvasId))
                GameManager.Instance.CanvasManager.SwitchCanvas(journeySelectorCanvasId);
        }

        private void OnDisable()
        {
            GameEventSystem.UnsubscribeAction("Play", _playHandler);
            if (_fadeCompleteHandler != null)
                GameEventSystem.UnsubscribeAction("StartJourneyFadeComplete", _fadeCompleteHandler);
            if (_storyLastStepStartedHandler != null)
                GameEventSystem.UnsubscribeAction("StoryLastStepStarted", _storyLastStepStartedHandler);
            if (_storyLastStepCompletedHandler != null)
                GameEventSystem.UnsubscribeAction("StoryLastStepCompleted", _storyLastStepCompletedHandler);
            if (_playerReadyForEncounterHandler != null)
                GameEventSystem.UnsubscribeAction("PlayerReadyForEncounter", _playerReadyForEncounterHandler);
        }

        private void Start()
        {
            if (journeysData == null || journeysData.JourneyCount == 0) return;
            if (ES3.KeyExists(CurrentSaveKey))
                ApplySaveData(ES3.Load<JourneySaveData>(CurrentSaveKey));
            else if (levelStates.Count == 0)
                InitializeLevelStates();
        }

        #region Stage Loading

        public void LoadCurrentStage()
        {
            int stage = GetFirstUncompletedStageNumber();
            if (stage <= 0) stage = 1;
            LoadStage(stage);
        }

        public void LoadStage(int stageNumber)
        {
            var journey = GetJourneyByNumber(stageNumber);
            if (journey == null || string.IsNullOrEmpty(journey.SceneName)) return;
            currentLevelNumber = stageNumber;
            _pendingStoryStageNumber = stageNumber;
            if (GameManager.Instance?.SceneManager != null)
            {
                string targetId = string.IsNullOrEmpty(journey.SceneId) ? "gameplay" : journey.SceneId;
                GameManager.Instance.SceneManager.StartStageScene(journey.SceneName, targetId, RunStartStoryInActiveScene);
            }
        }

        private static void RunStartStoryInActiveScene()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var jm in FindObjectsOfType<JourneyManager>())
            {
                if (jm.gameObject.scene == activeScene)
                {
                    jm.PlayStartStoryForStage(_pendingStoryStageNumber);
                    return;
                }
            }
            if (Instance != null)
                Instance.PlayStartStoryForStage(_pendingStoryStageNumber);
        }

        public void PlayStartStoryForStage(int stageNumber)
        {
            Instance = this;
            currentLevelNumber = stageNumber;
            var sequence = GetStartStoryForJourney(stageNumber);
            if (sequence == null || sequence.StepCount == 0) return;
            if (string.IsNullOrEmpty(storyCanvasId) || GameManager.Instance?.CanvasManager == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Story canvas id empty or CanvasManager null, skipping start story.");
                return;
            }
            GameManager.Instance.CanvasManager.SwitchCanvas(storyCanvasId);
            var display = FindObjectOfType<StorySequenceDisplay>();
            if (display == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] StorySequenceDisplay not found, skipping start story.");
                return;
            }
            _currentStoryDisplay = display;
            _isStartStoryPlaying = true;
            display.Play(sequence);
        }

        public void PlayStartStoryForCurrentStage()
        {
            PlayStartStoryForStage(currentLevelNumber);
        }

        public void RequestAdvanceStory()
        {
            if (_currentStoryDisplay == null) return;
            _currentStoryDisplay.RequestAdvance();
        }

        private void OnStoryLastStepStarted()
        {
            if (!_isStartStoryPlaying) return;
            StoryEncounterSpawner target = GetStartEncounterSpawner();
            if (target == null) return;

            _pendingEncounterSpawner = target;

            var player = FindObjectOfType<PlayerMovementController>();
            if (player != null && target.PlayerSpawnPoint != null)
            {
                Transform spawn = target.PlayerSpawnPoint;
                player.transform.position = spawn.position;
                player.transform.rotation = spawn.rotation;
                player.SetEncounterSpawner(target);
            }
            else if (player == null && target.PlayerPrefab != null && target.PlayerSpawnPoint != null)
            {
                Transform spawn = target.PlayerSpawnPoint;
                Instantiate(target.PlayerPrefab, spawn.position, spawn.rotation);
            }

            GameEventSystem.Publish(new UIActionEvent("PlayerSpawnedForEncounter"));
            if (showDebugLog) Debug.Log("[JourneyManager] StoryLastStepStarted: player spawned. Waiting for camera settle (PlayerReadyForEncounter) and last step completed.");
            CheckBothConditionsForEncounter();
        }

        private void OnPlayerReadyForEncounter()
        {
            if (!_isStartStoryPlaying) return;
            _playerReadyForEncounter = true;
            if (showDebugLog) Debug.Log("[JourneyManager] PlayerReadyForEncounter: camera settled. Waiting for last step completed if not yet.");
            CheckBothConditionsForEncounter();
        }

        private void OnStoryLastStepCompleted()
        {
            if (!_isStartStoryPlaying) return;
            _lastStepCompleted = true;
            if (showDebugLog) Debug.Log("[JourneyManager] StoryLastStepCompleted. Waiting for player ready if not yet.");
            CheckBothConditionsForEncounter();
        }

        private void CheckBothConditionsForEncounter()
        {
            if (!_playerReadyForEncounter || !_lastStepCompleted) return;

            _isStartStoryPlaying = false;
            _currentStoryDisplay = null;
            _playerReadyForEncounter = false;
            _lastStepCompleted = false;
            StoryEncounterSpawner target = _pendingEncounterSpawner;
            _pendingEncounterSpawner = null;

            GameEventSystem.Publish(new UIActionEvent("StorySequenceCompleted"));

            if (target != null)
            {
                target.StartEncounter();
                GameEventSystem.Publish(new UIActionEvent("EncounterStarted"));
                if (showDebugLog) Debug.Log("[JourneyManager] Both conditions met: StorySequenceCompleted published, canvas switch, encounter started (enemies after CD).");
            }
        }

        private StoryEncounterSpawner GetStartEncounterSpawner()
        {
            var journey = GetCurrentJourney();
            if (journey == null) return null;
            string encounterId = journey.StartEncounterId;
            if (string.IsNullOrEmpty(encounterId)) return null;
            var spawners = FindObjectsOfType<StoryEncounterSpawner>();
            for (int i = 0; i < spawners.Length; i++)
            {
                if (spawners[i] != null && string.Equals(spawners[i].EncounterId, encounterId, StringComparison.Ordinal))
                    return spawners[i];
            }
            if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] No StoryEncounterSpawner found with id '{encounterId}'.");
            return null;
        }

        #endregion

        #region Story State

        public void CompleteLevel(int stageNumber)
        {
            var state = GetLevelState(stageNumber);
            if (state != null) state.isCompleted = true;
            SetLevelUnlocked(stageNumber + 1, true);
            ES3.Save(CurrentSaveKey, GetSaveData());
        }

        private void SetLevelUnlocked(int stageNumber, bool unlocked)
        {
            var s = GetLevelState(stageNumber);
            if (s != null) s.isUnlocked = unlocked;
        }

        #endregion

        #region Getters

        public int GetTotalLevels() => journeysData != null ? journeysData.JourneyCount : 0;
        public int GetCurrentLevelNumber() => currentLevelNumber;

        public int GetFirstUncompletedStageNumber()
        {
            if (levelStates == null) return 1;
            foreach (var s in levelStates)
            {
                if (s != null && !s.isCompleted) return s.levelNumber;
            }
            return GetTotalLevels() > 0 ? GetTotalLevels() + 1 : 1;
        }

        public string GetCurrentStageDisplayName()
        {
            return GetStageDisplayNameForStageNumber(GetFirstUncompletedStageNumber());
        }

        private string GetStageDisplayNameForStageNumber(int stageNumber)
        {
            var journey = GetJourneyByNumber(stageNumber);
            if (journey != null && !string.IsNullOrEmpty(journey.JourneyTitle)) return journey.JourneyTitle;
            return $"Journey {stageNumber}";
        }

        public JourneyDataSO GetJourneyByNumber(int stageNumber)
        {
            return journeysData?.GetJourneyByNumber(stageNumber);
        }

        public JourneyDataSO GetCurrentJourney() => GetJourneyByNumber(currentLevelNumber);

        public string StoryCanvasId => storyCanvasId ?? "";
        public StorySequenceSO StartStorySequence => GetCurrentJourney()?.StartStorySequence;
        public StorySequenceSO EndStorySequence => GetCurrentJourney()?.EndStorySequence;
        public StorySequenceSO GetStartStoryForJourney(int stageNumber) => GetJourneyByNumber(stageNumber)?.StartStorySequence;
        public StorySequenceSO GetEndStoryForJourney(int stageNumber) => GetJourneyByNumber(stageNumber)?.EndStorySequence;

        #endregion

        #region Save

        public void SaveProgress() => ES3.Save(CurrentSaveKey, GetSaveData());

        public void ApplySaveData(JourneySaveData data)
        {
            if (data == null) return;
            ApplyData(data);
        }

        public void InitializeNewJourney()
        {
            if (journeysData == null || journeysData.JourneyCount == 0) return;
            InitializeLevelStates();
            currentPlayerHealth = 100;
        }

        public JourneySaveData GetSaveData()
        {
            var data = ToData();
            data.lastSavedTimestampUtc = DateTime.UtcNow.Ticks;
            return data;
        }

        public static bool HasAnySave() => ES3.KeyExists(CurrentSaveKey);

        public void ContinueJourney()
        {
            if (!HasAnySave()) return;
            LoadCurrentStage();
        }

        public void RequestContinueJourney()
        {
            if (!HasAnySave()) return;
            _pendingAfterFade = LoadCurrentStage;
            GameEventSystem.Publish(new UIActionEvent("StartJourney"));
        }

        public void NewJourney()
        {
            ES3.DeleteKey(CurrentSaveKey);
            InitializeNewJourney();
            LoadStage(1);
        }

        #endregion

        #region ES3

        private JourneySaveData ToData()
        {
            var data = new JourneySaveData { currentPlayerHealth = currentPlayerHealth };
            data.stages = new List<JourneySaveData.StageStateData>(levelStates.Count);
            foreach (var s in levelStates)
                data.stages.Add(new JourneySaveData.StageStateData { stageNumber = s.levelNumber, isUnlocked = s.isUnlocked, bestScore = s.bestScore, isCompleted = s.isCompleted });
            return data;
        }

        private void ApplyData(JourneySaveData data)
        {
            if (data?.stages == null || data.stages.Count == 0) return;
            levelStates.Clear();
            foreach (var d in data.stages)
                levelStates.Add(new LevelState { levelNumber = d.stageNumber, isUnlocked = d.isUnlocked, bestScore = d.bestScore, isCompleted = d.isCompleted });
            currentPlayerHealth = data.currentPlayerHealth;
        }

        #endregion

        #region State

        private void InitializeLevelStates()
        {
            levelStates.Clear();
            if (journeysData == null) return;
            for (int i = 0; i < journeysData.JourneyCount; i++)
            {
                int levelNumber = i + 1;
                bool unlock = levelNumber == 1;
                levelStates.Add(new LevelState { levelNumber = levelNumber, isUnlocked = unlock, bestScore = 0, isCompleted = false });
            }
        }

        private LevelState GetLevelState(int levelNumber)
        {
            foreach (var s in levelStates)
                if (s.levelNumber == levelNumber) return s;
            return null;
        }

        #endregion
    }
}