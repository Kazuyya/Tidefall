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
        private bool _tauntSfxTriggered = false;

        public AIAggroState(AIAgent agent) : base(agent) { }

        public override void OnStateEnter(AIStateType fromState)
        {
            base.OnStateEnter(fromState);
            if (Agent.Settings.ShowDebugLog) Debug.Log($"[{Agent.GetType().Name}] Entered Aggro State");

            _isTaunting = false;
            _tauntTimer = 0f;
            _tauntSfxTriggered = false;
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
            Agent.SetNavMeshRotationEnabled(true);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (_isTaunting)
            {
                _tauntTimer += deltaTime;
                TryTriggerTauntSfx();
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
                    Agent.SetNavMeshRotationEnabled(false);
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
                    Agent.SetNavMeshRotationEnabled(false);
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

        /// <summary>
        /// Plays taunt SFX once when normalized progress along TauntDuration reaches tauntSfxNormalizedTime (0–1), like AttackData audioEffects.
        /// </summary>
        private void TryTriggerTauntSfx()
        {
            if (_tauntSfxTriggered || Agent.Settings == null) return;

            string effectId = Agent.Settings.tauntSfxEffectName;
            if (string.IsNullOrEmpty(effectId)) return;

            float duration = Agent.Settings.TauntDuration;
            float normalized =
                duration > 0.0001f ? Mathf.Clamp01(_tauntTimer / duration) : 1f;
            float trigger = Mathf.Clamp01(Agent.Settings.tauntSfxNormalizedTime);

            if (normalized < trigger) return;

            _tauntSfxTriggered = true;

            CharacterEffectManager manager = CharacterEffectManager.Instance;
            if (manager == null) return;

            Vector3 pos = Agent.transform.position;
            if (Agent.Settings.tauntSfxPlaybackChannel == AudioPlaybackChannel.Bgm)
                manager.PlayBGM(effectId);
            else
                manager.PlayAudio(effectId, pos);
        }

        private void UpdateMovementAnimation()
        {
            if (Agent.NavMeshAgent == null) return;

            if (Agent.HasTarget && Agent.Target != null)
            {
                float ar = Agent.Settings.AttackRange;
                float closeSq = ar * ar * 1.69f; 
                Vector3 d = Agent.Target.position - Agent.transform.position;
                float distSq = d.x * d.x + d.z * d.z;
                Agent.SetNavMeshRotationEnabled(distSq > closeSq);
                Agent.FaceTarget();
            }
            else
                Agent.SetNavMeshRotationEnabled(true);

            if (Agent.NavMeshAgent.enabled && Agent.NavMeshAgent.isOnNavMesh)
            {
                Agent.UpdateMovementAnimation(1f);
            }
        }

    }
}