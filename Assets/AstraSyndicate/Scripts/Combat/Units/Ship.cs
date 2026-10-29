using System;
using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.Combat
{
    /// <summary>
    /// A combat unit: identity, faction, hull and death. Movement (ShipMotor), weapons (WeaponSystem) and
    /// control (player or AI) are separate components on the same object.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Ship : MonoBehaviour, IDamageable
    {
        private const float DefaultHull = 100f;

        [SerializeField] private ShipConfig config;
        [SerializeField] private Faction faction = Faction.Enemy;

        [Tooltip("Designer tag mission objectives can filter on, e.g. \"raider\" or \"flagship\".")]
        [SerializeField] private string unitTag;

        [Tooltip("Name shown on HUDs. Empty = the GameObject name.")]
        [SerializeField] private string displayName;

        private HitPoints _hull;
        private Rigidbody _body;
        private IEventBus _events;
        private bool _destroyed;

        public event Action<Ship, DamageInfo> Damaged;

        /// <summary>Raised once when the hull reaches zero, just before the GameObject is destroyed.</summary>
        public event Action<Ship, DamageInfo> Destroyed;

        public ShipConfig Config => config;

        public Faction Faction => faction;

        public string UnitTag => unitTag;

        /// <summary>Unique id for this play session, published with UnitDestroyedEvent.</summary>
        public string UnitId { get; private set; }

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        public bool IsAlive => !_destroyed;

        public float Hull => HullPoints.Current;

        public float MaxHull => HullPoints.Max;

        public float HullFraction => HullPoints.Fraction;

        public Vector3 Position => transform.position;

        public Vector3 Velocity => _body != null ? _body.linearVelocity : Vector3.zero;

        private HitPoints HullPoints => _hull ??= new HitPoints(config != null ? config.MaxHull : DefaultHull);

        /// <summary>Sets up a ship created from code (spawners, tests). Prefabs are set up in the inspector.</summary>
        public void Configure(ShipConfig shipConfig, Faction shipFaction, string shipTag = null, string shipDisplayName = null)
        {
            config = shipConfig;
            faction = shipFaction;
            unitTag = shipTag;
            displayName = shipDisplayName;
            _hull = null;
        }

        public void ApplyDamage(DamageInfo damage)
        {
            if (_destroyed || HullPoints.ApplyDamage(damage.Amount) <= 0f) return;

            Damaged?.Invoke(this, damage);
            _events?.Publish(new ShipDamagedEvent(this, damage));
            if (HullPoints.IsDepleted) Die(damage);
        }

        public void Repair(float amount)
        {
            if (!_destroyed) HullPoints.Restore(amount);
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            UnitId = $"{name}#{GetInstanceID()}";
            Services.TryGet(out _events);
        }

        private void OnEnable()
        {
            ShipRegistry.Register(this);
        }

        private void OnDisable()
        {
            ShipRegistry.Unregister(this);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_destroyed || config == null) return;

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < config.CollisionDamageThreshold) return;

            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Ship other = collision.rigidbody != null ? collision.rigidbody.GetComponent<Ship>() : null;
            float damage = (impactSpeed - config.CollisionDamageThreshold) * config.CollisionDamagePerSpeed;
            ApplyDamage(new DamageInfo(damage, point, other));
        }

        private void Die(DamageInfo damage)
        {
            _destroyed = true;
            Destroyed?.Invoke(this, damage);
            _events?.Publish(new ShipDestroyedEvent(this, damage.Source));
            _events?.Publish(new UnitDestroyedEvent(UnitId, faction, unitTag));

            if (config != null && config.ExplosionEffect != null)
            {
                PooledEffect.Spawn(config.ExplosionEffect, transform.position, transform.rotation, config.ExplosionScale);
            }
            Destroy(gameObject);
        }
    }
}
