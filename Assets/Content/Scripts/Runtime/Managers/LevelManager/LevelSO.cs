using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// ScriptableObject untuk data satu level (CONFIG SAJA - FIXED)
    /// Unlock status disimpan terpisah di GameStateSO runtime state
    /// </summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "LittleHeroJourney/Levels/Level")]
    public class LevelSO : ScriptableObject
    {
        [Header("Level Info")]
        [SerializeField] private int levelNumber;
        [SerializeField] private string levelName;
        [SerializeField] private string storySummary;
        [SerializeField] private float maxDurationSeconds = 300f;
        [SerializeField] private string loadTargetId = "level";
        
        [SerializeField]
        [SceneAttribute]
        private string sceneName;

        [Header("Unlock Settings")]
        [SerializeField] private bool startsUnlocked = false;
        [SerializeField] private bool requiresPreviousCompletion = true;

        #region Properties

        public int LevelNumber => levelNumber;
        public string LevelName => levelName;
        public string StorySummary => storySummary;
        public float MaxDurationSeconds => maxDurationSeconds;
        public string LoadTargetId => loadTargetId;
        public string SceneName => sceneName;
        public bool StartsUnlocked => startsUnlocked;
        public bool RequiresPreviousCompletion => requiresPreviousCompletion;

        #endregion
    }
}
