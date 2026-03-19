using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LittleHeroJourney
{
    public class StoryStepPanel : MonoBehaviour
    {
        [Header("Text (TMP)")]
        [SerializeField] private TextMeshProUGUI textNarrative;
        [SerializeField] private TextMeshProUGUI textDialogue;
        [SerializeField] private Image dialogueBackgroundImage;
        [SerializeField] private CanvasGroup textGroup;

        [Header("Background")]
        [SerializeField] private Image solidColorImage;
        [SerializeField] private Image customImage;
        [SerializeField] private CanvasGroup backgroundGroup;

        [Header("Defaults")]
        [SerializeField] private float defaultBackgroundFadeDuration = 0.35f;
        [SerializeField] private float defaultTextFadeDuration = 0.25f;
        [SerializeField] private float typewriterCharsPerSecond = 35f;

        private bool _completeNow;
        private bool _isAnimating;

        public bool IsAnimating => _isAnimating;

        public void ConfigureForStep(StorySequenceSO.StoryStep step)
        {
            if (step == null) return;

            if (solidColorImage != null)
            {
                solidColorImage.gameObject.SetActive(step.backgroundType == StoryBackgroundType.Solid);
                solidColorImage.enabled = step.backgroundType == StoryBackgroundType.Solid;
                if (step.backgroundType == StoryBackgroundType.Solid)
                    solidColorImage.color = step.GetDisplayColor();
            }

            if (customImage != null)
            {
                customImage.gameObject.SetActive(step.backgroundType == StoryBackgroundType.Custom);
                customImage.enabled = step.backgroundType == StoryBackgroundType.Custom;
                if (step.backgroundType == StoryBackgroundType.Custom)
                {
                    customImage.sprite = step.GetDisplayImage();
                    customImage.color = Color.white;
                }
                else
                {
                    customImage.sprite = null;
                }
            }

            SetupText(step);
        }

        private void SetupText(StorySequenceSO.StoryStep step)
        {
            if (step == null) return;
            if (step.IsNarrative)
            {
                if (textNarrative != null)
                {
                    textNarrative.text = step.GetDisplayNarrativeText();
                    textNarrative.maxVisibleCharacters = int.MaxValue;
                    SetTextAlpha(textNarrative, 1f);
                    textNarrative.gameObject.SetActive(true);
                }
                if (textDialogue != null)
                {
                    textDialogue.text = "";
                    textDialogue.gameObject.SetActive(false);
                }
            }
            else
            {
                if (textNarrative != null)
                {
                    textNarrative.text = "";
                    textNarrative.gameObject.SetActive(false);
                }
                if (textDialogue != null)
                {
                    textDialogue.text = step.GetDisplayDialogueText();
                    textDialogue.maxVisibleCharacters = int.MaxValue;
                    SetTextAlpha(textDialogue, 1f);
                    textDialogue.gameObject.SetActive(true);
                }
            }
        }

        public void SetInitialAlphas(float backgroundAlpha, float textAlpha)
        {
            if (backgroundGroup != null) backgroundGroup.alpha = backgroundAlpha;
            if (textGroup != null) textGroup.alpha = textAlpha;
        }

        public IEnumerator PlayInRoutine(StorySequenceSO.StoryStep step, bool animateBackground, bool animateDialoguePanel)
        {
            if (step == null) yield break;
            _completeNow = false;
            _isAnimating = true;

            if (step.contentType == StoryContentType.Dialogue && animateDialoguePanel && textDialogue != null)
            {
                textDialogue.maxVisibleCharacters = 0;
                SetTextAlpha(textDialogue, 0f);
            }

            float bgDur = step.backgroundFadeInDuration > 0f ? step.backgroundFadeInDuration : defaultBackgroundFadeDuration;
            if (animateBackground && step.backgroundInEffect == StoryBackgroundTransitionEffect.Fade && backgroundGroup != null)
            {
                yield return FadeCanvasGroup(backgroundGroup, backgroundGroup.alpha, 1f, bgDur);
            }
            else if (backgroundGroup != null)
            {
                backgroundGroup.alpha = 1f;
            }

            if (animateDialoguePanel && step.contentType == StoryContentType.Dialogue && textGroup != null)
            {
                yield return FadeCanvasGroup(textGroup, textGroup.alpha, 1f, defaultTextFadeDuration);
            }
            else if (textGroup != null && step.contentType == StoryContentType.Dialogue)
            {
                textGroup.alpha = 1f;
            }

            yield return PlayTextInRoutine(step);

            _isAnimating = false;
        }

        private IEnumerator PlayTextInRoutine(StorySequenceSO.StoryStep step)
        {
            if (step == null) yield break;
            TextMeshProUGUI tmp = GetActiveText();
            if (tmp == null) yield break;

            if (step.textInEffect == StoryTextEffect.None)
            {
                if (textGroup != null) textGroup.alpha = 1f;
                tmp.maxVisibleCharacters = int.MaxValue;
                SetTextAlpha(tmp, 1f);
                yield break;
            }

            if (step.contentType != StoryContentType.Dialogue && step.textInEffect == StoryTextEffect.Fade)
            {
                if (textGroup != null)
                {
                    float dur = defaultTextFadeDuration;
                    yield return FadeCanvasGroup(textGroup, textGroup.alpha, 1f, dur);
                }
                else
                {
                    float dur = defaultTextFadeDuration;
                    yield return FadeTextAlpha(tmp, 0f, 1f, dur);
                }
            }
            else if (step.textInEffect == StoryTextEffect.Typewriter)
            {
                if (textGroup != null) textGroup.alpha = 1f;
                SetTextAlpha(tmp, 1f);
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
        }

        public IEnumerator PlayOutRoutine(StorySequenceSO.StoryStep step)
        {
            if (step == null) yield break;
            _completeNow = false;
            _isAnimating = true;

            float bgDur = step.backgroundFadeOutDuration > 0f ? step.backgroundFadeOutDuration : defaultBackgroundFadeDuration;
            if (step.backgroundOutEffect == StoryBackgroundTransitionEffect.Fade && backgroundGroup != null)
            {
                yield return FadeCanvasGroup(backgroundGroup, backgroundGroup.alpha, 0f, bgDur);
            }
            else if (backgroundGroup != null)
            {
                backgroundGroup.alpha = 0f;
            }

            if (step.textOutEffect == StoryTextOutEffect.Fade)
            {
                float dur = defaultTextFadeDuration;
                if (textGroup != null) yield return FadeCanvasGroup(textGroup, textGroup.alpha, 0f, dur);
                else
                {
                    var tmp = GetActiveText();
                    if (tmp != null) yield return FadeTextAlpha(tmp, 1f, 0f, dur);
                }
            }
            else
            {
                if (textGroup != null) textGroup.alpha = 0f;
            }

            _isAnimating = false;
        }

        public IEnumerator PlayTextOutOnlyRoutine(StorySequenceSO.StoryStep step)
        {
            if (step == null) yield break;
            if (step.textOutEffect != StoryTextOutEffect.Fade) yield break;

            _completeNow = false;
            _isAnimating = true;

            float dur = defaultTextFadeDuration;
            if (textGroup != null)
            {
                yield return FadeCanvasGroup(textGroup, textGroup.alpha, 0f, dur);
            }
            else
            {
                var tmp = GetActiveText();
                if (tmp != null) yield return FadeTextAlpha(tmp, 1f, 0f, dur);
            }

            _isAnimating = false;
        }

        public void CompleteNow()
        {
            _completeNow = true;
            if (backgroundGroup != null) backgroundGroup.alpha = 1f;
            if (textGroup != null) textGroup.alpha = 1f;
            var tmp = GetActiveText();
            if (tmp != null)
            {
                tmp.maxVisibleCharacters = int.MaxValue;
                SetTextAlpha(tmp, 1f);
            }
            _isAnimating = false;
        }

        public void HideDialoguePanelImmediate()
        {
            if (textGroup != null)
                textGroup.alpha = 0f;

            if (textDialogue != null)
            {
                textDialogue.maxVisibleCharacters = 0;
                SetTextAlpha(textDialogue, 0f);
            }
        }

        public void HideAllTextImmediate()
        {
            if (textGroup != null)
                textGroup.alpha = 0f;

            if (textNarrative != null)
            {
                textNarrative.maxVisibleCharacters = 0;
                SetTextAlpha(textNarrative, 0f);
            }

            if (textDialogue != null)
            {
                textDialogue.maxVisibleCharacters = 0;
                SetTextAlpha(textDialogue, 0f);
            }
        }

        private TextMeshProUGUI GetActiveText()
        {
            if (textNarrative != null && textNarrative.gameObject.activeSelf) return textNarrative;
            if (textDialogue != null && textDialogue.gameObject.activeSelf) return textDialogue;
            return null;
        }

        private static void SetTextAlpha(TextMeshProUGUI tmp, float a)
        {
            if (tmp == null) return;
            Color c = tmp.color;
            c.a = Mathf.Clamp01(a);
            tmp.color = c;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup g, float from, float to, float duration)
        {
            if (g == null) yield break;
            if (duration <= 0f) { g.alpha = to; yield break; }
            float elapsed = 0f;
            while (elapsed < duration && !_completeNow)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                g.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            g.alpha = to;
        }

        private IEnumerator FadeTextAlpha(TextMeshProUGUI tmp, float from, float to, float duration)
        {
            if (tmp == null) yield break;
            if (duration <= 0f) { SetTextAlpha(tmp, to); yield break; }
            float elapsed = 0f;
            while (elapsed < duration && !_completeNow)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetTextAlpha(tmp, Mathf.Lerp(from, to, t));
                yield return null;
            }
            SetTextAlpha(tmp, to);
        }
    }
}

