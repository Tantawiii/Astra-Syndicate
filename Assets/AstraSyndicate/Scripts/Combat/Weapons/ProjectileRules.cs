using AstraSyndicate.Core;

namespace AstraSyndicate.Combat
{
    public static class ProjectileRules
    {
        /// <summary>
        /// Whether a shot from <paramref name="owner"/> stops at <paramref name="candidate"/>. A null candidate is
        /// scenery (asteroids...), which always stops shots. Shots pass through their owner, wrecks and ships of
        /// their own faction (no friendly fire); neutral damageables are always hit.
        /// </summary>
        public static bool CanHit(IDamageable owner, Faction shooterFaction, IDamageable candidate)
        {
            if (candidate == null) return true;
            if (ReferenceEquals(candidate, owner) || !candidate.IsAlive) return false;
            return candidate.Faction == Faction.Neutral || FactionRelations.AreHostile(shooterFaction, candidate.Faction);
        }
    }
}
