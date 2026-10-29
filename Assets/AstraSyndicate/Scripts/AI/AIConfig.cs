using UnityEngine;

namespace AstraSyndicate.AI
{
    /// <summary>How an AI pilot flies and fights. Used by enemy and wingman brains.</summary>
    [CreateAssetMenu(menuName = "Astra Syndicate/AI/Pilot Config", fileName = "AIConfig")]
    public sealed class AIConfig : ScriptableObject
    {
        [Header("Awareness")]
        [Tooltip("Hostiles closer than this are noticed, in meters.")]
        [SerializeField, Min(10f)] private float detectionRange = 750f;
        [Tooltip("Seconds between target re-evaluations.")]
        [SerializeField, Min(0.05f)] private float retargetInterval = 0.5f;

        [Header("Attack")]
        [Tooltip("Fire when the aim point is within this many degrees of the nose.")]
        [SerializeField, Range(1f, 45f)] private float fireConeDegrees = 8f;
        [Tooltip("Random aiming error in degrees; higher is easier to survive.")]
        [SerializeField, Range(0f, 15f)] private float aimErrorDegrees = 2.5f;
        [Tooltip("Beyond this distance the ship closes in at full throttle.")]
        [SerializeField, Min(0f)] private float pursuitDistance = 350f;
        [SerializeField, Range(0f, 1f)] private float attackThrottle = 0.65f;
        [Tooltip("Beyond this distance the ship boosts toward its target.")]
        [SerializeField, Min(0f)] private float boostDistance = 600f;
        [Tooltip("Closer than this the ship breaks off its attack run to avoid a collision.")]
        [SerializeField, Min(0f)] private float breakawayDistance = 60f;
        [SerializeField, Min(0f)] private float breakawayDuration = 2.2f;

        [Header("Patrol")]
        [SerializeField, Range(0f, 1f)] private float patrolThrottle = 0.45f;
        [SerializeField, Min(1f)] private float arriveRadius = 40f;
        [Tooltip("Without a patrol route the ship wanders within this radius of where it spawned.")]
        [SerializeField, Min(0f)] private float wanderRadius = 300f;
        [Tooltip("Ships give up a chase and head home when this far from where they spawned.")]
        [SerializeField, Min(0f)] private float leashDistance = 1600f;

        [Header("Retreat")]
        [Tooltip("Retreat when the hull falls to this fraction.")]
        [SerializeField, Range(0f, 1f)] private float retreatBelowHull = 0.3f;
        [Tooltip("Go back on patrol once repaired to this fraction.")]
        [SerializeField, Range(0f, 1f)] private float recoverAtHull = 0.75f;
        [Tooltip("Hull repaired per second while out of danger, as a fraction of max hull.")]
        [SerializeField, Min(0f)] private float repairPerSecond = 0.05f;
        [Tooltip("A retreating ship feels safe when no hostile is closer than this.")]
        [SerializeField, Min(0f)] private float safeDistance = 450f;

        [Header("Wingman")]
        [SerializeField, Min(1f)] private float formationSpacing = 14f;
        [Tooltip("Wingmen engage hostiles that come this close to their leader.")]
        [SerializeField, Min(0f)] private float defendRadius = 700f;
        [Tooltip("After a form-up order, wingmen ignore threats for this many seconds.")]
        [SerializeField, Min(0f)] private float regroupSeconds = 6f;

        [Header("Obstacle avoidance")]
        [SerializeField, Min(1f)] private float avoidanceLookAhead = 80f;
        [SerializeField, Min(0.1f)] private float avoidanceRadius = 4f;

        public float DetectionRange => detectionRange;

        public float RetargetInterval => retargetInterval;

        public float FireConeDegrees => fireConeDegrees;

        public float AimErrorDegrees => aimErrorDegrees;

        public float PursuitDistance => pursuitDistance;

        public float AttackThrottle => attackThrottle;

        public float BoostDistance => boostDistance;

        public float BreakawayDistance => breakawayDistance;

        public float BreakawayDuration => breakawayDuration;

        public float PatrolThrottle => patrolThrottle;

        public float ArriveRadius => arriveRadius;

        public float WanderRadius => wanderRadius;

        public float RepairPerSecond => repairPerSecond;

        public float SafeDistance => safeDistance;

        public float FormationSpacing => formationSpacing;

        public float DefendRadius => defendRadius;

        public float RegroupSeconds => regroupSeconds;

        public float AvoidanceLookAhead => avoidanceLookAhead;

        public float AvoidanceRadius => avoidanceRadius;

        public EnemyRules Rules => new EnemyRules(retreatBelowHull, recoverAtHull, leashDistance);
    }
}
