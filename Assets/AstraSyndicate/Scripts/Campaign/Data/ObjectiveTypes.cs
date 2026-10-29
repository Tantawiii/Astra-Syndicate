namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// Built-in objective type keys used by chapter configs. Types are strings rather than an enum so
    /// JSON stays readable and new types can be registered with ObjectiveFactory without edits here.
    /// </summary>
    public static class ObjectiveTypes
    {
        public const string DestroyEnemies = "destroy_enemies";
        public const string Survive = "survive";
        public const string ReachWaypoint = "reach_waypoint";

        public static readonly string[] BuiltIn = { DestroyEnemies, Survive, ReachWaypoint };
    }
}
