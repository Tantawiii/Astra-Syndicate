using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AstraSyndicate.AI;
using AstraSyndicate.Campaign;
using AstraSyndicate.Combat;
using AstraSyndicate.Core;
using AstraSyndicate.PlayerControl;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static AstraSyndicate.Tests.CampaignTestHost;
using Object = UnityEngine.Object;

namespace AstraSyndicate.Tests
{
    /// <summary>
    /// DEV-03: the Chapter 1 missions played through their real scenes, with real ships, encounters and
    /// waypoints feeding the mission objectives through events.
    /// </summary>
    public class Chapter1PlayModeTests
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
        public IEnumerator FirstContact_DestroyingTheScoutWing_CompletesTheMission()
        {
            yield return StartMission("ch01_m01");
            var flagship = Object.FindAnyObjectByType<PlayerFlightController>();
            Assert.IsNotNull(flagship, "Mission scenes need the player's flagship.");
            Assert.AreEqual(0f, Time.timeScale, "The world waits for launch during the briefing.");
            Assert.IsFalse(flagship.InputEnabled, "Flight controls stay locked during the briefing.");
            Assert.IsNull(Object.FindAnyObjectByType<EnemyWaveSpawner>(), "Encounters stay inactive until launch.");

            _host.Missions.Launch();
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(flagship.InputEnabled);

            var spawner = Object.FindAnyObjectByType<EnemyWaveSpawner>();
            Assert.IsNotNull(spawner, "The scout wing activates on launch.");
            yield return WaitUntil(() => spawner.Wave >= 1, 6f);
            Assert.AreEqual(5, spawner.AliveCount, "The scout wing has five raiders.");

            foreach (Ship raider in AliveHostiles())
            {
                raider.ApplyDamage(new DamageInfo(raider.MaxHull, raider.Position, flagship.Ship));
            }
            yield return null;

            Assert.AreEqual(MissionState.Completed, _host.Missions.State);
            Assert.IsTrue(_host.Progress.IsMissionCompleted("ch01_m01"));
            Assert.IsTrue(_host.Progress.IsMissionUnlocked("ch01_m02"));
            Assert.AreEqual(0f, Time.timeScale, "The world stops behind the results screen.");
            Assert.IsFalse(flagship.InputEnabled, "The cursor is handed back for the results screen.");
        }

        [UnityTest]
        public IEnumerator LosingTheFlagship_FailsTheMission()
        {
            yield return StartMission("ch01_m01");
            _host.Missions.Launch();
            yield return null;

            var flagship = Object.FindAnyObjectByType<PlayerFlightController>();
            flagship.Ship.ApplyDamage(new DamageInfo(100000f, flagship.transform.position, null));
            yield return null;

            Assert.AreEqual(MissionState.Failed, _host.Missions.State);
            Assert.AreEqual("Flagship destroyed", _host.Missions.LastResult?.FailureReason);
            Assert.IsFalse(_host.Progress.IsMissionCompleted("ch01_m01"));
        }

        [UnityTest]
        public IEnumerator HoldTheLine_RaiderWavesAttackAfterLaunch()
        {
            _host.Complete("ch01_m01");
            yield return StartMission("ch01_m02");
            _host.Missions.Launch();
            yield return null;

            var spawner = Object.FindAnyObjectByType<EnemyWaveSpawner>();
            Assert.IsNotNull(spawner, "The strike group activates on launch.");
            yield return WaitUntil(() => spawner.Wave >= 1, 6f);

            Assert.AreEqual(3, spawner.AliveCount, "The first wave has three raiders.");
            Assert.AreEqual(MissionState.Running, _host.Missions.State, "Holding the line takes 60 seconds.");
        }

        [UnityTest]
        public IEnumerator IntoTheRift_FlyingThroughTheRiftGate_CompletesTheMission()
        {
            _host.Complete("ch01_m01");
            _host.Complete("ch01_m02");
            yield return StartMission("ch01_m03");
            _host.Missions.Launch();
            yield return null;

            WaypointTrigger gate = ActiveGate("rift_gate");
            Assert.IsNotNull(gate, "The rift gate activates on launch.");
            var flagship = Object.FindAnyObjectByType<PlayerFlightController>();
            flagship.GetComponent<Rigidbody>().position = gate.transform.position;
            yield return WaitUntil(() => _host.Missions.State == MissionState.Completed, 3f);

            Assert.AreEqual(MissionState.Completed, _host.Missions.State);
            Assert.IsTrue(_host.Progress.IsChapterCompleted("ch01"));
            Assert.IsTrue(_host.Progress.IsChapterUnlocked("ch02"));
        }

        [UnityTest]
        public IEnumerator SharedScene_ActivatesOnlyThePlayedMissionsContent()
        {
            _host.Complete("ch01_m01");
            _host.Complete("ch01_m02");
            _host.Complete("ch01_m03");
            yield return StartMission("ch02_m01");
            Assert.AreEqual("Ch01_M03", SceneManager.GetActiveScene().name, "Chapter 2's placeholder reuses the rift scene.");

            _host.Missions.Launch();
            yield return null;

            Assert.IsNotNull(ActiveGate("beacon_drop"), "The beacon belongs to the mission being played.");
            Assert.IsNull(ActiveGate("rift_gate"), "Chapter 1's rift gate must stay hidden.");
        }

        private IEnumerator StartMission(string missionId)
        {
            yield return Await(_host.Director.StartMissionAsync(missionId), expected: true);
            yield return null; // MissionSceneController.Start begins the briefing.

            Assert.AreEqual(MissionState.Briefing, _host.Missions.State);
            Assert.AreEqual(missionId, _host.Missions.CurrentMission.id);
        }

        private static List<Ship> AliveHostiles()
        {
            return ShipRegistry.All.Where(ship => ship.IsAlive && ship.Faction == Faction.Enemy).ToList();
        }

        private static WaypointTrigger ActiveGate(string waypointId)
        {
            return Object.FindObjectsByType<WaypointTrigger>(FindObjectsSortMode.None).FirstOrDefault(gate => gate.WaypointId == waypointId);
        }

        // Real time: mission results freeze game time, so a Time.time timeout would never expire.
        private static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds)
        {
            float end = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        }
    }
}
