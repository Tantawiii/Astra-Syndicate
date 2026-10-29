using System.Collections.Generic;
using System.Text.RegularExpressions;
using AstraSyndicate.Campaign;
using AstraSyndicate.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static AstraSyndicate.Tests.TestCampaigns;

namespace AstraSyndicate.Tests
{
    public class MissionStateManagerTests
    {
        private readonly List<MissionEndedEvent> _ended = new List<MissionEndedEvent>();
        private readonly List<MissionState> _states = new List<MissionState>();
        private EventBus _events;
        private MissionStateManager _manager;

        [SetUp]
        public void SetUp()
        {
            _ended.Clear();
            _states.Clear();
            _events = new EventBus();
            _events.Subscribe<MissionEndedEvent>(e => _ended.Add(e));
            _events.Subscribe<MissionStateChangedEvent>(e => _states.Add(e.Current));
            _manager = new MissionStateManager(_events, ObjectiveFactory.CreateDefault());
        }

        [Test]
        public void BeginMission_EntersBriefingWithObjectives()
        {
            Assert.IsTrue(_manager.BeginMission(Mission("m1", Destroy("a", 1), Survive("b", 5f))));

            Assert.AreEqual(MissionState.Briefing, _manager.State);
            Assert.AreEqual("m1", _manager.CurrentMission.id);
            Assert.AreEqual(2, _manager.Objectives.Count);
            Assert.AreEqual(ObjectiveStatus.Inactive, _manager.Objectives[0].Status);
        }

        [Test]
        public void Launch_RequiresBriefing()
        {
            Assert.IsFalse(_manager.Launch());
            Assert.AreEqual(MissionState.Idle, _manager.State);
        }

        [Test]
        public void BeginMission_WhileAnotherIsLoaded_IsRejected()
        {
            _manager.BeginMission(Mission("m1", Destroy("o", 1)));

            Assert.IsFalse(_manager.BeginMission(Mission("m2", Destroy("o", 1))));
            Assert.AreEqual("m1", _manager.CurrentMission.id);
        }

        [Test]
        public void DestroyObjective_CompletesMissionAfterRequiredKills()
        {
            StartRunning(Mission("m1", Destroy("o", 3)));

            Kill();
            Kill();
            Assert.AreEqual(MissionState.Running, _manager.State);
            Assert.AreEqual("2/3", _manager.Objectives[0].ProgressText);

            Kill();
            Assert.AreEqual(MissionState.Completed, _manager.State);
            Assert.AreEqual(1, _ended.Count);
            Assert.IsTrue(_ended[0].Result.Succeeded);
            Assert.AreEqual("m1", _ended[0].Result.MissionId);
        }

        [Test]
        public void DestroyObjective_IgnoresNonEnemyKills()
        {
            StartRunning(Mission("m1", Destroy("o", 1)));

            Kill(Faction.Player);
            Kill(Faction.Neutral);

            Assert.AreEqual(MissionState.Running, _manager.State);
        }

        [Test]
        public void DestroyObjective_WithTag_OnlyCountsMatchingUnits()
        {
            StartRunning(Mission("m1", Destroy("o", 1, tag: "station")));

            Kill(tag: "raider");
            Assert.AreEqual(MissionState.Running, _manager.State);

            Kill(tag: "station");
            Assert.AreEqual(MissionState.Completed, _manager.State);
        }

        [Test]
        public void KillsDuringBriefing_DoNotCount()
        {
            _manager.BeginMission(Mission("m1", Destroy("o", 1)));
            Kill();

            _manager.Launch();

            Assert.AreEqual(MissionState.Running, _manager.State);
        }

        [Test]
        public void SurviveObjective_CompletesAfterDuration()
        {
            StartRunning(Mission("m1", Survive("o", 5f)));

            _manager.Tick(4f);
            Assert.AreEqual(MissionState.Running, _manager.State);

            _manager.Tick(1f);
            Assert.AreEqual(MissionState.Completed, _manager.State);
            Assert.AreEqual(5f, _ended[0].Result.ElapsedSeconds, 0.0001f);
        }

        [Test]
        public void ReachWaypoint_CompletesOnlyOnMatchingWaypoint()
        {
            StartRunning(Mission("m1", Reach("o", "gate")));

            _events.Publish(new WaypointReachedEvent("elsewhere"));
            Assert.AreEqual(MissionState.Running, _manager.State);

            _events.Publish(new WaypointReachedEvent("gate"));
            Assert.AreEqual(MissionState.Completed, _manager.State);
        }

