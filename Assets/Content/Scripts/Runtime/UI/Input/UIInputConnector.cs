using UnityEngine;
using LittleHeroJourney.InputSystem;

namespace LittleHeroJourney.UI
{
    public class UIInputConnector : MonoBehaviour
    {
        private TargetLockCameraController _targetLockCameraController;
        [SerializeField] private bool showDebugLog = false;

        private void Start()
        {
            _targetLockCameraController = FindObjectOfType<TargetLockCameraController>();
            
            if (_targetLockCameraController == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] TargetLockCameraController not found in scene!");
            }
        }

        public void OnAttackButtonPressed()
        {
            GameInputEvents.TriggerAttack();
        }

        public void OnDashButtonPressed()
        {
            GameInputEvents.TriggerDash();
        }

        public void OnLockButtonPressed()
        {
            if (_targetLockCameraController != null)
            {
                _targetLockCameraController.ToggleLockTarget();
            }
            else
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Cannot toggle lock target - TargetLockCameraController not found!");
            }
        }

        public void RestartLevel()
        {
            if (_targetLockCameraController != null)
                _targetLockCameraController.UnlockTarget();
            if (GameManager.Instance != null)
                GameManager.Instance.RetryLevel();
        }

        public void NextLevel()
        {
            if (_targetLockCameraController != null)
                _targetLockCameraController.UnlockTarget();
            if (GameManager.Instance != null)
                GameManager.Instance.LoadNextLevel();
        }

        public void ExitToMainMenu()
        {
            if (_targetLockCameraController != null)
                _targetLockCameraController.UnlockTarget();
            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMainMenu();
        }
    }
}
