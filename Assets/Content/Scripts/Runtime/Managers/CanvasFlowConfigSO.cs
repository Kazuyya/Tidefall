using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "CanvasFlowConfig", menuName = "Little Hero Journey/UI/Canvas Flow Config")]
    public class CanvasFlowConfigSO : ScriptableObject
    {
        public enum TransitionMode
        {
            [Tooltip("Show new canvas without closing previous (overlay). E.g. Gameplay -> Pause")]
            Direct,
            
            [Tooltip("Close previous instant, show new instant (no animation). Fast switch.")]
            SnapSwitch,
            
            [Tooltip("Close previous instant (no animation), then play IN animation on new canvas.")]
            SnapOutThenIn,
            
            [Tooltip("Wait previous OUT animation, then new IN animation. Smooth transition.")]
            WaitOutThenIn,
            
            [Tooltip("Wait previous OUT animation, then instant show new. Hybrid.")]
            WaitOutThenSnapIn,
            
            [Tooltip("Play new IN and old OUT at the same time, both with animation.")]
            ParallelInOut,
            
            [Tooltip("Snap new canvas IN instantly while old canvas plays OUT animation (simultaneous).")]
            ParallelOutSnapIn,
        }
        
        [Serializable]
        public class TransitionFromRule
        {
            [Tooltip("Source canvas. Empty or '*'/'any' for wildcard (applies to all sources)")]
            public string fromCanvasId;
            public TransitionMode mode = TransitionMode.WaitOutThenIn;
            [Tooltip("Close previous canvas after IN animation completes?")]
            public bool closeAfterInComplete = true;
        }
        
        [Serializable]
        public class TransitionToEntry
        {
            [Tooltip("Target canvas. Empty or '*'/'any' for wildcard (applies to all targets)")]
            public string toCanvasId;
            [Tooltip("Rules list by From Id for this target")]
            public List<TransitionFromRule> fromRules = new List<TransitionFromRule>();
        }
        
        [Header("Transition Rules")]
        [Tooltip("Rules for canvas transitions. First matching rule is used.")]
        public List<TransitionToEntry> transitionRules = new List<TransitionToEntry>();
    }
}
