using System;
using System.Collections;
using UnityEngine;

namespace LittleHeroJourney
{
    [System.Flags]
    public enum Faction
    {
        Player = 1 << 0,
        AI = 1 << 1,
    }


    public class Health : MonoBehaviour
    {
        #region Fields

        [Header("Health Settings")]
        [SerializeField] public float maxHealth = 100f;
        [SerializeField] private bool showDebugLog = false;

        [Header("Faction")]
        [Tooltip("Faction of this health object")]
        [SerializeField] private Faction faction = Faction.AI;

        [Header("Damage Feedback")]
        [Tooltip("Animation trigger name for damage feedback (duration taken from animation length)")]
        [SerializeField] public string damageAnimationTrigger = "damaged";

        [Header("Death")]
        [Tooltip("Animation trigger name for death")]
        [SerializeField] public string deathAnimationTrigger = "death";

        [Header("Death Effects")]
        [SerializeField] private CharacterDeathEffectsSO deathEffectsSO;

        [Header("Damage Effects")]
        [SerializeField] private CharacterDamageEffectsSO damageEffectsSO;

        [Header("Damager Rotation")]
        [SerializeField] private bool enableRotateToDamager = true;
        [SerializeField] private float damagerRotationSpeed = 720f;
        [SerializeField] private float rotationStopThreshold = 5f;

        private float currentHealth;
        private bool _isInDamageFeedback;
        private float _damageFeedbackEndTime;
        private bool _isDead;
        private Animator _animator;
        private PlayerMovementController _cachedMovement;
        private PlayerCombat _cachedCombat;
        private AIAgent _cachedAIAgent;
        private CharacterStats _cachedCharacterStats;
        private ICombatant _cachedCombatant;  // For interrupt check
        private bool[] _deathVFXTriggered;
        private bool[] _deathAudioTriggered;
        private bool[] _deathParticleTriggered;
        private bool[] _damageVFXTriggered;
        private bool[] _damageAudioTriggered;
        private bool[] _damageParticleTriggered;
        private Transform _damagerTarget;
        private bool _isRotatingToDamager;

        public event Action<float> OnHealthChanged;
        public event Action OnDeath;
        public event Action OnRevived;
        public event Action OnDeathAnimationComplete;

        #endregion

        #region Properties

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => currentHealth > 0 && !_isDead;
        public float HealthPercentage => maxHealth > 0f ? currentHealth / maxHealth : 0f;
        public Faction ObjectFaction => faction;
        public bool IsInDamageFeedback => _isInDamageFeedback;
        public bool IsDead => _isDead;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            currentHealth = maxHealth;
            _animator = Helper.GetAndCacheAnimator(this, searchInChildren: true, 
                showDebugLog: false, ignoreLayerName: "UI");
            Helper.CacheCompanionComponentsSilent<PlayerMovementController, PlayerCombat, AIAgent>(this,
                out _cachedMovement, out _cachedCombat, out _cachedAIAgent);
            _cachedCharacterStats = Helper.GetAndCacheComponent<CharacterStats>(this);
            
            // Cache ICombatant for interrupt checking
            _cachedCombatant = GetComponent<ICombatant>();
            
            if (_animator == null && showDebugLog) Debug.LogWarning($"[{GetType().Name}] No Animator for damage feedback.");
        }

        #endregion

        #region Health Management

        public void TakeDamage(float damage, Transform damager = null)
        {
            if (!IsAlive || _isDead || damage <= 0) return;

            // CHECK EYEFRAME - If in eyeframe, cannot take damage
            if (TryGetComponent<EyeframeManager>(out var eyeframeManager) && eyeframeManager.IsInEyeframe)
            {
                if (showDebugLog) Debug.Log($"[{GetType().Name}] Blocked damage - Player in eyeframe!");
                return;
            }

            float finalDamage = CalculateDamageWithDefense(damage);
            float oldHealth = currentHealth;
            currentHealth = Helper.ClampSub(currentHealth, finalDamage, 0f);
            OnHealthChanged?.Invoke(currentHealth);

            // Set damager for rotation
            if (enableRotateToDamager && damager != null)
            {
                _damagerTarget = damager;
                _isRotatingToDamager = true;
            }

            if (oldHealth > 0 && currentHealth <= 0)
            {
                HandleDeath();
            }
            else if (oldHealth > 0)
            {
                // CHECK INTERRUPTIBLE - Jika entity punya Super Armor, skip damage animation
                bool canBeInterrupted = true;
                
                if (_cachedCombatant != null)
                {
                    canBeInterrupted = _cachedCombatant.IsInterruptible;
                    
                    if (showDebugLog)
                    {
                        if (canBeInterrupted)
                            Debug.Log($"[{GetType().Name}] Damage taken - Entity is INTERRUPTIBLE, playing damage animation");
                        else
                            Debug.Log($"[{GetType().Name}] Damage taken - Entity has SUPER ARMOR, skipping damage animation");
                    }
                }
                
                if (canBeInterrupted)
                {
                    // Interrupt the entity's current action
                    if (_cachedCombatant != null)
                    {
                        _cachedCombatant.OnInterrupted();
                    }
                    
                    TriggerDamageFeedback();
                }
                // Else: Super Armor active - damage applied but no stagger/interrupt
            }
        }

