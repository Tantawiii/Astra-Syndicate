using System;
using System.Collections.Generic;
using System.IO;
using AstraSyndicate.App;
using AstraSyndicate.Campaign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AstraSyndicate.EditorTools
{
    /// <summary>
    /// Generates the Sprint 1 content: chapter assets, campaign catalog, game config, scenes and Build
    /// Settings. Safe to run again: existing assets and scenes are kept, so designer edits survive.
    /// </summary>
    public static class SprintSetup
    {
        public const string MainMenuScene = "MainMenu";

        private const string Root = "Assets/AstraSyndicate";
        private const string CampaignFolder = Root + "/Data/Campaign";
        private const string ScenesFolder = Root + "/Scenes";
        private const string ResourcesFolder = Root + "/Resources/AstraSyndicate";
        private const string Chapter01Path = CampaignFolder + "/Chapter01.asset";
        private const string Chapter02JsonPath = CampaignFolder + "/Chapter02.json";
        private const string CatalogPath = CampaignFolder + "/CampaignCatalog.asset";
        private const string GameConfigPath = ResourcesFolder + "/GameConfig.asset";
        private const string TemplateSampleScene = "Assets/Scenes/SampleScene.unity";

        private static readonly MissionSceneSpec[] MissionScenes =
        {
            new MissionSceneSpec("Ch01_M01", "ch01_m01", new Color(0.02f, 0.04f, 0.10f), new Vector3(-60f, -10f, 160f), 60f),
            new MissionSceneSpec("Ch01_M02", "ch01_m02", new Color(0.08f, 0.03f, 0.06f), new Vector3(70f, 5f, 200f), 90f),
            new MissionSceneSpec("Ch01_M03", "ch01_m03", new Color(0.02f, 0.07f, 0.07f), new Vector3(0f, -40f, 260f), 140f),
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
            EnsureFolder(CampaignFolder);
            EnsureFolder(ScenesFolder);
            EnsureFolder(ResourcesFolder);

            // Scenes first: opening a new scene unloads unused assets, which would leave the in-memory
            // references between freshly created assets below pointing at unloaded objects.
            CreateSceneIfMissing(MainMenuScene, PopulateMainMenu);
            foreach (MissionSceneSpec spec in MissionScenes)
            {
                CreateSceneIfMissing(spec.SceneName, () => PopulateMissionScene(spec));
            }
            ConfigureBuildSettings();

            ChapterConfig chapter01 = LoadOrCreate(Chapter01Path, CreateChapter01);
            AssetDatabase.ImportAsset(Chapter02JsonPath);
            var chapter02Json = AssetDatabase.LoadAssetAtPath<TextAsset>(Chapter02JsonPath);
            if (chapter02Json == null) Debug.LogWarning($"[Setup] {Chapter02JsonPath} is missing; the catalog will only contain Chapter 1.");

            CampaignCatalog catalog = LoadOrCreate(CatalogPath, () => CreateCatalog(chapter01, chapter02Json));
            GameConfig config = LoadOrCreate(GameConfigPath, () => CreateGameConfig(catalog));
            if (config.Catalog == null) AssignCatalog(config, catalog);
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

        private static void PopulateMissionScene(MissionSceneSpec spec)
        {
            CreateCamera(spec.Background);
            CreateSun();
            CreatePlanet(spec);

            var mission = new GameObject("Mission");
            mission.AddComponent<MissionSceneController>().FallbackMissionId = spec.FallbackMissionId;
            mission.AddComponent<DevMissionHud>();
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

        private static void CreateSun()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
        }

        private static void CreatePlanet(MissionSceneSpec spec)
        {
            GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planet.name = "Planet (placeholder)";
            Object.DestroyImmediate(planet.GetComponent<Collider>());
            planet.transform.position = spec.PlanetPosition;
            planet.transform.localScale = Vector3.one * spec.PlanetScale;
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

        /// <summary>Puts the main menu first and the mission scenes after it, keeping any other scenes the team added.</summary>
        private static void ConfigureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath(MainMenuScene), true) };
            foreach (MissionSceneSpec spec in MissionScenes)
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath(spec.SceneName), true));
            }

            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
            {
                if (existing.path == TemplateSampleScene || scenes.Exists(scene => scene.path == existing.path)) continue;
                scenes.Add(existing);
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static T LoadOrCreate<T>(string path, Func<T> create) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            T asset = create();
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[Setup] Created {path}");
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static string ScenePath(string sceneName)
        {
            return $"{ScenesFolder}/{sceneName}.unity";
        }

        private readonly struct MissionSceneSpec
        {
            public readonly string SceneName;
            public readonly string FallbackMissionId;
            public readonly Color Background;
            public readonly Vector3 PlanetPosition;
            public readonly float PlanetScale;

            public MissionSceneSpec(string sceneName, string fallbackMissionId, Color background, Vector3 planetPosition, float planetScale)
            {
                SceneName = sceneName;
                FallbackMissionId = fallbackMissionId;
                Background = background;
                PlanetPosition = planetPosition;
                PlanetScale = planetScale;
            }
        }
    }
}
