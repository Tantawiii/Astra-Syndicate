using System.Collections;
using AstraSyndicate.Campaign;
using AstraSyndicate.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static AstraSyndicate.Tests.CampaignTestHost;

namespace AstraSyndicate.Tests
{
    /// <summary>
    /// End-to-end check of the offline campaign loop with the real chapter content and scenes. Saves go
    /// to memory, so the test never touches the player's progress on disk.
    /// </summary>
    public class CampaignFlowPlayModeTests
    {
        private CampaignTestHost _host;

        [SetUp]
        public void SetUp()
        {
            _host = new CampaignTestHost();
        }

        [TearDown]
        public void TearDown()
        {
            _host.Dispose();
        }

        [UnityTest]
        public IEnumerator CampaignLoop_PlaysMissionRecordsProgressAndContinues()
        {
            yield return Await(_host.Director.StartMissionAsync("ch01_m01"), expected: true);
            yield return null; // MissionSceneController.Start

            Assert.AreEqual("Ch01_M01", SceneManager.GetActiveScene().name);
            Assert.AreEqual(MissionState.Briefing, _host.Missions.State);
            Assert.AreEqual("ch01_m01", _host.Missions.CurrentMission.id);

            Assert.IsTrue(_host.Missions.Launch());
            for (int i = 0; i < 5; i++)
            {
                _host.Events.Publish(new UnitDestroyedEvent($"scout_{i}", Faction.Enemy));
            }

            Assert.AreEqual(MissionState.Completed, _host.Missions.State);
            Assert.IsTrue(_host.Progress.IsMissionCompleted("ch01_m01"));
            Assert.AreEqual(1, _host.Progress.GetRecord("ch01_m01").attempts);
            Assert.IsTrue(_host.Progress.IsMissionUnlocked("ch01_m02"));

            yield return Await(_host.Director.NextMissionAsync(), expected: true);
            yield return null;

            Assert.AreEqual("Ch01_M02", SceneManager.GetActiveScene().name);
            Assert.AreEqual(MissionState.Briefing, _host.Missions.State);
            Assert.AreEqual("ch01_m02", _host.Missions.CurrentMission.id);

            yield return Await(_host.Director.ReturnToMenuAsync(), expected: true);

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            Assert.AreEqual(MissionState.Idle, _host.Missions.State);
        }

        [UnityTest]
        public IEnumerator Pause_FreezesTimeAndQuittingRestoresIt()
        {
            yield return Await(_host.Director.StartMissionAsync("ch01_m01"), expected: true);
            yield return null;
            _host.Missions.Launch();

            Assert.IsTrue(_host.Missions.Pause());
            Assert.AreEqual(0f, Time.timeScale);

            yield return Await(_host.Director.ReturnToMenuAsync(), expected: true);

            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(MissionState.Idle, _host.Missions.State);
        }

        [UnityTest]
        public IEnumerator LockedMission_IsNotStarted()
        {
            string sceneBefore = SceneManager.GetActiveScene().name;

            yield return Await(_host.Director.StartMissionAsync("ch01_m03"), expected: false);

            Assert.AreEqual(sceneBefore, SceneManager.GetActiveScene().name);
            Assert.AreEqual(MissionState.Idle, _host.Missions.State);
        }
    }
}
