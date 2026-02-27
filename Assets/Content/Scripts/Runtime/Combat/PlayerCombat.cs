using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

using LittleHeroJourney.InputSystem;

namespace LittleHeroJourney
{
    public class PlayerCombat : MonoBehaviour, ICombatant
    {
        #region Fields

        // New combat system
        [Header("Combat Profile")]
        public WeaponCombatProfileSO currentCombatProfile;

        [System.Serializable]
        public class WeaponEntry
        {
            public Weapon weaponComponent;
            public string weaponName;
        }

        [Header("Weapon Setup")]
        public List<WeaponEntry> availableWeapons = new List<WeaponEntry>();
        [HideInInspector] public Weapon currentWeapon;
        protected List<Weapon> _currentAttackWeapons = new List<Weapon>();
        protected Dictionary<Weapon, Vector2> _weaponTimingMap = new Dictionary<Weapon, Vector2>();
        protected List<object> _triggeredEffects = new List<object>();
        protected bool _isAttackFinishing;

        [Header("Attack State")]
        protected bool _isAttacking = false;
        private bool _attackLocked = false;
        private bool _comboWindowActive = false;
        private Queue<int> _inputBuffer = new Queue<int>();
        protected const int MAX_BUFFERED_INPUTS = 2;
        private bool _wasInAttackStateLastFrame = false;

        public bool IsAttacking => _isAttacking;
        
        public virtual bool IsInterruptible
        {
            get
            {
                if (!_isAttacking || _animator == null || currentSequence == null)
                    return true;
                
                AttackDataSO currentAttack = currentSequence.GetAttackAtIndex(currentAttackIndex);
                if (currentAttack == null)
                    return true;
                
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                float normalizedTime = stateInfo.normalizedTime % 1.0f;
                
                Vector2 interruptWindow = currentAttack.interruptibleWindow;
                bool isInInterruptibleWindow = normalizedTime >= interruptWindow.x && 
                                               normalizedTime <= interruptWindow.y;
                
                return isInInterruptibleWindow;
            }
        }

        private bool _stateTransitionInProgress = false;
        private InputAction attackAction;

        protected ComboSequenceSO currentSequence;
        protected int currentAttackIndex = 0;
        private float _lastAttackStartTime;
        private float _lastAttackEndTime = -999f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;
        public bool ShowDebugLog => showDebugLog;

        private PlayerMovementController _movementController;
        private Animator _animator;
        private Health _health;
        private AutoAimController _autoAimController;

        #endregion

        #region Initialization

        protected virtual void OnEnable()
        {
            GameInputEvents.OnAttack += TriggerAttack;

            foreach (var weaponEntry in availableWeapons)
            {
                if (weaponEntry.weaponComponent != null)
                {
                    weaponEntry.weaponComponent.OnHitEnemy += OnWeaponHitEnemy;
                }
            }
        }

        protected virtual void OnDisable()
        {
            GameInputEvents.OnAttack -= TriggerAttack;

            foreach (var weaponEntry in availableWeapons)
            {
                if (weaponEntry.weaponComponent != null)
                {
                    weaponEntry.weaponComponent.OnHitEnemy -= OnWeaponHitEnemy;
                }
            }

            if (attackAction != null) attackAction.Disable();
            
            DisableHitboxes();
        }

        /// <summary>
        /// Virtual method to get animator - can be overridden by subclasses (like AICombat)
        /// </summary>
        protected virtual Animator GetCombatAnimator()
        {
            return Helper.GetAndCacheAnimator(this, searchInChildren: true, ignoreLayerName: "");
        }

        private void Awake()
        {
            _movementController = GetComponent<PlayerMovementController>();
            // Use virtual method to allow AI override
            _animator = GetCombatAnimator();
            _health = GetComponent<Health>();
            _autoAimController = GetComponent<AutoAimController>();

            DisableHitboxes();
        }

        protected virtual void Start()
        {
            if (currentCombatProfile != null && currentCombatProfile.availableCombos.Count > 0)
            {
                currentSequence = currentCombatProfile.availableCombos[0];
            }

            foreach (var weaponEntry in availableWeapons)
            {
                if (weaponEntry.weaponComponent != null)
                {
                    weaponEntry.weaponComponent.ResetForNewAttack();
                    weaponEntry.weaponComponent.DisableWeapon();
                }
            }

            if (availableWeapons.Count > 0 && availableWeapons[0].weaponComponent != null)
            {
                currentWeapon = availableWeapons[0].weaponComponent;
            }

            SetupInputActions();
        }

