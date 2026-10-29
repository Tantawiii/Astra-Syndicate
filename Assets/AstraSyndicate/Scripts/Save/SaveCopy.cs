namespace AstraSyndicate.Save
{
    /// <summary>Which stored copy of a save key to access.</summary>
    public enum SaveCopy
    {
        /// <summary>The most recent write.</summary>
        Primary,

        /// <summary>The write before the most recent one.</summary>
        Backup,
    }
}
