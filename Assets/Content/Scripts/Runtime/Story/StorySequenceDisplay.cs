using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LittleHeroJourney
{
    public class StorySequenceDisplay : MonoBehaviour
    {
        [Header("Text (TMP)")]
        [SerializeField] private TextMeshProUGUI textNarrative;
        [SerializeField] private TextMeshProUGUI textDialogue;

        [Header("Background")]
        [SerializeField] private Image solidColorImage;
        [SerializeField] private Image customImage;

        [Header("Text effect settings")]
        [SerializeField] private float typewriterCharsPerSecond = 35f;
        [SerializeField] private float fadeInDuration = 0.4f;

        private Coroutine _effectCoroutine;
        private bool _isAnimationPlaying;
        private bool _completeNow;

        public bool IsAnimationPlaying => _isAnimationPlaying;

        public void ApplyStep(StorySequenceSO.StoryStep step)
        {
            if (step == null) return;
            StopEffectCoroutine();
            _completeNow = false;

            TextMeshProUGUI activeText = null;
            if (step.IsNarrative)
            {
                if (textNarrative != null)
                {
                    textNarrative.text = step.GetDisplayNarrativeText();
                    textNarrative.maxVisibleCharacters = int.MaxValue;
                    textNarrative.gameObject.SetActive(true);
                    SetTextAlpha(textNarrative, 1f);
                    activeText = textNarrative;
                }
                if (textDialogue != null)
                    textDialogue.gameObject.SetActive(false);
            }
            else
            {
                if (textNarrative != null)
                    textNarrative.gameObject.SetActive(false);
                if (textDialogue != null)
                {
                    textDialogue.text = step.GetDisplayDialogueText();
                    textDialogue.maxVisibleCharacters = int.MaxValue;
                    textDialogue.gameObject.SetActive(true);
                    SetTextAlpha(textDialogue, 1f);
                    activeText = textDialogue;
                }
            }

            if (step.backgroundType == StoryBackgroundType.Solid)
            {
                if (solidColorImage != null)
                {
                    solidColorImage.color = step.GetDisplayColor();
                    solidColorImage.enabled = true;
                    solidColorImage.gameObject.SetActive(true);
                }
                if (customImage != null)
                {
                    customImage.enabled = false;
                    customImage.gameObject.SetActive(false);
                }
            }
            else
            {
                if (solidColorImage != null)
                {
                    solidColorImage.enabled = false;
                    solidColorImage.gameObject.SetActive(false);
                }
                if (customImage != null)
                {
                    customImage.sprite = step.GetDisplayImage();
                    customImage.color = Color.white;
                    customImage.enabled = true;
                    customImage.gameObject.SetActive(true);
                }
            }

            if (activeText != null && step.textInEffect != StoryTextEffect.None)
            {
                _effectCoroutine = StartCoroutine(PlayTextInEffectRoutine(activeText, step.textInEffect));
            }
            else
            {
                _isAnimationPlaying = false;
            }
        }

        private void StopEffectCoroutine()
        {
            if (_effectCoroutine != null)
            {
                StopCoroutine(_effectCoroutine);
                _effectCoroutine = null;
            }
            _isAnimationPlaying = false;
        }

        private IEnumerator PlayTextInEffectRoutine(TextMeshProUGUI tmp, StoryTextEffect effect)
        {
            _isAnimationPlaying = true;
            if (tmp == null) { _isAnimationPlaying = false; yield break; }

            if (effect == StoryTextEffect.Typewriter)
            {
                tmp.ForceMeshUpdate();
                int totalChars = tmp.textInfo.characterCount;
                tmp.maxVisibleCharacters = 0;
                float delayPerChar = 1f / Mathf.Max(1f, typewriterCharsPerSecond);
                for (int i = 0; i <= totalChars && !_completeNow; i++)
                {
                    tmp.maxVisibleCharacters = i;
                    yield return new WaitForSeconds(delayPerChar);
                }
                tmp.maxVisibleCharacters = int.MaxValue;
            }
            else if (effect == StoryTextEffect.Fade)
            {
                float elapsed = 0f;
                while (elapsed < fadeInDuration && !_completeNow)
                {
                    elapsed += Time.deltaTime;
                    float a = Mathf.Clamp01(elapsed / fadeInDuration);
                    SetTextAlpha(tmp, a);
                    yield return null;
                }
                SetTextAlpha(tmp, 1f);
            }

            _effectCoroutine = null;
            _isAnimationPlaying = false;
        }

        private static void SetTextAlpha(TextMeshProUGUI tmp, float a)
        {
            if (tmp == null) return;
            Color c = tmp.color;
            c.a = a;
            tmp.color = c;
        }

        public void CompleteAnimationNow()
        {
            _completeNow = true;
            if (_effectCoroutine != null)
            {
                StopCoroutine(_effectCoroutine);
                _effectCoroutine = null;
            }
            if (textNarrative != null && textNarrative.gameObject.activeSelf)
            {
                textNarrative.maxVisibleCharacters = int.MaxValue;
                SetTextAlpha(textNarrative, 1f);
            }
            if (textDialogue != null && textDialogue.gameObject.activeSelf)
            {
                textDialogue.maxVisibleCharacters = int.MaxValue;
                SetTextAlpha(textDialogue, 1f);
            }
            _isAnimationPlaying = false;
        }

        public void Clear()
        {
            StopEffectCoroutine();
            if (textNarrative != null)
            {
                textNarrative.text = "";
                textNarrative.gameObject.SetActive(false);
            }
            if (textDialogue != null)
            {
                textDialogue.text = "";
                textDialogue.gameObject.SetActive(false);
            }
            if (solidColorImage != null)
            {
                solidColorImage.enabled = false;
                solidColorImage.gameObject.SetActive(false);
            }
            if (customImage != null)
            {
                customImage.sprite = null;
                customImage.enabled = false;
                customImage.gameObject.SetActive(false);
            }
        }

        public void OnNextClicked()
        {
            if (JourneyManager.Instance != null)
                JourneyManager.Instance.RequestAdvanceStory();
        }
    }
}