        protected virtual void SetupInputActions()
        {
             if (_movementController == null || _movementController.inputActionAsset == null)
             {
                 return;
             }
 
             try
             {
                 attackAction = _movementController.inputActionAsset.FindAction("Attack");
 
                 if (attackAction != null)
                 {
                     attackAction.Enable();
                 }
             }
            catch (System.Exception e)
            {
                if (ShowDebugLog) Debug.LogWarning($"[{GetType().Name}] Failed to setup attack input action: {e.Message}");
            }
        }

        protected virtual void OnWeaponHitEnemy(Collider enemy)
        {
            if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Weapon hit: {enemy.name}");
        }

        protected void DisableHitboxes()
        {
             foreach (var weaponEntry in availableWeapons)
             {
                 if (weaponEntry.weaponComponent != null)
                 {
                     weaponEntry.weaponComponent.DisableWeaponCollider();
                 }
             }
        }

        #endregion

        #region Combat Logic

        void Update()
        {
            UpdateComboWindowState();
            UpdateAttackState();
            UpdateTimers();
        }

        protected virtual void UpdateAttackState()
        {
            if (_isAttacking)
            {
                UpdateAttackProgress();
            }
        }

        protected virtual void UpdateTimers() {   }

        protected virtual void UpdateComboWindowState()
        {
            if (!_isAttacking || _animator == null || currentSequence == null)
            {
                if (_comboWindowActive)
                {
                    _comboWindowActive = false;
                }
                return;
            }

            AttackDataSO currentAttack = currentSequence.GetAttackAtIndex(currentAttackIndex);
            if (currentAttack == null)
            {
                if (_comboWindowActive)
                {
                    _comboWindowActive = false;
                }
                return;
            }

            // FIX: Attack terakhir TETAP punya combo window untuk trigger new combo/loop
            // Tidak lagi skip combo window untuk last attack

            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = stateInfo.normalizedTime % 1.0f;

            Vector2 inputWindow = currentAttack.inputWindow;
            const float EPSILON = 0.001f;  
            bool isWithinInputWindow = normalizedTime >= (inputWindow.x - EPSILON) && 
                                       normalizedTime <= (inputWindow.y + EPSILON);

            if (isWithinInputWindow)
            {
                if (!_comboWindowActive)
                {
                    _comboWindowActive = true;
                    bool isLastAttack = currentAttackIndex >= currentSequence.SequenceLength - 1;
                    string windowType = isLastAttack ? "NEW COMBO" : "COMBO";
                    if (ShowDebugLog) Debug.Log($"[{GetType().Name}] {windowType} window OPENED at normalizedTime: {normalizedTime:F3} (window: {inputWindow.x:F3}-{inputWindow.y:F3})");
                }
            }
            else
            {
                if (_comboWindowActive)
                {
                    _comboWindowActive = false;
                    if (ShowDebugLog && normalizedTime > inputWindow.y)
                    {
                        Debug.Log($"[{GetType().Name}] Combo window CLOSED - missed timing (normalizedTime: {normalizedTime:F3} > {inputWindow.y:F3})");
                    }
                }
            }
        }

        protected virtual void SetMovementDisabled(bool disabled)
        {
            if (_movementController != null)
            {
                _movementController.SetMovementDisabled(disabled);
            }
        }

        public virtual void SetAttackDisabled(bool disabled)
        {
            _attackLocked = disabled;
            if (disabled && _isAttacking)
            {
                ResetCombo();
            }
        }

        protected virtual void PerformAttack()
        {
            if (_isAttacking || _attackLocked)
            {
                return;
            }

            if (currentCombatProfile == null || currentCombatProfile.availableCombos.Count == 0)
            {
                return;
            }

            ComboSequenceSO sequence = currentCombatProfile.availableCombos[0];

            if (sequence == null || !sequence.IsValidSequence())
                return;

            currentSequence = sequence;

            AttackDataSO attackData = sequence.GetAttackAtIndex(currentAttackIndex);

            if (attackData == null)
            {
                ResetCombo();
                return;
            }

            ExecuteAttack(attackData, sequence);

            if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Attack started");
        }

