using System.Collections.Generic;
using System.Linq;

namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// Merges chapters from all sources (a later source replaces a chapter with the same id), sorts
    /// them by order and indexes the missions. Invalid entries are reported and skipped.
    /// </summary>
    public sealed class CampaignRepository : ICampaignRepository
    {
        private readonly List<ChapterData> _chapters = new List<ChapterData>();
        private readonly List<MissionData> _missions = new List<MissionData>();
        private readonly Dictionary<string, ChapterData> _chaptersById = new Dictionary<string, ChapterData>();
        private readonly Dictionary<string, int> _missionIndex = new Dictionary<string, int>();
        private readonly Dictionary<string, ChapterData> _chapterOfMission = new Dictionary<string, ChapterData>();

        public CampaignRepository(IEnumerable<IChapterSource> sources, ValidationReport report = null)
        {
            Report = report ?? new ValidationReport();

            foreach (ChapterData chapter in Merge(sources).OrderBy(chapter => chapter.order))
            {
                IndexMissions(chapter);
                _chapters.Add(chapter);
                _chaptersById.Add(chapter.id, chapter);
            }
        }

        /// <summary>Problems found while loading. Content checks are done separately by CampaignValidator.</summary>
        public ValidationReport Report { get; }

        public IReadOnlyList<ChapterData> Chapters => _chapters;

        public IReadOnlyList<MissionData> MissionsInOrder => _missions;

        public ChapterData GetChapter(string chapterId)
        {
            return chapterId != null && _chaptersById.TryGetValue(chapterId, out ChapterData chapter) ? chapter : null;
        }

        public MissionData GetMission(string missionId)
        {
            int index = GetMissionIndex(missionId);
            return index >= 0 ? _missions[index] : null;
        }

        public ChapterData GetChapterOfMission(string missionId)
        {
            return missionId != null && _chapterOfMission.TryGetValue(missionId, out ChapterData chapter) ? chapter : null;
        }

        public int GetMissionIndex(string missionId)
        {
            return missionId != null && _missionIndex.TryGetValue(missionId, out int index) ? index : -1;
        }

        public MissionData GetNextMission(string missionId)
        {
            int index = GetMissionIndex(missionId);
            return index >= 0 && index + 1 < _missions.Count ? _missions[index + 1] : null;
        }

        private List<ChapterData> Merge(IEnumerable<IChapterSource> sources)
        {
            var merged = new List<ChapterData>();
            var sourceOf = new Dictionary<string, string>();

            foreach (IChapterSource source in sources ?? Enumerable.Empty<IChapterSource>())
            {
                if (source == null) continue;

                var seenInSource = new HashSet<string>();
                foreach (ChapterData chapter in source.LoadChapters(Report))
                {
                    if (chapter == null) continue;

                    // Custom sources may hand over raw data; normalizing again is harmless.
                    ChapterDataUtility.Normalize(chapter);
                    if (chapter.id.Length == 0)
                    {
                        Report.AddError($"{source.SourceName}: a chapter has no id and was skipped.");
                        continue;
                    }
                    if (!seenInSource.Add(chapter.id))
                    {
                        Report.AddError($"{source.SourceName}: duplicate chapter id '{chapter.id}'; the later one was skipped.");
                        continue;
                    }

                    int existing = merged.FindIndex(other => other.id == chapter.id);
                    if (existing >= 0)
                    {
                        Report.AddNote($"Chapter '{chapter.id}' from {source.SourceName} replaces the one from {sourceOf[chapter.id]}.");
                        merged[existing] = chapter;
                    }
                    else
                    {
                        merged.Add(chapter);
                    }
                    sourceOf[chapter.id] = source.SourceName;
                }
            }
            return merged;
        }

        private void IndexMissions(ChapterData chapter)
        {
            var accepted = new List<MissionData>(chapter.missions.Count);
            foreach (MissionData mission in chapter.missions)
            {
                if (string.IsNullOrEmpty(mission.id))
                {
                    Report.AddError($"Chapter '{chapter.id}': a mission has no id and was skipped.");
                    continue;
                }
                if (_missionIndex.ContainsKey(mission.id))
                {
                    Report.AddError($"Chapter '{chapter.id}': mission id '{mission.id}' is already used; the duplicate was skipped.");
                    continue;
                }

                _missionIndex.Add(mission.id, _missions.Count);
                _missions.Add(mission);
                _chapterOfMission.Add(mission.id, chapter);
                accepted.Add(mission);
            }
            chapter.missions = accepted;
        }
    }
}
