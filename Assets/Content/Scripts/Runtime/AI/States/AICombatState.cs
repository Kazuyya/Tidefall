using UnityEngine;
using UnityEngine.AI;

namespace LittleHeroJourney
{
    public class AICombatState : AIState
    {
        private float _lastPathUpdateTime = 0f;
        private const float PATH_UPDATE_INTERVAL = 0.2f;
        private const float ATTACK_MOVE_STOPPING_DISTANCE = 0.3f;
        private bool _usingAttackMoveStoppingDistance = false;

        public AICombatState(AIAgent agent) : base(agent) { }

        public override void OnStateEnter(AIStateType fromState)
        {
            base.OnStateEnter(fromState);
            if (Agent.Settings.ShowDebugLog) Debug.Log($"[{Agent.GetType().Name}] Entered Combat State");

            if (Agent.NavMeshAgent != null && Agent.NavMeshAgent.enabled && Agent.NavMeshAgent.isOnNavMesh)
            {
                Agent.NavMeshAgent.speed = Agent.EffectiveMoveSpeed;
                Agent.SetNavMeshRotationEnabled(false);

                float distanceToTarget = Agent.Target != null ? Vector3.Distance(Agent.transform.position, Agent.Target.position) : float.MaxValue;
                if (distanceToTarget <= Agent.Settings.AttackRange)
                {
                    Agent.NavMeshAgent.isStopped = true;
                    Agent.NavMeshAgent.velocity = Vector3.zero;
                    Agent.NavMeshAgent.ResetPath();
                    Agent.UpdateAnimationSpeed(0f);
                }
                else
                {
                    Agent.NavMeshAgent.isStopped = false;
                }
            }
        }

        public override void OnStateExit(AIStateType toState)
        {
            base.OnStateExit(toState);
            if (Agent.NavMeshAgent != null && Agent.NavMeshAgent.enabled) Agent.NavMeshAgent.isStopped = true;
            Agent.SetNavMeshRotationEnabled(true);
            Agent.UpdateAnimationSpeed(0f);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (!Agent.HasTarget)
            {
                if (Agent.Settings.ShowDebugLog) Debug.Log($"[{Agent.GetType().Name}] CombatState: No target, transitioning to Aggro");
                Agent.TransitionToState(AIStateType.Aggro);
                return;
            }

            float dist = Vector3.Distance(Agent.transform.position, Agent.Target.position);
            if (!Agent.IsAttacking && ShouldTransitionToAggro(dist))
            {
                Agent.TransitionToState(AIStateType.Aggro);
                return;
            }

            Agent.FaceTarget();

            _lastPathUpdateTime += deltaTime;
            if (_lastPathUpdateTime >= PATH_UPDATE_INTERVAL)
            {
                UpdateCombatBehavior(dist);
                _lastPathUpdateTime = 0f;
            }

            UpdateCombatAnimation(dist);
        }

        private bool ShouldTransitionToAggro(float distanceToTarget)
        {
            float attackRangeWithBuffer = Agent.Settings.AttackRange + 0.2f;
            bool ok = distanceToTarget > attackRangeWithBuffer && distanceToTarget <= Agent.Settings.AggroRange;
            if (Agent.Settings.ShowDebugLog && ok)
                Debug.Log($"[{Agent.GetType().Name}] CombatState: Distance {distanceToTarget:F2} > AttackRange {Agent.Settings.AttackRange} && <= AggroRange {Agent.Settings.AggroRange}, transitioning to Aggro");
            return ok;
        }

        private void UpdateCombatBehavior(float distanceToTarget)
        {
            if (Agent.NavMeshAgent == null || Agent.Target == null || Agent.IsDead || !Agent.NavMeshAgent.enabled) return;
            if (distanceToTarget <= Agent.Settings.AttackRange)
                HandleInAttackRange();
            else
                HandleOutOfAttackRange();
        }

        private void HandleInAttackRange()
        {
            var combat = Agent.Combatant as AICombat;
            bool movementDisabledByWindow = combat != null && combat.GetIsInsideMovementDisableWindow();
            if (Agent.IsAttacking)
            {
                if (movementDisabledByWindow)
                {
                    ResetAttackMoveStoppingDistance();
                    if (!Agent.NavMeshAgent.isStopped)
                    {
                        Agent.NavMeshAgent.isStopped = true;
                        Agent.NavMeshAgent.velocity = Vector3.zero;
                        Agent.NavMeshAgent.ResetPath();
                    }
                    Agent.UpdateAnimationSpeed(0f);
                }
                else
                {
                    Agent.NavMeshAgent.isStopped = false;
                    if (!_usingAttackMoveStoppingDistance)
                    {
                        Agent.NavMeshAgent.stoppingDistance = ATTACK_MOVE_STOPPING_DISTANCE;
                        _usingAttackMoveStoppingDistance = true;
                    }
                    Agent.NavMeshAgent.SetDestination(Agent.Target.position);
                }
            }
            else
            {
                ResetAttackMoveStoppingDistance();
                if (!Agent.NavMeshAgent.isStopped)
                {
                    Agent.NavMeshAgent.isStopped = true;
                    Agent.NavMeshAgent.velocity = Vector3.zero;
                    Agent.NavMeshAgent.ResetPath();
                    if (Agent.Settings.ShowDebugLog) Debug.Log($"[{Agent.GetType().Name}] CombatState: Stopping agent - in attack range");
                }
                Agent.UpdateAnimationSpeed(0f);
                TryAttack();
            }
        }

        private void HandleOutOfAttackRange()
        {
            ResetAttackMoveStoppingDistance();
            if (Agent.NavMeshAgent.isStopped)
            {
                Agent.NavMeshAgent.isStopped = false;
                if (Agent.Settings.ShowDebugLog) Debug.Log($"[{Agent.GetType().Name}] CombatState: Resuming agent movement - moving to target");
            }
            Agent.NavMeshAgent.SetDestination(Agent.Target.position);
        }

        private void ResetAttackMoveStoppingDistance()
        {
            if (!_usingAttackMoveStoppingDistance) return;
            Agent.NavMeshAgent.stoppingDistance = Agent.Settings.StoppingDistance;
            _usingAttackMoveStoppingDistance = false;
        }


        private void TryAttack()
        {
            if (Agent.CombatCooldownTimer > 0f || Agent.IsDead) return;

            var combat = Agent.Combatant as AICombat;
            if (combat != null)
            {
                combat.TriggerAIAttack();
                Agent.SetCombatCooldownTimer(Agent.EffectiveCombatCooldownTime);
                if (Agent.Settings.ShowDebugLog)
                    Debug.Log($"[{Agent.GetType().Name}] Attack triggered, cooldown: {Agent.EffectiveCombatCooldownTime}s");
            }
        }

        private void UpdateCombatAnimation(float distanceToTarget)
        {
            if (Agent.NavMeshAgent == null || !Agent.NavMeshAgent.enabled) return;

            if (Agent.NavMeshAgent.isStopped)
            {
                Agent.UpdateAnimationSpeed(0f);
                return;
            }
            Agent.UpdateMovementAnimation(1f);
        }
    }
}