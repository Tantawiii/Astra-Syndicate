namespace AstraSyndicate.Save
{
    public readonly struct SaveCompletedEvent
    {
        public readonly string Key;

        public SaveCompletedEvent(string key)
        {
            Key = key;
        }
    }

    public readonly struct SaveFailedEvent
    {
        public readonly string Key;
        public readonly string Error;

        public SaveFailedEvent(string key, string error)
        {
            Key = key;
            Error = error;
        }
    }

    /// <summary>A stored copy could not be read (corrupted, tampered, or rejected by the protector).</summary>
    public readonly struct SaveLoadFailedEvent
    {
        public readonly string Key;

        /// <summary>True if the backup copy was loaded instead; false if no readable copy was left.</summary>
        public readonly bool RecoveredFromBackup;

        public SaveLoadFailedEvent(string key, bool recoveredFromBackup)
        {
            Key = key;
            RecoveredFromBackup = recoveredFromBackup;
        }
    }
}
