using AstraSyndicate.Campaign;
using NUnit.Framework;
using UnityEngine;
using static AstraSyndicate.Tests.TestCampaigns;

namespace AstraSyndicate.Tests
{
    public class CampaignRepositoryTests
    {
        [Test]
        public void Chapters_AreSortedByOrder()
        {
            CampaignRepository repository = Repository(
                Chapter("late", 5, Mission("m2", Destroy("o", 1))),
                Chapter("early", 1, Mission("m1", Destroy("o", 1))));

            Assert.AreEqual("early", repository.Chapters[0].id);
            Assert.AreEqual("m1", repository.MissionsInOrder[0].id);
            Assert.AreEqual(0, repository.GetMissionIndex("m1"));
            Assert.AreEqual(1, repository.GetMissionIndex("m2"));
        }

        [Test]
        public void GetNextMission_CrossesChapterBoundaries()
        {
            CampaignRepository repository = TwoChapters();

            Assert.AreEqual("m2", repository.GetNextMission("m1").id);
            Assert.AreEqual("m3", repository.GetNextMission("m2").id);
            Assert.IsNull(repository.GetNextMission("m3"));
            Assert.IsNull(repository.GetNextMission("unknown"));
            Assert.AreEqual("c2", repository.GetChapterOfMission("m3").id);
        }

        [Test]
        public void UnknownIds_ReturnNullOrMinusOne()
        {
            CampaignRepository repository = TwoChapters();

            Assert.IsNull(repository.GetMission(null));
            Assert.IsNull(repository.GetChapter("nope"));
            Assert.IsNull(repository.GetChapterOfMission(null));
            Assert.AreEqual(-1, repository.GetMissionIndex("nope"));
        }

        [Test]
        public void LaterSource_ReplacesChapterWithSameId()
        {
            var assets = new StubChapterSource("assets", Chapter("c1", 1, Mission("old", Destroy("o", 1))));
            var json = new StubChapterSource("json", Chapter("c1", 1, Mission("new", Destroy("o", 1))));
            var report = new ValidationReport();

            var repository = new CampaignRepository(new IChapterSource[] { assets, json }, report);

            Assert.AreEqual(1, repository.Chapters.Count);
            Assert.IsNotNull(repository.GetMission("new"));
            Assert.IsNull(repository.GetMission("old"));
            Assert.IsFalse(report.HasErrors);
            Assert.AreEqual(1, report.Notes.Count);
        }

        [Test]
        public void DuplicateMissionIds_AreReportedAndSkipped()
        {
            var report = new ValidationReport();
            var repository = new CampaignRepository(new IChapterSource[]
            {
                new StubChapterSource("test",
                    Chapter("c1", 1, Mission("m1", Destroy("o", 1))),
                    Chapter("c2", 2, Mission("m1", Destroy("o", 1)))),
            }, report);

            Assert.AreEqual(1, repository.MissionsInOrder.Count);
            Assert.AreEqual("c1", repository.GetChapterOfMission("m1").id);
            Assert.AreEqual(0, repository.GetChapter("c2").missions.Count);
            Assert.IsTrue(report.HasErrors);
        }

        [Test]
        public void ChapterWithoutId_IsReportedAndSkipped()
        {
            var report = new ValidationReport();
            var repository = new CampaignRepository(new IChapterSource[]
            {
                new StubChapterSource("test", Chapter("", 1, Mission("m1", Destroy("o", 1)))),
            }, report);

            Assert.AreEqual(0, repository.Chapters.Count);
            Assert.IsTrue(report.HasErrors);
        }

        [Test]
        public void ScriptableObjectSource_HandsOutCopiesOfTheAssetData()
        {
            var config = ScriptableObject.CreateInstance<ChapterConfig>();
            try
            {
                config.Chapter.id = "c1";
                config.Chapter.missions.Add(Mission("m1", Destroy("o", 1)));

                var chapters = new ScriptableObjectChapterSource(new[] { config }).LoadChapters(new ValidationReport());
                chapters[0].missions[0].title = "changed at runtime";

                Assert.AreEqual("m1", config.Chapter.missions[0].title);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
