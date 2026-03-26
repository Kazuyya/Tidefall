using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace LittleHeroJourney.UI
{
    public class JourneySelectorManager : MonoBehaviour
    {
        public JourneysDataSO journeysData;
        public JourneyDisplay[] journeyCards;
        public ScrollRect scrollRect;
        public Image detailPreviewImage;
        public TextMeshProUGUI detailTitleText;
        public TextMeshProUGUI detailDescriptionText;
        public Button playButton;
        private int _selectedStage = -1;
        private Coroutine _deferredSelectRoutine;
        private void OnEnable()
        {
            SetupFromJourneysData();
            UpdateDetails(null, null);
            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(OnPlayClicked);
                playButton.interactable = false;
            }
        }
        private void SetupFromJourneysData()
        {
            if (journeyCards == null || journeysData == null) return;
            int count = Mathf.Min(journeyCards.Length, journeysData.JourneyCount);
            for (int i = 0; i < count; i++)
            {
                var card = journeyCards[i];
                var data = journeysData.GetJourneyByIndex(i);
                if (card == null || data == null) continue;
                int stageNumber = i + 1;
                card.Configure(data.JourneyTitle, data.Description, data.SelectorCardImage, stageNumber, this);
                if (!card.gameObject.activeSelf) card.gameObject.SetActive(true);
            }
            for (int i = count; i < journeyCards.Length; i++)
                if (journeyCards[i] != null && journeyCards[i].gameObject.activeSelf) journeyCards[i].gameObject.SetActive(false);
            ApplyVisibilityByCompletion();
            AutoSelectLatestCompleted();
        }
        public void SelectJourney(int stageNumber)
        {
            if (journeysData == null) return;
            _selectedStage = stageNumber;
            var data = journeysData.GetJourneyByNumber(stageNumber);
            UpdateDetails(data != null ? data.SelectorDetailImage : null, data);
            if (playButton != null)
                playButton.interactable = true;
        }
        private void UpdateDetails(Sprite preview, JourneyDataSO data = null)
        {
            if (detailPreviewImage != null)
                detailPreviewImage.sprite = preview;
            if (detailTitleText != null)
                detailTitleText.text = data != null ? data.JourneyTitle : "";
            if (detailDescriptionText != null)
                detailDescriptionText.text = data != null ? data.Description : "";
        }
        private void OnPlayClicked()
        {
            if (_selectedStage <= 0) return;
            if (JourneyManager.Instance != null)
                JourneyManager.Instance.RequestLoadStage(_selectedStage);
        }
        private int GetLastCompletedStage()
        {
            if (JourneyManager.Instance == null) return 0;
            int firstUncompleted = JourneyManager.Instance.GetFirstUncompletedStageNumber();
            int lastCompleted = Mathf.Max(0, firstUncompleted - 1);
            int total = journeysData != null ? journeysData.JourneyCount : 0;
            if (total > 0) lastCompleted = Mathf.Min(lastCompleted, total);
            return lastCompleted;
        }
        private void ApplyVisibilityByCompletion()
        {
            int lastCompleted = GetLastCompletedStage();
            if (journeyCards == null) return;
            for (int i = 0; i < journeyCards.Length; i++)
            {
                var card = journeyCards[i];
                if (card == null) continue;
                int stageNumber = i + 1;
                bool show = stageNumber <= lastCompleted && stageNumber <= (journeysData != null ? journeysData.JourneyCount : 0);
                if (card.gameObject.activeSelf != show) card.gameObject.SetActive(show);
            }
        }
        private void AutoSelectLatestCompleted()
        {
            int lastCompleted = GetLastCompletedStage();
            if (lastCompleted <= 0)
            {
                UpdateDetails(null, null);
                if (playButton != null) playButton.interactable = false;
                return;
            }
            if (_deferredSelectRoutine != null) StopCoroutine(_deferredSelectRoutine);
            _deferredSelectRoutine = StartCoroutine(DeferredSelectAndScroll(lastCompleted));
        }
        private IEnumerator DeferredSelectAndScroll(int stageNumber)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            SelectJourney(stageNumber);
            yield return null;
            Canvas.ForceUpdateCanvases();
            AutoScrollToStage(stageNumber);
            _deferredSelectRoutine = null;
        }
        private void AutoScrollToStage(int stageNumber)
        {
            if (scrollRect == null || journeyCards == null || journeyCards.Length == 0) return;
            int countVisible = 0;
            for (int i = 0; i < journeyCards.Length; i++)
            {
                var c = journeyCards[i];
                if (c != null && c.gameObject.activeSelf) countVisible++;
            }
            if (countVisible <= 1) return;
            int index = Mathf.Clamp(stageNumber - 1, 0, journeyCards.Length - 1);
            if (journeyCards[index] == null || !journeyCards[index].gameObject.activeSelf) return;
            float t = 0f;
            int visibleIndex = 0;
            int targetVisibleIndex = -1;
            for (int i = 0; i < journeyCards.Length; i++)
            {
                var c = journeyCards[i];
                if (c == null || !c.gameObject.activeSelf) continue;
                if (i == index) targetVisibleIndex = visibleIndex;
                visibleIndex++;
            }
            if (targetVisibleIndex < 0) return;
            t = countVisible > 1 ? (float)targetVisibleIndex / (countVisible - 1) : 0f;
            if (scrollRect.vertical) scrollRect.verticalNormalizedPosition = 1f - t;
            if (scrollRect.horizontal) scrollRect.horizontalNormalizedPosition = t;
        }
    }
}