        protected virtual void ExecuteAttack(AttackDataSO attackData, ComboSequenceSO sequence)
        {
            if (_animator == null || attackData == null)
                return;

            _isAttacking = true;
            _lastAttackStartTime = Time.time;

            _isAttackFinishing = false;

            List<Weapon> attackWeapons = new List<Weapon>();
            _weaponTimingMap.Clear();

            // CRITICAL: Disable ALL weapon colliders first to prevent double-triggers from previous attack
            foreach (var weaponEntry in availableWeapons)
            {
                if (weaponEntry?.weaponComponent != null)
                {
                    weaponEntry.weaponComponent.DisableWeaponCollider();
                    // Verify collider is actually disabled
                    if (weaponEntry.weaponComponent.WeaponCollider != null && 
                        weaponEntry.weaponComponent.WeaponCollider.enabled)
                    {
                        if (ShowDebugLog) Debug.LogWarning($"[{GetType().Name}] WARNING: Weapon collider {weaponEntry.weaponComponent.gameObject.name} was not disabled! Force disabling.");
                        weaponEntry.weaponComponent.WeaponCollider.enabled = false;
                    }
                }
            }

            if (attackData.weaponTimings != null && attackData.weaponTimings.Count > 0)
            {
                foreach (var weaponTiming in attackData.weaponTimings)
                {
                    if (!string.IsNullOrEmpty(weaponTiming.weaponName))
                    {
                        PlayerCombat.WeaponEntry weaponEntry = availableWeapons.Find(w =>
                            w != null && w.weaponName == weaponTiming.weaponName);

                        if (weaponEntry != null && weaponEntry.weaponComponent != null)
                        {
                            attackWeapons.Add(weaponEntry.weaponComponent);
                            _weaponTimingMap[weaponEntry.weaponComponent] = weaponTiming.colliderTriggerWindow;

                            if (attackData.attackDamageData != null)
                            {
                                weaponEntry.weaponComponent.UpdateDamageData(attackData.attackDamageData);
                            }
                        }
                    }
                }
            }
            else
            {
                if (currentWeapon != null)
                {
                    attackWeapons.Add(currentWeapon);
                    _weaponTimingMap[currentWeapon] = new Vector2(0.3f, 0.7f);
                }
            }

            foreach (Weapon weapon in attackWeapons)
            {
                if (weapon != null)
                {
                    weapon.ResetForNewAttack();
                }
            }

            if (!string.IsNullOrEmpty(attackData.animationTriggerName))
            {
                _animator.SetTrigger(attackData.animationTriggerName);
            }
            else if (attackData.attackAnimation != null)
            {
                _animator.Play(attackData.attackAnimation.name);
            }

            _currentAttackWeapons = attackWeapons;

        }

        private void UpdateAttackProgress()
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = stateInfo.normalizedTime % 1.0f;  // Clamp to 0-1 to prevent double trigger on loop


            bool animatorInAttack = Helper.IsInAttackState(_animator);
            if (!animatorInAttack && Time.time - _lastAttackStartTime > 2.0f)
            {
                if (ShowDebugLog) Debug.LogWarning($"[{GetType().Name}] STATE DESYNC: _isAttacking=true but animator not in attack state for 2+ seconds. Forcing reset.");
                _isAttacking = false;
                ResetCombo();
                return;
            }

            AttackDataSO currentAttack = currentSequence?.GetAttackAtIndex(currentAttackIndex);

            if (currentAttack != null && _currentAttackWeapons != null && _currentAttackWeapons.Count > 0 && animatorInAttack && _wasInAttackStateLastFrame)
            {
                UpdateWeaponColliderTiming(normalizedTime);
                UpdateMovementDisableFromAttack(currentAttack, normalizedTime);
                TryTriggerAttackEffects(currentAttack, normalizedTime);
            }

            bool currentStateIsAttack = Helper.IsInAttackState(_animator);
            bool attackFinished = !_isAttackFinishing && TryDetectAttackFinished(currentStateIsAttack);
            _wasInAttackStateLastFrame = currentStateIsAttack;

            if (attackFinished)
            {
                int finishedAttackIndex = currentAttackIndex;
                OnAttackFinishedCleanup();
                ProcessComboBufferOnFinish(finishedAttackIndex);
            }
        }

