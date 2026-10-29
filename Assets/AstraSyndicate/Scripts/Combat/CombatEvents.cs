using AstraSyndicate.Core;

namespace AstraSyndicate.Combat
{
    public readonly struct ShipDamagedEvent
    {
        public readonly Ship Ship;
        public readonly DamageInfo Damage;

        public ShipDamagedEvent(Ship ship, DamageInfo damage)
        {
            Ship = ship;
            Damage = damage;
        }
    }

    /// <summary>Published alongside the module-neutral UnitDestroyedEvent, with direct references for combat systems.</summary>
    public readonly struct ShipDestroyedEvent
    {
        public readonly Ship Ship;

        /// <summary>The ship credited with the kill; null for environment damage.</summary>
        public readonly Ship Killer;

        public ShipDestroyedEvent(Ship ship, Ship killer)
        {
            Ship = ship;
            Killer = killer;
        }
    }

    /// <summary>A fleet commander (usually the player) gave an order to its wingmen.</summary>
    public readonly struct FleetOrderIssuedEvent
    {
        public readonly Faction Faction;
        public readonly FleetOrder Order;

        /// <summary>The ship to attack for <see cref="FleetOrder.AttackTarget"/>; null otherwise.</summary>
        public readonly Ship Target;

        public FleetOrderIssuedEvent(Faction faction, FleetOrder order, Ship target)
        {
            Faction = faction;
            Order = order;
            Target = target;
        }
    }
}
