using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private LevelManagerSO levelConfig;

        [Header("UI")]
        [SerializeField] private Transform levelButtonContainer;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;

        private LevelButton[] spawnedLevelButtons;
        private int currentLevelNumber = 0;

        [System.Serializable]
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
            if (levelConfig != null) SpawnLevelButtons();
        }

        private void Start()
        {
            if (levelConfig == null) return;
            if (ES3.KeyExists("GameState")) ApplyData(ES3.Load<GameStateData>("GameState"));
            else if (levelStates.Count == 0) InitializeLevelStates(levelConfig.Levels);
            RefreshAllLevelButtons();
        }

        private void SpawnLevelButtons()
        {
            foreach (Transform child in levelButtonContainer)
                Destroy(child.gameObject);

            int totalLevels = levelConfig.GetTotalLevels();
            if (totalLevels <= 0) return;

            spawnedLevelButtons = new LevelButton[totalLevels];

            for (int i = 0; i < totalLevels; i++)
            {
                LevelSO level = levelConfig.GetLevelByIndex(i);
                if (level == null) continue;

                GameObject buttonObj = Instantiate(levelConfig.LevelButtonPrefab, levelButtonContainer);
                buttonObj.name = $"StoryButton_{level.LevelNumber}";

                LevelButton lb = buttonObj.GetComponent<LevelButton>();
                if (lb == null) continue;

                spawnedLevelButtons[i] = lb;
                lb.SetLevelData(level);
                lb.SetLevelManager(this);
                lb.UpdateVisual();
            }
        }

        private void RefreshAllLevelButtons()
        {
            if (spawnedLevelButtons == null) return;
            for (int i = 0; i < spawnedLevelButtons.Length; i++)
                if (spawnedLevelButtons[i] != null) spawnedLevelButtons[i].UpdateVisual();
        }

        #region Story Loading

        public void LoadLevel(LevelSO level)
        {
            if (level == null || string.IsNullOrEmpty(level.SceneName)) return;
            currentLevelNumber = level.LevelNumber;
            if (GameManager.Instance?.SceneManager != null) GameManager.Instance.SceneManager.StartLevel(level);
        }

        public void LoadLevel(int levelNumber)
        {
            var level = GetLevel(levelNumber);
            if (level != null) LoadLevel(level);
        }

        #endregion

        #region Story State

        public void CompleteLevel(int levelNumber)
        {
            var state = GetLevelState(levelNumber);
            if (state != null) state.isCompleted = true;
            var next = levelConfig.GetLevelByNumber(levelNumber + 1);
            if (next != null && next.RequiresPreviousCompletion) SetLevelUnlocked(levelNumber + 1, true);
            RefreshLevelButton(levelNumber);
            RefreshLevelButton(levelNumber + 1);
            ES3.Save("GameState", ToData());
        }

        public void UnlockLevel(int levelNumber)
        {
            SetLevelUnlocked(levelNumber, true);
            RefreshLevelButton(levelNumber);
            ES3.Save("GameState", ToData());
        }

        private void RefreshLevelButton(int levelNumber)
        {
            int index = levelNumber - 1;
            if (spawnedLevelButtons == null || index < 0 || index >= spawnedLevelButtons.Length) return;
            if (spawnedLevelButtons[index] != null) spawnedLevelButtons[index].UpdateVisual();
        }

        #endregion

        #region Getters

        public LevelSO GetLevel(int levelNumber) => levelConfig?.GetLevelByNumber(levelNumber);
        public float GetLevelMaxDuration(int levelNumber) => GetLevel(levelNumber)?.MaxDurationSeconds ?? 0f;
        public int GetTotalLevels() => levelConfig != null ? levelConfig.GetTotalLevels() : 0;
        public int GetCurrentLevelNumber() => currentLevelNumber;

        #endregion

        #region Save

        [ContextMenu("Progress/Save (ES3)")]
        public void SaveProgress() => ES3.Save("GameState", ToData());

        [ContextMenu("Progress/Load (ES3)")]
        public void LoadProgress()
        {
            if (ES3.KeyExists("GameState")) ApplyData(ES3.Load<GameStateData>("GameState"));
            SpawnLevelButtons();
            RefreshAllLevelButtons();
        }

        [ContextMenu("Progress/Reset to Default")]
        public void ResetAllProgress()
        {
            InitializeLevelStates(levelConfig.Levels);
            currentPlayerHealth = 100;
            ES3.Save("GameState", ToData());
            SpawnLevelButtons();
            RefreshAllLevelButtons();
        }

        [ContextMenu("Progress/Delete Save")]
        public void DeleteSave() => ES3.DeleteKey("GameState");

        #endregion

        #region ES3

        [System.Serializable]
        private class LevelStateData
        {
            public int levelNumber;
            public bool isUnlocked;
            public int bestScore;
            public bool isCompleted;
        }

        [System.Serializable]
        private class GameStateData
        {
            public List<LevelStateData> levels = new List<LevelStateData>();
            public int currentPlayerHealth;
        }

        private GameStateData ToData()
        {
            var data = new GameStateData { currentPlayerHealth = currentPlayerHealth };
            data.levels = new List<LevelStateData>(levelStates.Count);
            foreach (var s in levelStates)
                data.levels.Add(new LevelStateData { levelNumber = s.levelNumber, isUnlocked = s.isUnlocked, bestScore = s.bestScore, isCompleted = s.isCompleted });
            return data;
        }

        private void ApplyData(GameStateData data)
        {
            if (data?.levels == null || data.levels.Count == 0) return;
            levelStates.Clear();
            foreach (var d in data.levels)
                levelStates.Add(new LevelState { levelNumber = d.levelNumber, isUnlocked = d.isUnlocked, bestScore = d.bestScore, isCompleted = d.isCompleted });
            currentPlayerHealth = data.currentPlayerHealth;
        }

        #endregion

        #region State

        private void InitializeLevelStates(List<LevelSO> levels)
        {
            levelStates.Clear();
            foreach (var level in levels)
                levelStates.Add(new LevelState { levelNumber = level.LevelNumber, isUnlocked = level.StartsUnlocked, bestScore = 0, isCompleted = false });
        }

        public bool IsLevelUnlocked(int levelNumber) => GetLevelState(levelNumber)?.isUnlocked ?? false;
        private void SetLevelUnlocked(int levelNumber, bool unlocked) { var s = GetLevelState(levelNumber); if (s != null) s.isUnlocked = unlocked; }
        private LevelState GetLevelState(int levelNumber) { foreach (var s in levelStates) if (s.levelNumber == levelNumber) return s; return null; }

        #endregion
    }
}
