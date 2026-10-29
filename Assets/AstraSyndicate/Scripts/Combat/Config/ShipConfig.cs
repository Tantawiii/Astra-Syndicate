using UnityEngine;

namespace AstraSyndicate.Combat
{
    /// <summary>Hull and handling of a ship class. Shared by every ship of that class.</summary>
    [CreateAssetMenu(menuName = "Astra Syndicate/Combat/Ship Config", fileName = "ShipConfig")]
    public sealed class ShipConfig : ScriptableObject
    {
        [Header("Hull")]
        [SerializeField, Min(1f)] private float maxHull = 100f;

        [Header("Speed (m/s)")]
        [SerializeField, Min(0f)] private float maxSpeed = 60f;
        [SerializeField, Min(0f)] private float boostSpeed = 100f;
        [Tooltip("Forward acceleration and braking, in m/s².")]
        [SerializeField, Min(0.1f)] private float acceleration = 45f;
        [Tooltip("Seconds of boost from a full charge.")]
        [SerializeField, Min(0f)] private float boostDuration = 3f;
        [Tooltip("Seconds to recharge boost from empty.")]
        [SerializeField, Min(0.1f)] private float boostRechargeTime = 5f;

        [Header("Handling")]
        [Tooltip("Maximum pitch rate, degrees per second.")]
        [SerializeField, Min(1f)] private float pitchRate = 80f;
        [Tooltip("Maximum yaw rate, degrees per second.")]
        [SerializeField, Min(1f)] private float yawRate = 75f;
        [Tooltip("How quickly the ship reaches the requested turn rate (1/s). Higher feels snappier.")]
        [SerializeField, Min(0.1f)] private float turnResponse = 7f;
        [Tooltip("How quickly sideways drift is cancelled (1/s). Higher gives tighter, more arcade turns.")]
        [SerializeField, Min(0f)] private float lateralGrip = 3f;
        [Tooltip("How strongly the ship rolls back to level (1/s).")]
        [SerializeField, Min(0f)] private float autoLevel = 3f;
        [Tooltip("Cosmetic bank of the model in full-rate turns, degrees.")]
        [SerializeField, Range(0f, 80f)] private float bankAngle = 35f;

        [Header("Collisions")]
        [Tooltip("Impacts slower than this (m/s) do no damage.")]
        [SerializeField, Min(0f)] private float collisionDamageThreshold = 8f;
        [SerializeField, Min(0f)] private float collisionDamagePerSpeed = 0.8f;

        [Header("Effects")]
        [SerializeField] private PooledEffect explosionEffect;
        [SerializeField, Min(0.1f)] private float explosionScale = 1f;

        public float MaxHull => maxHull;

        public float MaxSpeed => maxSpeed;

        public float BoostSpeed => boostSpeed;

        public float Acceleration => acceleration;

        public float BoostDuration => boostDuration;

        public float BoostRechargeTime => boostRechargeTime;

        public float PitchRate => pitchRate;

        public float YawRate => yawRate;

        public float TurnResponse => turnResponse;

        public float LateralGrip => lateralGrip;

        public float AutoLevel => autoLevel;

        public float BankAngle => bankAngle;

        public float CollisionDamageThreshold => collisionDamageThreshold;

        public float CollisionDamagePerSpeed => collisionDamagePerSpeed;

        public PooledEffect ExplosionEffect => explosionEffect;

        public float ExplosionScale => explosionScale;
    }
}
