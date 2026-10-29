using AstraSyndicate.Combat;
using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.AI
{
    /// <summary>
    /// Allied wingman: holds a formation slot on its leader, defends the leader against nearby hostiles and
    /// follows fleet orders (attack the commander's target, form up).
    /// </summary>
    [RequireComponent(typeof(Ship), typeof(ShipMotor))]
    public sealed class WingmanBrain : MonoBehaviour
    {
        // Beyond this distance a wingman flies straight to its slot instead of formating on the leader.
        private const float RejoinDistance = 60f;
        private const float RejoinBoostDistance = 150f;
        // How far ahead of the leader (meters) wingmen aim while in formation, to fly parallel to it.
        private const float FormationLookAhead = 40f;
        // Speed correction (m/s) per meter the slot is ahead of or behind the wingman.
        private const float CatchUpGain = 0.8f;
        // Auto-engaged targets are dropped once they get this much further than the defend radius from the leader.
        private const float PursuitLimitFactor = 1.5f;

        [SerializeField] private AIConfig config;
        [SerializeField] private Ship leader;
        [Tooltip("Position in the V formation: 0 = first on the left, 1 = first on the right, 2 = second on the left...")]
        [SerializeField, Min(0)] private int formationSlot;

        private Ship _ship;
        private ShipMotor _motor;
        private AttackManeuver _attack;
        private IEventBus _events;
        private Ship _target;
        private bool _targetOrdered;
        private float _regroupTimer;
        private float _retargetTimer;

        public Ship Leader => leader;

        public Ship Target => _target;

        public bool IsEngaging => _target != null;

        private bool HasLeader => leader != null && leader.IsAlive;

        private AttackManeuver Attack => _attack ??= new AttackManeuver(transform, _motor, GetComponentInChildren<WeaponSystem>(), config);

        public void Configure(AIConfig aiConfig, Ship leaderShip, int slot)
        {
            config = aiConfig;
            _attack = null;
            SetLeader(leaderShip, slot);
        }

        public void SetLeader(Ship leaderShip, int slot)
        {
            leader = leaderShip;
            formationSlot = Mathf.Max(0, slot);
        }

        private void Awake()
        {
            _ship = GetComponent<Ship>();
            _motor = GetComponent<ShipMotor>();
            if (Services.TryGet(out _events)) _events.Subscribe<FleetOrderIssuedEvent>(OnFleetOrder);
        }

        private void OnDestroy()
        {
            _events?.Unsubscribe<FleetOrderIssuedEvent>(OnFleetOrder);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (config == null || deltaTime <= 0f || !_ship.IsAlive) return;

            _regroupTimer -= deltaTime;
            if (_target != null && (!_target.IsAlive || ShouldDropTarget())) ClearTarget();
            if (_target == null && _regroupTimer <= 0f) LookForThreat(deltaTime);

            if (_target != null) Attack.Tick(_target, deltaTime);
            else HoldFormation();
        }

        private bool ShouldDropTarget()
        {
            if (_targetOrdered || !HasLeader) return false;

            float limit = config.DefendRadius * PursuitLimitFactor;
            return (_target.Position - leader.Position).sqrMagnitude > limit * limit;
        }

        private void LookForThreat(float deltaTime)
        {
            _retargetTimer -= deltaTime;
            if (_retargetTimer > 0f) return;
            _retargetTimer = config.RetargetInterval;

            Vector3 anchor = HasLeader ? leader.Position : transform.position;
            Ship threat = ShipRegistry.FindNearestHostile(_ship.Faction, anchor, config.DefendRadius);
            if (threat != null) Engage(threat, ordered: false);
        }

        private void HoldFormation()
        {
            if (!HasLeader)
            {
                _motor.SetThrottle(config.PatrolThrottle);
                _motor.SetBoost(false);
                return;
            }

            Transform leaderTransform = leader.transform;
            Vector3 slot = leader.Position + leaderTransform.rotation * Formation.SlotOffset(formationSlot, config.FormationSpacing);
            Vector3 toSlot = slot - transform.position;
            float distance = toSlot.magnitude;
            Vector3 leaderForward = leaderTransform.forward;

            Vector3 heading = distance > RejoinDistance ? toSlot : leaderForward * FormationLookAhead + toSlot;
            _motor.SetHeading(ObstacleAvoidance.Adjust(transform, heading, config));

            float leaderSpeed = Vector3.Dot(leader.Velocity, leaderForward);
            float catchUp = Vector3.Dot(toSlot, leaderForward) * CatchUpGain;
            float maxSpeed = Mathf.Max(1f, _motor.MaxSpeed);
            _motor.SetThrottle((leaderSpeed + catchUp) / maxSpeed);
            _motor.SetBoost(distance > RejoinBoostDistance);
        }

        private void OnFleetOrder(FleetOrderIssuedEvent order)
        {
            if (_ship == null || order.Faction != _ship.Faction) return;

            switch (order.Order)
            {
                case FleetOrder.AttackTarget:
                    if (order.Target != null && order.Target.IsAlive) Engage(order.Target, ordered: true);
                    break;
                case FleetOrder.FormUp:
                    ClearTarget();
                    _regroupTimer = config != null ? config.RegroupSeconds : 0f;
                    break;
            }
        }

        private void Engage(Ship target, bool ordered)
        {
            _target = target;
            _targetOrdered = ordered;
            Attack.Reset();
        }

        private void ClearTarget()
        {
            _target = null;
            _targetOrdered = false;
        }
    }
}
