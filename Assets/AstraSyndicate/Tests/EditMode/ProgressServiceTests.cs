using System.Collections.Generic;
using AstraSyndicate.Campaign;
using AstraSyndicate.Core;
using AstraSyndicate.Save;
using NUnit.Framework;

namespace AstraSyndicate.Tests
{
    public class ProgressServiceTests
    {
        private InMemorySaveStorage _storage;
        private EventBus _events;
        private CampaignRepository _campaign;
        private ProgressService _progress;

        [SetUp]
        public void SetUp()
        {
            _storage = new InMemorySaveStorage();
            _events = new EventBus();
            _campaign = TestCampaigns.TwoChapters();
            _progress = CreateProgress();
        }

        [Test]
        public void NewCampaign_OnlyFirstMissionIsUnlocked()
        {
            Assert.IsTrue(_progress.IsMissionUnlocked("m1"));
            Assert.IsFalse(_progress.IsMissionUnlocked("m2"));
            Assert.IsFalse(_progress.IsMissionUnlocked("m3"));
            Assert.IsTrue(_progress.IsChapterUnlocked("c1"));
            Assert.IsFalse(_progress.IsChapterUnlocked("c2"));
            Assert.IsFalse(_progress.HasProgress);
        }

        [Test]
        public void CompletingMission_UnlocksNextAndPublishesEvent()
        {
            var unlocked = new List<string>();
            _events.Subscribe<MissionUnlockedEvent>(e => unlocked.Add(e.MissionId));

            _progress.RecordResult(Win("m1"));

            Assert.IsTrue(_progress.IsMissionCompleted("m1"));
            Assert.IsTrue(_progress.IsMissionUnlocked("m2"));
            CollectionAssert.AreEqual(new[] { "m2" }, unlocked);
        }

        [Test]
        public void CompletingLastMissionOfChapter_UnlocksNextChapter()
        {
            var chapters = new List<string>();
            _events.Subscribe<ChapterUnlockedEvent>(e => chapters.Add(e.ChapterId));

            _progress.RecordResult(Win("m1"));
            _progress.RecordResult(Win("m2"));

            Assert.IsTrue(_progress.IsChapterCompleted("c1"));
            Assert.IsTrue(_progress.IsChapterUnlocked("c2"));
            CollectionAssert.AreEqual(new[] { "c2" }, chapters);
        }

        [Test]
        public void ReplayingCompletedMission_DoesNotRepeatUnlockEvents()
        {
            var unlocked = new List<string>();
            _events.Subscribe<MissionUnlockedEvent>(e => unlocked.Add(e.MissionId));

            _progress.RecordResult(Win("m1"));
            _progress.RecordResult(Win("m1"));

            Assert.AreEqual(1, unlocked.Count);
        }

        [Test]
        public void FailedMission_DoesNotUnlockNext()
        {
            _progress.RecordResult(Loss("m1"));

            Assert.IsFalse(_progress.IsMissionCompleted("m1"));
            Assert.IsFalse(_progress.IsMissionUnlocked("m2"));
        }

        [Test]
        public void BestTime_KeepsFastestCompletion()
        {
            _progress.RecordResult(Win("m1", 50f));
            _progress.RecordResult(Win("m1", 30f));
            _progress.RecordResult(Win("m1", 40f));

            MissionRecord record = _progress.GetRecord("m1");
            Assert.AreEqual(30f, record.bestTimeSeconds);
            Assert.AreEqual(3, record.completions);
        }

        [Test]
        public void MarkStarted_CountsAttemptsAndSetsContinueMission()
        {
            _progress.RecordResult(Win("m1"));
            _progress.MarkStarted("m1");
            _progress.MarkStarted("m1");

            Assert.AreEqual(2, _progress.GetRecord("m1").attempts);
            Assert.IsTrue(_progress.HasProgress);
            Assert.AreEqual("m2", _progress.GetContinueMission().id, "A completed last mission should continue with the next unlocked one.");
        }

        [Test]
        public void GetRecord_ReturnsACopy()
        {
            _progress.RecordResult(Win("m1"));

            _progress.GetRecord("m1").completed = false;

            Assert.IsTrue(_progress.IsMissionCompleted("m1"));
        }

        [Test]
        public void Progress_PersistsAcrossServiceInstances()
        {
            _progress.RecordResult(Win("m1", 12f));

            ProgressService reloaded = CreateProgress();

            Assert.IsTrue(reloaded.IsMissionCompleted("m1"));
            Assert.IsTrue(reloaded.IsMissionUnlocked("m2"));
            Assert.AreEqual(12f, reloaded.GetRecord("m1").bestTimeSeconds);
        }

        [Test]
        public void Reset_ClearsProgressAndDeletesSave()
        {
            _progress.RecordResult(Win("m1"));

            _progress.Reset();

            Assert.IsFalse(_progress.IsMissionCompleted("m1"));
            Assert.IsFalse(_progress.IsMissionUnlocked("m2"));
            Assert.IsFalse(_storage.Exists(ProgressService.SaveKey));
            Assert.IsFalse(CreateProgress().IsMissionCompleted("m1"));
        }

        [Test]
        public void ContinueMission_IsFirstUnlockedIncompleteMission()
        {
            Assert.AreEqual("m1", _progress.GetContinueMission().id);

            _progress.RecordResult(Win("m1"));
            _progress.RecordResult(Win("m2"));

            Assert.AreEqual("m3", _progress.GetContinueMission().id);
        }

        [Test]
        public void UnknownMission_IsNeverUnlockedOrRecorded()
        {
            _progress.RecordResult(Win("ghost"));

            Assert.IsFalse(_progress.IsMissionUnlocked("ghost"));
            Assert.IsNull(_progress.GetRecord("ghost"));
        }

        private ProgressService CreateProgress()
        {
            var saves = new SaveService(new JsonSaveSerializer(), _storage, new PassthroughDataProtector(), _events);
            return new ProgressService(saves, _campaign, _events);
        }

        private static MissionResult Win(string missionId, float seconds = 30f)
        {
            return new MissionResult(missionId, true, seconds, 0, 0, null);
        }

        private static MissionResult Loss(string missionId)
        {
            return new MissionResult(missionId, false, 10f, 0, 0, "test failure");
        }
    }
}
