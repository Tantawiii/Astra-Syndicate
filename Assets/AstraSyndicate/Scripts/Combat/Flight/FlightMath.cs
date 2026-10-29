using UnityEngine;

namespace AstraSyndicate.Combat
{
    /// <summary>Steering maths shared by the ship motor and the player's aim. Pure functions, unit-tested.</summary>
    public static class FlightMath
    {
        // Leveling is skipped near vertical, where "level" is undefined.
        private const float VerticalLimit = 0.95f;

        /// <summary>
        /// Local angular velocity (rad/s) that turns the nose toward <paramref name="desiredDirection"/>:
        /// x = pitch, y = yaw, z = 0. Rates are proportional to the angle error and clamped to the limits.
        /// </summary>
        public static Vector3 SteeringRates(Quaternion rotation, Vector3 desiredDirection, float maxPitchRate, float maxYawRate, float gain)
        {
            if (desiredDirection.sqrMagnitude < 1e-8f) return Vector3.zero;

            Vector3 local = Quaternion.Inverse(rotation) * desiredDirection.normalized;
            float yawError = Mathf.Atan2(local.x, local.z);
            // Positive rotation about +X pitches the nose down, so a target above needs a negative rate.
            float pitchError = -Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude);
            return new Vector3(
                Mathf.Clamp(pitchError * gain, -maxPitchRate, maxPitchRate),
                Mathf.Clamp(yawError * gain, -maxYawRate, maxYawRate),
                0f);
        }

        /// <summary>
        /// Signed roll (radians, around the nose) from the current up vector to "level": world up projected
        /// onto the plane the ship faces. Zero when the nose points almost straight up or down.
        /// </summary>
        public static float RollError(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > VerticalLimit) return 0f;

            Vector3 levelUp = Vector3.ProjectOnPlane(Vector3.up, forward).normalized;
            return Vector3.SignedAngle(rotation * Vector3.up, levelUp, forward) * Mathf.Deg2Rad;
        }

        /// <summary>Roll rate (rad/s around the nose) that brings the ship back toward level.</summary>
        public static float LevelingRollRate(Quaternion rotation, float strength)
        {
            return RollError(rotation) * strength;
        }

        /// <summary>Returns <paramref name="rotation"/> rolled the fraction <paramref name="t"/> of the way to level.</summary>
        public static Quaternion LevelRoll(Quaternion rotation, float t)
        {
            float error = RollError(rotation);
            if (error == 0f) return rotation;

            Vector3 forward = rotation * Vector3.forward;
            return Quaternion.AngleAxis(error * Mathf.Clamp01(t) * Mathf.Rad2Deg, forward) * rotation;
        }
    }
}
