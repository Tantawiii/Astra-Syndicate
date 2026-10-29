namespace AstraSyndicate.Campaign
{
    public readonly struct MissionStateChangedEvent
    {
        public readonly string MissionId;
        public readonly MissionState Previous;
        public readonly MissionState Current;

        public MissionStateChangedEvent(string missionId, MissionState previous, MissionState current)
        {
            MissionId = missionId;
            Previous = previous;
            Current = current;
        }
    }

    public readonly struct ObjectiveUpdatedEvent
    {
        public readonly string MissionId;
        public readonly string ObjectiveId;
        public readonly ObjectiveStatus Status;
        public readonly float Progress01;

        public ObjectiveUpdatedEvent(string missionId, string objectiveId, ObjectiveStatus status, float progress01)
        {
            MissionId = missionId;
            ObjectiveId = objectiveId;
            Status = status;
            Progress01 = progress01;
        }
    }

    /// <summary>A mission was completed or failed. Not published when a mission is exited early.</summary>
    public readonly struct MissionEndedEvent
    {
        public readonly MissionResult Result;

        public MissionEndedEvent(MissionResult result)
        {
            Result = result;
        }
    }

    public readonly struct MissionUnlockedEvent
    {
        public readonly string MissionId;

        public MissionUnlockedEvent(string missionId)
        {
            MissionId = missionId;
        }
    }

    public readonly struct ChapterUnlockedEvent
    {
        public readonly string ChapterId;

        public ChapterUnlockedEvent(string chapterId)
        {
            ChapterId = chapterId;
        }
    }

    /// <summary>Saved progress changed; views showing progress should refresh.</summary>
    public readonly struct ProgressChangedEvent
    {
        /// <summary>The mission whose record changed, or null when all progress changed (e.g. a reset).</summary>
        public readonly string MissionId;

        public ProgressChangedEvent(string missionId)
        {
            MissionId = missionId;
        }
    }

    public readonly struct ProgressResetEvent
    {
    }
}
