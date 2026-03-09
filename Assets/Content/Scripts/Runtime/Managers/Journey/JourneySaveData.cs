using System;
using System.Collections.Generic;

namespace LittleHeroJourney
{
    [Serializable]
    public class JourneySaveData
    {
        public List<StageStateData> stages = new List<StageStateData>();
        public int currentPlayerHealth = 100;
        public long lastSavedTimestampUtc; 

        [Serializable]
        public class StageStateData
        {
            public int stageNumber;
            public bool isUnlocked;
            public int bestScore;
            public bool isCompleted;
        }
    }
}
