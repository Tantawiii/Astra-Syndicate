using System.Collections;
using AstraSyndicate.App;
using AstraSyndicate.Campaign;
using AstraSyndicate.Core;
using AstraSyndicate.Save;
using AstraSyndicate.SceneFlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AstraSyndicate.Tests
{
    /// <summary>
    /// End-to-end check of the offline campaign loop with the real chapter content and scenes. Saves go
    /// to memory, so the test never touches the player's progress on disk.
    /// </summary>
    public class CampaignFlowPlayModeTests
    {
        private GameObject _host;
        private EventBus _events;
        private ProgressService _progress;
        private MissionStateManager _missions;
        private CampaignDirector _director;

        [SetUp]
        public void SetUp()
        {
            var config = Resources.Load<GameConfig>(GameConfig.ResourcesPath);
            Assert.IsNotNull(config, "Run Tools > Astra Syndicate > Setup Sprint 1 Content first.");

            _events = new EventBus();
            var saves = new SaveService(new JsonSaveSerializer(), new InMemorySaveStorage(), new PassthroughDataProtector(), _events);
            var campaign = new CampaignRepository(config.Catalog.CreateSources());
            _progress = new ProgressService(saves, campaign, _events);
            _missions = new MissionStateManager(_events, ObjectiveFactory.CreateDefault());

            _host = new GameObject("CampaignFlowTestHost");
            Object.DontDestroyOnLoad(_host);
            _host.AddComponent<MissionRunner>().Initialize(_missions, _events);
            var transition = new GameObject("Transition").AddComponent<FadeTransitionView>();
            transition.transform.SetParent(_host.transform, false);
            transition.FadeDuration = 0f;
            var scenes = new SceneLoader(transition, _events);
            _director = new CampaignDirector(campaign, _progress, _missions, scenes, _events, config.MainMenuScene);

            // Replace the bootstrapped services so the scene components use this isolated graph.
            Services.Register<IEventBus>(_events);
            Services.Register<ISaveService>(saves);
            Services.Register<ICampaignRepository>(campaign);
            Services.Register<IProgressService>(_progress);
            Services.Register<IMissionStateManager>(_missions);
            Services.Register<ISceneLoader>(scenes);
            Services.Register<ICampaignFlow>(_director);
        }

        [TearDown]
        public void TearDown()
        {
            _missions.Exit();
            _director.Dispose();
            Object.Destroy(_host);
            Services.Clear();
        }

        [UnityTest]
        public IEnumerator CampaignLoop_PlaysMissionRecordsProgressAndContinues()
        {
            yield return Await(_director.StartMissionAsync("ch01_m01"), expected: true);
            yield return null; // MissionSceneController.Start

            Assert.AreEqual("Ch01_M01", SceneManager.GetActiveScene().name);
            Assert.AreEqual(MissionState.Briefing, _missions.State);
            Assert.AreEqual("ch01_m01", _missions.CurrentMission.id);

            Assert.IsTrue(_missions.Launch());
            for (int i = 0; i < 5; i++)
            {
                _events.Publish(new UnitDestroyedEvent($"scout_{i}", Faction.Enemy));
            }

            Assert.AreEqual(MissionState.Completed, _missions.State);
            Assert.IsTrue(_progress.IsMissionCompleted("ch01_m01"));
            Assert.AreEqual(1, _progress.GetRecord("ch01_m01").attempts);
            Assert.IsTrue(_progress.IsMissionUnlocked("ch01_m02"));

            yield return Await(_director.NextMissionAsync(), expected: true);
            yield return null;

            Assert.AreEqual("Ch01_M02", SceneManager.GetActiveScene().name);
            Assert.AreEqual(MissionState.Briefing, _missions.State);
            Assert.AreEqual("ch01_m02", _missions.CurrentMission.id);

            yield return Await(_director.ReturnToMenuAsync(), expected: true);

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            Assert.AreEqual(MissionState.Idle, _missions.State);
        }

        [UnityTest]
        public IEnumerator Pause_FreezesTimeAndQuittingRestoresIt()
        {
            yield return Await(_director.StartMissionAsync("ch01_m01"), expected: true);
            yield return null;
            _missions.Launch();

            Assert.IsTrue(_missions.Pause());
            Assert.AreEqual(0f, Time.timeScale);

            yield return Await(_director.ReturnToMenuAsync(), expected: true);

            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(MissionState.Idle, _missions.State);
        }

        [UnityTest]
        public IEnumerator LockedMission_IsNotStarted()
        {
            string sceneBefore = SceneManager.GetActiveScene().name;

            yield return Await(_director.StartMissionAsync("ch01_m03"), expected: false);

            Assert.AreEqual(sceneBefore, SceneManager.GetActiveScene().name);
            Assert.AreEqual(MissionState.Idle, _missions.State);
        }

        private static IEnumerator Await(Awaitable<bool> operation, bool expected)
        {
            var awaiter = operation.GetAwaiter();
            while (!awaiter.IsCompleted)
            {
                yield return null;
            }
            Assert.AreEqual(expected, awaiter.GetResult());
        }
    }
}
