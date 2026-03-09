using UnityEngine;

namespace LittleHeroJourney.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("No save (pertama kali)")]
        [SerializeField] private GameObject panelNoSave;
        [Header("Sudah ada save")]
        [SerializeField] private GameObject panelHasSave;

        private void OnEnable()
        {
            RefreshPanels();
        }

        public void RefreshPanels()
        {
            bool hasSave = GameManager.Instance != null && GameManager.Instance.HasAnyJourneySave;
            if (panelNoSave != null) panelNoSave.SetActive(!hasSave);
            if (panelHasSave != null) panelHasSave.SetActive(hasSave);
        }

        public void StartJourney()
        {
            if (JourneyManager.Instance != null)
            {
                JourneyManager.Instance.InitializeNewJourney();
                JourneyManager.Instance.LoadStage(1);
            }
        }

        public void ContinueJourney()
        {
            if (JourneyManager.Instance != null)
                JourneyManager.Instance.ContinueJourney();
        }

        public void NewJourney()
        {
            if (JourneyManager.Instance != null)
                JourneyManager.Instance.NewJourney();
        }
    }
}
