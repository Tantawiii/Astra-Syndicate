using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AstraSyndicate.App;
using AstraSyndicate.Campaign;
using AstraSyndicate.Security;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AstraSyndicate.EditorTools
{
    /// <summary>
    /// Generates the Sprint 1 content: combat assets, the mission scenes and the combat sandbox, chapter assets,
    /// campaign catalog, game config, the save protector and Build Settings. Safe to run again: existing assets
    /// and scenes are kept, so designer edits survive.
    /// </summary>
    public static class SprintSetup
    {
        public const string MainMenuScene = "MainMenu";

        private const string Root = "Assets/AstraSyndicate";
        private const string CampaignFolder = Root + "/Data/Campaign";
        private const string SecurityFolder = Root + "/Data/Security";
        private const string ScenesFolder = Root + "/Scenes";
        private const string ResourcesFolder = Root + "/Resources/AstraSyndicate";
        private const string Chapter01Path = CampaignFolder + "/Chapter01.asset";
        private const string Chapter02JsonPath = CampaignFolder + "/Chapter02.json";
        private const string CatalogPath = CampaignFolder + "/CampaignCatalog.asset";
        private const string SaveProtectorPath = SecurityFolder + "/SaveProtector_AES.asset";
        private const string GameConfigPath = ResourcesFolder + "/GameConfig.asset";
        private const string TemplateSampleScene = "Assets/Scenes/SampleScene.unity";

        private static readonly MissionSceneSpec[] MissionScenes =
        {
            new MissionSceneSpec("Ch01_M01", MissionSceneSetup.PopulateFirstContact),
            new MissionSceneSpec("Ch01_M02", MissionSceneSetup.PopulateHoldTheLine),
            new MissionSceneSpec("Ch01_M03", MissionSceneSetup.PopulateIntoTheRift),
        };

        [MenuItem("Tools/Astra Syndicate/Setup Sprint 1 Content", priority = 0)]
        private static void SetupFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Setup] Exit Play Mode before generating content.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Run();
            EditorSceneManager.OpenScene(ScenePath(MainMenuScene));
        }

        /// <summary>
        /// Batch-mode entry point:
        /// Unity.exe -batchmode -projectPath &lt;path&gt; -executeMethod AstraSyndicate.EditorTools.SprintSetup.RunBatch
        /// </summary>
        public static void RunBatch()
        {
            bool valid = Run();
            if (Application.isBatchMode) EditorApplication.Exit(valid ? 0 : 1);
        }

        /// <summary>Creates whatever is missing and returns true if the campaign validates.</summary>
        public static bool Run()
        {
            SetupUtility.EnsureFolder(CampaignFolder);
            SetupUtility.EnsureFolder(SecurityFolder);
            SetupUtility.EnsureFolder(ScenesFolder);
            SetupUtility.EnsureFolder(ResourcesFolder);

            // Combat assets are saved to disk immediately; the scenes below reload them by path.
            CombatContentSetup.EnsureAssets();

            // Scenes before the campaign assets: opening a new scene unloads unused assets, which would leave
            // the in-memory references between freshly created assets below pointing at unloaded objects.
            CreateSceneIfMissing(MainMenuScene, PopulateMainMenu);
            foreach (MissionSceneSpec spec in MissionScenes)
            {
                CreateOrRebuildMissionScene(spec);
            }
            CreateSceneIfMissing(CombatContentSetup.SandboxScene, CombatContentSetup.PopulateSandboxScene);
            ConfigureBuildSettings();

            ChapterConfig chapter01 = SetupUtility.LoadOrCreate(Chapter01Path, CreateChapter01);
            AssetDatabase.ImportAsset(Chapter02JsonPath);
            var chapter02Json = AssetDatabase.LoadAssetAtPath<TextAsset>(Chapter02JsonPath);
            if (chapter02Json == null) Debug.LogWarning($"[Setup] {Chapter02JsonPath} is missing; the catalog will only contain Chapter 1.");

            CampaignCatalog catalog = SetupUtility.LoadOrCreate(CatalogPath, () => CreateCatalog(chapter01, chapter02Json));
            GameConfig config = SetupUtility.LoadOrCreate(GameConfigPath, () => CreateGameConfig(catalog));
            if (config.Catalog == null) AssignCatalog(config, catalog);

            // Saves are encrypted by the reference AES protector unless another provider is already assigned.
            var protector = SetupUtility.LoadOrCreate(SaveProtectorPath, ScriptableObject.CreateInstance<AesSaveProtectorProvider>);
            if (config.SaveProtector == null)
            {
                SetupUtility.Assign(config, ("saveProtector", protector));
                EditorUtility.SetDirty(config);
                Debug.Log($"[Setup] Save files are now protected by {SaveProtectorPath}.");
            }
            AssetDatabase.SaveAssets();

            return CampaignValidation.ValidateAndLog(config.Catalog);
        }

        private static ChapterConfig CreateChapter01()
        {
            var config = ScriptableObject.CreateInstance<ChapterConfig>();
            ChapterData chapter = config.Chapter;
            chapter.id = "ch01";
            chapter.title = "Chapter 1: Frontier Watch";
            chapter.synopsis = "Placeholder story. Your fleet is posted to the Kepler relay just as Syndicate raiders start probing the frontier.";
            chapter.order = 1;

            chapter.missions.Add(Mission(
                "ch01_m01", "First Contact", "Ch01_M01", 0f,
                "Raider scouts are circling the relay. Intercept and destroy them before they report back.",
                Objective("destroy_scouts", ObjectiveTypes.DestroyEnemies, "Destroy the raider scouts", targetCount: 5)));

            chapter.missions.Add(Mission(
                "ch01_m02", "Hold the Line", "Ch01_M02", 0f,
                "A raider strike group is inbound. Keep the relay alive until reinforcements jump in.",
                Objective("defend_relay", ObjectiveTypes.Survive, "Defend the relay", durationSeconds: 60f),
                Objective("thin_the_herd", ObjectiveTypes.DestroyEnemies, "Destroy 10 raiders", targetCount: 10, optional: true)));

            chapter.missions.Add(Mission(
                "ch01_m03", "Into the Rift", "Ch01_M03", 180f,
                "The raiders fell back through the rift. Reach the rift gate before it collapses.",
                Objective("reach_gate", ObjectiveTypes.ReachWaypoint, "Reach the rift gate", targetId: "rift_gate")));

            return config;
        }

        private static MissionData Mission(string id, string title, string sceneName, float timeLimitSeconds, string briefing, params ObjectiveData[] objectives)
        {
            return new MissionData
            {
                id = id,
                title = title,
                sceneName = sceneName,
                timeLimitSeconds = timeLimitSeconds,
                briefing = briefing,
                objectives = new List<ObjectiveData>(objectives),
            };
        }

        private static ObjectiveData Objective(string id, string type, string description, int targetCount = 0, float durationSeconds = 0f, string targetId = "", bool optional = false)
        {
            return new ObjectiveData
            {
                id = id,
                type = type,
                description = description,
                targetCount = targetCount,
                durationSeconds = durationSeconds,
                targetId = targetId,
                optional = optional,
            };
        }

        private static CampaignCatalog CreateCatalog(ChapterConfig chapter01, TextAsset chapter02Json)
        {
            var catalog = ScriptableObject.CreateInstance<CampaignCatalog>();
            var serialized = new SerializedObject(catalog);
            SetObjectList(serialized.FindProperty("chapterAssets"), chapter01);
            SetObjectList(serialized.FindProperty("jsonChapters"), chapter02Json);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return catalog;
        }

        private static GameConfig CreateGameConfig(CampaignCatalog catalog)
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            AssignCatalog(config, catalog);
            return config;
        }

        private static void AssignCatalog(GameConfig config, CampaignCatalog catalog)
        {
            var serialized = new SerializedObject(config);
            serialized.FindProperty("campaignCatalog").objectReferenceValue = catalog;
            serialized.FindProperty("mainMenuScene").stringValue = MainMenuScene;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void SetObjectList(SerializedProperty list, params Object[] items)
        {
            list.ClearArray();
            foreach (Object item in items)
            {
                if (item == null) continue;

                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
            }
        }

        private static void PopulateMainMenu()
        {
            CreateCamera(new Color(0.01f, 0.02f, 0.05f));
            new GameObject("MainMenu").AddComponent<DevMainMenu>();
        }

        private static void CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.SetPositionAndRotation(new Vector3(0f, 8f, -30f), Quaternion.Euler(12f, 0f, 0f));

            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.farClipPlane = 5000f;
            go.AddComponent<AudioListener>();
        }

        private static void CreateSceneIfMissing(string sceneName, Action populate)
        {
            string path = ScenePath(sceneName);
            if (File.Exists(path)) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            populate();
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[Setup] Created {path}");
        }

        /// <summary>
        /// Creates a mission scene, or rebuilds one generated before combat existed (the Task 1.1 placeholders have no
        /// MissionCombatBinder). Rebuilding reuses the scene asset, so its GUID and Build Settings entry stay valid.
        /// Scenes that already have combat are left alone, so designer edits survive.
        /// </summary>
        private static void CreateOrRebuildMissionScene(MissionSceneSpec spec)
        {
            string path = ScenePath(spec.SceneName);
            if (!File.Exists(path))
            {
                CreateSceneIfMissing(spec.SceneName, spec.Populate);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool hasCombat = scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<MissionCombatBinder>(true) != null);
            if (hasCombat) return;

            foreach (GameObject root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
            spec.Populate();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Setup] Rebuilt placeholder scene {path} with combat content");
        }

        /// <summary>
        /// Puts the main menu first, then the mission scenes and the combat sandbox, keeping any other scenes
        /// the team added.
        /// </summary>
        private static void ConfigureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath(MainMenuScene), true) };
            foreach (MissionSceneSpec spec in MissionScenes)
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath(spec.SceneName), true));
            }
            scenes.Add(new EditorBuildSettingsScene(ScenePath(CombatContentSetup.SandboxScene), true));

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path == TemplateSampleScene || scenes.Exists(scene => scene.path == existing.path)) continue;
                scenes.Add(existing);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static string ScenePath(string sceneName)
        {
            return $"{ScenesFolder}/{sceneName}.unity";
        }

        private readonly struct MissionSceneSpec
        {
            public readonly string SceneName;
            public readonly Action Populate;

            public MissionSceneSpec(string sceneName, Action populate)
            {
                SceneName = sceneName;
                Populate = populate;
            }
        }
    }
}
