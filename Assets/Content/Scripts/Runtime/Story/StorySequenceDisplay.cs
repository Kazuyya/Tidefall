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
        [SerializeField] private Image customImageBottom;

        [Header("Text effect settings")]
        [SerializeField] private float typewriterCharsPerSecond = 35f;
        [SerializeField] private float fadeInDuration = 0.4f;
        [SerializeField] private float customImageFadeDuration = 0.4f;

        private Coroutine _effectCoroutine;
        private Coroutine _applyStepCoroutine;
        private bool _isAnimationPlaying;
        private bool _completeNow;
        private int _currentCustomSlot = -1;
        private bool _currentStepUseFadeOutOnExit = true;

        public bool IsAnimationPlaying => _isAnimationPlaying;

        public void ApplyStep(StorySequenceSO.StoryStep step)
        {
            if (step == null) return;
            StopEffectCoroutine();
            if (_applyStepCoroutine != null)
            {
                StopCoroutine(_applyStepCoroutine);
                _applyStepCoroutine = null;
            }
            _completeNow = false;

            if (step.backgroundType == StoryBackgroundType.Custom && (customImage != null || customImageBottom != null))
            {
                _isAnimationPlaying = true;
                _applyStepCoroutine = StartCoroutine(ApplyStepWithCustomBackgroundRoutine(step));
                return;
            }

            ApplyStepImmediate(step);
        }

        private void ApplyStepImmediate(StorySequenceSO.StoryStep step)
        {
            TextMeshProUGUI activeText = SetupTextForStep(step);
            if (step.backgroundType == StoryBackgroundType.Solid)
            {
                HideCustomImages();
                _currentCustomSlot = -1;
                if (solidColorImage != null)
                {
                    solidColorImage.color = step.GetDisplayColor();
                    solidColorImage.enabled = true;
                    solidColorImage.gameObject.SetActive(true);
                }
            }
            else
            {
                if (solidColorImage != null)
                {
                    solidColorImage.enabled = false;
                    solidColorImage.gameObject.SetActive(false);
                }
                Image single = customImage != null ? customImage : customImageBottom;
                if (single != null)
                {
                    single.sprite = step.GetDisplayImage();
                    SetImageAlpha(single, 1f);
                    single.enabled = true;
                    single.gameObject.SetActive(true);
                }
                _currentCustomSlot = 0;
            }

            if (activeText != null && step.textInEffect != StoryTextEffect.None)
                _effectCoroutine = StartCoroutine(PlayTextInEffectRoutine(activeText, step.textInEffect));
            else
                _isAnimationPlaying = false;
        }

        private TextMeshProUGUI SetupTextForStep(StorySequenceSO.StoryStep step)
        {
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
                if (textDialogue != null) textDialogue.gameObject.SetActive(false);
            }
            else
            {
                if (textNarrative != null) textNarrative.gameObject.SetActive(false);
                if (textDialogue != null)
                {
                    textDialogue.text = step.GetDisplayDialogueText();
                    textDialogue.maxVisibleCharacters = int.MaxValue;
                    textDialogue.gameObject.SetActive(true);
                    SetTextAlpha(textDialogue, 1f);
                    activeText = textDialogue;
                }
            }
            return activeText;
        }

        private IEnumerator ApplyStepWithCustomBackgroundRoutine(StorySequenceSO.StoryStep step)
        {
            if (solidColorImage != null)
            {
                solidColorImage.enabled = false;
                solidColorImage.gameObject.SetActive(false);
            }

            Image top = customImage;
            Image bottom = customImageBottom;
            if (top == null) top = bottom;
            if (bottom == null) bottom = top;
            if (top == null) { _isAnimationPlaying = false; yield break; }

            int nextSlot = _currentCustomSlot < 0 ? 0 : (1 - _currentCustomSlot);
            Image showImage = nextSlot == 0 ? top : bottom;
            Image hideImage = nextSlot == 0 ? bottom : top;

            if (hideImage != null && hideImage != showImage)
            {
                hideImage.gameObject.SetActive(false);
            }

            _currentStepUseFadeOutOnExit = step.useCustomImageFadeOutOnExit;

            Sprite sp = step.GetDisplayImage();
            if (showImage != null)
            {
                showImage.sprite = sp;
                showImage.color = Color.white;
                SetImageAlpha(showImage, step.useCustomImageFadeIn ? 0f : 1f);
                showImage.enabled = true;
                showImage.gameObject.SetActive(true);
            }

            if (showImage != null && step.useCustomImageFadeIn)
            {
                float dur = step.customImageFadeInDuration > 0f ? step.customImageFadeInDuration : customImageFadeDuration;
                if (dur > 0f)
                {
                    yield return FadeImageRoutine(showImage, 0f, 1f, dur);
                    if (_completeNow && showImage != null) SetImageAlpha(showImage, 1f);
                }
                else
                    SetImageAlpha(showImage, 1f);
            }
            _currentCustomSlot = nextSlot;

            TextMeshProUGUI activeText = SetupTextForStep(step);
            if (activeText != null && step.textInEffect != StoryTextEffect.None)
                yield return PlayTextInEffectRoutine(activeText, step.textInEffect);
            else
                _isAnimationPlaying = false;

            _applyStepCoroutine = null;
        }

        private IEnumerator FadeImageRoutine(Image img, float fromA, float toA, float duration = -1f)
        {
            if (img == null) yield break;
            if (duration <= 0f) duration = customImageFadeDuration;
            if (duration <= 0f) { SetImageAlpha(img, toA); yield break; }
            float elapsed = 0f;
            while (elapsed < duration && !_completeNow)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float a = Mathf.Lerp(fromA, toA, t);
                SetImageAlpha(img, a);
                yield return null;
            }
            if (img != null) SetImageAlpha(img, toA);
        }

        private static void SetImageAlpha(Image img, float a)
        {
            if (img == null) return;
            Color c = img.color;
            c.a = Mathf.Clamp01(a);
            img.color = c;
        }

        private void HideCustomImages()
        {
            if (customImage != null)
            {
                customImage.sprite = null;
                customImage.enabled = false;
                customImage.gameObject.SetActive(false);
            }
            if (customImageBottom != null)
            {
                customImageBottom.sprite = null;
                customImageBottom.enabled = false;
                customImageBottom.gameObject.SetActive(false);
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
            if (_applyStepCoroutine != null)
            {
                StopCoroutine(_applyStepCoroutine);
                _applyStepCoroutine = null;
            }
            if (customImage != null && customImage.gameObject.activeSelf)
                SetImageAlpha(customImage, 1f);
            if (customImageBottom != null && customImageBottom.gameObject.activeSelf)
                SetImageAlpha(customImageBottom, 1f);
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

        public IEnumerator PrepareForNextStepRoutine()
        {
            StopEffectCoroutine();
            if (_applyStepCoroutine != null)
            {
                StopCoroutine(_applyStepCoroutine);
                _applyStepCoroutine = null;
            }
            if (_currentCustomSlot >= 0 && _currentStepUseFadeOutOnExit)
            {
                Image cur = _currentCustomSlot == 0 ? customImage : customImageBottom;
                if (cur != null && cur.gameObject.activeSelf)
                {
                    yield return FadeImageRoutine(cur, 1f, 0f);
                    cur.gameObject.SetActive(false);
                }
            }
            if (_currentCustomSlot >= 0)
            {
                Image cur = _currentCustomSlot == 0 ? customImage : customImageBottom;
                if (cur != null) cur.gameObject.SetActive(false);
                _currentCustomSlot = -1;
            }
            ClearInternal();
        }

        public void Clear()
        {
            StopEffectCoroutine();
            if (_applyStepCoroutine != null)
            {
                StopCoroutine(_applyStepCoroutine);
                _applyStepCoroutine = null;
            }
            _currentCustomSlot = -1;
            ClearInternal();
        }

        private void ClearInternal()
        {
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
            HideCustomImages();
        }

        public void OnNextClicked()
        {
            if (JourneyManager.Instance != null)
                JourneyManager.Instance.RequestAdvanceStory();
        }
    }
}
