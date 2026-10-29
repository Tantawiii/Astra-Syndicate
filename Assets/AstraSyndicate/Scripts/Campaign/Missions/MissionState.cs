namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// Mission lifecycle: Idle → Briefing → Running ⇄ Paused → Completed | Failed → Idle.
    /// </summary>
    public enum MissionState
    {
        /// <summary>No mission loaded (menus, transitions).</summary>
        Idle,

        /// <summary>Mission loaded; objectives are known but not tracked yet.</summary>
        Briefing,

        Running,
        Paused,
        Completed,
        Failed,
    }
}
