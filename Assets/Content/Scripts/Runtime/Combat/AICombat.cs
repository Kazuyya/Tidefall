using UnityEngine;

namespace LittleHeroJourney
{
    public class AICombat : PlayerCombat, ICombatant
    {
        private AIAgent _aiAgent;
        private bool _movementDisabledByCombat = false;
        private int _currentLoopCount = 0;

        public bool IsMovementDisabledByCombat => _movementDisabledByCombat;

        public bool GetIsInsideMovementDisableWindow()
        {
            if (!IsAttacking || Animator == null || currentSequence == null) return false;
            AttackDataSO attack = currentSequence.GetAttackAtIndex(currentAttackIndex);
            if (attack == null) return false;
            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            float normTime = state.normalizedTime % 1f;
            if (normTime < 0.15f) return true;
            return attack.IsMovementDisabledAt(normTime);
        }

        protected override Animator GetCombatAnimator()
        {
            return Helper.GetAndCacheAnimator(this, searchInChildren: true, 
                showDebugLog: false, ignoreLayerName: "UI");
        }

        protected override void OnEnable()
        {
            _aiAgent = GetComponent<AIAgent>();
            
            foreach (var weaponEntry in availableWeapons)
            {
                if (weaponEntry.weaponComponent != null)
                {
                    weaponEntry.weaponComponent.OnHitEnemy += OnWeaponHitEnemy;
                }
            }
        }

        protected override void OnDisable()
        {
            foreach (var weaponEntry in availableWeapons)
            {
                if (weaponEntry.weaponComponent != null)
                {
                    weaponEntry.weaponComponent.OnHitEnemy -= OnWeaponHitEnemy;
                }
            }
            
            DisableHitboxes();
        }

        protected override void Start()
        {
            // Initialize base class (will auto-detect animator for AI)
            base.Start();
            
            foreach (var weaponEntry in availableWeapons)
            {
                if (weaponEntry.weaponComponent == null) continue;
                weaponEntry.weaponComponent.OnHitEnemy += OnWeaponHitEnemy;
                weaponEntry.weaponComponent.ResetForNewAttack();
                weaponEntry.weaponComponent.DisableWeapon();
            }

            if (availableWeapons.Count > 0 && availableWeapons[0].weaponComponent != null)
                currentWeapon = availableWeapons[0].weaponComponent;
        }

        private void Update()
        {
            UpdateComboWindowState();
            UpdateAttackState();
            UpdateTimers();
        }

        protected override void SetMovementDisabled(bool disabled)
        {
            if (_aiAgent == null) return;

            if (disabled && !_movementDisabledByCombat)
            {
                if (_aiAgent.NavMeshAgent != null) _aiAgent.NavMeshAgent.updateRotation = false;
                _movementDisabledByCombat = true;
            }
            else if (!disabled && _movementDisabledByCombat)
            {
                if (_aiAgent.NavMeshAgent != null) _aiAgent.NavMeshAgent.updateRotation = true;
                _movementDisabledByCombat = false;
            }
        }

        // Override UpdateComboWindowState for AI - simplified logic, no timing window needed
        protected override void UpdateComboWindowState()
        {
            if (!IsAttacking || Animator == null || currentSequence == null)
            {
                if (ComboWindowActive)
                {
                    // Combo window will be managed by base class property setter
                    // We can't directly set it, so we'll handle it in attack finish logic
                }
                return;
            }

            AttackDataSO currentAttack = currentSequence.GetAttackAtIndex(currentAttackIndex);
            if (currentAttack == null)
            {
                return;
            }

            // For AI: combo window is simply "are we in a combo sequence and not at last attack?"
            // No timing window check needed - AI uses probability-based decision instead
            bool isLastAttackInSequence = currentAttackIndex >= currentSequence.SequenceLength - 1;
            
            if (isLastAttackInSequence)
            {
                // Last attack - check if loop is allowed
                if (currentSequence.allowComboLoop)
                {
                    // Allow combo window for potential loop
                    // Will be checked in ShouldContinueCombo()
                }
                else
                {
                    // No loop allowed, no combo window for last attack
                    return;
                }
            }
            else
            {
                // Not last attack - combo window is "active" (AI can continue)
                // The actual decision happens in ShouldContinueCombo() when attack finishes
            }
        }

