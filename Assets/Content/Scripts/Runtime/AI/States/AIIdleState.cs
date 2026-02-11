using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LittleHeroJourney
{
    public class AIIdleState : AIState
    {
        private Vector3 _currentPatrolTarget;
        private float _waitTimer = 0f;
        private bool _isWaiting = false;

        public AIIdleState(AIAgent agent) : base(agent) { }

        public override void OnStateEnter(AIStateType fromState)
        {
            base.OnStateEnter(fromState);
            if (Agent.Settings.ShowDebugLog) Debug.Log($"[{Agent.GetType().Name}] Entered Idle State");

            if (Agent.NavMeshAgent != null && Agent.NavMeshAgent.enabled && Agent.NavMeshAgent.isOnNavMesh)
            {
                Agent.NavMeshAgent.isStopped = true;
                Agent.NavMeshAgent.velocity = Vector3.zero;
            }

            _isWaiting = true;
            _waitTimer = Agent.Settings.IdleWaitTime;
            Agent.UpdateAnimationSpeed(0f);
        }

        public override void OnStateExit(AIStateType toState)
        {
            base.OnStateExit(toState);
            _isWaiting = false;
            _waitTimer = 0f;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (_isWaiting)
            {
                _waitTimer -= deltaTime;
                if (_waitTimer <= 0f) StartPatrol();
            }
            else
            {
                if (Agent.NavMeshAgent != null && !Agent.NavMeshAgent.pathPending &&
                    Agent.NavMeshAgent.remainingDistance <= Agent.NavMeshAgent.stoppingDistance)
                {
                    Agent.NavMeshAgent.isStopped = true;
                    _isWaiting = true;
                    _waitTimer = Agent.Settings.IdleWaitTime;
                    Agent.UpdateAnimationSpeed(0f);
                }
                else
                {
                    UpdateMovementAnimation();
                }
            }
        }

        private void StartPatrol()
        {
            if (Agent.NavMeshAgent == null) return;

            Vector3 randomDirection = Random.insideUnitSphere * Agent.Settings.IdleMoveRadius;
            randomDirection.y = 0f;
            Vector3 patrolTarget = Agent.transform.position + randomDirection;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(patrolTarget, out hit, Agent.Settings.IdleMoveRadius, NavMesh.AllAreas))
            {
                _currentPatrolTarget = hit.position;
                Agent.NavMeshAgent.SetDestination(_currentPatrolTarget);
                Agent.NavMeshAgent.isStopped = false;
                _isWaiting = false;
            }
            else
            {
                _waitTimer = Agent.Settings.IdleWaitTime;
            }
        }

        private void UpdateMovementAnimation()
        {
            if (Agent.NavMeshAgent == null) return;
            Agent.UpdateMovementAnimation(1f);
        }
    }
}