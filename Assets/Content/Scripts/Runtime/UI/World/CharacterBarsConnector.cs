using UnityEngine;

namespace LittleHeroJourney.UI
{
    public class CharacterBarsConnector : MonoBehaviour
    {
        #region Fields

        [Header("Bars")]
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private StunBar stunBar;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string spawnTrigger = "In";
        [SerializeField] private string deathTrigger = "Out";

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;

        private Health _health;
        private StunManager _stunManager;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Setup

        public void Initialize()
        {
            _health = GetComponent<Health>();
            _stunManager = GetComponent<StunManager>();

            if (_health == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] No Health component found!");
            }

            if (_stunManager == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] No StunManager component found!");
            }

            ConnectBars();
        }

        public void InitializeForTargets(Health healthTarget, StunManager stunTarget = null)
        {
            _health = healthTarget;
            _stunManager = stunTarget;

            if (_health == null)
            {
                if (showDebugLog) Debug.LogWarning($"[{GetType().Name}] Health target is null!");
                return;
            }

            if (showDebugLog) Debug.Log($"[{GetType().Name}] Initialized for targets: {_health.gameObject.name}");
            
            ConnectBars();
        }

        private void ConnectBars()
        {
            if (healthBar != null && _health != null)
            {
                healthBar.SetupForTarget(_health);
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Health bar connected to {_health.gameObject.name}");
            }
            else if (healthBar == null && showDebugLog)
            {
                Debug.LogWarning($"[{GetType().Name}] Health bar is not assigned!");
            }

            if (stunBar != null && _stunManager != null)
            {
                stunBar.SetupForTarget(_stunManager);
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Stun bar connected");
            }
            else if (stunBar == null && showDebugLog)
            {
                Debug.LogWarning($"[{GetType().Name}] Stun bar is not assigned!");
            }

            if (_health != null)
            {
                _health.OnDeath += PlayDeathAnimation;
            }
        }

        public void Cleanup()
        {
            if (healthBar != null)
                healthBar.Cleanup();

            if (stunBar != null)
                stunBar.Cleanup();

            if (_health != null)
            {
                _health.OnDeath -= PlayDeathAnimation;
            }
        }

        #endregion

        #region Spawn/Death Animations

        public void PlaySpawnAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger(spawnTrigger);
            }
            else if (showDebugLog)
            {
                Debug.LogWarning($"[{GetType().Name}] No Animator found for spawn animation!");
            }
        }

        public void PlayDeathAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger(deathTrigger);
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Playing death animation");
            }
        }

        #endregion

        #region Properties

        public Health Health => _health;
        public StunManager StunManager => _stunManager;
        public HealthBar HealthBar => healthBar;
        public StunBar StunBar => stunBar;

        #endregion
    }
}