        // Decision method for AI combo continuation
        private bool ShouldContinueCombo()
        {
            if (currentSequence == null)
            {
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo decision: STOP (no sequence) ");
                return false;
            }

            bool isLastAttack = currentAttackIndex >= currentSequence.SequenceLength - 1;
            int currentIdx = currentAttackIndex;
            AttackDataSO currentAttackData = currentSequence.GetAttackAtIndex(currentAttackIndex);

            if (isLastAttack)
            {
                if (!currentSequence.allowComboLoop)
                {
                    if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo decision: STOP | last attack {currentIdx}, allowComboLoop=false");
                    return false;
                }

                if (currentSequence.maxComboLoops > 0 && _currentLoopCount >= currentSequence.maxComboLoops)
                {
                    if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo decision: STOP | last attack {currentIdx}, max loops reached ({_currentLoopCount}/{currentSequence.maxComboLoops})");
                    return false;
                }

                if (currentAttackData == null)
                {
                    if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo decision: STOP | last attack {currentIdx}, no attack data");
                    return false;
                }

                float continueChance = currentAttackData.aiComboContinueChance;
                float roll = Random.value;
                bool shouldContinue = roll < continueChance;

                if (shouldContinue)
                {
                    _currentLoopCount++;
                    if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo decision: CONTINUE -> LOOP to attack 0 | last attack {currentIdx}, chance={continueChance * 100:F0}%, roll={roll:F2}, loops={_currentLoopCount}");
                }
                else
                {
                    if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo decision: STOP | last attack {currentIdx}, chance={continueChance * 100:F0}%, roll={roll:F2} (failed)");
                }

                return shouldContinue;
            }

            if (currentAttackData == null)
            {
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo decision: STOP | attack {currentIdx}, no attack data");
                return false;
            }

            float chance = currentAttackData.aiComboContinueChance;
            float r = Random.value;
            bool cont = r < chance;

            if (cont)
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo decision: CONTINUE -> attack {currentIdx + 1} | from {currentIdx}, chance={chance * 100:F0}%, roll={r:F2}");
            else
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo decision: STOP | at attack {currentIdx}, chance={chance * 100:F0}%, roll={r:F2} (failed)");

            return cont;
        }

        // Track attack state for AI (separate from base class)
        private bool _aiWasInAttackStateLastFrame = false;

        // Override UpdateAttackState to use AI decision system instead of buffered input
        protected override void UpdateAttackState()
        {
            if (!IsAttacking) 
            {
                _aiWasInAttackStateLastFrame = false;
                return;
            }

            if (Animator == null)
            {
                base.UpdateAttackState();
                return;
            }

            // Check if attack finished BEFORE base class handles it
            AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
            bool currentStateIsAttack = Helper.IsInAttackState(Animator);

            // Check if attack just finished (transition from attack to non-attack)
            bool attackJustFinished = IsAttacking && _aiWasInAttackStateLastFrame && !currentStateIsAttack;

            if (attackJustFinished)
            {
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] ⚡ Attack finished detected (AI) - IsAttacking={IsAttacking}, wasInAttack={_aiWasInAttackStateLastFrame}, nowInAttack={currentStateIsAttack}");
                // Attack finished - handle with AI decision system BEFORE base class resets
                // Clear buffered input to prevent base class from handling
                // Queue automatically clears on finish, no need to manually set
                HandleAttackFinished();
                _aiWasInAttackStateLastFrame = false;
                return; // Don't call base - we already handled it
            }

            // Update tracking
            _aiWasInAttackStateLastFrame = currentStateIsAttack;

            // Update weapon timing and effects manually (don't call base to avoid attack finish handling)
            UpdateWeaponTimingAndEffects(stateInfo);
        }

