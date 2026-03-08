using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// Satu stage story (1 stage = 1 "level" di logic). Unlock status di GameState.
    /// </summary>
    [CreateAssetMenu(fileName = "Story_", menuName = "Little Hero Journey/Story/Stage")]
    public class LevelSO : ScriptableObject
    {
        [Header("Story Info")]
        [SerializeField] private int levelNumber;
        [SerializeField] private string levelName;
        [SerializeField] private string storySummary;
        [SerializeField] private float maxDurationSeconds = 300f;
        [SerializeField] private string loadTargetId = "story";
        
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
