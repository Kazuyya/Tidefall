using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

namespace LittleHeroJourney
{
    public abstract class PlayerMovementState
    {
        protected PlayerMovementController Controller { get; private set; }
        protected KinematicCharacterMotor Motor => Controller.Motor;

        public PlayerMovementState(PlayerMovementController controller)
        {
            Controller = controller;
        }

        public virtual void OnStateEnter(PlayerState fromState) { }
        public virtual void OnStateExit(PlayerState toState) { }
        public virtual void Update(float deltaTime) { }
        public virtual void SetInputs(ref PlayerMovementInputs inputs) { }
        public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) { }
        public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime) { }
        public virtual void BeforeCharacterUpdate(float deltaTime) { }
        public virtual void UpdateAnimation(float deltaTime) { }
    }
}