        private void UpdateWeaponTimingAndEffects(AnimatorStateInfo stateInfo)
        {
            if (currentSequence == null) return;

            float normalizedTime;
            bool animatorInAttack;

            bool transitioning = Animator.IsInTransition(0);
            bool nextIsAttack = transitioning && Helper.IsNextStateAttack(Animator);

            if (nextIsAttack)
            {
                AnimatorStateInfo nextInfo = Animator.GetNextAnimatorStateInfo(0);
                normalizedTime = nextInfo.normalizedTime % 1.0f;
                animatorInAttack = true;
            }
            else
            {
                normalizedTime = stateInfo.normalizedTime % 1.0f;
                animatorInAttack = Helper.IsInAttackState(Animator);
            }

            if (!animatorInAttack) return;
            AttackDataSO currentAttack = currentSequence.GetAttackAtIndex(currentAttackIndex);
            if (currentAttack == null || _currentAttackWeapons == null || _currentAttackWeapons.Count == 0) return;
            UpdateWeaponColliderTiming(normalizedTime);
            UpdateMovementDisableFromAttack(currentAttack, normalizedTime);
            TryTriggerAttackEffects(currentAttack, normalizedTime);
        }

        private void HandleAttackFinished()
        {
            if (currentSequence == null) return;

            // Queue automatically clears on finish, no manual clear needed
            // Base class handles queue processing

            // Close combo window
            if (ComboWindowActive)
            {
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo window CLOSED - attack finished");
            }

            // Mark attack as finished (similar to base class)
            _isAttacking = false;
            _isAttackFinishing = false;

            // Use AI decision system
            bool continueCombo = ShouldContinueCombo();

            if (continueCombo)
            {
                if (currentAttackIndex >= currentSequence.SequenceLength - 1)
                {
                    currentAttackIndex = 0;
                    if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo result: LOOP -> attack 0 (loop #{_currentLoopCount})");
                }
                else
                {
                    currentAttackIndex++;
                    if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo result: NEXT -> attack {currentAttackIndex}");
                }

                AttackDataSO nextAttack = currentSequence.GetAttackAtIndex(currentAttackIndex);
                if (nextAttack != null)
                {
                    ExecuteAttack(nextAttack, currentSequence);
                }
                else
                {
                    _currentLoopCount = 0;
                    if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo result: STOP (no next attack data) -> reset");
                    ResetCombo();
                }
            }
            else
            {
                _currentLoopCount = 0;
                if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo result: STOP -> reset");
                ResetCombo();
            }
        }

        // Override PerformAttack to reset loop count when starting new combo
        protected override void PerformAttack()
        {
            // Reset loop count when starting new combo
            _currentLoopCount = 0;
            base.PerformAttack();
        }

        // Override ResetCombo to reset loop count
        protected override void ResetCombo()
        {
            _currentLoopCount = 0;
            base.ResetCombo();
            if (ShowDebugLog) Debug.Log($"[{GetType().Name}] Combo reset - loop count reset to 0");
        }
        
        // Override OnInterrupted to reset AI-specific state
        public override void OnInterrupted()
        {
            _currentLoopCount = 0;
            _aiWasInAttackStateLastFrame = false;
            
            // Call base implementation to reset attack state
            base.OnInterrupted();
            
            if (ShowDebugLog) Debug.Log($"[{GetType().Name}] AI INTERRUPTED - Attack cancelled, loop count reset");
        }

        public void TriggerAIAttack()
        {
            if (_aiAgent != null && _aiAgent.IsDead) return;

            if (currentCombatProfile != null)
            {
                if (ComboWindowActive)
                {
                    if (BufferedInputCount < MAX_BUFFERED_INPUTS)
                    {
                        // Queue handles buffering automatically
                        if (ShowDebugLog) Debug.Log($"[{GetType().Name}] AI attack buffered for combo");
                    }
                }
                else if (!IsAttacking && !AttackLocked && !StateTransitionInProgress)
                {
                    PerformAttack();
                }
            }
            else
            {
                if (ShowDebugLog) Debug.LogWarning($"[{GetType().Name}] No combat profile assigned!");
            }
        }
    }
}
