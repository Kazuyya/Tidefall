using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "SceneFlowConfig", menuName = "Little Hero Journey/Scene/Scene Flow Config")]
    public class SceneFlowConfigSO : ScriptableObject
    {
        public enum SceneKind
        {
            Splash,
            Loading,
            MainMenu,
            Gameplay
        }

        [Serializable]
        public class SceneIdEntry
        {
            public SceneKind kind;
            public string id;
            [SceneAttribute] public string scenePath;
        }

        [Serializable]
        public class TransitionFromRule
        {
            public string fromId;
            public bool useLoading;
            public bool closeBeforeLoading;
        }

        [Serializable]
        public class TransitionToEntry
        {
            public string toId;
            public List<TransitionFromRule> fromRules = new List<TransitionFromRule>();
        }

        public bool useSplash = false;
        public string splashId;
        public string mainMenuId;

        public List<SceneIdEntry> scenes = new List<SceneIdEntry>();
        public List<TransitionToEntry> transitions = new List<TransitionToEntry>();
    }
}
