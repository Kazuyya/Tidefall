using UnityEngine;

namespace LittleHeroJourney.UI
{
    public class MainMenuController : MonoBehaviour
    {
        public void StartJourney()
        {
            if (JourneyManager.Instance != null)
                JourneyManager.Instance.NewJourney();
        }

        public void ContinueJourney()
        {
            if (JourneyManager.Instance != null)
                JourneyManager.Instance.ContinueJourney();
        }
    }
}
