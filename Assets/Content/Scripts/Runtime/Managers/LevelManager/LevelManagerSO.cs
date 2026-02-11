using UnityEngine;
using System.Collections.Generic;

namespace LittleHeroJourney
{
    /// <summary>
    /// ScriptableObject untuk konfigurasi semua level
    /// </summary>
    [CreateAssetMenu(fileName = "LevelManager_Config", menuName = "LittleHeroJourney/Levels/Manager")]
    public class LevelManagerSO : ScriptableObject
    {
        [Header("Level List")]
        [SerializeField] private List<LevelSO> levels = new List<LevelSO>();

        [Header("UI Prefab")]
        [Tooltip("Prefab untuk level button di level selector")]
        [SerializeField] private GameObject levelButtonPrefab;

        [Header("Settings")]
        [SerializeField] private bool showDebugLog = false;

        #region Properties

        public List<LevelSO> Levels => levels;
        public GameObject LevelButtonPrefab => levelButtonPrefab;
        public bool ShowDebugLog => showDebugLog;

        #endregion

        #region Methods

        /// <summary>
        /// Get level by number
        /// </summary>
        public LevelSO GetLevelByNumber(int levelNumber)
        {
            foreach (var level in levels)
            {
                if (level.LevelNumber == levelNumber)
                    return level;
            }
            return null;
        }

        /// <summary>
        /// Get total levels
        /// </summary>
        public int GetTotalLevels()
        {
            return levels.Count;
        }

        /// <summary>
        /// Get level by index
        /// </summary>
        public LevelSO GetLevelByIndex(int index)
        {
            if (index >= 0 && index < levels.Count)
                return levels[index];
            return null;
        }

        /// <summary>
        /// Get all levels
        /// </summary>
        public List<LevelSO> GetAllLevels()
        {
            return new List<LevelSO>(levels);
        }

        #endregion
    }
}
