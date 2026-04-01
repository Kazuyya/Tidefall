using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney.UI
{
    [CreateAssetMenu(fileName = "TutorialSequence_", menuName = "Little Hero Journey/Tutorial Sequence")]
    public class TutorialSequenceSO : ScriptableObject
    {
        [Serializable]
        public class TutorialStep
        {
            public Sprite image;
            [TextArea(2, 8)] public string text;
        }

        [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

        public int StepCount => steps != null ? steps.Count : 0;
        public IReadOnlyList<TutorialStep> Steps => steps;
        public TutorialStep GetStep(int index) => steps != null && index >= 0 && index < steps.Count ? steps[index] : null;
    }
}
