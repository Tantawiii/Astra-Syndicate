using System;
using AstraSyndicate.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AstraSyndicate.Combat
{
    /// <summary>
    /// A gun group on a ship: fires pooled projectiles from its muzzles at the configured rate. Muzzles swivel
    /// a few degrees toward the requested aim point (gimbal), which lets pilots and AI hit leading shots.
    /// </summary>
    public sealed class WeaponSystem : MonoBehaviour
    {
        [SerializeField] private WeaponConfig config;

        [Tooltip("Points shots are fired from; their forward axis is the firing direction.")]
        [SerializeField] private Transform[] muzzles = Array.Empty<Transform>();

        private Ship _owner;
        private Rigidbody _body;
        private FireControl _fireControl;
        private int _nextMuzzle;

        public WeaponConfig Config => config;

        public float Range => config != null ? config.Range : 0f;

        public float ProjectileSpeed => config != null ? config.ProjectileSpeed : 0f;

        private FireControl Trigger => _fireControl ??= new FireControl(config.ShotsPerSecond);

        public void Configure(WeaponConfig weaponConfig, params Transform[] weaponMuzzles)
        {
            config = weaponConfig;
            muzzles = weaponMuzzles ?? Array.Empty<Transform>();
            _fireControl = null;
            _nextMuzzle = 0;
        }

        /// <summary>
        /// Fires if the weapon is ready. <paramref name="aimPoint"/> is the world point to converge on; null
        /// fires straight along the muzzles. Returns true if a shot was fired.
        /// </summary>
        public bool TryFire(Vector3? aimPoint = null)
        {
            if (config == null || config.ProjectilePrefab == null || muzzles.Length == 0) return false;
            if (_owner != null && !_owner.IsAlive) return false;
            if (!Trigger.TryFire(Time.time)) return false;

            if (config.AlternateMuzzles)
            {
                FireFrom(muzzles[_nextMuzzle], aimPoint);
                _nextMuzzle = (_nextMuzzle + 1) % muzzles.Length;
            }
            else
            {
                foreach (Transform muzzle in muzzles)
                {
                    FireFrom(muzzle, aimPoint);
                }
            }
            return true;
        }

        private void Awake()
        {
            _owner = GetComponentInParent<Ship>();
            _body = GetComponentInParent<Rigidbody>();
        }

        private void FireFrom(Transform muzzle, Vector3? aimPoint)
        {
            if (muzzle == null) return;

            Vector3 inherited = _body != null ? _body.linearVelocity : Vector3.zero;
            Faction faction = _owner != null ? _owner.Faction : Faction.Neutral;
            Projectile projectile = PrefabPool<Projectile>.Get(config.ProjectilePrefab);
            projectile.Launch(
                muzzle.position,
                AimDirection(muzzle, aimPoint) * config.ProjectileSpeed + inherited,
                config.Damage,
                config.Range,
                _owner,
                faction,
                config.ImpactEffect);
        }

        private Vector3 AimDirection(Transform muzzle, Vector3? aimPoint)
        {
            Vector3 direction = muzzle.forward;
            if (aimPoint.HasValue)
            {
                Vector3 toAim = aimPoint.Value - muzzle.position;
                if (toAim.sqrMagnitude > 1f && Vector3.Angle(direction, toAim) <= config.GimbalDegrees) direction = toAim.normalized;
            }

            float spread = config.SpreadDegrees;
            if (spread <= 0f) return direction;
            return Quaternion.AngleAxis(Random.Range(-spread, spread), muzzle.up)
                   * Quaternion.AngleAxis(Random.Range(-spread, spread), muzzle.right)
                   * direction;
        }
    }
}
