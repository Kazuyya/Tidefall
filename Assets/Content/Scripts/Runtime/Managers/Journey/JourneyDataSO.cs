using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "JourneyData_", menuName = "Little Hero Journey/Journey Data")]
    public class JourneyDataSO : ScriptableObject
    {
        [SerializeField] private string journeyId = "";
        [SceneAttribute] [SerializeField] private string sceneName;
        [SerializeField] private string sceneId = "";
        [SerializeField] private string journeyTitle = "";
        [TextArea(2, 4)] [SerializeField] private string description = "";
        [SerializeField] private StorySequenceSO startStorySequence;
        [SerializeField] private StorySequenceSO endStorySequence;

        public string JourneyId => journeyId;
        public string SceneName => sceneName ?? "";
        public string SceneId => sceneId ?? "";
        public string JourneyTitle => string.IsNullOrEmpty(journeyTitle) ? $"Journey {journeyId}" : journeyTitle;
        public string Description => description ?? "";
        public StorySequenceSO StartStorySequence => startStorySequence;
        public StorySequenceSO EndStorySequence => endStorySequence;
    }
}