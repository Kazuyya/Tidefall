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

        #region Setup

        /// <summary>
        /// Setup button berdasarkan LevelSO dan game state
        /// </summary>
        private void Setup()
        {
            if (levelData == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] LevelData not set! (Should be set by LevelManager on spawn)");
                return;
            }

            // Get references
            levelManager = FindObjectOfType<LevelManager>();
            levelButton = GetComponent<Button>();

            if (levelButton == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Button component not found!");
                return;
            }

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Setup Level {levelData.LevelNumber}");

            // Update visual
            UpdateVisual();

            // Setup button listener
            levelButton.onClick.AddListener(OnButtonClicked);
            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] onClick listener added for Level {levelData.LevelNumber}");
        }

        #endregion

        #region Pointer Click Handler (untuk click detection)

        public void OnPointerClick(PointerEventData eventData)
        {
            if (showDebugLog) Debug.Log($"[{GetType().Name}] OnPointerClick called for Level {levelData?.LevelNumber}");
            OnButtonClicked();
        }

        #endregion

        #region Visual Update

        /// <summary>
        /// Update button visual berdasarkan unlock status
        /// Unlocked: Show level number text, hide lock icon, button enabled
        /// Locked: Hide level number text, show lock icon, button disabled
        /// </summary>
        public void UpdateVisual()
        {
            if (levelData == null) return;

            bool isUnlocked = levelManager != null && levelManager.IsLevelUnlocked(levelData.LevelNumber);

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Level {levelData.LevelNumber} - Unlocked: {isUnlocked}");

            // UNLOCKED STATE
            if (isUnlocked)
            {
                // Show level number
                if (levelNumberText != null)
                {
                    levelNumberText.gameObject.SetActive(true);
                    levelNumberText.text = levelData.LevelNumber.ToString();
                }

                // Hide lock icon
                if (lockedIcon != null)
                {
                    lockedIcon.SetActive(false);
                }

                // Enable button
                if (levelButton != null)
                {
                    levelButton.interactable = true;
                }
            }
            // LOCKED STATE
            else
            {
                // Hide level number
                if (levelNumberText != null)
                {
                    levelNumberText.gameObject.SetActive(false);
                }

                // Show lock icon
                if (lockedIcon != null)
                {
                    lockedIcon.SetActive(true);
                }

                // Disable button
                if (levelButton != null)
                {
                    levelButton.interactable = false;
                }
            }

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Updated visual: Level {levelData.LevelNumber} (Text: {(isUnlocked ? "visible" : "hidden")}, Lock Icon: {(!isUnlocked ? "visible" : "hidden")})");
        }

        #endregion

        #region Button Click Handler

        /// <summary>
        /// Called ketika button di-click
        /// </summary>
        private void OnButtonClicked()
        {
            if (showDebugLog) Debug.Log($"[{GetType().Name}] OnButtonClicked called for Level {levelData?.LevelNumber}");

            if (levelManager == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] LevelManager is NULL!");
                return;
            }

            bool isUnlocked = levelManager.IsLevelUnlocked(levelData.LevelNumber);
            
            if (!isUnlocked)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Level {levelData.LevelNumber} is locked!");
                return;
            }

            if (showDebugLog) Debug.Log($"[{GetType().Name}] Loading level: {levelData.LevelName}");
            // Load level
            levelManager.LoadLevel(levelData);
        }

        #endregion

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
