using UnityEngine;

namespace LittleHeroJourney.UI
{
    /// <summary>
    /// Simple stun bar - update slider saat stun berubah
    /// Attach ke enemy prefab
    /// </summary>
    public class StunBar : StatBar
    {
        #region Fields

        private StunManager targetStunManager;

        #endregion

        #region Setup

        /// <summary>
        /// Setup stun bar untuk target
        /// </summary>
        public void SetupForTarget(StunManager target)
        {
            if (target == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Target stun manager is null!");
                return;
            }

            Cleanup();
            targetStunManager = target;

            // Subscribe
            targetStunManager.OnStunHealthChanged += OnStunChanged;

            // Set initial value
            OnStunChanged(targetStunManager.StunHealthPercentage);
        }

        /// <summary>
        /// Cleanup - unsubscribe
        /// </summary>
        public void Cleanup()
        {
            if (targetStunManager != null)
            {
                targetStunManager.OnStunHealthChanged -= OnStunChanged;
            }
            targetStunManager = null;
        }

        #endregion

        #region Callbacks

        private void OnStunChanged(float stunPercentage)
        {
            if (targetStunManager == null) return;

            // Update slider dengan current dan max stun
            SetValue(targetStunManager.CurrentStunHealth, targetStunManager.MaxStunHealth);
        }

        #endregion

        #region Properties

        public StunManager TargetStunManager => targetStunManager;

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion
    }
}
