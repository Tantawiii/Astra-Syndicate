using System;
using AstraSyndicate.Combat;
using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.PlayerControl
{
    /// <summary>
    /// Flies the player's flagship with mouse aim: the mouse (or right stick) moves an aim direction and the ship
    /// turns to follow it within its handling limits. Also handles throttle, boost, firing, lock-on and fleet
    /// orders, and reports the flagship's loss as PlayerDefeatedEvent.
    /// </summary>
    [RequireComponent(typeof(Ship), typeof(ShipMotor))]
    public sealed class PlayerFlightController : MonoBehaviour
    {
        // How quickly the aim rolls back to level, per second.
        private const float AimLeveling = 2f;

        [Header("Aim")]
        [Tooltip("Degrees of aim per pixel of mouse movement.")]
        [SerializeField, Min(0.001f)] private float mouseSensitivity = 0.08f;
        [Tooltip("Degrees per second at full stick deflection; also used by the A/D turn keys.")]
        [SerializeField, Min(1f)] private float stickAimRate = 110f;
        [SerializeField] private bool invertPitch;
        [Tooltip("How far the aim may lead the ship's nose, in degrees.")]
        [SerializeField, Range(10f, 120f)] private float maxAimLead = 65f;
        [Tooltip("Distance of the aim point when no target is locked; guns converge there.")]
        [SerializeField, Min(10f)] private float aimDistance = 450f;

        [Header("Throttle")]
        [SerializeField, Range(0f, 1f)] private float cruiseThrottle = 0.55f;
        [SerializeField, Range(0f, 1f)] private float slowThrottle = 0.1f;

        [Header("Targeting")]
        [SerializeField, Min(10f)] private float targetingRange = 1500f;
        [Tooltip("Guns aim at the lead point when it is within this many degrees of the crosshair.")]
        [SerializeField, Range(0f, 30f)] private float leadAssistDegrees = 6f;

        private Ship _ship;
        private ShipMotor _motor;
        private WeaponSystem[] _weapons = Array.Empty<WeaponSystem>();
        private FlightControls _controls;
        private IEventBus _events;
        private Quaternion _aim;
        private bool _inputEnabled = true;

        /// <summary>Raised when the player gives a fleet order (for HUD feedback).</summary>
        public event Action<FleetOrder, Ship> OrderIssued;

        public Ship Ship => _ship;

        public ShipMotor Motor => _motor;

        public Quaternion AimRotation => _aim;

        public Vector3 AimDirection => _aim * Vector3.forward;

        /// <summary>World point under the crosshair; guns converge here.</summary>
        public Vector3 AimPoint => transform.position + AimDirection * ConvergenceDistance;

        public Ship Target { get; private set; }

        /// <summary>Where to shoot to hit the locked target, if it can be hit.</summary>
        public Vector3? LeadPoint { get; private set; }

        /// <summary>When false (pause menus, briefings) flight input is ignored and the cursor is released.</summary>
        public bool InputEnabled
        {
            get => _inputEnabled;
            set
            {
                _inputEnabled = value;
                ApplyCursor(value);
            }
        }

        private float ConvergenceDistance
        {
            get
            {
                if (Target == null) return aimDistance;
                return Mathf.Clamp(Vector3.Distance(transform.position, Target.Position), 50f, targetingRange);
            }
        }

        private void Awake()
        {
            _ship = GetComponent<Ship>();
            _motor = GetComponent<ShipMotor>();
            _weapons = GetComponentsInChildren<WeaponSystem>();
            _controls = new FlightControls();
            _aim = transform.rotation;
            Services.TryGet(out _events);
            _ship.Destroyed += OnShipDestroyed;
        }

        private void OnEnable()
        {
            _controls.Enable();
        }

        private void OnDisable()
        {
            _controls.Disable();
            ApplyCursor(false);
        }

        private void OnDestroy()
        {
            _ship.Destroyed -= OnShipDestroyed;
            _controls.Dispose();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            if (Target != null && !Target.IsAlive) Target = null;

            if (_inputEnabled)
            {
                bool freeCursor = _controls.FreeCursorHeld;
                ApplyCursor(!freeCursor);
                if (!freeCursor) UpdateAim(deltaTime);
                UpdateThrottle();
                if (_controls.TargetPressed) Target = TargetSelection.SelectByAim(_ship.Faction, transform.position, AimDirection, targetingRange, Target);
                UpdateOrders();
            }

            _motor.SetHeading(AimDirection);
            UpdateLeadPoint();
            if (_inputEnabled && _controls.Fire) Fire();
        }

        private void UpdateAim(float deltaTime)
        {
            Vector2 pointer = _controls.AimPointerDelta * mouseSensitivity;
            Vector2 stick = _controls.AimStick * (stickAimRate * deltaTime);
            float yaw = pointer.x + stick.x + _controls.Turn * stickAimRate * deltaTime;
            float pitch = (pointer.y + stick.y) * (invertPitch ? 1f : -1f);

            _aim = FlightMath.LevelRoll(_aim * Quaternion.Euler(pitch, yaw, 0f), AimLeveling * deltaTime);

            // Keep the aim close enough to the nose for the ship to follow it.
            Vector3 aimForward = _aim * Vector3.forward;
            if (Vector3.Angle(transform.forward, aimForward) > maxAimLead)
            {
                Vector3 clamped = Vector3.RotateTowards(transform.forward, aimForward, maxAimLead * Mathf.Deg2Rad, 0f);
                _aim = Quaternion.LookRotation(clamped, _aim * Vector3.up);
            }
        }

        private void UpdateThrottle()
        {
            float input = _controls.Throttle;
            float throttle = input >= 0f
                ? Mathf.Lerp(cruiseThrottle, 1f, input)
                : Mathf.Lerp(cruiseThrottle, slowThrottle, -input);
            _motor.SetThrottle(throttle);
            _motor.SetBoost(_controls.Boost);
        }

        private void UpdateOrders()
        {
            if (_controls.OrderAttackPressed && Target != null) IssueOrder(FleetOrder.AttackTarget, Target);
            if (_controls.OrderFormUpPressed) IssueOrder(FleetOrder.FormUp, null);
        }

        private void UpdateLeadPoint()
        {
            LeadPoint = null;
            if (Target == null || _weapons.Length == 0) return;

            if (Ballistics.TryPredictIntercept(transform.position, _motor.Velocity, _weapons[0].ProjectileSpeed, Target.Position, Target.Velocity, out Vector3 lead))
            {
                LeadPoint = lead;
            }
        }

        private void Fire()
        {
            Vector3 aimPoint = AimPoint;
            if (LeadPoint.HasValue && Vector3.Angle(AimDirection, LeadPoint.Value - transform.position) <= leadAssistDegrees)
            {
                aimPoint = LeadPoint.Value;
            }
            foreach (WeaponSystem weapon in _weapons) weapon.TryFire(aimPoint);
        }

        private void IssueOrder(FleetOrder order, Ship target)
        {
            _events?.Publish(new FleetOrderIssuedEvent(_ship.Faction, order, target));
            OrderIssued?.Invoke(order, target);
        }

        private void OnShipDestroyed(Ship ship, DamageInfo damage)
        {
            ApplyCursor(false);
            _events?.Publish(new PlayerDefeatedEvent("Flagship destroyed"));
        }

        private static void ApplyCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