        private bool TryDetectAttackFinished(bool currentStateIsAttack)
        {
            if (_isAttacking && _wasInAttackStateLastFrame && !currentStateIsAttack)
            {
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Attack finished");
                MarkAttackFinished();
                return true;
            }
            if (_isAttacking && Time.time - _lastAttackStartTime > 3.0f)
            {
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Attack timeout");
                MarkAttackFinished();
                return true;
            }
            return false;
        }

        private void MarkAttackFinished()
        {
            _isAttackFinishing = true;
            _attackLocked = true;
            _isAttacking = false;
            _lastAttackEndTime = Time.time;
            _attackLocked = false;
        }

        private void OnAttackFinishedCleanup()
        {
            foreach (Weapon weapon in _currentAttackWeapons)
            {
                if (weapon != null && weapon.WeaponCollider != null) weapon.DisableWeaponCollider();
            }
            if (_animator != null) ResetAttackTriggersForSequence();
            if (_comboWindowActive)
            {
                _comboWindowActive = false;
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo window CLOSED - attack finished");
            }
        }

        private void ProcessComboBufferOnFinish(int finishedAttackIndex)
        {
            if (_inputBuffer.Count > 0)
            {
                int bufferedAtAttackIndex = _inputBuffer.Dequeue();
                if (bufferedAtAttackIndex <= finishedAttackIndex)
                {
                    bool isLastAttack = currentSequence == null || currentAttackIndex >= currentSequence.SequenceLength - 1;
                    if (!isLastAttack)
                    {
                        currentAttackIndex++;
                        _stateTransitionInProgress = true;
                        ExecuteAttack(currentSequence.GetAttackAtIndex(currentAttackIndex), currentSequence);
                        _stateTransitionInProgress = false;
                        if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo continued to attack #{currentAttackIndex}. Queue remaining: {_inputBuffer.Count}");
                    }
                    else
                    {
                        if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Last attack finished, starting NEW COMBO from buffered input.");
                        ResetCombo();
                        PerformAttack();
                    }
                }
                else
                {
                    _inputBuffer.Clear();
                    if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Buffered input from old combo chain, clearing buffer.");
                    ResetCombo();
                }
            }
            else if (!_isAttacking)
                ResetCombo();
        }

        protected void UpdateWeaponColliderTiming(float normalizedTime)
        {
            if (_currentAttackWeapons == null) return;
            const float EPSILON = 0.001f;
            foreach (Weapon weapon in _currentAttackWeapons)
            {
                if (weapon == null || weapon.WeaponCollider == null || !_weaponTimingMap.ContainsKey(weapon)) continue;
                Vector2 weaponTiming = _weaponTimingMap[weapon];
                bool shouldColliderBeActive = normalizedTime >= weaponTiming.x && normalizedTime <= (weaponTiming.y + EPSILON);
                if (shouldColliderBeActive && !weapon.WeaponCollider.enabled)
                {
                    weapon.EnableWeaponCollider();
                    if (ShowDebugLog) Debug.Log($"[weapon-collider] ENABLE {weapon.gameObject.name} t={normalizedTime:F3} window={weaponTiming.x:F3}-{weaponTiming.y:F3}");
                }
                else if (!shouldColliderBeActive && weapon.WeaponCollider.enabled)
                {
                    weapon.DisableWeaponCollider();
                    if (ShowDebugLog) Debug.Log($"[weapon-collider] DISABLE {weapon.gameObject.name} t={normalizedTime:F3} window={weaponTiming.x:F3}-{weaponTiming.y:F3}");
                }
            }
        }

        protected virtual void UpdateMovementDisableFromAttack(AttackDataSO currentAttack, float normalizedTime)
        {
            if (currentAttack == null) return;
            bool shouldMovementBeDisabled = normalizedTime >= currentAttack.movementDisableWindow.x &&
                                           normalizedTime <= currentAttack.movementDisableWindow.y;
            SetMovementDisabled(shouldMovementBeDisabled);
        }

