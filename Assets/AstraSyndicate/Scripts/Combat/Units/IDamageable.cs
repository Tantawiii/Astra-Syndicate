using AstraSyndicate.Core;

namespace AstraSyndicate.Combat
{
    /// <summary>Anything projectiles can damage. Colliders without one (asteroids...) simply stop shots.</summary>
    public interface IDamageable
    {
        Faction Faction { get; }

        bool IsAlive { get; }

        void ApplyDamage(DamageInfo damage);
    }
}
