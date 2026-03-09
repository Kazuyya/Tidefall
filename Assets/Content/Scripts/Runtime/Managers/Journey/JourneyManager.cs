using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    public class JourneyManager : MonoBehaviour
    {
        public const string CurrentSaveKey = "GameState";

        public static JourneyManager Instance { get; private set; }

        [Serializable]
        public class StageEntry
        {
            public int stageNumber = 1;
            [SceneAttribute]
            public string sceneName;
            public string journeyTitle = "Stage 1";
        }

        [Header("Stages")]
        [SerializeField] private List<StageEntry> stages = new List<StageEntry>();

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

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            if (stages == null || stages.Count == 0) return;
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
            var entry = GetStageByNumber(stageNumber);
            if (entry == null || string.IsNullOrEmpty(entry.sceneName)) return;
            currentLevelNumber = stageNumber;
            if (GameManager.Instance?.SceneManager != null)
                GameManager.Instance.SceneManager.StartStageScene(entry.sceneName);
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

        public int GetTotalLevels() => stages != null ? stages.Count : 0;
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
            var entry = GetStageByNumber(stageNumber);
            if (entry != null && !string.IsNullOrEmpty(entry.journeyTitle)) return entry.journeyTitle;
            return $"Stage {stageNumber}";
        }

        private StageEntry GetStageByNumber(int stageNumber)
        {
            if (stages == null) return null;
            foreach (var s in stages)
                if (s != null && s.stageNumber == stageNumber) return s;
            return null;
        }

        private StageEntry GetStageByIndex(int index)
        {
            if (stages == null || index < 0 || index >= stages.Count) return null;
            return stages[index];
        }

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
            if (stages == null || stages.Count == 0) return;
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
            if (stages == null) return;
            for (int i = 0; i < stages.Count; i++)
            {
                var entry = GetStageByIndex(i);
                if (entry == null) continue;
                bool unlock = entry.stageNumber == 1;
                levelStates.Add(new LevelState { levelNumber = entry.stageNumber, isUnlocked = unlock, bestScore = 0, isCompleted = false });
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