        protected void TryTriggerAttackEffects(AttackDataSO currentAttack, float normalizedTime)
        {
            if (currentAttack == null) return;
            Vector3 pos = transform.position;
            var manager = CharacterEffectManager.Instance;
            if (currentAttack.particleEffects != null)
                foreach (var e in currentAttack.particleEffects)
                    if (e.IsValid && normalizedTime >= e.triggerTime && !_triggeredEffects.Contains(e.effectName))
                    { manager?.PlayParticle(e.effectName, pos); _triggeredEffects.Add(e.effectName); }
            if (currentAttack.vfxEffects != null)
                foreach (var e in currentAttack.vfxEffects)
                    if (e.IsValid && normalizedTime >= e.triggerTime && !_triggeredEffects.Contains(e.effectName))
                    { manager?.PlayVFX(e.effectName, pos); _triggeredEffects.Add(e.effectName); }
            if (currentAttack.audioEffects != null)
                foreach (var e in currentAttack.audioEffects)
                    if (e.IsValid && normalizedTime >= e.triggerTime && !_triggeredEffects.Contains(e.effectName))
                    { manager?.PlayAudio(e.effectName, pos); _triggeredEffects.Add(e.effectName); }
        }

        protected virtual void ResetCombo()
        {
            foreach (Weapon weapon in _currentAttackWeapons)
            {
                if (weapon != null && weapon.WeaponCollider != null)
                {
                    weapon.DisableWeaponCollider();
                }
            }

            _weaponTimingMap.Clear();
            _triggeredEffects.Clear();
            _inputBuffer.Clear();

            // Stop auto aim when combo resets
            if (_autoAimController != null)
            {
                _autoAimController.StopAutoAim();
            }

            // Full state reset like in TriggerEffect
            if (_animator != null)
            {
                ResetAttackTriggersForSequence();
            }

            // Re-enable movement when combo resets
            if (_movementController != null)
            {
                _movementController.SetMovementDisabled(false);
            }

            _attackLocked = true;
            currentAttackIndex = 0;
            currentSequence = null;
            _isAttacking = false;
            _isAttackFinishing = false;
            _comboWindowActive = false;
            _stateTransitionInProgress = false;
            _attackLocked = false;  // ✓ MUST be false at end!

            if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo reset");
        }

        protected virtual void TriggerEffect(UnityEngine.Object effect)
        {
            if (effect is ParticleSystem particleSystem)
            {
                particleSystem.Play();
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Triggered particle effect");
            }
            else if (effect is UnityEngine.VFX.VisualEffect visualEffect)
            {
                visualEffect.Play();
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Triggered VFX effect");
            }
            else if (effect is AudioClip audioClip)
            {
                AudioSource.PlayClipAtPoint(audioClip, transform.position);
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Triggered audio effect");
            }
            else
            {
                if (ShowDebugLog) Debug.LogWarning($"[{GetType().Name}] Unknown effect type: {effect.GetType()}");
            }
        }


