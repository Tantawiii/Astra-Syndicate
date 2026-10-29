using AstraSyndicate.Core;

namespace AstraSyndicate.Combat
{
    public static class FactionRelations
    {
        /// <summary>Player and enemy factions fight each other; neutral objects are nobody's enemy.</summary>
        public static bool AreHostile(Faction a, Faction b)
        {
            return a != Faction.Neutral && b != Faction.Neutral && a != b;
        }
    }
}
