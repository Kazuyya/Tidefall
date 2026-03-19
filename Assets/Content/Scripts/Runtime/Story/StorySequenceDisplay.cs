using System;
using System.Collections;
using UnityEngine;

namespace LittleHeroJourney
{
    public class StorySequenceDisplay : MonoBehaviour
    {
        [Header("Pooling")]
        [SerializeField] private StoryStepPanel stepPanelPrefab;
        [SerializeField] private Transform stepPanelParent;

        [Header("Timing")]
        [Tooltip("Optional delay before the first story step starts playing (seconds).")]
        [SerializeField] private float initialDelayBeforeFirstStep = 1f;

        private StoryStepPanel[] _pool;
        private int _activeIndex = -1;
        private Coroutine _playRoutine;

        private StorySequenceSO _sequence;
        private int _stepIndex;
        private bool _advanceRequested;
        private StoryStepPanel _animatingPanel;

        public bool IsPlaying => _playRoutine != null;

        public bool IsAnimating
        {
            get
            {
                if (_pool == null) return false;
                for (int i = 0; i < _pool.Length; i++)
                {
                    if (_pool[i] != null && _pool[i].gameObject.activeSelf && _pool[i].IsAnimating)
                        return true;
                }
                return false;
            }
        }

        public void Play(StorySequenceSO sequence)
        {
            if (sequence == null || sequence.StepCount == 0) return;
            EnsurePool();
            Stop();
            _sequence = sequence;
            _stepIndex = 0;
            _activeIndex = -1;
            _advanceRequested = false;
            _playRoutine = StartCoroutine(PlayRoutine());
        }

