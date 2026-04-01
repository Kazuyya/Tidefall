using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LittleHeroJourney.UI
{
    public class TutorialManager : MonoBehaviour
    {
        [SerializeField] private Image tutorialImage;
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private bool hideWhenNoData = true;

        private TutorialSequenceSO _currentSequence;
        private int _currentIndex;

        private void OnEnable()
        {
            if (_currentSequence == null || _currentSequence.StepCount == 0)
            {
                if (hideWhenNoData) gameObject.SetActive(false);
                return;
            }
            _currentIndex = Mathf.Clamp(_currentIndex, 0, _currentSequence.StepCount - 1);
            RefreshCurrentStep();
        }

        public void SetSequence(TutorialSequenceSO sequence, bool resetIndex = true)
        {
            _currentSequence = sequence;
            if (resetIndex) _currentIndex = 0;
            if (isActiveAndEnabled) RefreshCurrentStep();
        }

        public void Next()
        {
            if (_currentSequence == null) return;
            if (_currentIndex >= _currentSequence.StepCount - 1) return;
            _currentIndex++;
            RefreshCurrentStep();
        }

        public void Previous()
        {
            if (_currentSequence == null) return;
            if (_currentIndex <= 0) return;
            _currentIndex--;
            RefreshCurrentStep();
        }

        public void CloseTutorial()
        {
            gameObject.SetActive(false);
        }

        private void RefreshCurrentStep()
        {
            if (_currentSequence == null || _currentSequence.StepCount == 0)
            {
                if (hideWhenNoData) gameObject.SetActive(false);
                return;
            }

            var step = _currentSequence.GetStep(_currentIndex);
            if (step == null) return;

            if (tutorialImage != null)
            {
                tutorialImage.sprite = step.image;
                tutorialImage.enabled = step.image != null;
            }

            if (tutorialText != null)
                tutorialText.text = step.text ?? string.Empty;

            if (previousButton != null)
                previousButton.interactable = _currentIndex > 0;
            if (nextButton != null)
                nextButton.interactable = _currentIndex < _currentSequence.StepCount - 1;
        }
    }
}
