using System.Collections.Generic;
using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.Combat
{
    /// <summary>All active ships, for targeting and AI awareness queries.</summary>
    public static class ShipRegistry
    {
        private static readonly List<Ship> Ships = new List<Ship>();

        public static IReadOnlyList<Ship> All => Ships;

        /// <summary>Nearest living ship hostile to <paramref name="faction"/> within <paramref name="maxDistance"/>, or null.</summary>
        public static Ship FindNearestHostile(Faction faction, Vector3 position, float maxDistance)
        {
            Ship nearest = null;
            float nearestSqr = maxDistance * maxDistance;
            foreach (Ship ship in Ships)
            {
                if (!ship.IsAlive || !FactionRelations.AreHostile(faction, ship.Faction)) continue;

                float sqr = (ship.Position - position).sqrMagnitude;
                if (sqr > nearestSqr) continue;
                nearest = ship;
                nearestSqr = sqr;
            }
            return nearest;
        }

        public static int CountAlive(Faction faction)
        {
            int count = 0;
            foreach (Ship ship in Ships)
            {
                if (ship.IsAlive && ship.Faction == faction) count++;
            }
            return count;
        }

        internal static void Register(Ship ship)
        {
            if (!Ships.Contains(ship)) Ships.Add(ship);
        }

        internal static void Unregister(Ship ship)
        {
            Ships.Remove(ship);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeEnter()
        {
            Ships.Clear();
        }
    }
}
