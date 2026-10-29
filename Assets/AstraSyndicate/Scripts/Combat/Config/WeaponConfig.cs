using UnityEngine;

namespace AstraSyndicate.Combat
{
    [CreateAssetMenu(menuName = "Astra Syndicate/Combat/Weapon Config", fileName = "WeaponConfig")]
    public sealed class WeaponConfig : ScriptableObject
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private PooledEffect impactEffect;

        [SerializeField, Min(0.1f)] private float shotsPerSecond = 6f;
        [Tooltip("Muzzle speed in m/s; shots also inherit the ship's velocity.")]
        [SerializeField, Min(1f)] private float projectileSpeed = 300f;
        [SerializeField, Min(0f)] private float damage = 6f;
        [Tooltip("Distance a shot travels before it fades out, in meters.")]
        [SerializeField, Min(1f)] private float range = 650f;
        [Tooltip("Random inaccuracy per shot, in degrees.")]
        [SerializeField, Range(0f, 10f)] private float spreadDegrees = 0.5f;
        [Tooltip("How far muzzles may swivel toward the aim point, in degrees.")]
        [SerializeField, Range(0f, 30f)] private float gimbalDegrees = 5f;
        [Tooltip("Fire the muzzles one after another instead of all at once.")]
        [SerializeField] private bool alternateMuzzles = true;

        public Projectile ProjectilePrefab => projectilePrefab;

        public PooledEffect ImpactEffect => impactEffect;

        public float ShotsPerSecond => shotsPerSecond;

        public float ProjectileSpeed => projectileSpeed;

        public float Damage => damage;

        public float Range => range;

        public float SpreadDegrees => spreadDegrees;

        public float GimbalDegrees => gimbalDegrees;

        public bool AlternateMuzzles => alternateMuzzles;
    }
}