        private void ResetAttackTriggersForSequence()
        {
            if (_animator == null)
                return;

            if (currentSequence != null)
            {
                for (int i = 0; i < currentSequence.SequenceLength; i++)
                {
                    AttackDataSO attackData = currentSequence.GetAttackAtIndex(i);
                    if (attackData != null && !string.IsNullOrEmpty(attackData.animationTriggerName))
                    {
                        _animator.ResetTrigger(attackData.animationTriggerName);
                    }
                }
            }
            else
            {
                string[] commonTriggers = { "Attack1", "Attack2", "Attack3", "Attack4", "Attack5" };
                foreach (string trigger in commonTriggers)
                {
                    if (_animator.parameters.Any(p => p.name == trigger))
                    {
                        _animator.ResetTrigger(trigger);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public void TriggerAttack()
        {
            if (_movementController != null && _movementController.IsDashing)
            {
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] TriggerAttack BLOCKED - Currently dashing");
                return;
            }

            // Check if player is dead
            if (_health != null && !_health.IsAlive)
            {
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] TriggerAttack BLOCKED - Player is dead");
                return;
            }

            if (ShowDebugLog) Debug.Log($"[{GetType().Name}] TriggerAttack called - IsAttacking: {_isAttacking}, AttackLocked: {_attackLocked}, AttackIndex: {currentAttackIndex}");

            if (currentCombatProfile != null)
            {
                if (_isAttacking && !_attackLocked && !_stateTransitionInProgress)
                {
                    if (_inputBuffer.Count < MAX_BUFFERED_INPUTS)
                    {
                        _inputBuffer.Enqueue(currentAttackIndex);
                        
                        bool isLastAttack = currentSequence != null && 
                                           currentAttackIndex >= currentSequence.SequenceLength - 1;
                        string bufferType = isLastAttack ? "for NEW COMBO" : "for next attack";
                        if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Attack buffered {bufferType} via TriggerAttack at attack #{currentAttackIndex}. Queue size: {_inputBuffer.Count}");
                    }
                }
                else if (!_isAttacking && !_attackLocked && !_stateTransitionInProgress)
                {
                    PerformAttack();
                }
            }
            else
            {
                if (ShowDebugLog) Debug.LogWarning($"[{GetType().Name}] No combat profile assigned!");
            }
        }

        /// <summary>
        /// Called ketika entity di-interrupt oleh damage.
        /// Reset semua attack state, disable weapons, dan clear buffers.
        /// </summary>
        public virtual void OnInterrupted()
        {
            if (!_isAttacking) return;
            
            if (ShowDebugLog) Debug.Log($"[{GetType().Name}] INTERRUPTED - Resetting attack state");
            
            // Disable all weapon colliders immediately
            foreach (Weapon weapon in _currentAttackWeapons)
            {
                if (weapon != null && weapon.WeaponCollider != null)
                {
                    weapon.DisableWeaponCollider();
                }
            }
            
            // Reset all attack triggers di animator
            if (_animator != null)
            {
                ResetAttackTriggersForSequence();
            }
            
            // Clear state
            _weaponTimingMap.Clear();
            _triggeredEffects.Clear();
            _inputBuffer.Clear();
            
            // Stop auto aim
            if (_autoAimController != null)
            {
                _autoAimController.StopAutoAim();
            }
            
            // Re-enable movement
            if (_movementController != null)
            {
                _movementController.SetMovementDisabled(false);
            }
            
            // Reset all flags
            currentAttackIndex = 0;
            currentSequence = null;
            _isAttacking = false;
            _isAttackFinishing = false;
            _comboWindowActive = false;
            _stateTransitionInProgress = false;
            _attackLocked = false;
        }

        #endregion

        #region Protected Properties for Inheritance

        protected bool ComboWindowActive => _comboWindowActive;
        protected int BufferedInputCount => _inputBuffer.Count;  // ✓ Queue size accessor
        protected bool AttackLocked => _attackLocked;
        protected bool StateTransitionInProgress => _stateTransitionInProgress;
        protected Animator Animator => _animator;

        protected void SetAnimator(Animator animator)
        {
            _animator = animator;
        }

        /// <summary>
        /// Get current attack data if attacking, else null
        /// </summary>
        public AttackDataSO GetCurrentAttackData()
        {
            if (!_isAttacking || currentSequence == null)
                return null;
            return currentSequence.GetAttackAtIndex(currentAttackIndex);
        }

        /// <summary>
        /// Get normalized time of current attack animation (0-1)
        /// </summary>
        public float GetCurrentAttackNormalizedTime()
        {
            if (_animator == null || !_isAttacking)
                return 0f;
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.normalizedTime % 1.0f;
        }

        /// <summary>
        /// Check if currently within input window time
        /// </summary>
        public bool IsWithinInputWindow()
        {
            if (!_isAttacking)
                return false;
            
            AttackDataSO currentAttack = GetCurrentAttackData();
            if (currentAttack == null)
                return false;

            float normalizedTime = GetCurrentAttackNormalizedTime();
            Vector2 inputWindow = currentAttack.inputWindow;
            const float EPSILON = 0.001f;
            return normalizedTime >= (inputWindow.x - EPSILON) && normalizedTime <= (inputWindow.y + EPSILON);
        }

        /// <summary>
        /// Check if currently within movement disable window time
        /// </summary>
        public bool IsWithinMovementDisableWindow()
        {
            if (!_isAttacking)
                return false;
            
            AttackDataSO currentAttack = GetCurrentAttackData();
            if (currentAttack == null)
                return false;

            float normalizedTime = GetCurrentAttackNormalizedTime();
            Vector2 movementWindow = currentAttack.movementDisableWindow;
            const float EPSILON = 0.001f;
            return normalizedTime >= (movementWindow.x - EPSILON) && normalizedTime <= (movementWindow.y + EPSILON);
        }

        #endregion
    }
}
