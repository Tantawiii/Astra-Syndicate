using UnityEngine;

namespace AstraSyndicate.AI
{
    internal static class ObstacleAvoidance
    {
        /// <summary>
        /// Bends <paramref name="desiredHeading"/> away from scenery straight ahead (colliders without a
        /// Rigidbody, such as asteroids). Other ships are left to the attack run's break-off logic.
        /// </summary>
        public static Vector3 Adjust(Transform self, Vector3 desiredHeading, AIConfig config)
        {
            Vector3 forward = self.forward;
            bool blocked = Physics.SphereCast(
                self.position, config.AvoidanceRadius, forward, out RaycastHit hit,
                config.AvoidanceLookAhead, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (!blocked || hit.rigidbody != null) return desiredHeading;

            Vector3 away = Vector3.ProjectOnPlane(hit.normal, forward);
            if (away.sqrMagnitude < 1e-4f) away = self.up;
            float urgency = 1f - hit.distance / config.AvoidanceLookAhead;
            return desiredHeading.normalized + away.normalized * (1f + 3f * urgency);
        }
    }
}
