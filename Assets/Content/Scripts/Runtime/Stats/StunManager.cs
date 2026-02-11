using System;
using UnityEngine;

namespace LittleHeroJourney
{
    public class StunManager : MonoBehaviour
    {
        private Health _health;
        private PlayerMovementController _movement;
        private PlayerCombat _combat;
        private AIAgent _aiAgent;
        private Animator _animator;
        private bool _isStunned;
        private float _stunEndTime;
        private float _currentStunHealth;

        [Header("Stun")]
        [Min(1)] public float maxStunHealth = 100f;
        [Min(0)] public float stunResistance = 20f;
        [Min(0)] public float stunDuration = 3f;
        
        [Header("Animator Parameters")]
        [SerializeField] private string stunAnimatorTrigger = "stunTrigger";
        [SerializeField] private string stunAnimatorBool = "stunned";

        [Header("Debug")]
        [SerializeField] private bool showDebugLog;

        public event Action OnStunned;
        public event Action OnStunEnded;
        public event Action<float> OnStunHealthChanged;

        #region Properties

        public bool IsStunned => _isStunned;
        public float CurrentStunHealth => _currentStunHealth;
        public float MaxStunHealth => maxStunHealth;
        public float StunHealthPercentage => maxStunHealth > 0 ? _currentStunHealth / maxStunHealth : 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Use GetComponentWithFallback for better component discovery
            _health = Helper.GetComponentWithFallback<Health>(this);
            _movement = Helper.GetComponentWithFallback<PlayerMovementController>(this);
            _combat = Helper.GetComponentWithFallback<PlayerCombat>(this);
            _aiAgent = Helper.GetComponentWithFallback<AIAgent>(this);
            _animator = Helper.GetAndCacheAnimator(this, searchInChildren: true, 
                showDebugLog: false, ignoreLayerName: "UI");
            
            _currentStunHealth = 0f;
            if (_health == null) Debug.LogWarning($"[{GetType().Name}] No Health.");
        }

        private void Update()
        {
            if (_health == null) return;

            if (_isStunned)
            {
                float elapsedTime = Time.time - (_stunEndTime - stunDuration);
                float progress = Mathf.Clamp01(elapsedTime / stunDuration);
                _currentStunHealth = Mathf.Lerp(maxStunHealth, 0f, progress);
                OnStunHealthChanged?.Invoke(StunHealthPercentage);

                if (Time.time >= _stunEndTime)
                    EndStun();
            }
        }

        #endregion

        #region Stun Damage

        public void AddStunDamage(float stunDamage, float attackerImpact)
        {
            if (_health == null || !_health.IsAlive || _isStunned || stunDamage <= 0) return;

            float finalStun = CalculateStunDamage(stunDamage, attackerImpact);
            if (finalStun <= 0)
            {
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Stun blocked. Current {_currentStunHealth:F1}/{maxStunHealth}");
                return;
            }

            float before = _currentStunHealth;
            AddStunHealth(finalStun);
            OnStunHealthChanged?.Invoke(StunHealthPercentage);

            if (showDebugLog) Debug.Log($"[{GetType().Name}] Stun +{finalStun:F1} (raw {stunDamage}, Impact {attackerImpact}). Bar {before:F1} -> {_currentStunHealth:F1} / {maxStunHealth}");
            if (_currentStunHealth >= maxStunHealth) TriggerStun();
        }

        private float CalculateStunDamage(float incomingStunDamage, float attackerImpact)
        {
            float beforeResist = incomingStunDamage * (attackerImpact / 100f);
            if (beforeResist <= 0f) return 0f;
            return Helper.ApplyPercentReduction(beforeResist, stunResistance, 0.1f);
        }

        private void AddStunHealth(float amount)
        {
            if (amount <= 0) return;
            _currentStunHealth = Helper.ClampAdd(_currentStunHealth, amount, 0f, maxStunHealth);
        }

        #endregion

        #region Stun Management

        private void TriggerStun()
        {
            if (_isStunned) return;
            _isStunned = true;
            _stunEndTime = Time.time + stunDuration;
            _currentStunHealth = maxStunHealth;
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Stunned {stunDuration}s");
            SetControls(false);
            SetAnimatorStunned(true);
            OnStunned?.Invoke();
        }

        private void EndStun()
        {
            if (!_isStunned) return;
            _isStunned = false;
            _currentStunHealth = 0f;
            OnStunHealthChanged?.Invoke(0f);
            if (showDebugLog) Debug.Log($"[{GetType().Name}] Stun ended");
            SetControls(true);
            SetAnimatorStunned(false);
            OnStunEnded?.Invoke();
        }

        /// <param name="controlsOn">true = unstun (enable controls), false = stun (disable)</param>
        private void SetControls(bool controlsOn)
        {
            if (_health == null || _health.IsDead) return;
            if (_movement != null) { _movement.SetMovementDisabled(!controlsOn); _movement.SetDashingDisabled(!controlsOn); }
            if (_combat != null) _combat.SetAttackDisabled(!controlsOn);
            if (_aiAgent != null) _aiAgent.enabled = controlsOn;
        }

        private void SetAnimatorStunned(bool stunned)
        {
            if (_animator == null) return;
            
            if (stunned)
            {
                // Trigger animation start with trigger parameter (fires once)
                if (!string.IsNullOrEmpty(stunAnimatorTrigger))
                {
                    _animator.SetTrigger(stunAnimatorTrigger);
                    if (showDebugLog) Debug.Log($"[{GetType().Name}] Animator trigger: {stunAnimatorTrigger}");
                }
                // Set bool to maintain stunned state
                if (!string.IsNullOrEmpty(stunAnimatorBool))
                {
                    _animator.SetBool(stunAnimatorBool, true);
                    if (showDebugLog) Debug.Log($"[{GetType().Name}] Animator bool: {stunAnimatorBool} = true");
                }
            }
            else
            {
                // Set bool to false to allow exit transition
                if (!string.IsNullOrEmpty(stunAnimatorBool))
                {
                    _animator.SetBool(stunAnimatorBool, false);
                    if (showDebugLog) Debug.Log($"[{GetType().Name}] Animator bool: {stunAnimatorBool} = false");
                }
            }
        }

        public void ResetStun()
        {
            _isStunned = false;
            _stunEndTime = 0f;
            _currentStunHealth = 0f;
            SetControls(true);
            OnStunHealthChanged?.Invoke(0f);
        }

        #endregion
    }
}
