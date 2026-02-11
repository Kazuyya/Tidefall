using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

namespace LittleHeroJourney
{
    public static class MovementHelper
    {
        #region Ground Following

        public static Vector3 CalculateGroundPosition(Vector3 horizontalPosition, KinematicCharacterMotor motor, float rayDistance = 3f)
        {
            RaycastHit groundHit;
            Vector3 rayStart = horizontalPosition + (motor.CharacterUp * 1f);

            if (Physics.Raycast(rayStart, -motor.CharacterUp, out groundHit, rayDistance, motor.CollidableLayers, QueryTriggerInteraction.Ignore))
            {
                float normalY = Vector3.Dot(groundHit.normal, motor.CharacterUp);
                if (normalY > 0.7f)
                {
                    return new Vector3(horizontalPosition.x, groundHit.point.y, horizontalPosition.z);
                }
            }

            return horizontalPosition;
        }

        public static void ApplyGroundFollowing(Vector3 targetPosition, KinematicCharacterMotor motor,
            float minSmoothFactor, float maxSmoothFactor, float smoothScaleThreshold)
        {
            float heightDifference = Mathf.Abs(motor.TransientPosition.y - targetPosition.y);

            if (heightDifference > 0.01f)
            {
                float clampedThreshold = Mathf.Max(smoothScaleThreshold, 0.1f);

                float smoothFactor = Mathf.Lerp(minSmoothFactor, maxSmoothFactor,
                    Mathf.Clamp01(heightDifference / clampedThreshold));

                float maxLerpFactor = 20f;
                float lerpFactor = Mathf.Min(smoothFactor * Time.deltaTime, maxLerpFactor * Time.deltaTime);

                Vector3 smoothedPosition = Vector3.Lerp(
                    motor.TransientPosition,
                    targetPosition,
                    lerpFactor
                );

                motor.SetPosition(smoothedPosition);
            }
        }

        #endregion

        #region Dash Calculations

        public static float CalculateDashProgress(float timeRemaining, float dashDuration)
        {
            return 1f - (timeRemaining / dashDuration);
        }

        public static Vector3 CalculateHorizontalDashPosition(float progress, Vector3 startPosition, Vector3 targetPosition)
        {
            Vector3 dashDirection = (targetPosition - startPosition).normalized;
            float totalDistance = Vector3.Distance(startPosition, targetPosition);
            float currentDistance = progress * totalDistance;

            return startPosition + (dashDirection * currentDistance);
        }

        public static void KeepPlayerGroundedDuringDash(PlayerMovementController controller)
        {
            if (!controller.dashSettings.EnableGroundFollowing)
                return;

            float totalDistance = controller.DashDistanceRemaining + Vector3.Distance(controller.Motor.TransientPosition, controller.DashStartPosition);
            float currentDistance = Vector3.Distance(controller.Motor.TransientPosition, controller.DashStartPosition);
            float dashProgress = Mathf.Clamp01(currentDistance / totalDistance);

            Vector3 horizontalTarget = CalculateHorizontalDashPosition(dashProgress, controller.DashStartPosition, controller.DashTargetPosition);
            Vector3 targetPosition = CalculateGroundPosition(horizontalTarget, controller.Motor);

            ApplyGroundFollowing(targetPosition, controller.Motor,
                controller.dashSettings.MinGroundSmoothFactor, controller.dashSettings.MaxGroundSmoothFactor, controller.dashSettings.GroundSmoothScaleThreshold);
        }

        public static Vector3 CalculateGroundFollowingTarget(Vector3 startPosition, Vector3 dashDirection, float maxDistance, KinematicCharacterMotor motor)
        {
            const int sampleCount = 3;
            Vector3 targetPosition = startPosition;

            for (int i = 1; i <= sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                Vector3 samplePoint = startPosition + (dashDirection * maxDistance * t);

                RaycastHit groundHit;
                Vector3 rayStart = samplePoint + (motor.CharacterUp * 2f);

                if (Physics.Raycast(rayStart, -motor.CharacterUp, out groundHit, 5f, motor.CollidableLayers, QueryTriggerInteraction.Ignore))
                {
                    targetPosition = new Vector3(samplePoint.x, groundHit.point.y, samplePoint.z);

                    float normalY = Vector3.Dot(groundHit.normal, motor.CharacterUp);
                    if (normalY < 0.7f)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            float horizontalDistance = Vector3.Distance(
                new Vector3(startPosition.x, 0, startPosition.z),
                new Vector3(targetPosition.x, 0, targetPosition.z)
            );

            if (horizontalDistance < 0.5f)
            {
                targetPosition = startPosition + (dashDirection * 0.5f);
            }

            return targetPosition;
        }

        public static float CheckDashObstacle(Vector3 dashDirection, float maxDistance, KinematicCharacterMotor motor, bool showDebugLog, string className)
        {
            Vector3 rayStart = motor.TransientPosition + (motor.CharacterUp * motor.Capsule.height * 0.5f);

            RaycastHit hit;
            if (Physics.Raycast(rayStart, dashDirection, out hit, maxDistance, motor.CollidableLayers, QueryTriggerInteraction.Ignore))
            {
                float normalY = Vector3.Dot(hit.normal, motor.CharacterUp);
                bool isWalkableSurface = normalY > 0.7f;

                if (!isWalkableSurface)
                {
                    float resultDistance = Mathf.Max(0.1f, hit.distance - 0.5f);
                    return resultDistance;
                }
            }

            return maxDistance;
        }

        #endregion
    }
}