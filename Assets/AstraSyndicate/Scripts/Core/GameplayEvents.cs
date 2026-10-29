// Gameplay facts shared between modules. Combat publishes them and campaign objectives consume
// them (later also UI, audio and analytics), so neither side has to reference the other.
namespace AstraSyndicate.Core
{
    /// <summary>A unit (ship, turret, station...) was destroyed.</summary>
    public readonly struct UnitDestroyedEvent
    {
        public readonly string UnitId;
        public readonly Faction Faction;

        /// <summary>Optional designer tag objectives can filter on, e.g. "raider" or "rift_station".</summary>
        public readonly string Tag;

        public UnitDestroyedEvent(string unitId, Faction faction, string tag = null)
        {
            UnitId = unitId;
            Faction = faction;
            Tag = tag;
        }
    }

    /// <summary>The player's fleet reached a named waypoint.</summary>
    public readonly struct WaypointReachedEvent
    {
        public readonly string WaypointId;

        public WaypointReachedEvent(string waypointId)
        {
            WaypointId = waypointId;
        }
    }

    /// <summary>The player's fleet can no longer fight (flagship lost, all ships destroyed...).</summary>
    public readonly struct PlayerDefeatedEvent
    {
        public readonly string Reason;

        public PlayerDefeatedEvent(string reason)
        {
            Reason = reason;
        }
    }
}
