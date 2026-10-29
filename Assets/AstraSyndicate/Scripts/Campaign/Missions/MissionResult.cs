namespace AstraSyndicate.Campaign
{
    /// <summary>Outcome of one mission attempt. Published with MissionEndedEvent and recorded as progress.</summary>
    public readonly struct MissionResult
    {
        public readonly string MissionId;
        public readonly bool Succeeded;
        public readonly float ElapsedSeconds;
        public readonly int OptionalObjectivesCompleted;
        public readonly int OptionalObjectivesTotal;

        /// <summary>Why the mission failed; null on success.</summary>
        public readonly string FailureReason;

        public MissionResult(string missionId, bool succeeded, float elapsedSeconds, int optionalObjectivesCompleted, int optionalObjectivesTotal, string failureReason)
        {
            MissionId = missionId;
            Succeeded = succeeded;
            ElapsedSeconds = elapsedSeconds;
            OptionalObjectivesCompleted = optionalObjectivesCompleted;
            OptionalObjectivesTotal = optionalObjectivesTotal;
            FailureReason = failureReason;
        }
    }
}
