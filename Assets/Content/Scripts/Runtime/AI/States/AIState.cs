using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    public enum AIStateType
    {
        Idle,       // Patrol area, not aware of player
        Aggro,      // Noticed player, following but not attacking
        Combat,     // Active combat, attacking player
        Stagger     // Stunned/interrupted (placeholder for future)
    }

    public abstract class AIState
    {
        protected AIAgent Agent { get; private set; }
        protected AISettingsSO Settings => Agent.Settings;

        public AIState(AIAgent agent)
        {
            Agent = agent;
        }

        public virtual void OnStateEnter(AIStateType fromState) { }
        public virtual void OnStateExit(AIStateType toState) { }
        public virtual void Update(float deltaTime) { }
        public virtual void FixedUpdate(float fixedDeltaTime) { }
    }
}