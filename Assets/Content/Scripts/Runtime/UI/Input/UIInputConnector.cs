using UnityEngine;
using LittleHeroJourney;

namespace LittleHeroJourney.UI
{
    public class UIInputConnector : MonoBehaviour
    {
        private static float _lastExitToMainMenuRequestTime;

        public void InvokeAction(string actionId)
        {
            InvokeActionInternal(actionId, null);
        }

        public void InvokeActionWithPayload(string actionId, string payload)
        {
            InvokeActionInternal(actionId, payload);
        }

        private void InvokeActionInternal(string actionId, string payload)
        {
            if (string.IsNullOrEmpty(actionId)) return;
            if (LoadingManager.Instance != null && LoadingManager.Instance.IsLoading) return;
            if (string.Equals(actionId, "ExitToMainMenu", System.StringComparison.Ordinal))
            {
                if (AdsManager.IsShowing) return;
                float now = Time.unscaledTime;
                if (now - _lastExitToMainMenuRequestTime < 1f) return;
                _lastExitToMainMenuRequestTime = now;
            }
            if (string.Equals(actionId, "Pause", System.StringComparison.Ordinal))
            {
                if (AdsManager.IsShowing) return;
                if (SceneManager.Instance != null)
                {
                    var cfg = SceneManager.Instance.Config;
                    string menuId = cfg != null ? cfg.mainMenuId : null;
                    if (!string.IsNullOrEmpty(menuId) && string.Equals(SceneManager.Instance.CurrentId, menuId, System.StringComparison.OrdinalIgnoreCase))
                        return;
                }
            }
            GameEventSystem.Publish(new UIActionEvent(actionId, payload));
        }
    }
}
