using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using LittleHeroJourney.UI;

namespace LittleHeroJourney
{
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class AIAgent : MonoBehaviour
    {
        [Header("AI Settings")]
        public AISettingsSO Settings;

        [Header("Spawn Effects")]
        [SerializeField] private CharacterSpawnEffectsSO spawnEffectsSO;

        protected NavMeshAgent _navMeshAgent;
        protected Animator _animator;
        protected Transform _target;
        protected ICombatant _combatant;
        protected Health _health;
        protected AIState _currentState;
        protected AIIdleState _idleState;
        protected AIAggroState _aggroState;
        protected AICombatState _combatState;
        protected AIStateType _currentStateType;

        protected float _detectionTimer = 0f;
        protected bool _hasTarget = false;
        private float _aggroTimer = 0f;
        protected bool _isAggro = false;
        protected Vector3 _idleCenterPosition;
        private bool _hasDoneInitialTaunt = false;
        private bool _isPreparingMovement = false;
        private float _movementPreparationTimer = 0f;
        private float _combatCooldownTimer = 0f;

        protected float _currentAnimationSpeed = 0f;
        private int _cachedSpeedParameterHash = -1;
        private bool _isDead = false;
        private LevelableStats _levelable;

        public NavMeshAgent NavMeshAgent => _navMeshAgent;
        /// <summary>Move speed from LevelableStats if present and &gt; 0, else from Settings.</summary>
        public float EffectiveMoveSpeed => _levelable != null && _levelable.MoveSpeed > 0f ? _levelable.MoveSpeed : (Settings != null ? Settings.MoveSpeed : 0f);
        /// <summary>Cooldown from LevelableStats if present and &gt; 0, else from Settings.</summary>
        public float EffectiveCombatCooldownTime => _levelable != null && _levelable.AttackCooldown > 0f ? _levelable.AttackCooldown : (Settings != null ? Settings.CombatCooldownTime : 0f);
        public Animator Animator => _animator;
        public Transform Target => _target;
        public bool HasTarget => _hasTarget;
        public ICombatant Combatant => _combatant;  // ← CHANGED: Public property for interface

        public float AggroTimer => _aggroTimer;
        public bool HasDoneInitialTaunt => _hasDoneInitialTaunt;
        public float CombatCooldownTimer => _combatCooldownTimer;
        public bool IsPreparingMovement => _isPreparingMovement;
        public float MovementPreparationTimer => _movementPreparationTimer;
        public bool IsDead => _isDead;

        public void SetNavMeshRotationEnabled(bool enabled)
        {
            if (_navMeshAgent != null)
            {
                _navMeshAgent.updateRotation = enabled;
            }
        }

        public bool IsNavMeshRotationEnabled => _navMeshAgent != null && _navMeshAgent.updateRotation;
        public void SetHasDoneInitialTaunt(bool value) => _hasDoneInitialTaunt = value;
        public void SetAggroTimer(float value) => _aggroTimer = value;
        public void SetCombatCooldownTimer(float value) => _combatCooldownTimer = value;

        public void StartMovementPreparation()
        {
            _isPreparingMovement = true;
            _movementPreparationTimer = Settings.MovementPreparationTime;
            UpdateAnimationSpeed(0f);
        }

        public void UpdateMovementPreparation(float deltaTime)
        {
            if (!_isPreparingMovement) return;

            _movementPreparationTimer -= deltaTime;
            if (_movementPreparationTimer <= 0f)
            {
                _isPreparingMovement = false;
                _movementPreparationTimer = 0f;
            }
        }

        /// <summary>
        /// Check if AI is currently attacking (no reflection, type-safe)
        /// </summary>
        public bool IsAttacking
        {
            get
            {
                return _combatant != null && _combatant.IsAttacking;
            }
        }

        protected virtual void Awake()
        {
            InitializeComponents();
            InitializeStates();
            
            // Trigger spawn effects immediately
            if (spawnEffectsSO != null)
            {
                PlaySpawnEffects();
            }

            // Trigger spawn scale animation (0→1 with bouncy curve)
            CharacterSpawnDeathAnimator scaleAnimator = GetComponent<CharacterSpawnDeathAnimator>();
            if (scaleAnimator != null)
            {
                scaleAnimator.PlaySpawnAnimation();
            }

            // Trigger spawn UI animation (health bar fade in)
            CharacterBarsConnector barsConnector = GetComponent<CharacterBarsConnector>();
            if (barsConnector != null)
            {
                barsConnector.PlaySpawnAnimation();
            }
        }

        protected virtual void OnDestroy()
        {
            if (_health != null)
            {
                _health.OnDeath -= HandleDeath;
            }
        }

        protected virtual void InitializeComponents()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            if (_navMeshAgent == null)
            {
                _navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
                if (Settings.ShowDebugLog) Debug.Log($"[{GetType().Name}] NavMeshAgent auto-added");
            }

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.height = 2f;
                capsule.radius = 0.5f;
                capsule.center = Vector3.up;
                if (Settings.ShowDebugLog) Debug.Log($"[{GetType().Name}] CapsuleCollider auto-added");
            }

