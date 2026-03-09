using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "JourneysData", menuName = "Little Hero Journey/Journeys Data")]
    public class JourneysDataSO : ScriptableObject
    {
        [SerializeField] private List<JourneyDataSO> journeys = new List<JourneyDataSO>();

        public int JourneyCount => journeys != null ? journeys.Count : 0;

        public JourneyDataSO GetJourneyByNumber(int stageNumber)
        {
            if (journeys == null || stageNumber < 1 || stageNumber > journeys.Count) return null;
            return journeys[stageNumber - 1];
        }

        public JourneyDataSO GetJourneyByIndex(int index)
        {
            if (journeys == null || index < 0 || index >= journeys.Count) return null;
            return journeys[index];
        }
    }
}
