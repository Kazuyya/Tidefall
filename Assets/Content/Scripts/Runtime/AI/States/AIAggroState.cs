using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LittleHeroJourney
{
    public class AIAggroState : AIState
    {
        private float _lastPathUpdateTime = 0f;
        private const float PATH_UPDATE_INTERVAL = 0.3f;
        private bool _isTaunting = false;
        private float _tauntTimer = 0f;

        public AIAggroState(AIAgent agent) : base(agent) { }

        public override void OnStateEnter(AIStateType fromState)
        {
            base.OnStateEnter(fromState);
            if (Agent.Settings.ShowDebugLog) Debug.Log($"[{Agent.GetType().Name}] Entered Aggro State");

            _isTaunting = false;
            _tauntTimer = 0f;
            Agent.UpdateAnimationSpeed(0f);

            if (!Agent.HasDoneInitialTaunt)
            {
                _isTaunting = true;
                if (Agent.NavMeshAgent != null && Agent.NavMeshAgent.enabled && Agent.NavMeshAgent.isOnNavMesh)
                {
                    Agent.NavMeshAgent.isStopped = true;
                }
                if (Agent.Animator != null)
                {
                    Agent.Animator.SetTrigger(Agent.Settings.tauntTriggerName);
                    Agent.SetHasDoneInitialTaunt(true);
                }
            }
            else
            {
                Agent.StartMovementPreparation();
                if (Agent.NavMeshAgent != null && Agent.NavMeshAgent.enabled && Agent.NavMeshAgent.isOnNavMesh)
                {
                    Agent.NavMeshAgent.speed = Agent.EffectiveMoveSpeed;
                }
            }
        }

        public override void OnStateExit(AIStateType toState)
        {
            base.OnStateExit(toState);
            if (Agent.Animator != null) Agent.Animator.ResetTrigger(Agent.Settings.tauntTriggerName);
            if (Agent.NavMeshAgent != null && Agent.NavMeshAgent.enabled && Agent.NavMeshAgent.isOnNavMesh)
            {
                Agent.NavMeshAgent.isStopped = true;
            }
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (_isTaunting)
            {
                _tauntTimer += deltaTime;
                if (_tauntTimer >= Agent.Settings.TauntDuration)
                {
                    _isTaunting = false;
                    if (Agent.Animator != null) Agent.Animator.ResetTrigger(Agent.Settings.tauntTriggerName);
                    Agent.StartMovementPreparation();
                    if (Agent.NavMeshAgent != null && Agent.NavMeshAgent.enabled && Agent.NavMeshAgent.isOnNavMesh)
                    {
                        Agent.NavMeshAgent.speed = Agent.EffectiveMoveSpeed;
                    }
                }
                else
                {
                    Agent.FaceTarget();
                    Agent.UpdateAnimationSpeed(0f);
                    return;
                }
            }

            if (Agent.IsPreparingMovement)
            {
                Agent.UpdateMovementPreparation(deltaTime);
                if (!Agent.IsPreparingMovement)
                {
                    if (Agent.NavMeshAgent != null && Agent.NavMeshAgent.enabled && Agent.NavMeshAgent.isOnNavMesh)
                    {
                        Agent.NavMeshAgent.isStopped = false;
                    }
                }
                else
                {
                    Agent.FaceTarget();
                    Agent.UpdateAnimationSpeed(0f);
                    return;
                }
            }

            if (Agent.HasTarget)
            {
                Agent.SetAggroTimer(Agent.AggroTimer + deltaTime);
                if (Agent.AggroTimer >= Agent.Settings.AggroBuildupTime)
                {
                    float distanceToTarget = Vector3.Distance(Agent.transform.position, Agent.Target.position);
                    float attackRangeWithBuffer = Agent.Settings.AttackRange - 0.1f;
                    if (distanceToTarget <= attackRangeWithBuffer)
                    {
                        Agent.TransitionToState(AIStateType.Combat);
                        return;
                    }
                }
            }
            else
            {
                Agent.SetAggroTimer(0f);
                Agent.TransitionToState(AIStateType.Idle);
                return;
            }

            _lastPathUpdateTime += deltaTime;
            if (_lastPathUpdateTime >= PATH_UPDATE_INTERVAL)
            {
                UpdatePathToTarget();
                _lastPathUpdateTime = 0f;
            }

            UpdateMovementAnimation();
        }


        private void UpdatePathToTarget()
        {
            if (Agent.NavMeshAgent == null || Agent.Target == null) return;
            if (!Agent.NavMeshAgent.enabled || !Agent.NavMeshAgent.isOnNavMesh) return;

            float distanceToTarget = Vector3.Distance(Agent.transform.position, Agent.Target.position);
            Vector3 targetPosition = Agent.Target.position;

            if (distanceToTarget < Agent.Settings.AttackRange * 0.8f)
            {
                Vector3 directionFromTarget = (Agent.transform.position - Agent.Target.position).normalized;
                targetPosition = Agent.Target.position + directionFromTarget * (Agent.Settings.AttackRange * 0.5f);
            }

            Agent.NavMeshAgent.SetDestination(targetPosition);
        }

        private void UpdateMovementAnimation()
        {
            if (Agent.NavMeshAgent == null) return;

            if (Agent.HasTarget && Agent.Target != null) Agent.FaceTarget();
            if (Agent.NavMeshAgent.enabled && Agent.NavMeshAgent.isOnNavMesh)
            {
                Agent.UpdateMovementAnimation(1f);
            }
        }

    }
}