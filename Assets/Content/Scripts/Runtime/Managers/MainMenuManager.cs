using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LittleHeroJourney
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("BGM")]
        [Tooltip("BGM effect ID in AudioSet (same name as effectName in Audio Set)")]
        [SerializeField] private string bgmEffectId = "MainMenuBGM";

        [Header("Fade out (before load scene gameplay)")]
        [Tooltip("Duration of fade out BGM before change scene (call FadeOutBGM then wait for this before LoadScene)")]
        [SerializeField] private float fadeOutDuration = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog;

        [Header("Main Menu UI")]
        [Tooltip("Main primary button label (Start / Journeys).")]
        [SerializeField] private TextMeshProUGUI primaryButtonLabel;
        [Tooltip("Primary button (publishes Play).")]
        [SerializeField] private Button primaryButton;
        [Tooltip("Continue button (publishes ContinueJourney). Hidden until journey 1 completed.")]
        [SerializeField] private GameObject continueButtonRoot;
        [SerializeField] private Button continueButton;

        private System.Action _startJourneyHandler;

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(bgmEffectId) && CharacterEffectManager.Instance != null)
            {
                CharacterEffectManager.Instance.PlayBGM(bgmEffectId);
                if (showDebugLog) Debug.Log($"[{GetType().Name}] BGM started: {bgmEffectId}");
            }

            _startJourneyHandler = OnStartJourney;
            GameEventSystem.SubscribeAction("StartJourney", _startJourneyHandler);

            RefreshMenuUI();
        }

        private void OnDisable()
        {
            if (_startJourneyHandler != null)
            {
                GameEventSystem.UnsubscribeAction("StartJourney", _startJourneyHandler);
                _startJourneyHandler = null;
            }
            if (CharacterEffectManager.Instance != null)
                CharacterEffectManager.Instance.StopBGM();
        }

        private void RefreshMenuUI()
        {
            int totalJourneys = JourneyManager.Instance != null ? JourneyManager.Instance.GetTotalLevels() : 0;
            int firstUncompleted = JourneyManager.Instance != null ? JourneyManager.Instance.GetFirstUncompletedStageNumber() : 1;
            bool hasAnySave = JourneyManager.HasAnySave();
            bool hasProgressBeyondJourney1 = JourneyManager.Instance != null && hasAnySave && firstUncompleted > 1;
            bool allJourneysCompleted = JourneyManager.Instance != null && hasAnySave && totalJourneys > 0 && firstUncompleted > totalJourneys;

            if (primaryButton != null)
            {
                primaryButton.onClick.RemoveAllListeners();
                primaryButton.onClick.AddListener(() => GameEventSystem.Publish(new UIActionEvent("Play")));
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() => GameEventSystem.Publish(new UIActionEvent("ContinueJourney")));
            }

            if (continueButtonRoot != null)
                continueButtonRoot.SetActive(hasProgressBeyondJourney1 && !allJourneysCompleted);

            if (primaryButtonLabel != null)
                primaryButtonLabel.text = hasProgressBeyondJourney1 ? "journeys" : "start";

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] RefreshMenuUI: showContinue={(hasProgressBeyondJourney1 && !allJourneysCompleted)}, allCompleted={allJourneysCompleted}, primaryLabel={(primaryButtonLabel != null ? primaryButtonLabel.text : "<null>")}");
        }

        private void OnStartJourney()
        {
            FadeOutBGM();
            StartCoroutine(WaitThenPublishFadeComplete());
        }

        private IEnumerator WaitThenPublishFadeComplete()
        {
            yield return new WaitForSeconds(fadeOutDuration);
            GameEventSystem.Publish(new UIActionEvent("StartJourneyFadeComplete"));
        }

        public void FadeOutBGM()
        {
            if (CharacterEffectManager.Instance != null)
                CharacterEffectManager.Instance.FadeOutBGM(fadeOutDuration);
        }

        public void FadeOutBGM(float duration)
        {
            if (CharacterEffectManager.Instance != null)
                CharacterEffectManager.Instance.FadeOutBGM(duration);
        }

        public float DefaultFadeOutDuration => fadeOutDuration;

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
