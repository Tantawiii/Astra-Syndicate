using UnityEngine;

namespace AstraSyndicate.Combat
{
    public static class Ballistics
    {
        /// <summary>
        /// Where to aim so a projectile fired now (inheriting the shooter's velocity) meets a target moving
        /// at constant velocity. Returns false, with the target's current position, when no intercept exists.
        /// </summary>
        public static bool TryPredictIntercept(
            Vector3 shooterPosition,
            Vector3 shooterVelocity,
            float projectileSpeed,
            Vector3 targetPosition,
            Vector3 targetVelocity,
            out Vector3 aimPoint)
        {
            aimPoint = targetPosition;
            Vector3 relativePosition = targetPosition - shooterPosition;
            Vector3 relativeVelocity = targetVelocity - shooterVelocity;

            // Solve |relativePosition + relativeVelocity * t| = projectileSpeed * t for the earliest t > 0.
            float a = Vector3.Dot(relativeVelocity, relativeVelocity) - projectileSpeed * projectileSpeed;
            float b = 2f * Vector3.Dot(relativePosition, relativeVelocity);
            float c = Vector3.Dot(relativePosition, relativePosition);

            float time;
            if (Mathf.Abs(a) < 1e-4f)
            {
                if (Mathf.Abs(b) < 1e-6f) return false;
                time = -c / b;
            }
            else
            {
                float discriminant = b * b - 4f * a * c;
                if (discriminant < 0f) return false;

                float root = Mathf.Sqrt(discriminant);
                time = EarliestPositive((-b - root) / (2f * a), (-b + root) / (2f * a));
            }

            if (time <= 0f) return false;
            aimPoint = targetPosition + relativeVelocity * time;
            return true;
        }

        private static float EarliestPositive(float first, float second)
        {
            if (first > 0f && second > 0f) return Mathf.Min(first, second);
            return Mathf.Max(first, second);
        }
    }
}
