using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

namespace LittleHeroJourney
{
    public class DashingMovementState : PlayerMovementState
    {
        private float _groundFollowingYVelocity = 0f;
        private float _groundCheckTimer = 0f;
        private const float GROUND_CHECK_INTERVAL = 0.1f;
        private Vector3 _lastGroundPosition = Vector3.zero;

        public DashingMovementState(PlayerMovementController controller) : base(controller) { }

    public override void OnStateEnter(PlayerState fromState)
    {
        _groundFollowingYVelocity = 0f;
        
        Controller.DashTargetPosition = Vector3.zero;

        Controller.DashDirection = Motor.CharacterForward;
        Controller.DashMaxDistance = Controller.CheckDashObstacle(Controller.DashDirection, Controller.dashSettings.DashDistance);
        Controller.DashStartPosition = Motor.TransientPosition;
        Controller.DashTargetPosition = Controller.CalculateGroundFollowingTarget(Controller.DashStartPosition, Controller.DashDirection, Controller.DashMaxDistance);

        float dashDistance = Vector3.Distance(Controller.DashStartPosition, Controller.DashTargetPosition);
        Controller.DashTimeRemaining = dashDistance / Controller.dashSettings.DashSpeed;
        Controller.DashDistanceRemaining = dashDistance;

        Motor.ForceUnground();
        Controller.DashVelocity = Controller.DashDirection * Controller.dashSettings.DashSpeed;
        Controller.CurrentDashSpeed = 0f;

        if (Controller.EyeframeManager != null)
        {
            Controller.EyeframeManager.SetEyeframe(true);
        }

        if (Controller.dashSettings.ShowDebugLog)
        {
            Debug.Log($"[{Controller.GetType().Name}] Dash started - Distance: {dashDistance:F1}m");
        }
    }

    public override void OnStateExit(PlayerState toState)
    {
        Controller.DashCooldownTimer = Controller.dashSettings.DashCooldown;
        Controller.DashVelocity = Vector3.zero;
        Controller.CurrentDashSpeed = 0f;
        _groundFollowingYVelocity = 0f;
        
        if (Controller.EyeframeManager != null)
        {
            Controller.EyeframeManager.SetEyeframe(false);
        }
        
        if (Controller.dashSettings.ShowDebugLog)
        {
            Debug.Log($"[{Controller.GetType().Name}] Dash ended");
        }
    }

    public override void SetInputs(ref PlayerMovementInputs inputs)
    {
        // During dash, ignore movement input
        Controller.MoveInputVector = Vector3.zero;
    }

    public override void BeforeCharacterUpdate(float deltaTime)
    {
        Controller.DashTimeRemaining -= deltaTime;

        float sqrDistanceToTarget = (Motor.TransientPosition - Controller.DashTargetPosition).sqrMagnitude;
        if (sqrDistanceToTarget < 0.01f || Controller.DashTimeRemaining <= 0f)
        {
            Controller.TransitionToState(PlayerState.Default);
        }
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        Controller.CurrentDashSpeed = Mathf.Lerp(Controller.CurrentDashSpeed, Controller.dashSettings.DashSpeed, 1f - Mathf.Exp(-Controller.dashSettings.DashAccelerationSharpness * deltaTime));
        
        float targetYVelocity = CalculateGroundFollowingVelocity(deltaTime);
        
        currentVelocity = Controller.DashDirection * Controller.CurrentDashSpeed;
        currentVelocity.y = targetYVelocity;

        Controller.UpdateAnimationSpeed(1f);
    }

    private float CalculateGroundFollowingVelocity(float deltaTime)
    {
        _groundCheckTimer += deltaTime;
        if (_groundCheckTimer < GROUND_CHECK_INTERVAL)
        {
            return _groundFollowingYVelocity;
        }
        _groundCheckTimer = 0f;

        Vector3 rayStart = Motor.TransientPosition + (Motor.CharacterUp * 0.5f);
        RaycastHit groundHit;

        if (Physics.Raycast(rayStart, -Motor.CharacterUp, out groundHit, 5f, Motor.CollidableLayers, QueryTriggerInteraction.Ignore))
        {
            float normalY = Vector3.Dot(groundHit.normal, Motor.CharacterUp);
            if (normalY > 0.6f)
            {
                float targetY = groundHit.point.y;
                float currentY = Motor.TransientPosition.y;
                float heightDifference = targetY - currentY;

                if (Mathf.Abs(heightDifference) > 0.02f)
                {
                    float targetVelocity = heightDifference / deltaTime;
                    
                    float maxVelocity = 15f;
                    targetVelocity = Mathf.Clamp(targetVelocity, -maxVelocity, maxVelocity);
                    
                    _groundFollowingYVelocity = Mathf.Lerp(_groundFollowingYVelocity, targetVelocity, 0.3f);
                    
                    return _groundFollowingYVelocity;
                }
                else
                {
                    _groundFollowingYVelocity = Mathf.Lerp(_groundFollowingYVelocity, 0f, 0.5f);
                    return _groundFollowingYVelocity;
                }
            }
        }

        _groundFollowingYVelocity = Mathf.Lerp(_groundFollowingYVelocity, -5f, 0.1f);
        return _groundFollowingYVelocity;
    }
}
}