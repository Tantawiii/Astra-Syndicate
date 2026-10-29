using System.Collections.Generic;
using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.Combat
{
    public static class TargetSelection
    {
        private static readonly List<Ship> Candidates = new List<Ship>();

        /// <summary>
        /// Picks the hostile closest to the aim line within <paramref name="range"/>. If that ship is already
        /// <paramref name="current"/>, the next one in aim order is picked, so pressing "target" again cycles.
        /// </summary>
        public static Ship SelectByAim(Faction faction, Vector3 origin, Vector3 aimDirection, float range, Ship current)
        {
            Candidates.Clear();
            float rangeSqr = range * range;
            foreach (Ship ship in ShipRegistry.All)
            {
                if (!ship.IsAlive || !FactionRelations.AreHostile(faction, ship.Faction)) continue;
                if ((ship.Position - origin).sqrMagnitude <= rangeSqr) Candidates.Add(ship);
            }
            if (Candidates.Count == 0) return null;

            Candidates.Sort((a, b) => Vector3.Angle(aimDirection, a.Position - origin).CompareTo(Vector3.Angle(aimDirection, b.Position - origin)));
            Ship best = Candidates[0];
            return best == current ? Candidates[1 % Candidates.Count] : best;
        }
    }
}
