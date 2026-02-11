using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// Manages invulnerability frames (eyeframe) for the player.
    /// During eyeframe, the player cannot take damage.
    /// </summary>
    public class EyeframeManager : MonoBehaviour
    {
        #region Fields

        private bool _isInEyeframe = false;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;

        #endregion

        #region Properties

        public bool IsInEyeframe => _isInEyeframe;

        #endregion

        #region Eyeframe Control

        /// <summary>
        /// Set eyeframe active/inactive state.
        /// </summary>
        public void SetEyeframe(bool active)
        {
            if (_isInEyeframe == active) return;

            _isInEyeframe = active;

            if (showDebugLog)
            {
                Debug.Log($"[{GetType().Name}] Eyeframe {(active ? "ACTIVATED" : "DEACTIVATED")}");
            }
        }

        #endregion
    }
}
