using System.Collections.Generic;

namespace AstraSyndicate.Campaign
{
    /// <summary>Read-only view of the loaded campaign. Treat the returned data objects as immutable.</summary>
    public interface ICampaignRepository
    {
        /// <summary>Chapters in play order.</summary>
        IReadOnlyList<ChapterData> Chapters { get; }

        /// <summary>Every mission in play order, across all chapters.</summary>
        IReadOnlyList<MissionData> MissionsInOrder { get; }

        ChapterData GetChapter(string chapterId);

        MissionData GetMission(string missionId);

        ChapterData GetChapterOfMission(string missionId);

        /// <summary>Position of the mission in <see cref="MissionsInOrder"/>, or -1 if unknown.</summary>
        int GetMissionIndex(string missionId);

        /// <summary>The mission after this one (possibly in the next chapter), or null at the end of the campaign.</summary>
        MissionData GetNextMission(string missionId);
    }
}
