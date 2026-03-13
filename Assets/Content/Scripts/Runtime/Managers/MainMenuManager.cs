using System.Collections;
using UnityEngine;

namespace LittleHeroJourney
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("BGM")]
        [Tooltip("ID effect BGM di AudioSet (nama yang sama dengan effectName di Audio Set)")]
        [SerializeField] private string bgmEffectId = "MainMenuBGM";

        [Header("Fade out (before load scene gameplay)")]
        [Tooltip("Duration of fade out BGM before change scene (call FadeOutBGM then wait for this before LoadScene)")]
        [SerializeField] private float fadeOutDuration = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog;

        private System.Action _startJourneyHandler;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(bgmEffectId)) return;
            if (CharacterEffectManager.Instance != null)
            {
                CharacterEffectManager.Instance.PlayBGM(bgmEffectId);
                if (showDebugLog) Debug.Log($"[{GetType().Name}] BGM started: {bgmEffectId}");
            }

            _startJourneyHandler = OnStartJourney;
            GameEventSystem.SubscribeAction("StartJourney", _startJourneyHandler);
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