        private void TriggerDamageFeedback()
        {
            if (_animator == null) return;
            if (!string.IsNullOrEmpty(damageAnimationTrigger))
                _animator.SetTrigger(damageAnimationTrigger);
            float len = Helper.GetCurrentAnimationLength(_animator);
            _isInDamageFeedback = true;
            _damageFeedbackEndTime = Time.time + len;
            SetDamageFeedbackState(true);
            
            _damageVFXTriggered = null;
            _damageAudioTriggered = null;
            _damageParticleTriggered = null;
            
            MonitorDamageAnimationForEffects();
        }

        private void Update()
        {
            if (_isInDamageFeedback && Time.time >= _damageFeedbackEndTime)
            {
                EndDamageFeedback();
            }

            if (_isInDamageFeedback && damageEffectsSO != null)
            {
                MonitorDamageAnimationForEffects();
            }

            // Smooth rotate to damager
            if (_isRotatingToDamager && _damagerTarget != null)
            {
                SmoothRotateToDamager();
            }

            // Monitor death animation and trigger effects at normalized time
            if (_isDead)
            {
                MonitorDeathAnimationForEffects();
            }
        }

        private void MonitorDeathAnimationForEffects()
        {
            if (_animator == null || deathEffectsSO == null) return;
            float normalizedTime = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            MonitorTimedEffects(normalizedTime, deathEffectsSO.VFXEffects, deathEffectsSO.AudioEffects, deathEffectsSO.ParticleEffects,
                ref _deathVFXTriggered, ref _deathAudioTriggered, ref _deathParticleTriggered);
        }

        private void MonitorDamageAnimationForEffects()
        {
            if (_animator == null || damageEffectsSO == null || !_isInDamageFeedback) return;
            float normalizedTime = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            MonitorTimedEffects(normalizedTime, damageEffectsSO.VFXEffects, damageEffectsSO.AudioEffects, damageEffectsSO.ParticleEffects,
                ref _damageVFXTriggered, ref _damageAudioTriggered, ref _damageParticleTriggered);
        }

        private void MonitorTimedEffects(float normalizedTime,
            System.Collections.Generic.IReadOnlyList<VFXEffectTiming> vfxList,
            System.Collections.Generic.IReadOnlyList<AudioEffectTiming> audioList,
            System.Collections.Generic.IReadOnlyList<ParticleEffectTiming> particleList,
            ref bool[] vfxTriggered, ref bool[] audioTriggered, ref bool[] particleTriggered)
        {
            if (vfxTriggered == null && vfxList != null) vfxTriggered = new bool[vfxList.Count];
            if (audioTriggered == null && audioList != null) audioTriggered = new bool[audioList.Count];
            if (particleTriggered == null && particleList != null) particleTriggered = new bool[particleList.Count];

            var manager = CharacterEffectManager.Instance;
            Vector3 pos = transform.position;

            if (vfxList != null && vfxTriggered != null)
                for (int i = 0; i < vfxList.Count; i++)
                {
                    var e = vfxList[i];
                    if (!e.IsValid || vfxTriggered[i] || normalizedTime < e.triggerTime) continue;
                    manager?.PlayVFX(e.effectName, pos, Quaternion.identity, e.positionOffset, e.followCharacter ? transform : null, e.followCharacter);
                    vfxTriggered[i] = true;
                }

            if (audioList != null && audioTriggered != null)
                for (int i = 0; i < audioList.Count; i++)
                {
                    var e = audioList[i];
                    if (!e.IsValid || audioTriggered[i] || normalizedTime < e.triggerTime) continue;
                    manager?.PlayAudio(e.effectName, pos + e.positionOffset);
                    audioTriggered[i] = true;
                }

            if (particleList != null && particleTriggered != null)
                for (int i = 0; i < particleList.Count; i++)
                {
                    var e = particleList[i];
                    if (!e.IsValid || particleTriggered[i] || normalizedTime < e.triggerTime) continue;
                    manager?.PlayParticle(e.effectName, pos, Quaternion.identity, e.positionOffset, e.followCharacter ? transform : null, e.followCharacter);
                    particleTriggered[i] = true;
                }
        }

        private void EndDamageFeedback()
        {
            _isInDamageFeedback = false;
            SetDamageFeedbackState(false);
        }