        [Test]
        public void TimeLimit_FailsMission()
        {
            MissionData mission = Mission("m1", Destroy("o", 1));
            mission.timeLimitSeconds = 10f;
            StartRunning(mission);

            _manager.Tick(9f);
            Assert.AreEqual(1f, _manager.TimeRemaining.Value, 0.0001f);
            Assert.AreEqual(MissionState.Running, _manager.State);

            _manager.Tick(1.5f);
            Assert.AreEqual(MissionState.Failed, _manager.State);
            Assert.IsFalse(_ended[0].Result.Succeeded);
            Assert.IsFalse(string.IsNullOrEmpty(_ended[0].Result.FailureReason));
        }

        [Test]
        public void MissionWithoutTimeLimit_HasNoTimeRemaining()
        {
            StartRunning(Mission("m1", Destroy("o", 1)));

            Assert.IsNull(_manager.TimeRemaining);
        }

        [Test]
        public void PlayerDefeated_FailsMissionWithReason()
        {
            StartRunning(Mission("m1", Destroy("o", 1)));

            _events.Publish(new PlayerDefeatedEvent("Flagship lost"));

            Assert.AreEqual(MissionState.Failed, _manager.State);
            Assert.AreEqual("Flagship lost", _ended[0].Result.FailureReason);
        }

        [Test]
        public void Pause_FreezesMissionTimeUntilResumed()
        {
            StartRunning(Mission("m1", Survive("o", 5f)));
            _manager.Tick(2f);

            Assert.IsTrue(_manager.Pause());
            _manager.Tick(10f);
            Assert.AreEqual(MissionState.Paused, _manager.State);
            Assert.AreEqual(2f, _manager.ElapsedSeconds, 0.0001f);

            Assert.IsTrue(_manager.Resume());
            _manager.Tick(3f);
            Assert.AreEqual(MissionState.Completed, _manager.State);
        }

        [Test]
        public void OptionalObjectives_DoNotBlockCompletionAndAreCounted()
        {
            StartRunning(Mission("m1", Destroy("main", 3), Destroy("bonus", 2, optional: true)));

            Kill();
            Kill();
            Assert.AreEqual(MissionState.Running, _manager.State, "Completing only the optional objective must not end the mission.");

            Kill();
            Assert.AreEqual(MissionState.Completed, _manager.State);
            Assert.AreEqual(1, _ended[0].Result.OptionalObjectivesCompleted);
            Assert.AreEqual(1, _ended[0].Result.OptionalObjectivesTotal);
        }

        [Test]
        public void Exit_ReturnsToIdleAndStopsListening()
        {
            StartRunning(Mission("m1", Destroy("o", 1)));

            _manager.Exit();
            Kill();

            Assert.AreEqual(MissionState.Idle, _manager.State);
            Assert.IsNull(_manager.CurrentMission);
            Assert.AreEqual(0, _ended.Count);
        }

        [Test]
        public void Exit_AfterCompletion_AllowsNextMission()
        {
            StartRunning(Mission("m1", Destroy("o", 1)));
            Kill();

            _manager.Exit();

            Assert.IsTrue(_manager.BeginMission(Mission("m2", Destroy("o", 1))));
            Assert.IsTrue(_manager.LastResult == null, "A new mission starts without a result.");
        }

        [Test]
        public void Pause_DuringBriefing_IsRejected()
        {
            _manager.BeginMission(Mission("m1", Destroy("o", 1)));

            Assert.IsFalse(_manager.Pause());
            Assert.AreEqual(MissionState.Briefing, _manager.State);
        }

        [Test]
        public void UnknownObjectiveType_IsSkippedWithError()
        {
            ObjectiveData unknown = Destroy("bad", 1);
            unknown.type = "teleport";

            LogAssert.Expect(LogType.Error, new Regex("teleport"));
            _manager.BeginMission(Mission("m1", Destroy("o", 1), unknown));

            Assert.AreEqual(1, _manager.Objectives.Count);
        }

        [Test]
        public void StateChanges_ArePublishedInOrder()
        {
            StartRunning(Mission("m1", Destroy("o", 1)));
            _manager.Pause();
            _manager.Resume();
            Kill();
            _manager.Exit();

            CollectionAssert.AreEqual(
                new[] { MissionState.Briefing, MissionState.Running, MissionState.Paused, MissionState.Running, MissionState.Completed, MissionState.Idle },
                _states);
        }

        private void StartRunning(MissionData mission)
        {
            Assert.IsTrue(_manager.BeginMission(mission));
            Assert.IsTrue(_manager.Launch());
        }

        private void Kill(Faction faction = Faction.Enemy, string tag = null)
        {
            _events.Publish(new UnitDestroyedEvent("unit", faction, tag));
        }
    }
}
