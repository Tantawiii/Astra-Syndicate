using System;
using AstraSyndicate.Campaign;
using NUnit.Framework;
using static AstraSyndicate.Tests.TestCampaigns;

namespace AstraSyndicate.Tests
{
    public class CampaignValidatorTests
    {
        [Test]
        public void ValidMission_HasNoErrors()
        {
            ValidationReport report = Validate(Mission("m1", Destroy("kill", 3), Survive("bonus", 10f, optional: true)));

            Assert.IsFalse(report.HasErrors, string.Join("\n", report.Errors));
        }

        [Test]
        public void UnknownObjectiveType_IsAnError()
        {
            ObjectiveData objective = Destroy("o", 1);
            objective.type = "teleport";

            Assert.IsTrue(Validate(Mission("m1", objective)).HasErrors);
        }

        [Test]
        public void MissingSceneName_IsAnError()
        {
            MissionData mission = Mission("m1", Destroy("o", 1));
            mission.sceneName = "";

            Assert.IsTrue(Validate(mission).HasErrors);
        }

        [Test]
        public void SceneNotInBuildSettings_IsAnError()
        {
            Assert.IsTrue(Validate(Mission("m1", Destroy("o", 1)), sceneExists: _ => false).HasErrors);
        }

        [Test]
        public void MissionWithOnlyOptionalObjectives_IsAnError()
        {
            Assert.IsTrue(Validate(Mission("m1", Destroy("o", 1, optional: true))).HasErrors);
        }

        [Test]
        public void MissionWithoutObjectives_IsAnError()
        {
            Assert.IsTrue(Validate(Mission("m1")).HasErrors);
        }

        [Test]
        public void SurviveLongerThanTimeLimit_IsAnError()
        {
            MissionData mission = Mission("m1", Survive("hold", 120f));
            mission.timeLimitSeconds = 60f;

            Assert.IsTrue(Validate(mission).HasErrors);
        }

        [Test]
        public void ReachWaypointWithoutTarget_IsAnError()
        {
            Assert.IsTrue(Validate(Mission("m1", Reach("go", ""))).HasErrors);
        }

        [Test]
        public void DestroyWithoutCount_IsAnError()
        {
            Assert.IsTrue(Validate(Mission("m1", Destroy("o", 0))).HasErrors);
        }

        [Test]
        public void DuplicateObjectiveIds_IsAnError()
        {
            Assert.IsTrue(Validate(Mission("m1", Destroy("o", 1), Destroy("o", 2))).HasErrors);
        }

        [Test]
        public void EmptyCampaign_IsAnError()
        {
            Assert.IsTrue(CampaignValidator.Validate(Repository(), ObjectiveFactory.CreateDefault()).HasErrors);
        }

        private static ValidationReport Validate(MissionData mission, Func<string, bool> sceneExists = null)
        {
            return CampaignValidator.Validate(Repository(Chapter("c1", 1, mission)), ObjectiveFactory.CreateDefault(), sceneExists);
        }
    }
}
