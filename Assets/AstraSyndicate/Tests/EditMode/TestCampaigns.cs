using System.Collections.Generic;
using AstraSyndicate.Campaign;

namespace AstraSyndicate.Tests
{
    /// <summary>Chapter source that returns fixed data.</summary>
    internal sealed class StubChapterSource : IChapterSource
    {
        private readonly ChapterData[] _chapters;

        public StubChapterSource(string sourceName, params ChapterData[] chapters)
        {
            SourceName = sourceName;
            _chapters = chapters;
        }

        public string SourceName { get; }

        public IReadOnlyList<ChapterData> LoadChapters(ValidationReport report)
        {
            return _chapters;
        }
    }

    /// <summary>Builders for campaign test data.</summary>
    internal static class TestCampaigns
    {
        public static ObjectiveData Destroy(string id, int count, string tag = "", bool optional = false)
        {
            return new ObjectiveData { id = id, type = ObjectiveTypes.DestroyEnemies, description = id, targetCount = count, targetId = tag, optional = optional };
        }

        public static ObjectiveData Survive(string id, float seconds, bool optional = false)
        {
            return new ObjectiveData { id = id, type = ObjectiveTypes.Survive, description = id, durationSeconds = seconds, targetId = "", optional = optional };
        }

        public static ObjectiveData Reach(string id, string waypointId)
        {
            return new ObjectiveData { id = id, type = ObjectiveTypes.ReachWaypoint, description = id, targetId = waypointId };
        }

        public static MissionData Mission(string id, params ObjectiveData[] objectives)
        {
            return new MissionData { id = id, title = id, sceneName = "Scene_" + id, objectives = new List<ObjectiveData>(objectives) };
        }

        public static ChapterData Chapter(string id, int order, params MissionData[] missions)
        {
            return new ChapterData { id = id, title = id, order = order, missions = new List<MissionData>(missions) };
        }

        public static CampaignRepository Repository(params ChapterData[] chapters)
        {
            return new CampaignRepository(new IChapterSource[] { new StubChapterSource("test", chapters) });
        }

        /// <summary>Chapter c1 holds missions m1 and m2; chapter c2 holds m3.</summary>
        public static CampaignRepository TwoChapters()
        {
            return Repository(
                Chapter("c1", 1, Mission("m1", Destroy("o", 1)), Mission("m2", Destroy("o", 1))),
                Chapter("c2", 2, Mission("m3", Destroy("o", 1))));
        }
    }
}
