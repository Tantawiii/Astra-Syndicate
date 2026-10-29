namespace AstraSyndicate.AI
{
    /// <summary>Thresholds that drive enemy state changes.</summary>
    public readonly struct EnemyRules
    {
        public readonly float RetreatBelowHull;
        public readonly float RecoverAtHull;
        public readonly float LeashDistance;

        public EnemyRules(float retreatBelowHull, float recoverAtHull, float leashDistance)
        {
            RetreatBelowHull = retreatBelowHull;
            RecoverAtHull = recoverAtHull;
            LeashDistance = leashDistance;
        }
    }

    /// <summary>What an enemy pilot knows when deciding what to do next.</summary>
    public readonly struct EnemySituation
    {
        public readonly EnemyState State;
        public readonly float HullFraction;

        /// <summary>A hostile is within detection range.</summary>
        public readonly bool HasTarget;

        public readonly float DistanceFromHome;

        public EnemySituation(EnemyState state, float hullFraction, bool hasTarget, float distanceFromHome)
        {
            State = state;
            HullFraction = hullFraction;
            HasTarget = hasTarget;
            DistanceFromHome = distanceFromHome;
        }
    }

    /// <summary>The enemy state machine's transition rules, as a pure function so they can be unit-tested.</summary>
    public static class EnemyDecision
    {
        // Patrolling ships only start a chase well inside their leash, so they don't flip-flop at its edge.
        private const float EngageLeashFraction = 0.75f;

        public static EnemyState Next(in EnemySituation situation, in EnemyRules rules)
        {
            bool badlyDamaged = situation.HullFraction <= rules.RetreatBelowHull;
            switch (situation.State)
            {
                case EnemyState.Patrol:
                    if (badlyDamaged) return EnemyState.Retreat;
                    return situation.HasTarget && situation.DistanceFromHome < rules.LeashDistance * EngageLeashFraction
                        ? EnemyState.Attack
                        : EnemyState.Patrol;

                case EnemyState.Attack:
                    if (badlyDamaged) return EnemyState.Retreat;
                    return situation.HasTarget && situation.DistanceFromHome <= rules.LeashDistance
                        ? EnemyState.Attack
                        : EnemyState.Patrol;

                case EnemyState.Retreat:
                    return situation.HullFraction >= rules.RecoverAtHull ? EnemyState.Patrol : EnemyState.Retreat;

                default:
                    return EnemyState.Patrol;
            }
        }
    }
}
