using UnityEngine;

namespace LittleHeroJourney
{
    public class IframeManager : MonoBehaviour
    {
        #region Fields

        private bool _isInIframe = false;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;

        #endregion

        #region Properties

        public bool IsInIframe => _isInIframe;

        #endregion

        #region Iframe Control
        public void SetIframe(bool active)
        {
            if (_isInIframe == active) return;

            _isInIframe = active;

            if (showDebugLog)
                Debug.Log($"[{GetType().Name}] Iframe {(active ? "ACTIVATED" : "DEACTIVATED")}");
        }

        #endregion
    }
}
