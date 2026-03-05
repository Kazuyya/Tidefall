using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using LittleHeroJourney.UI;
using DanielLochner.Assets.SimpleScrollSnap;

namespace LittleHeroJourney
{
    /// <summary>
    /// Manages level selection and spawning level buttons
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private LevelManagerSO levelConfig;
        

        [Header("UI")]
        [SerializeField] private Transform levelButtonContainer;
        [SerializeField] private SimpleScrollSnap simpleScrollSnap;

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

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            if (levelConfig != null)
            {
                SpawnLevelButtons();
                SetScrollSnapToLastPanel();
            }
        }

        private void Start()
        {
            if (levelConfig == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] LevelManagerSO not assigned!");
                return;
            }

            if (ES3.KeyExists("GameState"))
            {
                var data = ES3.Load<GameStateData>("GameState");
                ApplyData(data);
            }
            else if (levelStates.Count == 0)
            {
                InitializeLevelStates(levelConfig.Levels);
            }

            RefreshAllLevelButtons();

            var canvasManager = GameManager.Instance != null ? GameManager.Instance.CanvasManager : null;
            if (canvasManager != null)
                canvasManager.OnOpenComplete += OnCanvasOpenComplete;
        }

        private void OnDestroy()
        {
            var canvasManager = GameManager.Instance != null ? GameManager.Instance.CanvasManager : null;
            if (canvasManager != null)
                canvasManager.OnOpenComplete -= OnCanvasOpenComplete;
        }

        private void OnCanvasOpenComplete(GameCanvas canvas)
        {
            if (canvas == null || canvas.ID != "Level") return;
            StartCoroutine(RefreshScrollSnapAfterLayout());
        }

        private IEnumerator RefreshScrollSnapAfterLayout()
        {
            yield return null;
            yield return null;
            EnsureScrollSnapAtLastPanel();
        }

        #endregion

        #region Level Button Spawning

        private void SpawnLevelButtons()
        {
            // Clear existing buttons
            foreach (Transform child in levelButtonContainer)
            {
                Destroy(child.gameObject);
            }

            int totalLevels = levelConfig.GetTotalLevels();
            spawnedLevelButtons = new LevelButton[totalLevels];

            for (int i = totalLevels - 1; i >= 0; i--)
            {
                LevelSO level = levelConfig.GetLevelByIndex(i);
                if (level == null) continue;

                // Spawn button prefab
                GameObject buttonObj = Instantiate(levelConfig.LevelButtonPrefab, levelButtonContainer);
                buttonObj.name = $"LevelButton_{level.LevelNumber}";

                // Get LevelButton component
                LevelButton levelButton = buttonObj.GetComponent<LevelButton>();
                if (levelButton == null)
                {
                    if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Level button prefab doesn't have LevelButton component!");
                    continue;
                }

                spawnedLevelButtons[i] = levelButton;

                levelButton.SetLevelData(level);
                levelButton.SetLevelManager(this);
                levelButton.UpdateVisual();
            }

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Spawned {totalLevels} level buttons");
        }

        private void SetScrollSnapToLastPanel()
        {
            if (simpleScrollSnap == null) return;
            int totalLevels = levelConfig.GetTotalLevels();
            if (totalLevels <= 0) return;
            int lastPanelIndex = totalLevels - 1;
            simpleScrollSnap.StartingPanel = lastPanelIndex;
            if (showDebugLog) Debug.Log($"[{GetType().Name}] SetScrollSnapToLastPanel: StartingPanel = {lastPanelIndex} (10 level = index 9).");
        }

                public void EnsureScrollSnapAtLastPanel()
        {
            if (simpleScrollSnap == null) return;
            Canvas.ForceUpdateCanvases();
            simpleScrollSnap.Refresh();
        }

        private void RefreshAllLevelButtons()
        {
            if (spawnedLevelButtons == null) return;
            for (int i = 0; i < spawnedLevelButtons.Length; i++)
            {
                if (spawnedLevelButtons[i] != null)
                    spawnedLevelButtons[i].UpdateVisual();
            }
        }

        #endregion

        #region Level Loading

        public void LoadLevel(LevelSO level)
        {
            if (string.IsNullOrEmpty(level.SceneName))
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Level scene name is empty!");
                return;
            }

            // Track current level yang dimainkan
            currentLevelNumber = level.LevelNumber;

            if (GameManager.Instance.SceneManager != null)
            {
                GameManager.Instance.SceneManager.StartLevel(level);
            }

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Loading level: {level.LevelName}");
        }

        /// <summary>
        /// Load level berdasarkan level number
        /// </summary>
        public void LoadLevel(int levelNumber)
        {
            LevelSO level = GetLevel(levelNumber);
            if (level == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Level {levelNumber} not found!");
                return;
            }
            LoadLevel(level);
        }

        #endregion

        #region Level State Management

        /// <summary>
        /// Mark level as completed dan auto-unlock next jika diperlukan
        /// </summary>
        public void CompleteLevel(int levelNumber)
        {
            var state = GetLevelState(levelNumber);
            if (state != null)
            {
                state.isCompleted = true;
            }

            var nextLevel = levelConfig.GetLevelByNumber(levelNumber + 1);
            if (nextLevel != null && nextLevel.RequiresPreviousCompletion)
            {
                SetLevelUnlocked(levelNumber + 1, true);
            }
            RefreshLevelButton(levelNumber);
            RefreshLevelButton(levelNumber + 1);
            ES3.Save("GameState", ToData());

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Completed level: {levelNumber}");
        }

        /// <summary>
        /// Unlock level manually dan refresh button visual
        /// </summary>
        public void UnlockLevel(int levelNumber)
        {
            SetLevelUnlocked(levelNumber, true);
            RefreshLevelButton(levelNumber);
            ES3.Save("GameState", ToData());

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Unlocked level: {levelNumber}");
        }

        /// <summary>
        /// Refresh button visual
        /// </summary>
        private void RefreshLevelButton(int levelNumber)
        {
            int index = levelNumber - 1;
            if (index >= 0 && index < spawnedLevelButtons.Length && spawnedLevelButtons[index] != null)
            {
                spawnedLevelButtons[index].UpdateVisual();
            }
        }

        #endregion

        #region Getters

        public LevelSO GetLevel(int levelNumber)
        {
            return levelConfig.GetLevelByNumber(levelNumber);
        }
        
        public float GetLevelMaxDuration(int levelNumber)
        {
            var level = GetLevel(levelNumber);
            return level != null ? level.MaxDurationSeconds : 0f;
        }

        public int GetTotalLevels()
        {
            return levelConfig.GetTotalLevels();
        }

        public int GetCurrentLevelNumber()
        {
            return currentLevelNumber;
        }

        #endregion

        #region Save System
        [ContextMenu("Progress/Save (ES3)")]
        public void SaveProgress()
        {
            ES3.Save("GameState", ToData());
        }
        [ContextMenu("Progress/Load (ES3)")]
        public void LoadProgress()
        {
            if (ES3.KeyExists("GameState"))
            {
                var data = ES3.Load<GameStateData>("GameState");
                ApplyData(data);
            }
            SpawnLevelButtons();
            SetScrollSnapToLastPanel();
            RefreshAllLevelButtons();
        }
        [ContextMenu("Progress/Reset to Default")]
        public void ResetAllProgress()
        {
            InitializeLevelStates(levelConfig.Levels);
            currentPlayerHealth = 100;
            ES3.Save("GameState", ToData());
            SpawnLevelButtons();
            SetScrollSnapToLastPanel();
            RefreshAllLevelButtons();
        }
        [ContextMenu("Progress/Delete Save")]
        public void DeleteSave()
        {
            ES3.DeleteKey("GameState");
        }

        #endregion
        #region ES3 Data Conversion
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
            var data = new GameStateData();
            data.currentPlayerHealth = currentPlayerHealth;
            data.levels = new List<LevelStateData>(levelStates.Count);
            for (int i = 0; i < levelStates.Count; i++)
            {
                var s = levelStates[i];
                data.levels.Add(new LevelStateData
                {
                    levelNumber = s.levelNumber,
                    isUnlocked = s.isUnlocked,
                    bestScore = s.bestScore,
                    isCompleted = s.isCompleted
                });
            }
            return data;
        }

        private void ApplyData(GameStateData data)
        {
            if (data == null || data.levels == null || data.levels.Count == 0)
                return;

            levelStates.Clear();
            for (int i = 0; i < data.levels.Count; i++)
            {
                var d = data.levels[i];
                levelStates.Add(new LevelState
                {
                    levelNumber = d.levelNumber,
                    isUnlocked = d.isUnlocked,
                    bestScore = d.bestScore,
                    isCompleted = d.isCompleted
                });
            }
            currentPlayerHealth = data.currentPlayerHealth;
        }
        #endregion

        #region State Helpers
        private void InitializeLevelStates(List<LevelSO> levels)
        {
            levelStates.Clear();
            foreach (var level in levels)
            {
                levelStates.Add(new LevelState
                {
                    levelNumber = level.LevelNumber,
                    isUnlocked = level.StartsUnlocked,
                    bestScore = 0,
                    isCompleted = false
                });
            }
        }

        public bool IsLevelUnlocked(int levelNumber)
        {
            var s = GetLevelState(levelNumber);
            return s != null && s.isUnlocked;
        }

        private void SetLevelUnlocked(int levelNumber, bool unlocked)
        {
            var s = GetLevelState(levelNumber);
            if (s != null) s.isUnlocked = unlocked;
        }

        private LevelState GetLevelState(int levelNumber)
        {
            for (int i = 0; i < levelStates.Count; i++)
            {
                if (levelStates[i].levelNumber == levelNumber)
                    return levelStates[i];
            }
            return null;
        }
        #endregion
    }
}