        public void Stop()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }
            if (_pool != null)
            {
                for (int i = 0; i < _pool.Length; i++)
                {
                    if (_pool[i] != null)
                        _pool[i].gameObject.SetActive(false);
                }
            }
            _sequence = null;
            _stepIndex = 0;
            _activeIndex = -1;
            _advanceRequested = false;
        }

        /// <summary>
        /// Click behavior:
        /// - If currently animating (background/text in/out): complete immediately.
        /// - Otherwise: request immediate advance (skips remaining delay).
        /// </summary>
        public void RequestAdvance()
        {
            if (_sequence != null)
            {
                var step = _sequence.GetStep(_stepIndex);
                if (step != null && !step.canSkipStep) return;
            }
            if (IsAnimating)
            {
                CompleteAnimationsNow();
                return;
            }
            _advanceRequested = true;
        }

        public void CompleteAnimationsNow()
        {
            if (_pool == null) return;
            for (int i = 0; i < _pool.Length; i++)
            {
                if (_pool[i] != null && _pool[i].gameObject.activeSelf)
                {
                    if (_pool[i] == _animatingPanel)
                        _pool[i].CompleteNow();
                    else
                        _pool[i].HideAllTextImmediate();
                }
            }
        }

        private void EnsurePool()
        {
            if (_pool != null && _pool.Length == 2 && _pool[0] != null && _pool[1] != null) return;
            if (stepPanelPrefab == null) return;
            if (stepPanelParent == null) stepPanelParent = transform;

            _pool = new StoryStepPanel[2];
            for (int i = 0; i < 2; i++)
            {
                var inst = Instantiate(stepPanelPrefab, stepPanelParent);
                inst.gameObject.SetActive(false);
                _pool[i] = inst;
            }
        }

        private IEnumerator PlayRoutine()
        {
            if (_sequence == null || _pool == null || _pool.Length < 2) { _playRoutine = null; yield break; }

            StorySequenceSO.StoryStep previousStep = null;
            StoryStepPanel previousPanel = null;

            if (initialDelayBeforeFirstStep > 0f)
            {
                float elapsedInitial = 0f;
                _advanceRequested = false;
                while (elapsedInitial < initialDelayBeforeFirstStep && !_advanceRequested)
                {
                    elapsedInitial += Time.deltaTime;
                    yield return null;
                }
                _advanceRequested = false;
            }

            while (_sequence != null && _stepIndex < _sequence.StepCount)
            {
                var step = _sequence.GetStep(_stepIndex);
                if (step == null) { _stepIndex++; continue; }

                int incomingIndex = _activeIndex < 0 ? 0 : (1 - _activeIndex);
                StoryStepPanel incoming = _pool[incomingIndex];
                if (incoming == null) { _stepIndex++; continue; }

                if (previousPanel != null && previousStep != null)
                {
                    previousPanel.HideAllTextImmediate();

                    bool willAnimateBackground = step.backgroundInEffect == StoryBackgroundTransitionEffect.Fade &&
                                                 (previousStep.backgroundType != step.backgroundType ||
                                                  previousStep.GetDisplayColor() != step.GetDisplayColor() ||
                                                  previousStep.GetDisplayImage() != step.GetDisplayImage());
                    if (willAnimateBackground)
                    {
                        previousPanel.HideDialoguePanelImmediate();
                    }
                }

                incoming.gameObject.SetActive(true);
                incoming.transform.SetAsLastSibling();
                incoming.ConfigureForStep(step);

                bool animateBackground = step.backgroundInEffect == StoryBackgroundTransitionEffect.Fade &&
                                         (previousStep == null ||
                                          previousStep.backgroundType != step.backgroundType ||
                                          previousStep.GetDisplayColor() != step.GetDisplayColor() ||
                                          previousStep.GetDisplayImage() != step.GetDisplayImage());

                bool animateDialoguePanel = step.contentType == StoryContentType.Dialogue &&
                                            (previousStep == null || previousStep.contentType != StoryContentType.Dialogue);

                float initialBg = animateBackground ? 0f : 1f;
                float initialText;
                if (step.contentType == StoryContentType.Dialogue)
                {
                    initialText = 0f;
                }
                else
                {
                    initialText = step.backgroundInEffect == StoryBackgroundTransitionEffect.Fade
                        ? 0f
                        : (step.textInEffect == StoryTextEffect.Fade ? 0f : 1f);
                }
                incoming.SetInitialAlphas(initialBg, initialText);

                // Last step: publish as soon as we're about to show it (before anim in), so player spawns while step plays
                if (_sequence != null && _stepIndex == _sequence.StepCount - 1)
                    GameEventSystem.Publish(new UIActionEvent("StoryLastStepStarted"));

                _animatingPanel = incoming;
                yield return StartCoroutine(incoming.PlayInRoutine(step, animateBackground, animateDialoguePanel));
                _animatingPanel = null;

                if (previousPanel != null)
                    previousPanel.gameObject.SetActive(false);

                _activeIndex = incomingIndex;
                previousPanel = incoming;
                previousStep = step;

                float delay = Mathf.Max(0f, step.delayAfterTextComplete);
                float elapsed = 0f;
                _advanceRequested = false;
                while (elapsed < delay && !_advanceRequested)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (!_advanceRequested && step.textOutEffect == StoryTextOutEffect.Fade)
                {
                    yield return StartCoroutine(incoming.PlayTextOutOnlyRoutine(step));
                }

                if (_sequence != null && _stepIndex == _sequence.StepCount - 1)
                    GameEventSystem.Publish(new UIActionEvent("StoryLastStepCompleted"));

                _stepIndex++;
                _advanceRequested = false;
            }

            if (previousPanel != null && previousStep != null)
            {
                if (previousStep.backgroundOutEffect == StoryBackgroundTransitionEffect.Fade)
                    yield return StartCoroutine(previousPanel.PlayOutRoutine(previousStep));
                previousPanel.gameObject.SetActive(false);
            }

            _playRoutine = null;
        }

        public void OnNextClicked()
        {
            if (_sequence != null)
            {
                var step = _sequence.GetStep(_stepIndex);
                if (step != null && !step.canSkipStep) return;
            }
            if (JourneyManager.Instance != null)
                JourneyManager.Instance.RequestAdvanceStory();
        }
    }
}
