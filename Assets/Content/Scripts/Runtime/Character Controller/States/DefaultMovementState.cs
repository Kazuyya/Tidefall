using UnityEngine;
using KinematicCharacterController;

namespace LittleHeroJourney
{
    public class DefaultMovementState : PlayerMovementState
    {
        public DefaultMovementState(PlayerMovementController controller) : base(controller) { }

    public override void OnStateEnter(PlayerState fromState) { }

    public override void OnStateExit(PlayerState toState) { }

    public override void SetInputs(ref PlayerMovementInputs inputs)
    {
        Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
        if (cameraPlanarDirection.sqrMagnitude == 0f)
        {
            cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
        }

        if (!Controller.IsPlayerMovementInputIgnored)
        {
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxis.x, 0f, inputs.MoveAxis.y), 1f);
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);
            Controller.MoveInputVector = cameraPlanarRotation * moveInputVector;
        }

        // During auto aim, don't apply any rotation input
        if (Controller.AutoAimController != null && Controller.AutoAimController.IsAutoAiming)
        {
            Controller.LookInputVector = Vector3.zero;
            return;
        }

        switch (Controller.movementSettings.OrientationMethod)
        {
            case OrientationMethod.TowardsCamera:
                Controller.LookInputVector = cameraPlanarDirection;
                break;
            case OrientationMethod.TowardsMovement:
                Controller.LookInputVector = Controller.MoveInputVector.normalized;
                break;
        }
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (Controller.AutoAimController != null && Controller.AutoAimController.IsAutoAiming)
            return;

        if (Controller.LookInputVector.sqrMagnitude > 0f && Controller.movementSettings.OrientationSharpness > 0f)
        {
            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, Controller.LookInputVector, 1 - Mathf.Exp(-Controller.movementSettings.OrientationSharpness * deltaTime)).normalized;
            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
        }

        Vector3 currentUp = (currentRotation * Vector3.up);
        Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-Controller.movementSettings.OrientationSharpness * deltaTime));
        currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
       Vector3 effectiveMoveInput = Controller.IsMovementDisabled ? Vector3.zero : Controller.MoveInputVector;

        if (Motor.GroundingStatus.IsStableOnGround)
        {
            float currentVelocityMagnitude = currentVelocity.magnitude;
            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

        Vector3 inputRight = Vector3.Cross(effectiveMoveInput, Motor.CharacterUp);
        Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * effectiveMoveInput.magnitude;
            Vector3 targetMovementVelocity = reorientedInput * Controller.EffectiveMaxStableMoveSpeed;

            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-Controller.movementSettings.StableMovementSharpness * deltaTime));
        }
        else
        {
            if (effectiveMoveInput.sqrMagnitude > 0f)
            {
                Vector3 addedVelocity = effectiveMoveInput * Controller.movementSettings.AirAccelerationSpeed * deltaTime;
                Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                if (currentVelocityOnInputsPlane.magnitude < Controller.EffectiveMaxAirMoveSpeed)
                {
                    Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, Controller.EffectiveMaxAirMoveSpeed);
                    addedVelocity = newTotal - currentVelocityOnInputsPlane;
                }
                else
                {
                    if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                    {
                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                    }
                }

                currentVelocity += addedVelocity;
            }

            currentVelocity += Controller.movementSettings.Gravity * deltaTime;
            currentVelocity *= (1f / (1f + (Controller.movementSettings.Drag * deltaTime)));
        }

        if (Controller.InternalVelocityAdd.sqrMagnitude > 0f)
        {
            currentVelocity += Controller.InternalVelocityAdd;
            Controller.InternalVelocityAdd = Vector3.zero;
        }

        if (Controller.IsKnockedBack)
            currentVelocity = Vector3.zero;

        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
        float currentSpeedMagnitude = horizontalVelocity.magnitude;
        float normalizedSpeed = Mathf.Clamp01(currentSpeedMagnitude / Mathf.Max(0.001f, Controller.EffectiveMaxStableMoveSpeed));
        Controller.UpdateAnimationSpeed(normalizedSpeed);
    }
}
}