        private void SmoothRotateToDamager()
        {
            if (_damagerTarget == null)
            {
                _isRotatingToDamager = false;
                return;
            }

            // For Player: use PlayerMovementController
            if (_cachedMovement != null)
            {
                _cachedMovement.SetForcedRotationTarget(_damagerTarget, damagerRotationSpeed, rotationStopThreshold);
                _isRotatingToDamager = false; // Let movement controller handle it
                return;
            }

            // For AI/Enemies: direct transform rotation
            // Disable NavMesh auto-rotation to prevent conflict
            if (_cachedAIAgent != null)
            {
                _cachedAIAgent.SetNavMeshRotationEnabled(false);
            }

            Vector3 directionToDamager = (_damagerTarget.position - transform.position).normalized;
            directionToDamager.y = 0f;

            if (directionToDamager != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToDamager);
                float step = damagerRotationSpeed * Time.deltaTime;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, step);

                // Stop rotating when close enough
                if (Quaternion.Angle(transform.rotation, targetRotation) < rotationStopThreshold)
                {
                    _isRotatingToDamager = false;
                    // Re-enable NavMesh auto-rotation
                    if (_cachedAIAgent != null)
                    {
                        _cachedAIAgent.SetNavMeshRotationEnabled(true);
                    }
                }

                if (showDebugLog)
                {
                    Debug.Log($"[{GetType().Name}] Rotating to damager: {_damagerTarget.name}");
                }
            }
        }

        private void SetDamageFeedbackState(bool active)
        {
            if (_isDead) return;
            if (_cachedMovement != null) { _cachedMovement.SetMovementDisabled(active); _cachedMovement.SetDashingDisabled(active); }
            if (_cachedCombat != null) _cachedCombat.SetAttackDisabled(active);
        }

        private void SetDeathControlsDisabled(bool disabled)
        {
            if (_cachedMovement != null) { _cachedMovement.SetMovementDisabled(disabled); _cachedMovement.SetDashingDisabled(disabled); }
            if (_cachedCombat != null) _cachedCombat.SetAttackDisabled(disabled);
            if (_cachedAIAgent != null) _cachedAIAgent.enabled = !disabled;
        }


        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0) return;

            float oldHealth = currentHealth;
            currentHealth = Helper.ClampAdd(currentHealth, amount, 0f, maxHealth);

            if (showDebugLog) Debug.Log($"[{GetType().Name}] Heal +{amount}. Health {oldHealth} -> {currentHealth}");

            OnHealthChanged?.Invoke(currentHealth);
        }

        public void SetHealth(float newHealth)
        {
            float oldHealth = currentHealth;
            currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);

            if (showDebugLog) Debug.Log($"[{GetType().Name}] Health set {oldHealth} -> {currentHealth}");

            OnHealthChanged?.Invoke(currentHealth);

            if (currentHealth > 0 && oldHealth <= 0 && !_isDead)
            {
                OnRevived?.Invoke();
            }
            else if (currentHealth <= 0 && oldHealth > 0 && !_isDead)
            {
                HandleDeath();
            }
        }

        public void Revive(float healthPercentage = 1f)
        {
            float targetHealth = maxHealth * Mathf.Clamp01(healthPercentage);
            SetHealth(targetHealth);

            if (IsAlive)
            {
                _isDead = false;
                SetDeathControlsDisabled(false);
                if (_cachedAIAgent != null) _cachedAIAgent.Revive();
                OnRevived?.Invoke();
            }
        }

        private void HandleDeath()
        {
            _isDead = true;
            _deathVFXTriggered = null;
            _deathAudioTriggered = null;
            _deathParticleTriggered = null;
            if (showDebugLog) Debug.Log($"[{GetType().Name}] {gameObject.name} died.");

            if (_animator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
            {
                _animator.SetTrigger(deathAnimationTrigger);
            }

            // Permanently disable controls on death
            SetDeathControlsDisabled(true);

            OnDeath?.Invoke();

            StartCoroutine(MonitorDeathAnimation());
        }

        private IEnumerator MonitorDeathAnimation()
        {
            if (_animator == null)
            {
                OnDeathAnimationComplete?.Invoke();
                yield break;
            }

            AnimatorStateInfo deathState;
            float timeout = 5f;
            float elapsed = 0f;

            do
            {
                deathState = _animator.GetCurrentAnimatorStateInfo(0);
                elapsed += Time.deltaTime;
                yield return null;
            }
            while (deathState.normalizedTime < 1f && elapsed < timeout);

            OnDeathAnimationComplete?.Invoke();
        }

        #endregion

        #region Defense

        private float CalculateDamageWithDefense(float incomingDamage)
        {
            return _cachedCharacterStats != null ? _cachedCharacterStats.CalculateDamageWithDefense(incomingDamage) : incomingDamage;
        }

        #endregion

        #region Utility Methods

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            _isDead = false;
            SetDeathControlsDisabled(false);
            if (_cachedAIAgent != null) _cachedAIAgent.Revive();
            OnHealthChanged?.Invoke(currentHealth);
        }

        public void Initialize(float maxHealthValue, float? currentHealthValue = null)
        {
            maxHealth = maxHealthValue;
            currentHealth = currentHealthValue ?? maxHealth;
            _isDead = false;
            SetDeathControlsDisabled(false);
        }

        #endregion
    }
}
