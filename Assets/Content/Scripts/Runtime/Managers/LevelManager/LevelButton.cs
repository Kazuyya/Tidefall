using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace LittleHeroJourney
{
    public class LevelButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI levelNumberText;
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private TextMeshProUGUI storySummaryText;
        [SerializeField] private GameObject lockedIcon;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;

        private LevelSO levelData;
        private LevelManager levelManager;
        private Button levelButton;

        #region Unity Lifecycle

        private void Start()
        {
            Setup();
        }

        #endregion

        private void Setup()
        {
            if (levelData == null) return;
            levelManager = FindObjectOfType<LevelManager>();
            levelButton = GetComponent<Button>();
            if (levelButton == null) return;
            UpdateVisual();
            levelButton.onClick.AddListener(OnButtonClicked);
        }

        public void OnPointerClick(PointerEventData eventData) => OnButtonClicked();

        public void UpdateVisual()
        {
            if (levelData == null) return;
            bool isUnlocked = levelManager != null && levelManager.IsLevelUnlocked(levelData.LevelNumber);

            if (levelNumberText != null) { levelNumberText.gameObject.SetActive(isUnlocked); levelNumberText.text = levelData.LevelNumber.ToString(); }
            if (levelNameText != null) { levelNameText.gameObject.SetActive(true); levelNameText.text = string.IsNullOrEmpty(levelData.LevelName) ? $"Story {levelData.LevelNumber}" : levelData.LevelName; }
            if (storySummaryText != null) { storySummaryText.gameObject.SetActive(true); storySummaryText.text = CleanSummary(levelData.StorySummary); }
            if (lockedIcon != null) lockedIcon.SetActive(!isUnlocked);
            if (levelButton != null) levelButton.interactable = isUnlocked;
        }

        private void OnButtonClicked()
        {
            if (levelManager == null || !levelManager.IsLevelUnlocked(levelData.LevelNumber)) return;
            levelManager.LoadLevel(levelData);
        }

        private static string CleanSummary(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var line in raw.Split('\n'))
            {
                var t = line.Trim();
                if (t.StartsWith("//") || t.StartsWith("<!--")) continue;
                var noHtml = System.Text.RegularExpressions.Regex.Replace(t, "<[^>]+>", " ").Trim();
                if (noHtml.Length > 0) { if (sb.Length > 0) sb.Append('\n'); sb.Append(noHtml); }
            }
            return sb.ToString();
        }

        #region Getters & Setters

        public LevelSO GetLevelData() => levelData;

        public void SetLevelData(LevelSO level)
        {
            levelData = level;
        }

        public void SetLevelManager(LevelManager manager)
        {
            levelManager = manager;
        }

        #endregion
    }
}
