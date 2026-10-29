using AstraSyndicate.Campaign;
using AstraSyndicate.Core;
using AstraSyndicate.Save;
using AstraSyndicate.SceneFlow;
using UnityEngine;

namespace AstraSyndicate.App
{
    /// <summary>
    /// Composition root. Runs before the first scene loads (whichever scene that is, so any scene can
    /// be played directly), builds every service with its dependencies and registers them for
    /// MonoBehaviours to resolve.
    /// </summary>
    public static class GameBootstrapper
    {
        public static bool IsInitialized { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            IsInitialized = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (IsInitialized) return;

            var config = Resources.Load<GameConfig>(GameConfig.ResourcesPath);
            if (config == null)
            {
                Debug.LogWarning($"[Astra] No GameConfig at Resources/{GameConfig.ResourcesPath}, so the game was not bootstrapped. Run Tools > Astra Syndicate > Setup Sprint 1 Content.");
                return;
            }
            if (config.Catalog == null)
            {
                Debug.LogError("[Astra] GameConfig has no Campaign Catalog assigned, so the game was not bootstrapped.", config);
                return;
            }

            var events = new EventBus();

            ISaveDataProtector protector = config.SaveProtector != null
                ? config.SaveProtector.CreateProtector()
                : new PassthroughDataProtector();
            var saves = new SaveService(new JsonSaveSerializer(), new FileSaveStorage(config.SaveDirectory), protector, events);

            ObjectiveFactory objectiveFactory = ObjectiveFactory.CreateDefault();
            var campaign = new CampaignRepository(config.Catalog.CreateSources());
            ValidationReport report = campaign.Report;
            report.Merge(CampaignValidator.Validate(campaign, objectiveFactory, Application.CanStreamedLevelBeLoaded));
            report.Log("[Campaign]", config.Catalog);

            var progress = new ProgressService(saves, campaign, events);
            var missions = new MissionStateManager(events, objectiveFactory);

            var root = new GameObject("[Astra Syndicate]");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<MissionRunner>().Initialize(missions, events);

            var transition = new GameObject("SceneTransition").AddComponent<FadeTransitionView>();
            transition.transform.SetParent(root.transform, false);
            transition.FadeDuration = config.TransitionFadeSeconds;
            var scenes = new SceneLoader(transition, events);

            var director = new CampaignDirector(campaign, progress, missions, scenes, events, config.MainMenuScene);

            Services.Register<IEventBus>(events);
            Services.Register<ISaveService>(saves);
            Services.Register<ICampaignRepository>(campaign);
            Services.Register<IProgressService>(progress);
            Services.Register<IMissionStateManager>(missions);
            Services.Register<ISceneLoader>(scenes);
            Services.Register<ICampaignFlow>(director);

            IsInitialized = true;
            Debug.Log($"[Astra] Ready: {campaign.Chapters.Count} chapters, {campaign.MissionsInOrder.Count} missions. Saves: {config.SaveDirectory}");
        }
    }
}
