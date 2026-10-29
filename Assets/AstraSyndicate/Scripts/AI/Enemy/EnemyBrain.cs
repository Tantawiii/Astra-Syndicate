using System;
using AstraSyndicate.Combat;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AstraSyndicate.AI
{
    /// <summary>
    /// Enemy pilot with three behaviours: patrol (route or wander), attack (dogfight the nearest hostile) and
    /// retreat (flee when badly damaged, repair once safe, then patrol again). Transitions come from
    /// <see cref="EnemyDecision"/>.
    /// </summary>
    [RequireComponent(typeof(Ship), typeof(ShipMotor))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        // Keep chasing a target this far beyond detection range so ships don't flip-flop at the edge.
        private const float TargetKeepFactor = 1.5f;
        // Retreating ships drift back toward home so they don't flee out of the arena.
        private const float HomeBias = 0.35f;

        [SerializeField] private AIConfig config;

        [Tooltip("Optional route to fly while patrolling. Without one the ship wanders around where it spawned.")]
        [SerializeField] private PatrolRoute patrolRoute;

        private Ship _ship;
        private ShipMotor _motor;
        private AttackManeuver _attack;
        private Vector3 _home;
        private Vector3 _patrolPoint;
        private bool _hasPatrolPoint;
        private int _routeIndex;
        private Ship _target;
        private float _retargetTimer;

        public event Action<EnemyBrain, EnemyState> StateChanged;

        public EnemyState State { get; private set; } = EnemyState.Patrol;

        public Ship Target => _target;

        private AttackManeuver Attack => _attack ??= new AttackManeuver(transform, _motor, GetComponentInChildren<WeaponSystem>(), config);

        public void Configure(AIConfig aiConfig)
        {
            config = aiConfig;
            _attack = null;
        }

        public void SetPatrolRoute(PatrolRoute route)
        {
            patrolRoute = route;
            _routeIndex = route != null && route.Count > 0 ? route.NearestIndex(transform.position) : 0;
            _hasPatrolPoint = false;
        }

        private void Awake()
        {
            _ship = GetComponent<Ship>();
            _motor = GetComponent<ShipMotor>();
            _home = transform.position;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (config == null || deltaTime <= 0f || !_ship.IsAlive) return;

            UpdateTarget(deltaTime);
            var situation = new EnemySituation(State, _ship.HullFraction, _target != null, Vector3.Distance(transform.position, _home));
            EnemyState next = EnemyDecision.Next(situation, config.Rules);
            if (next != State) ChangeState(next);

            switch (State)
            {
                case EnemyState.Patrol:
                    Patrol();
                    break;
                case EnemyState.Attack:
                    Attack.Tick(_target, deltaTime);
                    break;
                case EnemyState.Retreat:
                    Retreat(deltaTime);
                    break;
            }
        }

        private void UpdateTarget(float deltaTime)
        {
            if (_target != null && !_target.IsAlive) _target = null;

            _retargetTimer -= deltaTime;
            if (_retargetTimer > 0f) return;
            _retargetTimer = config.RetargetInterval;

            float keepRange = config.DetectionRange * TargetKeepFactor;
            if (_target != null && (_target.Position - transform.position).sqrMagnitude <= keepRange * keepRange) return;
            _target = ShipRegistry.FindNearestHostile(_ship.Faction, transform.position, config.DetectionRange);
        }

        private void ChangeState(EnemyState next)
        {
            State = next;
            _hasPatrolPoint = false;
            Attack.Reset();
            StateChanged?.Invoke(this, next);
        }

        private void Patrol()
        {
            if (!_hasPatrolPoint || (transform.position - _patrolPoint).sqrMagnitude < config.ArriveRadius * config.ArriveRadius)
            {
                PickPatrolPoint();
            }
            FlyTowards(_patrolPoint, config.PatrolThrottle, boost: false);
        }

        private void PickPatrolPoint()
        {
            if (patrolRoute != null && patrolRoute.Count > 0)
            {
                _patrolPoint = patrolRoute.GetPoint(_routeIndex);
                _routeIndex = (_routeIndex + 1) % patrolRoute.Count;
            }
            else
            {
                _patrolPoint = _home + Random.insideUnitSphere * config.WanderRadius;
            }
            _hasPatrolPoint = true;
        }

        private void Retreat(float deltaTime)
        {
            Vector3 position = transform.position;
            Ship threat = ShipRegistry.FindNearestHostile(_ship.Faction, position, config.SafeDistance);
            if (threat != null)
            {
                Vector3 away = (position - threat.Position).normalized;
                Vector3 towardHome = (_home - position).normalized;
                FlyTowards(position + away + towardHome * HomeBias, 1f, boost: true);
                return;
            }

            FlyTowards(_home, config.PatrolThrottle, boost: false);
            _ship.Repair(config.RepairPerSecond * _ship.MaxHull * deltaTime);
        }

        private void FlyTowards(Vector3 point, float throttle, bool boost)
        {
            _motor.SetHeading(ObstacleAvoidance.Adjust(transform, point - transform.position, config));
            _motor.SetThrottle(throttle);
            _motor.SetBoost(boost);
        }
    }
}
