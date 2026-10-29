using System;
using System.Collections.Generic;
using AstraSyndicate.Core;
using AstraSyndicate.Save;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// Offline campaign progress, saved immediately after every change. Unlocks are derived from
    /// completions and campaign order instead of being stored, so adding or reordering content can
    /// never leave a save in an impossible state.
    /// </summary>
    public sealed class ProgressService : IProgressService
    {
        public const string SaveKey = "campaign_progress";

        private readonly ISaveService _saves;
        private readonly ICampaignRepository _campaign;
        private readonly IEventBus _events;
        private readonly Dictionary<string, MissionRecord> _records = new Dictionary<string, MissionRecord>();
        private CampaignProgressData _data;

        public ProgressService(ISaveService saves, ICampaignRepository campaign, IEventBus events)
        {
            _saves = saves ?? throw new ArgumentNullException(nameof(saves));
            _campaign = campaign ?? throw new ArgumentNullException(nameof(campaign));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public bool HasProgress
        {
            get
            {
                EnsureLoaded();
                return _records.Count > 0;
            }
        }

        public bool IsMissionCompleted(string missionId)
        {
            return FindRecord(missionId)?.completed == true;
        }

        public bool IsMissionUnlocked(string missionId)
        {
            int index = _campaign.GetMissionIndex(missionId);
            if (index < 0) return false;
            if (index == 0 || IsMissionCompleted(missionId)) return true;
            return IsMissionCompleted(_campaign.MissionsInOrder[index - 1].id);
        }

        public bool IsChapterUnlocked(string chapterId)
        {
            ChapterData chapter = _campaign.GetChapter(chapterId);
            return chapter != null && chapter.missions.Count > 0 && IsMissionUnlocked(chapter.missions[0].id);
        }

        public bool IsChapterCompleted(string chapterId)
        {
            ChapterData chapter = _campaign.GetChapter(chapterId);
            if (chapter == null || chapter.missions.Count == 0) return false;

            foreach (MissionData mission in chapter.missions)
            {
                if (!IsMissionCompleted(mission.id)) return false;
            }
            return true;
        }

        public MissionRecord GetRecord(string missionId)
        {
            return FindRecord(missionId)?.Clone();
        }

        public MissionData GetContinueMission()
        {
            IReadOnlyList<MissionData> missions = _campaign.MissionsInOrder;
            if (missions.Count == 0) return null;

            EnsureLoaded();
            MissionData last = _campaign.GetMission(_data.lastMissionId);
            if (last != null && !IsMissionCompleted(last.id) && IsMissionUnlocked(last.id)) return last;

            foreach (MissionData mission in missions)
            {
                if (IsMissionUnlocked(mission.id) && !IsMissionCompleted(mission.id)) return mission;
            }
            return last ?? missions[missions.Count - 1];
        }

        public void MarkStarted(string missionId)
        {
            if (_campaign.GetMission(missionId) == null) return;

            MissionRecord record = GetOrCreateRecord(missionId);
            record.attempts++;
            _data.lastMissionId = missionId;
            Persist(missionId);
        }

        public void RecordResult(MissionResult result)
        {
            if (_campaign.GetMission(result.MissionId) == null)
            {
                Debug.LogWarning($"[Progress] Ignoring result for unknown mission '{result.MissionId}'.");
                return;
            }

            MissionRecord record = GetOrCreateRecord(result.MissionId);
            _data.lastMissionId = result.MissionId;
            if (!result.Succeeded)
            {
                Persist(result.MissionId);
                return;
            }

            MissionData next = _campaign.GetNextMission(result.MissionId);
            ChapterData nextChapter = next != null ? _campaign.GetChapterOfMission(next.id) : null;
            bool nextWasUnlocked = next != null && IsMissionUnlocked(next.id);
            bool nextChapterWasUnlocked = nextChapter != null && IsChapterUnlocked(nextChapter.id);

            if (record.completions == 0 || result.ElapsedSeconds < record.bestTimeSeconds)
            {
                record.bestTimeSeconds = result.ElapsedSeconds;
            }
            record.completions++;
            record.completed = true;
            Persist(result.MissionId);

            if (next != null && !nextWasUnlocked && IsMissionUnlocked(next.id))
            {
                _events.Publish(new MissionUnlockedEvent(next.id));
            }
            if (nextChapter != null && !nextChapterWasUnlocked && IsChapterUnlocked(nextChapter.id))
            {
                _events.Publish(new ChapterUnlockedEvent(nextChapter.id));
            }
        }

        public void Reset()
        {
            _saves.Delete(SaveKey);
            _data = new CampaignProgressData();
            _records.Clear();
            _events.Publish(new ProgressResetEvent());
            _events.Publish(new ProgressChangedEvent(null));
        }

        private void EnsureLoaded()
        {
            if (_data != null) return;

            if (!_saves.TryLoad(SaveKey, out CampaignProgressData loaded) || loaded == null)
            {
                loaded = new CampaignProgressData();
            }

            // Version 1 is the first format; migrations from older schemas will go here.
            loaded.schemaVersion = CampaignProgressData.CurrentSchemaVersion;

            _records.Clear();
            foreach (MissionRecord record in loaded.missions ?? new List<MissionRecord>())
            {
                if (record != null && !string.IsNullOrEmpty(record.missionId)) _records[record.missionId] = record;
            }
            loaded.missions = new List<MissionRecord>(_records.Values);
            _data = loaded;
        }

        private MissionRecord FindRecord(string missionId)
        {
            EnsureLoaded();
            return missionId != null && _records.TryGetValue(missionId, out MissionRecord record) ? record : null;
        }

        private MissionRecord GetOrCreateRecord(string missionId)
        {
            MissionRecord record = FindRecord(missionId);
            if (record != null) return record;

            record = new MissionRecord { missionId = missionId };
            _records.Add(missionId, record);
            _data.missions.Add(record);
            return record;
        }

        private void Persist(string missionId)
        {
            _saves.Save(SaveKey, _data);
            _events.Publish(new ProgressChangedEvent(missionId));
        }
    }
}
