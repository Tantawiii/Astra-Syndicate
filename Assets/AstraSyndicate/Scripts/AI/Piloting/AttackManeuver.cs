using AstraSyndicate.Combat;
using UnityEngine;

namespace AstraSyndicate.AI
{
    /// <summary>
    /// Dogfighting shared by enemies and wingmen: lead the target, fire when it is inside the cone, and break
    /// off before colliding, then come around for another run.
    /// </summary>
    internal sealed class AttackManeuver
    {
        private readonly Transform _self;
        private readonly ShipMotor _motor;
        private readonly WeaponSystem _weapon;
        private readonly AIConfig _config;
        private float _breakawayTime;
        private Vector3 _breakawayDirection;

        public AttackManeuver(Transform self, ShipMotor motor, WeaponSystem weapon, AIConfig config)
        {
            _self = self;
            _motor = motor;
            _weapon = weapon;
            _config = config;
        }

        public void Reset()
        {
            _breakawayTime = 0f;
        }

        public void Tick(Ship target, float deltaTime)
        {
            Vector3 position = _self.position;
            Vector3 toTarget = target.Position - position;
            float distance = toTarget.magnitude;

            if (_breakawayTime > 0f)
            {
                _breakawayTime -= deltaTime;
                Steer(_breakawayDirection, 1f, boost: false);
                return;
            }
            if (distance < _config.BreakawayDistance)
            {
                StartBreakaway(toTarget);
                return;
            }

            Vector3 aimPoint = target.Position;
            if (_weapon != null)
            {
                Ballistics.TryPredictIntercept(position, _motor.Velocity, _weapon.ProjectileSpeed, target.Position, target.Velocity, out aimPoint);
            }
            Vector3 aimDirection = aimPoint - position;
            Steer(aimDirection, distance > _config.PursuitDistance ? 1f : _config.AttackThrottle, distance > _config.BoostDistance);

            if (_weapon != null && distance <= _weapon.Range && Vector3.Angle(_self.forward, aimDirection) <= _config.FireConeDegrees)
            {
                float errorRadius = distance * Mathf.Tan(_config.AimErrorDegrees * Mathf.Deg2Rad);
                _weapon.TryFire(aimPoint + Random.insideUnitSphere * errorRadius);
            }
        }

        private void StartBreakaway(Vector3 toTarget)
        {
            _breakawayTime = _config.BreakawayDuration;
            Vector3 side = Vector3.Cross(toTarget, _self.up);
            if (side.sqrMagnitude < 1e-4f) side = _self.right;
            float sign = Random.value < 0.5f ? -1f : 1f;
            _breakawayDirection = (_self.forward + side.normalized * sign + Random.insideUnitSphere * 0.4f).normalized;
        }

        private void Steer(Vector3 direction, float throttle, bool boost)
        {
            _motor.SetHeading(ObstacleAvoidance.Adjust(_self, direction, _config));
            _motor.SetThrottle(throttle);
            _motor.SetBoost(boost);
        }
    }
}
