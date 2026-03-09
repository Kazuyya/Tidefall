using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    public enum StoryBackgroundType
    {
        Solid,
        Custom
    }

    public enum StoryTextEffect
    {
        None,
        Typewriter,
        Fade
    }

    public enum StoryTextOutEffect
    {
        None,
        Fade
    }

    [CreateAssetMenu(fileName = "StorySequence_", menuName = "Little Hero Journey/Story Sequence")]
    public class StorySequenceSO : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique id or name for this sequence (e.g. stage1_intro).")]
        [SerializeField] private string sequenceId = "";

        [Serializable]
        public class StoryStep
        {
            public StoryBackgroundType backgroundType = StoryBackgroundType.Solid;
            [HideInInspector] public Sprite backgroundImage;
            [HideInInspector] public Color backgroundColor = Color.black;

            [TextArea(2, 4)]
            public string textTop = "";

            [TextArea(2, 4)]
            public string textBottom = "";

            public StoryTextEffect textInEffect = StoryTextEffect.None;
            public StoryTextOutEffect textOutEffect = StoryTextOutEffect.None;
        }

        [Header("Steps (play in order)")]
        [SerializeField] private List<StoryStep> steps = new List<StoryStep>();

        public string SequenceId => sequenceId;
        public IReadOnlyList<StoryStep> Steps => steps;
        public int StepCount => steps != null ? steps.Count : 0;
        public StoryStep GetStep(int index) => steps != null && index >= 0 && index < steps.Count ? steps[index] : null;
    }
}
