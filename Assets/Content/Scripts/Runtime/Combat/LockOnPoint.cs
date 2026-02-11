using UnityEngine;

namespace LittleHeroJourney
{
    /// <summary>
    /// Lock-on point for camera targeting
    /// Attach this to a child GameObject (e.g., head) of an enemy/AI
    /// The parent must have a Health component
    /// </summary>
    public class LockOnPoint : MonoBehaviour, ILockOnTarget
    {
        private Health _health;
        [SerializeField] private bool showDebugLog = false;

        private void Awake()
        {
            // Get Health from parent or this GameObject
            _health = GetComponentInParent<Health>();
            
            if (_health == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] No Health component found in parent! Lock-on may not work properly.");
            }
        }

        public UnityEngine.Transform GetLockOnTransform()
        {
            return transform;
        }

        public Health GetHealth()
        {
            return _health;
        }
    }
}
