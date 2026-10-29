namespace AstraSyndicate.AI
{
    public enum EnemyState
    {
        /// <summary>Flying a patrol route (or wandering near home) looking for hostiles.</summary>
        Patrol,

        /// <summary>Chasing and shooting a hostile.</summary>
        Attack,

        /// <summary>Badly damaged: fleeing, then repairing once out of danger.</summary>
        Retreat,
    }
}