            _animator = Helper.GetAndCacheAnimator(this, searchInChildren: true, 
                showDebugLog: false, ignoreLayerName: "UI");
            if (_animator == null)
            {
                if (Settings.ShowDebugLog) Debug.LogWarning($"[{GetType().Name}] Animator component not found!");
            }

            _combatant = GetComponent<ICombatant>();  // ← CHANGED: Use interface
            if (_combatant == null)
            {
                if (Settings.ShowDebugLog) Debug.LogWarning($"[{GetType().Name}] ICombatant component not found!");
            }

            _levelable = GetComponent<LevelableStats>();
            _health = GetComponent<Health>();
            if (_health != null)
            {
                _health.OnDeath += HandleDeath;
            }

            if (_navMeshAgent != null)
            {
                _navMeshAgent.speed = EffectiveMoveSpeed;
                _navMeshAgent.angularSpeed = Settings.RotationSpeed;
                _navMeshAgent.stoppingDistance = Settings.StoppingDistance;
                _navMeshAgent.updateRotation = true;
                _navMeshAgent.updatePosition = true;
            }
        }

        protected virtual void InitializeStates()
        {
            _idleCenterPosition = transform.position;

            _idleState = new AIIdleState(this);
            _aggroState = new AIAggroState(this);
            _combatState = new AICombatState(this);

            TransitionToState(AIStateType.Idle);
        }

        public void TransitionToState(AIStateType newState)
        {
            AIStateType previousState = _currentStateType;

            if (_currentState != null) _currentState.OnStateExit(newState);

            _currentStateType = newState;

            switch (newState)
            {
                case AIStateType.Idle:
                    _currentState = _idleState;
                    break;
                case AIStateType.Aggro:
                    _currentState = _aggroState;
                    break;
                case AIStateType.Combat:
                    _currentState = _combatState;
                    break;
            }

            if (_currentState != null) _currentState.OnStateEnter(previousState);

            if (Settings.ShowDebugLog) Debug.Log($"[{GetType().Name}] Transitioned from {previousState} to {newState}");
        }

        protected virtual void UpdateDetection()
        {
            _detectionTimer += Time.deltaTime;
            if (_detectionTimer < Settings.DetectionUpdateInterval) return;

            _detectionTimer = 0f;

            if (_combatCooldownTimer > 0f) _combatCooldownTimer -= Settings.DetectionUpdateInterval;

            Transform nearestTarget = FindNearestTarget();
            SetTarget(nearestTarget);

            if (_hasTarget && _isAggro)
            {
                float distanceToTarget = Vector3.Distance(transform.position, _target.position);
                if (distanceToTarget > Settings.LoseAggroDistance)
                {
                    _isAggro = false;
                    _aggroTimer = 0f;
                    TransitionToState(AIStateType.Idle);
                }
            }
        }

        protected virtual Transform FindNearestTarget()
        {
            Transform target = Helper.FindNearestTarget(
                transform.position, Settings.DetectionRange, Settings.TargetLayers, out float distance);
            return target;
        }

        public virtual void SetTarget(Transform newTarget)
        {
            bool hadTarget = _hasTarget;
            _target = newTarget;
            _hasTarget = newTarget != null;

            if (_hasTarget)
            {
                float distanceToTarget = Vector3.Distance(transform.position, _target.position);

                if (_currentStateType == AIStateType.Idle)
                {
                    TransitionToState(AIStateType.Aggro);
                    _aggroTimer = 0f;
                }
            }
            else
            {
                _isAggro = false;
                _aggroTimer = 0f;
                _combatCooldownTimer = 0f;

                if (_currentStateType != AIStateType.Idle)
                {
                    TransitionToState(AIStateType.Idle);
                }
            }
        }

        public virtual bool IsTargetInAttackRange()
        {
            if (_target == null) return false;
            return Vector3.Distance(transform.position, _target.position) <= Settings.AttackRange;
        }

        protected virtual void Update()
        {
            // Skip all AI behavior if dead
            if (_isDead) return;

            UpdateDetection();
            UpdateCombatTimers();

            if (_currentState != null)
                _currentState.Update(Time.deltaTime);
        }

        protected virtual void FixedUpdate()
        {
            // Skip all AI behavior if dead
            if (_isDead) return;

            if (_currentState != null)
                _currentState.FixedUpdate(Time.fixedDeltaTime);
        }

        protected virtual void UpdateCombatTimers()
        {
        }

        public virtual void UpdateAnimationSpeed(float normalizedSpeed)
        {
            if (_animator != null && !string.IsNullOrEmpty(Settings.speedParameterName))
            {
                if (_cachedSpeedParameterHash == -1)
                {
                    _cachedSpeedParameterHash = Animator.StringToHash(Settings.speedParameterName);
                }

                float lerpSpeed = (normalizedSpeed <= 0f) ? 5f : 8f;
                _currentAnimationSpeed = Mathf.Lerp(_currentAnimationSpeed, normalizedSpeed, Time.deltaTime * lerpSpeed);

                if (normalizedSpeed <= 0f && _currentAnimationSpeed < 0.001f)
                    _currentAnimationSpeed = 0f;
                else if (Mathf.Abs(_currentAnimationSpeed - normalizedSpeed) < 0.01f)
                    _currentAnimationSpeed = normalizedSpeed;

                _animator.SetFloat(_cachedSpeedParameterHash, _currentAnimationSpeed);
            }
        }

        protected virtual void HandleDeath()
        {
            _isDead = true;

            if (_navMeshAgent != null)
            {
                _navMeshAgent.enabled = false;
            }

            // Disable capsule collider to prevent further interactions
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.enabled = false;
            }

            TransitionToState(AIStateType.Idle);

            if (Settings.ShowDebugLog)
            {
                Debug.Log($"[{GetType().Name}] AI died - all behavior and collision disabled");
            }
        }

        public virtual void Revive()
        {
            _isDead = false;

            if (_navMeshAgent != null)
            {
                _navMeshAgent.enabled = true;
            }

            // Re-enable capsule collider
            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.enabled = true;
            }

            TransitionToState(AIStateType.Idle);

            if (Settings.ShowDebugLog)
            {
                Debug.Log($"[{GetType().Name}] AI revived - behavior and collision re-enabled");
            }
        }

        public virtual void FaceTarget()
        {
            if (_target == null || _navMeshAgent == null) return;

            Vector3 directionToTarget = (_target.position - transform.position).normalized;
            directionToTarget.y = 0f;

            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    Settings.RotationSpeed * Time.deltaTime
                );
            }
        }

        public virtual void UpdateMovementAnimation(float speedMultiplier = 1f)
        {
            if (_navMeshAgent == null || !_navMeshAgent.enabled || !_navMeshAgent.isOnNavMesh) return;

            float remainingDistance = _navMeshAgent.remainingDistance;
            float stoppingDistance = _navMeshAgent.stoppingDistance;
            float currentSpeed = _navMeshAgent.velocity.magnitude;
            bool isStopped = _navMeshAgent.isStopped;

            if (Settings.ShowDebugLog && remainingDistance > stoppingDistance + 0.5f && currentSpeed < 0.1f && !isStopped)
            {
                Debug.LogWarning($"[{GetType().Name}] STUCK DETECTED: Distance={remainingDistance:F2}, StopDist={stoppingDistance:F2}, Velocity={currentSpeed:F3}, IsStopped={isStopped} | State: {_currentStateType}");
            }

            if (isStopped || remainingDistance <= stoppingDistance + 0.1f)
            {
                UpdateAnimationSpeed(0f);
                return;
            }

            if (remainingDistance > stoppingDistance + 0.5f && currentSpeed < 0.1f)
            {
                UpdateAnimationSpeed(0f);
                return;
            }

            float maxSpeed = EffectiveMoveSpeed * speedMultiplier;
            float normalizedSpeed = Mathf.Clamp01(currentSpeed / maxSpeed);
            UpdateAnimationSpeed(normalizedSpeed);
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmos()
        {
            if (!Settings.ShowDebugGizmos || Settings == null) return;

            DrawDetectionRange();
            DrawAttackRange();
            DrawTargetLine();
        }

        protected virtual void DrawDetectionRange()
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, Settings.DetectionRange);
        }

        protected virtual void DrawAttackRange()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, Settings.AttackRange);
        }

        protected virtual void DrawTargetLine()
        {
            if (_target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _target.position);
                Gizmos.DrawWireCube(_target.position, Vector3.one * 0.5f);
            }
        }
#endif

        #region Effect Methods

        /// <summary>
        /// Play spawn effects immediately
        /// </summary>
        private void PlaySpawnEffects()
        {
            if (spawnEffectsSO == null) return;

            CharacterEffectManager manager = CharacterEffectManager.Instance;
            if (manager == null) return;

            // Play VFX effects
            foreach (var vfx in spawnEffectsSO.VFXEffects)
            {
                if (vfx.IsValid)
                    manager.PlayVFX(vfx.effectName, transform.position, transform.rotation);
            }

            // Play Audio effects
            foreach (var audio in spawnEffectsSO.AudioEffects)
            {
                if (audio.IsValid)
                    manager.PlayAudio(audio.effectName, transform.position);
            }

            // Play Particle effects
            foreach (var particle in spawnEffectsSO.ParticleEffects)
            {
                if (particle.IsValid)
                    manager.PlayParticle(particle.effectName, transform.position, transform.rotation);
            }
        }

        #endregion
    }
}
