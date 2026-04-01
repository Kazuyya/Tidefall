using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LittleHeroJourney.UI
{
    public class TutorialManager : MonoBehaviour
    {
        private const bool ForceTutorialTrace = true;
        [SerializeField] private Image tutorialImage;
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private TextMeshProUGUI nextButtonText;
        [SerializeField] private string nextButtonLabel = "Next";
        [SerializeField] private string doneButtonLabel = "Done";
        [SerializeField] private bool hideWhenNoData = true;

        private TutorialSequenceSO _currentSequence;
        private string _currentTutorialId;
        private int _currentIndex;

        private void OnEnable()
        {
            BindButtons();
            if (_currentSequence == null || _currentSequence.StepCount == 0)
            {
                TraceTutorial("OnEnable without sequence, waiting SetSequence.");
                return;
            }
            _currentIndex = Mathf.Clamp(_currentIndex, 0, _currentSequence.StepCount - 1);
            RefreshCurrentStep();
        }

        private void OnDisable()
        {
            UnbindButtons();
        }

        public void SetSequence(TutorialSequenceSO sequence, bool resetIndex = true, string tutorialId = "")
        {
            _currentSequence = sequence;
            _currentTutorialId = tutorialId;
            if (resetIndex) _currentIndex = 0;
            TraceTutorial("SetSequence id=" + _currentTutorialId + " stepCount=" + (_currentSequence != null ? _currentSequence.StepCount : 0) + " active=" + gameObject.activeSelf);
            if (isActiveAndEnabled) RefreshCurrentStep();
        }

        public void Next()
        {
            if (_currentSequence == null) return;
            if (_currentIndex >= _currentSequence.StepCount - 1)
            {
                GameEventSystem.Publish(new UIActionEvent("TutorialCompleted", _currentTutorialId));
                CloseTutorial();
                return;
            }
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
                TraceTutorial("Refresh skipped, no sequence.");
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

            int lastIndex = _currentSequence.StepCount - 1;
            bool isFirst = _currentIndex <= 0;
            bool isLast = _currentIndex >= lastIndex;

            if (previousButton != null)
            {
                previousButton.gameObject.SetActive(!isFirst);
                previousButton.interactable = !isFirst;
            }
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
            if (nextButtonText != null)
                nextButtonText.text = isLast ? doneButtonLabel : nextButtonLabel;
            TraceTutorial("Refresh step index=" + _currentIndex + " last=" + isLast + " first=" + isFirst + " id=" + _currentTutorialId);
        }

        private void BindButtons()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(Next);
                nextButton.onClick.AddListener(Next);
            }
            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(Previous);
                previousButton.onClick.AddListener(Previous);
            }
        }

        private void UnbindButtons()
        {
            if (nextButton != null)
                nextButton.onClick.RemoveListener(Next);
            if (previousButton != null)
                previousButton.onClick.RemoveListener(Previous);
        }

        private void TraceTutorial(string msg)
        {
            if (!ForceTutorialTrace) return;
            Debug.Log("[TutorialManager] " + msg);
        }
    }
}
