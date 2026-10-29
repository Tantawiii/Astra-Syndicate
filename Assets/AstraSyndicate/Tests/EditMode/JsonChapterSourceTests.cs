using AstraSyndicate.Campaign;
using NUnit.Framework;

namespace AstraSyndicate.Tests
{
    public class JsonChapterSourceTests
    {
        private const string ValidJson = @"{
            ""id"": ""ch09"",
            ""title"": ""Test Chapter"",
            ""order"": 9,
            ""missions"": [
                {
                    ""id"": ""ch09_m01"",
                    ""title"": ""Test Mission"",
                    ""sceneName"": ""Ch01_M01"",
                    ""timeLimitSeconds"": 120,
                    ""objectives"": [
                        { ""id"": ""kill"", ""type"": ""destroy_enemies"", ""description"": ""Kill"", ""targetCount"": 3 },
                        { ""id"": ""bonus"", ""type"": ""survive"", ""durationSeconds"": 30, ""optional"": true }
                    ]
                }
            ]
        }";

        [Test]
        public void TryParse_ValidJson_ReadsChapterMissionsAndObjectives()
        {
            Assert.IsTrue(JsonChapterSource.TryParse(ValidJson, "test.json", out ChapterData chapter, out string error), error);

            Assert.AreEqual("ch09", chapter.id);
            Assert.AreEqual(9, chapter.order);
            MissionData mission = chapter.missions[0];
            Assert.AreEqual("ch09_m01", mission.id);
            Assert.AreEqual("Ch01_M01", mission.sceneName);
            Assert.AreEqual(120f, mission.timeLimitSeconds);
            Assert.AreEqual(2, mission.objectives.Count);
            Assert.AreEqual(ObjectiveTypes.DestroyEnemies, mission.objectives[0].type);
            Assert.AreEqual(3, mission.objectives[0].targetCount);
            Assert.IsTrue(mission.objectives[1].optional);
        }

        [Test]
        public void TryParse_MissingLists_AreNormalizedToEmpty()
        {
            Assert.IsTrue(JsonChapterSource.TryParse("{\"id\":\"c\"}", "test.json", out ChapterData chapter, out _));

            Assert.IsNotNull(chapter.missions);
            Assert.AreEqual(0, chapter.missions.Count);
        }

        [Test]
        public void TryParse_TrimsIdentifiers()
        {
            Assert.IsTrue(JsonChapterSource.TryParse("{\"id\":\"  c1 \",\"missions\":[{\"id\":\" m1\",\"sceneName\":\"S \"}]}", "test.json", out ChapterData chapter, out _));

            Assert.AreEqual("c1", chapter.id);
            Assert.AreEqual("m1", chapter.missions[0].id);
            Assert.AreEqual("S", chapter.missions[0].sceneName);
        }

        [Test]
        public void TryParse_InvalidJson_ReturnsFalseWithError()
        {
            Assert.IsFalse(JsonChapterSource.TryParse("{ not json", "broken.json", out ChapterData chapter, out string error));

            Assert.IsNull(chapter);
            StringAssert.Contains("broken.json", error);
        }

        [Test]
        public void TryParse_EmptyText_ReturnsFalse()
        {
            Assert.IsFalse(JsonChapterSource.TryParse("  ", "empty.json", out _, out string error));
            StringAssert.Contains("empty.json", error);
        }

        [Test]
        public void TryParse_MissingId_ReturnsFalse()
        {
            Assert.IsFalse(JsonChapterSource.TryParse("{\"title\":\"No id\"}", "noid.json", out ChapterData chapter, out string error));

            Assert.IsNull(chapter);
            StringAssert.Contains("id", error);
        }

        [Test]
        public void LoadChapters_ReportsBadDocumentsAndKeepsGoodOnes()
        {
            var source = new JsonChapterSource(new[]
            {
                new JsonChapterSource.Document("good.json", ValidJson),
                new JsonChapterSource.Document("bad.json", "nope"),
            });
            var report = new ValidationReport();

            var chapters = source.LoadChapters(report);

            Assert.AreEqual(1, chapters.Count);
            Assert.AreEqual(1, report.Errors.Count);
            StringAssert.Contains("bad.json", report.Errors[0]);
        }
    }
}
