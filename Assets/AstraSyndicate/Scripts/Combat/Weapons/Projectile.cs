using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.Combat
{
    /// <summary>
    /// A pooled shot. Instead of relying on the physics engine (fast shots would tunnel through ships), it
    /// sweeps a sphere along its path every frame and applies damage to the first valid thing it touches.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        private const int MaxHits = 16;
        private static readonly RaycastHit[] Hits = new RaycastHit[MaxHits];

        [Tooltip("Collision radius of the shot, in meters.")]
        [SerializeField, Min(0.01f)] private float radius = 0.35f;
        [SerializeField] private TrailRenderer trail;

        private Vector3 _velocity;
        private float _damage;
        private float _remainingDistance;
        private Ship _owner;
        private Faction _faction;
        private PooledEffect _impactEffect;
        private bool _inFlight;

        public bool InFlight => _inFlight;

        public void Configure(float collisionRadius, TrailRenderer trailRenderer)
        {
            radius = collisionRadius;
            trail = trailRenderer;
        }

        public void Launch(Vector3 position, Vector3 velocity, float damage, float maxDistance, Ship owner, Faction faction, PooledEffect impactEffect)
        {
            Quaternion rotation = velocity.sqrMagnitude > 0f ? Quaternion.LookRotation(velocity) : transform.rotation;
            transform.SetPositionAndRotation(position, rotation);
            _velocity = velocity;
            _damage = damage;
            _remainingDistance = maxDistance;
            _owner = owner;
            _faction = faction;
            _impactEffect = impactEffect;
            _inFlight = true;
            if (trail != null) trail.Clear();
        }

        private void Update()
        {
            if (!_inFlight) return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            Vector3 origin = transform.position;
            Vector3 step = _velocity * deltaTime;
            float distance = step.magnitude;
            if (distance <= 0f)
            {
                Release();
                return;
            }

            if (TryFindHit(origin, step / distance, distance, out RaycastHit hit, out IDamageable target))
            {
                // Colliders that already overlap the start of the sweep report distance 0 and no point.
                Vector3 point = hit.distance > 0f ? hit.point : origin;
                target?.ApplyDamage(new DamageInfo(_damage, point, _owner));
                if (_impactEffect != null) PooledEffect.Spawn(_impactEffect, point, Quaternion.LookRotation(-step));
                Release();
                return;
            }

            transform.position = origin + step;
            _remainingDistance -= distance;
            if (_remainingDistance <= 0f) Release();
        }

        private bool TryFindHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit best, out IDamageable target)
        {
            best = default;
            target = null;
            bool found = false;

            int count = Physics.SphereCastNonAlloc(origin, radius, direction, Hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                IDamageable candidate = hit.collider.GetComponentInParent<IDamageable>();
                if (!ProjectileRules.CanHit(_owner, _faction, candidate)) continue;
                if (found && hit.distance >= best.distance) continue;

                best = hit;
                target = candidate;
                found = true;
            }
            return found;
        }

        private void Release()
        {
            _inFlight = false;
            _owner = null;
            PrefabPool<Projectile>.Release(this);
        }
    }
}
