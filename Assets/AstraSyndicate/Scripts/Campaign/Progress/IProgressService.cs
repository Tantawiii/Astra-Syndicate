namespace AstraSyndicate.Campaign
{
    /// <summary>The player's offline campaign progress: completions, unlocks and records.</summary>
    public interface IProgressService
    {
        /// <summary>True once any mission has been launched.</summary>
        bool HasProgress { get; }

        bool IsMissionUnlocked(string missionId);

        bool IsMissionCompleted(string missionId);

        bool IsChapterUnlocked(string chapterId);

        bool IsChapterCompleted(string chapterId);

        /// <summary>A copy of the mission's record, or null if it was never played.</summary>
        MissionRecord GetRecord(string missionId);

        /// <summary>
        /// The mission "Continue" should launch: the last played mission if it is still unfinished,
        /// otherwise the first unlocked unfinished one, otherwise the last played (or final) mission.
        /// </summary>
        MissionData GetContinueMission();

        /// <summary>Counts an attempt. Called when a mission is launched.</summary>
        void MarkStarted(string missionId);

        /// <summary>Stores the result and saves. Completing a mission unlocks the next one.</summary>
        void RecordResult(MissionResult result);

        /// <summary>Erases all campaign progress, including the save file.</summary>
        void Reset();
    }
}
