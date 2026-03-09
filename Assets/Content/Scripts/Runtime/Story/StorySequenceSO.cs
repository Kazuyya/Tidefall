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

    public enum StoryContentType
    {
        Narrative,
        Dialogue
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
        [SerializeField] private string sequenceId = "";

        [Serializable]
        public class StoryStep
        {
            public string stepId = "";
            public StoryBackgroundType backgroundType = StoryBackgroundType.Solid;
            [HideInInspector] public Sprite backgroundImage;
            [HideInInspector] public Color backgroundColor = Color.black;

            public StoryContentType contentType = StoryContentType.Narrative;
            [HideInInspector] public string narrativeText = "";
            [HideInInspector] public string speakerName = "";
            [HideInInspector] public string dialogueLine = "";

            public StoryTextEffect textInEffect = StoryTextEffect.None;
            public StoryTextOutEffect textOutEffect = StoryTextOutEffect.None;

            public Color GetDisplayColor()
            {
                return backgroundType == StoryBackgroundType.Solid ? backgroundColor : Color.white;
            }

            public Sprite GetDisplayImage()
            {
                return backgroundType == StoryBackgroundType.Custom ? backgroundImage : null;
            }

            public bool IsNarrative => contentType == StoryContentType.Narrative;

            public string GetDisplayNarrativeText() => narrativeText ?? "";

            public string GetDisplayDialogueText()
            {
                if (string.IsNullOrEmpty(speakerName)) return dialogueLine ?? "";
                return (speakerName ?? "").Trim() + ": " + (dialogueLine ?? "");
            }
        }

        [Header("Steps (play in order)")]
        [SerializeField] private List<StoryStep> steps = new List<StoryStep>();

        public string SequenceId => sequenceId;
        public IReadOnlyList<StoryStep> Steps => steps;
        public int StepCount => steps != null ? steps.Count : 0;
        public StoryStep GetStep(int index) => steps != null && index >= 0 && index < steps.Count ? steps[index] : null;
    }
}
