using UnityEngine;

namespace LittleHeroJourney.UI
{
    /// <summary>
    /// Simple health bar - update slider saat health berubah
    /// Attach ke enemy prefab
    /// </summary>
    public class HealthBar : StatBar
    {
        #region Fields

        private Health targetHealth;

        #endregion

        #region Setup

        /// <summary>
        /// Setup health bar untuk target
        /// </summary>
        public void SetupForTarget(Health target)
        {
            if (target == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Target health is null!");
                return;
            }

            Cleanup();
            targetHealth = target;

            // Subscribe
            targetHealth.OnHealthChanged += OnHealthChanged;

            // Set initial value
            OnHealthChanged(targetHealth.CurrentHealth);
        }

        /// <summary>
        /// Cleanup - unsubscribe
        /// </summary>
        public void Cleanup()
        {
            if (targetHealth != null)
            {
                targetHealth.OnHealthChanged -= OnHealthChanged;
            }
            targetHealth = null;
        }

        #endregion

        #region Callbacks

        private void OnHealthChanged(float currentHealth)
        {
            if (targetHealth == null) return;

            // Update slider dengan current dan max health
            SetValue(currentHealth, targetHealth.MaxHealth);
        }

        #endregion

        #region Properties

        public Health TargetHealth => targetHealth;

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion
    }
}
