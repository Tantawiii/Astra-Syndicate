using UnityEngine;

namespace AstraSyndicate.Combat
{
    /// <summary>
    /// Physics-based arcade flight. Controllers (player or AI) only set a heading, a throttle and boost;
    /// the motor turns the Rigidbody toward the heading within the ship's turn rates, accelerates along the
    /// nose, bleeds off sideways drift and keeps the ship rolled level. The model banks into turns.
    /// </summary>
    [RequireComponent(typeof(Ship), typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class ShipMotor : MonoBehaviour
    {
        // Turn rate (rad/s) requested per radian of heading error.
        private const float SteeringGain = 3f;
        private const float BankSharpness = 6f;

        [Tooltip("Child holding the visible model. It banks into turns without affecting physics.")]
        [SerializeField] private Transform visualRoot;

        private Ship _ship;
        private Rigidbody _body;
        private Vector3 _heading;
        private bool _hasHeading;
        private float _throttle = 0.5f;
        private bool _boostRequested;
        private float _bank;

        /// <summary>Requested speed as a fraction of max speed.</summary>
        public float Throttle => _throttle;

        public bool IsBoosting { get; private set; }

        /// <summary>Remaining boost, 0..1.</summary>
        public float BoostCharge { get; private set; } = 1f;

        public Vector3 Velocity => _body != null ? _body.linearVelocity : Vector3.zero;

        public float Speed => Velocity.magnitude;

        public float MaxSpeed => Config != null ? Config.MaxSpeed : 0f;

        private ShipConfig Config => _ship != null ? _ship.Config : null;

        public void Configure(Transform model)
        {
            visualRoot = model;
        }

        public void SetThrottle(float throttle01)
        {
            _throttle = Mathf.Clamp01(throttle01);
        }

        public void SetBoost(bool boost)
        {
            _boostRequested = boost;
        }

        /// <summary>Direction the nose should turn toward. A zero vector is ignored.</summary>
        public void SetHeading(Vector3 worldDirection)
        {
            if (worldDirection.sqrMagnitude < 1e-6f) return;

            _heading = worldDirection.normalized;
            _hasHeading = true;
        }

        private void Awake()
        {
            _ship = GetComponent<Ship>();
            _body = GetComponent<Rigidbody>();
            _body.useGravity = false;
            _body.linearDamping = 0f;
            _body.angularDamping = 0f;
            _heading = transform.forward;
        }

        private void FixedUpdate()
        {
            ShipConfig config = Config;
            if (config == null || !_ship.IsAlive) return;

            float deltaTime = Time.fixedDeltaTime;
            UpdateBoost(config, deltaTime);
            ApplyThrust(config, deltaTime);
            ApplySteering(config, deltaTime);
        }

        private void LateUpdate()
        {
            UpdateBanking();
        }

        private void UpdateBoost(ShipConfig config, float deltaTime)
        {
            IsBoosting = _boostRequested && BoostCharge > 0f && config.BoostDuration > 0f;
            if (IsBoosting)
            {
                BoostCharge = Mathf.Max(0f, BoostCharge - deltaTime / config.BoostDuration);
            }
            else if (!_boostRequested)
            {
                // Recharging waits for the button to be released, so an empty boost doesn't stutter.
                BoostCharge = Mathf.Min(1f, BoostCharge + deltaTime / config.BoostRechargeTime);
            }
        }

        private void ApplyThrust(ShipConfig config, float deltaTime)
        {
            float targetSpeed = IsBoosting ? config.BoostSpeed : _throttle * config.MaxSpeed;
            Vector3 forward = _body.rotation * Vector3.forward;
            Vector3 velocity = _body.linearVelocity;
            float forwardSpeed = Vector3.Dot(velocity, forward);
            Vector3 drift = velocity - forward * forwardSpeed;

            float maxChange = config.Acceleration * deltaTime;
            float speedChange = Mathf.Clamp(targetSpeed - forwardSpeed, -maxChange, maxChange);
            Vector3 change = forward * speedChange - drift * Mathf.Clamp01(config.LateralGrip * deltaTime);
            _body.AddForce(change, ForceMode.VelocityChange);
        }

        private void ApplySteering(ShipConfig config, float deltaTime)
        {
            Quaternion rotation = _body.rotation;
            Vector3 localRates = _hasHeading
                ? FlightMath.SteeringRates(rotation, _heading, config.PitchRate * Mathf.Deg2Rad, config.YawRate * Mathf.Deg2Rad, SteeringGain)
                : Vector3.zero;
            localRates.z = FlightMath.LevelingRollRate(rotation, config.AutoLevel);

            Vector3 desired = rotation * localRates;
            Vector3 change = (desired - _body.angularVelocity) * Mathf.Clamp01(config.TurnResponse * deltaTime);
            _body.AddTorque(change, ForceMode.VelocityChange);
        }

        private void UpdateBanking()
        {
            ShipConfig config = Config;
            if (visualRoot == null || config == null || _body == null) return;

            float yawRate = transform.InverseTransformDirection(_body.angularVelocity).y;
            float maxYawRate = Mathf.Max(0.01f, config.YawRate * Mathf.Deg2Rad);
            // Bank right (right wing down, negative roll) when turning right.
            float targetBank = -Mathf.Clamp(yawRate / maxYawRate, -1f, 1f) * config.BankAngle;
            _bank = Mathf.Lerp(_bank, targetBank, 1f - Mathf.Exp(-BankSharpness * Time.deltaTime));
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, _bank);
        }
    }
}
