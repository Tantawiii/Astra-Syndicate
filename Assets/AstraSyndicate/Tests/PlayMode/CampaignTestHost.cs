using System;
using System.Collections;
using AstraSyndicate.App;
using AstraSyndicate.Campaign;
using AstraSyndicate.Core;
using AstraSyndicate.Save;
using AstraSyndicate.SceneFlow;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AstraSyndicate.Tests
{
    /// <summary>
    /// An isolated campaign for play mode tests: the real chapters and scenes, but in-memory saves and instant
    /// transitions. Its services replace the bootstrapped ones, so scene components use this graph.
    /// </summary>
    internal sealed class CampaignTestHost : IDisposable
    {
        // Generous: a scene load takes well under a second; this only stops a broken load from hanging the run.
        private const float NavigationTimeoutSeconds = 30f;

        private readonly GameObject _root;

        public CampaignTestHost()
        {
            var config = Resources.Load<GameConfig>(GameConfig.ResourcesPath);
            Assert.IsNotNull(config, "Run Tools > Astra Syndicate > Setup Sprint 1 Content first.");

            Events = new EventBus();
            var saves = new SaveService(new JsonSaveSerializer(), new InMemorySaveStorage(), new PassthroughDataProtector(), Events);
            Campaign = new CampaignRepository(config.Catalog.CreateSources());
            Progress = new ProgressService(saves, Campaign, Events);
            Missions = new MissionStateManager(Events, ObjectiveFactory.CreateDefault());

            _root = new GameObject("CampaignTestHost");
            Object.DontDestroyOnLoad(_root);
            _root.AddComponent<MissionRunner>().Initialize(Missions, Events);
            var transition = new GameObject("Transition").AddComponent<FadeTransitionView>();
            transition.transform.SetParent(_root.transform, false);
            transition.FadeDuration = 0f;
            var scenes = new SceneLoader(transition, Events);
            Director = new CampaignDirector(Campaign, Progress, Missions, scenes, Events, config.MainMenuScene);

            Services.Register<IEventBus>(Events);
            Services.Register<ISaveService>(saves);
            Services.Register<ICampaignRepository>(Campaign);
            Services.Register<IProgressService>(Progress);
            Services.Register<IMissionStateManager>(Missions);
            Services.Register<ISceneLoader>(scenes);
            Services.Register<ICampaignFlow>(Director);
        }

        public EventBus Events { get; }

        public CampaignRepository Campaign { get; }

        public ProgressService Progress { get; }

        public MissionStateManager Missions { get; }

        public CampaignDirector Director { get; }

        /// <summary>Records a completed mission, e.g. to unlock the ones after it.</summary>
        public void Complete(string missionId)
        {
            Progress.RecordResult(new MissionResult(missionId, true, 30f, 0, 0, null));
        }

        public void Dispose()
        {
            Missions.Exit();
            Director.Dispose();
            Object.Destroy(_root);
            Services.Clear();
            Time.timeScale = 1f;
        }

        /// <summary>Waits for a campaign navigation call and checks whether it was accepted.</summary>
        public static IEnumerator Await(Awaitable<bool> operation, bool expected)
        {
            var awaiter = operation.GetAwaiter();
            float end = Time.realtimeSinceStartup + NavigationTimeoutSeconds;
            while (!awaiter.IsCompleted)
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail($"Campaign navigation did not finish within {NavigationTimeoutSeconds} s.");
                yield return null;
            }
            Assert.AreEqual(expected, awaiter.GetResult());
        }
    }
}
