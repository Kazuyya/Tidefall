using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LittleHeroJourney.UI
{
    public class JourneyDisplay : MonoBehaviour
    {
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI numberText;
        public Image previewImage;
        private Button _selectButton;
        private JourneySelectorManager _selector;
        private int _stageNumber;
        public void Configure(string title, string description, Sprite preview, int stageNumber, JourneySelectorManager selector)
        {
            if (titleText != null) titleText.text = title ?? "";
            if (descriptionText != null) descriptionText.text = description ?? "";
            if (numberText != null) numberText.text = "#" + stageNumber;
            if (previewImage != null) previewImage.sprite = preview;
            _stageNumber = stageNumber;
            _selector = selector;
            if (_selectButton == null) _selectButton = GetComponent<Button>();
            if (_selectButton != null)
            {
                _selectButton.onClick.RemoveAllListeners();
                _selectButton.onClick.AddListener(OnSelected);
            }
        }
        private void OnSelected()
        {
            if (_selector != null)
                _selector.SelectJourney(_stageNumber);
        }
    }
}
