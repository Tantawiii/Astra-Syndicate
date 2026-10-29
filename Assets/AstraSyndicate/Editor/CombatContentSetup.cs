using System.Collections.Generic;
using System.IO;
using AstraSyndicate.AI;
using AstraSyndicate.App;
using AstraSyndicate.Combat;
using AstraSyndicate.Core;
using AstraSyndicate.PlayerControl;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace AstraSyndicate.EditorTools
{
    /// <summary>
    /// Generates the combat content from primitives (zero-budget art): materials, starfield skybox, asteroid
    /// meshes, effects, projectiles, combat configs, ship prefabs and the CombatSandbox scene, plus the scene
    /// building blocks the mission scenes use. Existing assets are kept, so tuning done in the editor survives re-runs.
    /// </summary>
    internal static class CombatContentSetup
    {
        public const string SandboxScene = "CombatSandbox";

        private const string Root = "Assets/AstraSyndicate";
        private const string MaterialsFolder = Root + "/Art/Materials";
        private const string TexturesFolder = Root + "/Art/Textures";
        private const string MeshesFolder = Root + "/Art/Meshes";
        private const string CombatDataFolder = Root + "/Data/Combat";
        private const string ShipsFolder = Root + "/Prefabs/Ships";
        private const string WeaponsFolder = Root + "/Prefabs/Weapons";
        private const string EffectsFolder = Root + "/Prefabs/Effects";

        private const string FlagshipPath = ShipsFolder + "/Flagship.prefab";
        private const string WingmanPath = ShipsFolder + "/Wingman.prefab";
        private const string RaiderPath = ShipsFolder + "/SyndicateRaider.prefab";
        private const string SpaceDustPath = EffectsFolder + "/FX_SpaceDust.prefab";
        private const string SkyboxPath = MaterialsFolder + "/M_StarfieldSkybox.mat";
        private const string AsteroidMaterialPath = MaterialsFolder + "/M_Asteroid.mat";
        private const string GateMaterialPath = MaterialsFolder + "/M_Gate.mat";
        private const string PlayerHullMaterialPath = MaterialsFolder + "/M_PlayerHull.mat";
        private const string PlayerAccentMaterialPath = MaterialsFolder + "/M_PlayerAccent.mat";

        private const int AsteroidVariants = 3;
        private const int AsteroidCount = 70;
        private const int WingmanCount = 2;
        private const float FormationSpacing = 14f;

        private enum HullShape
        {
            Flagship,
            Fighter,
            Raider,
        }

        private enum PilotKind
        {
            Player,
            Wingman,
            Enemy,
        }

        public static void EnsureAssets()
        {
            foreach (string folder in new[] { MaterialsFolder, TexturesFolder, MeshesFolder, CombatDataFolder, ShipsFolder, WeaponsFolder, EffectsFolder })
            {
                SetupUtility.EnsureFolder(folder);
            }

            Material playerHull = LitMaterial("M_PlayerHull", new Color(0.55f, 0.62f, 0.72f), 0.6f, 0.55f);
            Material playerAccent = UnlitMaterial("M_PlayerAccent", new Color(0.35f, 0.85f, 1f));
            Material enemyHull = LitMaterial("M_EnemyHull", new Color(0.5f, 0.15f, 0.13f), 0.5f, 0.45f);
            Material enemyAccent = UnlitMaterial("M_EnemyAccent", new Color(1f, 0.5f, 0.15f));
            Material playerBolt = UnlitMaterial("M_PlayerBolt", new Color(0.6f, 1f, 1f));
            Material enemyBolt = UnlitMaterial("M_EnemyBolt", new Color(1f, 0.45f, 0.25f));
            LitMaterial("M_Asteroid", new Color(0.36f, 0.33f, 0.31f), 0f, 0.12f);
            UnlitMaterial("M_Gate", new Color(0.35f, 1f, 0.55f));
            Material glow = ParticleMaterial("M_ParticlesAdditive", additive: true);
            Material dust = ParticleMaterial("M_SpaceDust", additive: false);
            SkyboxMaterial(StarfieldTexture());
            for (int i = 0; i < AsteroidVariants; i++) AsteroidMesh(i);

            PooledEffect impact = BurstEffect("FX_Impact", glow, duration: 0.4f, particles: 14,
                lifetime: new Vector2(0.12f, 0.3f), speed: new Vector2(8f, 28f), size: new Vector2(0.4f, 1.2f),
                start: new Color(1f, 0.85f, 0.5f), end: new Color(1f, 0.3f, 0.1f), radius: 0.2f);
            PooledEffect explosion = BurstEffect("FX_Explosion", glow, duration: 1.4f, particles: 48,
                lifetime: new Vector2(0.35f, 1.2f), speed: new Vector2(4f, 30f), size: new Vector2(2f, 6.5f),
                start: new Color(1f, 0.85f, 0.45f), end: new Color(0.7f, 0.15f, 0.05f), radius: 1.5f);
            SpaceDust(dust);

            Projectile playerShot = ProjectilePrefab("Projectile_PlayerBolt", playerBolt, glow, new Color(0.5f, 1f, 1f));
            Projectile enemyShot = ProjectilePrefab("Projectile_EnemyBolt", enemyBolt, glow, new Color(1f, 0.5f, 0.25f));

            WeaponConfig flagshipGuns = WeaponStats("Weapon_FlagshipCannons", playerShot, impact, shotsPerSecond: 8f, projectileSpeed: 340f, damage: 7f, range: 750f, spread: 0.35f, gimbal: 6f);
            WeaponConfig wingmanGuns = WeaponStats("Weapon_WingmanCannons", playerShot, impact, shotsPerSecond: 5f, projectileSpeed: 300f, damage: 4f, range: 600f, spread: 0.8f, gimbal: 8f);
            WeaponConfig raiderGuns = WeaponStats("Weapon_RaiderBlasters", enemyShot, impact, shotsPerSecond: 3.5f, projectileSpeed: 260f, damage: 5f, range: 550f, spread: 1.2f, gimbal: 6f);

            ShipConfig flagshipStats = ShipStats("Ship_Flagship", hull: 320f, maxSpeed: 65f, boostSpeed: 115f, acceleration: 40f,
                pitchRate: 70f, yawRate: 65f, turnResponse: 6f, bankAngle: 25f, explosion, explosionScale: 2.5f);
            ShipConfig wingmanStats = ShipStats("Ship_Wingman", hull: 130f, maxSpeed: 72f, boostSpeed: 115f, acceleration: 48f,
                pitchRate: 85f, yawRate: 80f, turnResponse: 7f, bankAngle: 40f, explosion, explosionScale: 1.4f);
            ShipConfig raiderStats = ShipStats("Ship_SyndicateRaider", hull: 60f, maxSpeed: 58f, boostSpeed: 95f, acceleration: 45f,
                pitchRate: 80f, yawRate: 75f, turnResponse: 6.5f, bankAngle: 40f, explosion, explosionScale: 1.2f);

            AIConfig raiderPilot = PilotStats("AI_SyndicateRaider", detectionRange: 750f, fireCone: 8f, aimError: 2.5f);
            AIConfig wingmanPilot = PilotStats("AI_Wingman", detectionRange: 700f, fireCone: 10f, aimError: 3.5f);

            ShipPrefab(FlagshipPath, HullShape.Flagship, PilotKind.Player, Faction.Player, "flagship", "Flagship", flagshipStats, flagshipGuns, null, playerHull, playerAccent, mass: 30f);
            ShipPrefab(WingmanPath, HullShape.Fighter, PilotKind.Wingman, Faction.Player, "wingman", string.Empty, wingmanStats, wingmanGuns, wingmanPilot, playerHull, playerAccent, mass: 12f);
            ShipPrefab(RaiderPath, HullShape.Raider, PilotKind.Enemy, Faction.Enemy, "raider", "Syndicate Raider", raiderStats, raiderGuns, raiderPilot, enemyHull, enemyAccent, mass: 10f);

            AssetDatabase.SaveAssets();
        }

        /// <summary>Fills the (new, empty, active) sandbox scene with endless waves and a waypoint gate.</summary>
        public static void PopulateSandboxScene()
        {
            Vector3[] spawnPositions =
            {
                new Vector3(0f, 60f, 1000f),
                new Vector3(-850f, -40f, 650f),
                new Vector3(850f, 30f, 700f),
            };
            var gatePosition = new Vector3(-380f, 40f, 820f);

            CombatStage stage = BuildStage(4242, new List<Vector3>(spawnPositions) { gatePosition }.ToArray());
            CreateWaypointGate("sandbox_gate", gatePosition, null);
            EnemyWaveSpawner spawner = CreateEnemyWaves("Enemy Waves", null, spawnPositions,
                firstWaveSize: 3, extraShipsPerWave: 1, maxWaveSize: 8, totalWaves: 0, firstWaveDelay: 2f, timeBetweenWaves: 6f);

            new GameObject("Combat Sandbox").AddComponent<CombatSandboxController>().Configure(stage.Player, spawner);
        }

        /// <summary>
        /// The shared part of every combat scene, built in the active scene: lighting and skybox, the player fleet at
        /// the origin facing +Z, the chase camera, the flight HUD and an asteroid belt that keeps clear of the start
        /// and of <paramref name="keepClear"/> (spawn points, gates). Assets are loaded from disk because creating or
        /// opening a scene can unload in-memory copies.
        /// </summary>
        public static CombatStage BuildStage(int asteroidSeed, params Vector3[] keepClear)
        {
            var meshes = new Mesh[AsteroidVariants];
            for (int i = 0; i < meshes.Length; i++) meshes[i] = SetupUtility.Load<Mesh>(AsteroidMeshPath(i));

            RenderSettings.skybox = SetupUtility.Load<Material>(SkyboxPath);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.18f, 0.24f);

            CreateSun();
            PlayerFlightController player = CreatePlayerFleet(SetupUtility.Load<GameObject>(FlagshipPath), SetupUtility.Load<GameObject>(WingmanPath));
            Camera camera = CreateChaseCamera(player, SetupUtility.Load<GameObject>(SpaceDustPath));
            CreateAsteroidField(meshes, SetupUtility.Load<Material>(AsteroidMaterialPath), asteroidSeed, keepClear);
            new GameObject("Flight HUD").AddComponent<DevFlightHud>().Configure(player, camera);
            return new CombatStage(player, camera);
        }

        /// <summary>
        /// A raider wave spawner with its spawn points (facing the player start) and a patrol route around each.
        /// Set <paramref name="totalWaves"/> to 0 for endless waves.
        /// </summary>
        public static EnemyWaveSpawner CreateEnemyWaves(string name, Transform parent, Vector3[] spawnPositions,
            int firstWaveSize, int extraShipsPerWave, int maxWaveSize, int totalWaves, float firstWaveDelay, float timeBetweenWaves)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            Transform[] points = CreateSpawnPoints(root.transform, spawnPositions);
            PatrolRoute[] routes = CreatePatrolRoutes(root.transform, points);

            var spawner = root.AddComponent<EnemyWaveSpawner>();
            spawner.Configure(SetupUtility.Load<GameObject>(RaiderPath).GetComponent<Ship>(), points, routes);
            SetupUtility.Assign(spawner,
                ("firstWaveSize", firstWaveSize), ("extraShipsPerWave", extraShipsPerWave), ("maxWaveSize", maxWaveSize),
                ("totalWaves", totalWaves), ("firstWaveDelay", firstWaveDelay), ("timeBetweenWaves", timeBetweenWaves));
            return spawner;
        }

        /// <summary>A ring gate (facing the player start) whose trigger publishes WaypointReachedEvent for the flagship.</summary>
        public static WaypointTrigger CreateWaypointGate(string waypointId, Vector3 position, Transform parent)
        {
            const float radius = 24f;
            const int segments = 16;
            Material material = SetupUtility.Load<Material>(GateMaterialPath);

            var gate = new GameObject($"Waypoint Gate ({waypointId})");
            gate.transform.SetParent(parent, false);
            gate.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-position));
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var offset = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                Part(PrimitiveType.Cube, $"Segment {i + 1}", gate.transform, offset, new Vector3(0f, 0f, angle * Mathf.Rad2Deg),
                    new Vector3(2f, 2f * Mathf.PI * radius / segments, 2f), material);
            }

            var trigger = gate.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;
            var waypoint = gate.AddComponent<WaypointTrigger>();
            waypoint.Configure(waypointId, "flagship", once: true);
            return waypoint;
        }

        /// <summary>The Kepler relay: a large static set piece the fleet defends in "Hold the Line". Solid, so ships can collide with it.</summary>
        public static void CreateRelayStation(Vector3 position)
        {
            Material hull = SetupUtility.Load<Material>(PlayerHullMaterialPath);
            Material accent = SetupUtility.Load<Material>(PlayerAccentMaterialPath);

            Transform relay = new GameObject("Kepler Relay").transform;
            relay.position = position;
            SolidPart(PrimitiveType.Cylinder, "Spine", relay, Vector3.zero, Vector3.zero, new Vector3(9f, 45f, 9f), hull);
            SolidPart(PrimitiveType.Cylinder, "Ring Upper", relay, new Vector3(0f, 25f, 0f), Vector3.zero, new Vector3(46f, 1.5f, 46f), hull);
            SolidPart(PrimitiveType.Cylinder, "Ring Lower", relay, new Vector3(0f, -25f, 0f), Vector3.zero, new Vector3(34f, 1.5f, 34f), hull);
            SolidPart(PrimitiveType.Sphere, "Dish", relay, new Vector3(0f, 49f, 0f), Vector3.zero, new Vector3(18f, 6f, 18f), hull);
            SolidPart(PrimitiveType.Sphere, "Beacon", relay, new Vector3(0f, 54f, 0f), Vector3.zero, new Vector3(4f, 4f, 4f), accent);
        }

        // ---- Materials and textures ------------------------------------------------------------------

        private static Material LitMaterial(string name, Color color, float metallic, float smoothness)
        {
            return SetupUtility.LoadOrCreate($"{MaterialsFolder}/{name}.mat", () =>
            {
                var material = new Material(FindShader("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", color);
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Smoothness", smoothness);
                return material;
            });
        }

        private static Material UnlitMaterial(string name, Color color)
        {
            return SetupUtility.LoadOrCreate($"{MaterialsFolder}/{name}.mat", () =>
            {
                var material = new Material(FindShader("Universal Render Pipeline/Unlit"));
                material.SetColor("_BaseColor", color);
                return material;
            });
        }

        // Starts from the render pipeline's default particle material (soft round sprite) and makes it transparent.
        private static Material ParticleMaterial(string name, bool additive)
        {
            return SetupUtility.LoadOrCreate($"{MaterialsFolder}/{name}.mat", () =>
            {
                RenderPipelineAsset pipeline = GraphicsSettings.defaultRenderPipeline;
                Material source = pipeline != null ? pipeline.defaultParticleMaterial : null;
                Material material = source != null ? new Material(source) : new Material(FindShader("Universal Render Pipeline/Particles/Unlit"));
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", additive ? 2f : 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
                return material;
            });
        }

        private static void SkyboxMaterial(Texture2D stars)
        {
            SetupUtility.LoadOrCreate(SkyboxPath, () =>
            {
                var material = new Material(FindShader("Skybox/Panoramic"));
                material.SetTexture("_MainTex", stars);
                material.SetFloat("_Mapping", 1f);
                material.SetFloat("_ImageType", 0f);
                material.SetFloat("_Exposure", 1f);
                material.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
                material.DisableKeyword("_MAPPING_6_FRAMES_LAYOUT");
                return material;
            });
        }

        // Equirectangular star panorama with a faint nebula band. Noise is wrapped around the horizontal
        // axis so the panorama has no seam.
        private static Texture2D StarfieldTexture()
        {
            string path = $"{TexturesFolder}/T_Starfield.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int width = 2048;
            const int height = 1024;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                float band = Mathf.Exp(-Mathf.Pow((v - 0.56f) / 0.12f, 2f));
                for (int x = 0; x < width; x++)
                {
                    float angle = (float)x / width * Mathf.PI * 2f;
                    float cloud = Mathf.PerlinNoise(Mathf.Cos(angle) * 1.8f + 7.3f, v * 5f)
                                  * Mathf.PerlinNoise(Mathf.Sin(angle) * 2.4f + 3.1f, v * 7f + 11f);
                    float nebula = cloud * cloud * band;
                    pixels[y * width + x] = new Color(0.008f + nebula * 0.20f, 0.010f + nebula * 0.08f, 0.024f + nebula * 0.28f);
                }
            }

            var random = new Random(1977);
            for (int i = 0; i < 3200; i++)
            {
                int x = random.Next(width);
                int y = random.Next(height);
                float brightness = Mathf.Pow((float)random.NextDouble(), 3f) * 0.9f + 0.1f;
                float warmth = (float)random.NextDouble();
                var star = new Color(brightness * (0.85f + 0.15f * warmth), brightness * 0.9f, brightness * (1.05f - 0.25f * warmth));
                AddPixel(pixels, width, height, x, y, star);
                if (brightness < 0.7f) continue;

                // Bright stars get a small cross-shaped glow.
                Color glow = star * 0.35f;
                AddPixel(pixels, width, height, x + 1, y, glow);
                AddPixel(pixels, width, height, x - 1, y, glow);
                AddPixel(pixels, width, height, x, y + 1, glow);
                AddPixel(pixels, width, height, x, y - 1, glow);
            }

            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.mipmapEnabled = false;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            Debug.Log($"[Setup] Created {path}");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void AddPixel(Color[] pixels, int width, int height, int x, int y, Color color)
        {
            if (y < 0 || y >= height) return;
            x = (x % width + width) % width;
            pixels[y * width + x] += color;
        }

        // ---- Meshes -----------------------------------------------------------------------------------

        private static string AsteroidMeshPath(int variant)
        {
            return $"{MeshesFolder}/Asteroid_{variant}.asset";
        }

        // A unit sphere pushed in and out by 3D noise. Displacement depends only on vertex direction, so the
        // sphere's duplicated seam vertices move together and the mesh stays closed.
        private static void AsteroidMesh(int variant)
        {
            SetupUtility.LoadOrCreate(AsteroidMeshPath(variant), () =>
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Mesh mesh = Object.Instantiate(sphere.GetComponent<MeshFilter>().sharedMesh);
                Object.DestroyImmediate(sphere);

                float seed = 13.7f * (variant + 1);
                Vector3[] vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 direction = vertices[i].normalized;
                    float noise = Noise3(direction * 1.6f, seed) * 0.6f + Noise3(direction * 4.1f, seed + 5f) * 0.25f;
                    vertices[i] = direction * (0.5f * (0.7f + noise));
                }
                mesh.vertices = vertices;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                mesh.name = $"Asteroid_{variant}";
                return mesh;
            });
        }

        private static float Noise3(Vector3 point, float seed)
        {
            return (Mathf.PerlinNoise(point.x + seed, point.y + seed * 0.5f)
                    + Mathf.PerlinNoise(point.y + seed * 1.3f, point.z + seed)
                    + Mathf.PerlinNoise(point.z + seed * 0.7f, point.x + seed * 1.9f)) / 3f;
        }

        // ---- Effects and projectiles ------------------------------------------------------------------

        private static PooledEffect BurstEffect(string name, Material material, float duration, int particles,
            Vector2 lifetime, Vector2 speed, Vector2 size, Color start, Color end, float radius)
        {
            return SetupUtility.LoadOrCreatePrefab<PooledEffect>($"{EffectsFolder}/{name}.prefab", () =>
            {
                var go = new GameObject(name);
                var system = go.AddComponent<ParticleSystem>();
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                ParticleSystem.MainModule main = system.main;
                main.duration = duration;
                main.loop = false;
                main.playOnAwake = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
                main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
                main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
                main.startColor = new ParticleSystem.MinMaxGradient(start, Color.Lerp(start, end, 0.4f));
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.stopAction = ParticleSystemStopAction.Callback;
                main.maxParticles = particles * 2;

                ParticleSystem.EmissionModule emission = system.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particles) });

                ParticleSystem.ShapeModule shape = system.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = radius;

                ParticleSystem.ColorOverLifetimeModule colors = system.colorOverLifetime;
                colors.enabled = true;
                colors.color = new ParticleSystem.MinMaxGradient(Fade(start, end));

                ParticleSystem.SizeOverLifetimeModule sizes = system.sizeOverLifetime;
                sizes.enabled = true;
                sizes.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

                ConfigureRenderer(go.GetComponent<ParticleSystemRenderer>(), material);
                go.AddComponent<PooledEffect>();
                return go;
            });
        }

        // Faint motion-reference particles that follow the camera but stay put in the world, so speed is
        // readable against an otherwise empty background. They stretch with camera velocity.
        private static void SpaceDust(Material material)
        {
            SetupUtility.LoadOrCreatePrefab<ParticleSystem>(SpaceDustPath, () =>
            {
                var go = new GameObject("FX_SpaceDust");
                var system = go.AddComponent<ParticleSystem>();
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                ParticleSystem.MainModule main = system.main;
                main.duration = 5f;
                main.loop = true;
                main.prewarm = true;
                main.startLifetime = 8f;
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
                main.startColor = new Color(0.8f, 0.85f, 1f, 0.55f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 1000;

                ParticleSystem.EmissionModule emission = system.emission;
                emission.rateOverTime = 90f;

                ParticleSystem.ShapeModule shape = system.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 70f;
                shape.radiusThickness = 1f;

                ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
                ConfigureRenderer(renderer, material);
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.cameraVelocityScale = 0.06f;
                renderer.velocityScale = 0f;
                renderer.lengthScale = 1f;
                return go;
            });
        }

        private static Projectile ProjectilePrefab(string name, Material bolt, Material trailMaterial, Color color)
        {
            return SetupUtility.LoadOrCreatePrefab<Projectile>($"{WeaponsFolder}/{name}.prefab", () =>
            {
                var go = new GameObject(name);
                Part(PrimitiveType.Capsule, "Bolt", go.transform, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.22f, 1.2f, 0.22f), bolt);

                var trail = go.AddComponent<TrailRenderer>();
                trail.time = 0.07f;
                trail.startWidth = 0.35f;
                trail.endWidth = 0f;
                trail.minVertexDistance = 0.4f;
                trail.sharedMaterial = trailMaterial;
                trail.colorGradient = Fade(color, color);
                trail.shadowCastingMode = ShadowCastingMode.Off;
                trail.receiveShadows = false;

                go.AddComponent<Projectile>().Configure(0.35f, trail);
                return go;
            });
        }

        private static void ConfigureRenderer(ParticleSystemRenderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Gradient Fade(Color start, Color end)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        // ---- Configs ----------------------------------------------------------------------------------

        private static WeaponConfig WeaponStats(string name, Projectile projectile, PooledEffect impact,
            float shotsPerSecond, float projectileSpeed, float damage, float range, float spread, float gimbal)
        {
            return SetupUtility.LoadOrCreate($"{CombatDataFolder}/{name}.asset", () =>
            {
                var config = ScriptableObject.CreateInstance<WeaponConfig>();
                SetupUtility.Assign(config,
                    ("projectilePrefab", projectile), ("impactEffect", impact), ("shotsPerSecond", shotsPerSecond),
                    ("projectileSpeed", projectileSpeed), ("damage", damage), ("range", range),
                    ("spreadDegrees", spread), ("gimbalDegrees", gimbal));
                return config;
            });
        }

        private static ShipConfig ShipStats(string name, float hull, float maxSpeed, float boostSpeed, float acceleration,
            float pitchRate, float yawRate, float turnResponse, float bankAngle, PooledEffect explosion, float explosionScale)
        {
            return SetupUtility.LoadOrCreate($"{CombatDataFolder}/{name}.asset", () =>
            {
                var config = ScriptableObject.CreateInstance<ShipConfig>();
                SetupUtility.Assign(config,
                    ("maxHull", hull), ("maxSpeed", maxSpeed), ("boostSpeed", boostSpeed), ("acceleration", acceleration),
                    ("pitchRate", pitchRate), ("yawRate", yawRate), ("turnResponse", turnResponse), ("bankAngle", bankAngle),
                    ("explosionEffect", explosion), ("explosionScale", explosionScale));
                return config;
            });
        }

        private static AIConfig PilotStats(string name, float detectionRange, float fireCone, float aimError)
        {
            return SetupUtility.LoadOrCreate($"{CombatDataFolder}/{name}.asset", () =>
            {
                var config = ScriptableObject.CreateInstance<AIConfig>();
                SetupUtility.Assign(config,
                    ("detectionRange", detectionRange), ("fireConeDegrees", fireCone), ("aimErrorDegrees", aimError),
                    ("formationSpacing", FormationSpacing));
                return config;
            });
        }

        // ---- Ships ------------------------------------------------------------------------------------

        private static void ShipPrefab(string path, HullShape shape, PilotKind pilot, Faction faction, string unitTag, string displayName,
            ShipConfig stats, WeaponConfig guns, AIConfig ai, Material hullMaterial, Material accentMaterial, float mass)
        {
            SetupUtility.LoadOrCreatePrefab<Ship>(path, () =>
            {
                var root = new GameObject(Path.GetFileNameWithoutExtension(path));
                var body = root.AddComponent<Rigidbody>();
                body.mass = mass;
                body.useGravity = false;
                body.linearDamping = 0f;
                body.angularDamping = 0f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                Transform model = new GameObject("Model").transform;
                model.SetParent(root.transform, false);
                ShipLayout layout = BuildModel(shape, model, hullMaterial, accentMaterial);

                var collider = root.AddComponent<BoxCollider>();
                collider.center = layout.ColliderCenter;
                collider.size = layout.ColliderSize;

                root.AddComponent<Ship>().Configure(stats, faction, unitTag, displayName);
                root.AddComponent<ShipMotor>().Configure(model);

                // Muzzles hang off the root, not the banking model, so shots follow the physics heading.
                var muzzles = new Transform[layout.Muzzles.Length];
                for (int i = 0; i < muzzles.Length; i++)
                {
                    muzzles[i] = new GameObject($"Muzzle {i + 1}").transform;
                    muzzles[i].SetParent(root.transform, false);
                    muzzles[i].localPosition = layout.Muzzles[i];
                }
                root.AddComponent<WeaponSystem>().Configure(guns, muzzles);

                switch (pilot)
                {
                    case PilotKind.Player:
                        root.AddComponent<PlayerFlightController>();
                        break;
                    case PilotKind.Wingman:
                        root.AddComponent<WingmanBrain>().Configure(ai, null, 0);
                        break;
                    case PilotKind.Enemy:
                        root.AddComponent<EnemyBrain>().Configure(ai);
                        break;
                }
                return root;
            });
        }

        private static ShipLayout BuildModel(HullShape shape, Transform model, Material hull, Material accent)
        {
            switch (shape)
            {
                case HullShape.Flagship:
                    Part(PrimitiveType.Cube, "Hull", model, Vector3.zero, Vector3.zero, new Vector3(3.2f, 1.8f, 14f), hull);
                    Part(PrimitiveType.Cube, "Prow", model, new Vector3(0f, -0.2f, 8.4f), Vector3.zero, new Vector3(2.2f, 1.2f, 3.2f), hull);
                    Part(PrimitiveType.Cube, "Wing", model, new Vector3(0f, -0.4f, -2f), Vector3.zero, new Vector3(13f, 0.3f, 3.6f), hull);
                    Part(PrimitiveType.Cube, "Pod L", model, new Vector3(-3.6f, -0.4f, -1.2f), Vector3.zero, new Vector3(1.6f, 1.6f, 8.5f), hull);
                    Part(PrimitiveType.Cube, "Pod R", model, new Vector3(3.6f, -0.4f, -1.2f), Vector3.zero, new Vector3(1.6f, 1.6f, 8.5f), hull);
                    Part(PrimitiveType.Cube, "Bridge", model, new Vector3(0f, 1.3f, -2.8f), Vector3.zero, new Vector3(1.8f, 1.1f, 3.2f), hull);
                    Part(PrimitiveType.Sphere, "Canopy", model, new Vector3(0f, 1.6f, -1.4f), Vector3.zero, new Vector3(1.2f, 0.7f, 1.6f), accent);
                    Part(PrimitiveType.Cylinder, "Engine C", model, new Vector3(0f, 0f, -7.3f), new Vector3(90f, 0f, 0f), new Vector3(1.4f, 0.4f, 1.4f), accent);
                    Part(PrimitiveType.Cylinder, "Engine L", model, new Vector3(-3.6f, -0.4f, -5.6f), new Vector3(90f, 0f, 0f), new Vector3(1.1f, 0.3f, 1.1f), accent);
                    Part(PrimitiveType.Cylinder, "Engine R", model, new Vector3(3.6f, -0.4f, -5.6f), new Vector3(90f, 0f, 0f), new Vector3(1.1f, 0.3f, 1.1f), accent);
                    return new ShipLayout(new Vector3(0f, 0f, 0.5f), new Vector3(13.5f, 3.6f, 19f),
                        new Vector3(-3.6f, -0.4f, 3.4f), new Vector3(3.6f, -0.4f, 3.4f));

                case HullShape.Fighter:
                    Part(PrimitiveType.Capsule, "Fuselage", model, Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(1.5f, 3.4f, 1.5f), hull);
                    Part(PrimitiveType.Cube, "Wing", model, new Vector3(0f, -0.15f, -0.8f), Vector3.zero, new Vector3(8f, 0.2f, 2.2f), hull);
                    Part(PrimitiveType.Cube, "Pod L", model, new Vector3(-4f, -0.15f, -0.4f), Vector3.zero, new Vector3(0.45f, 0.45f, 2.8f), hull);
                    Part(PrimitiveType.Cube, "Pod R", model, new Vector3(4f, -0.15f, -0.4f), Vector3.zero, new Vector3(0.45f, 0.45f, 2.8f), hull);
                    Part(PrimitiveType.Cube, "Fin", model, new Vector3(0f, 0.85f, -2.5f), Vector3.zero, new Vector3(0.18f, 1.3f, 1.8f), hull);
                    Part(PrimitiveType.Sphere, "Canopy", model, new Vector3(0f, 0.55f, 1.2f), Vector3.zero, new Vector3(0.85f, 0.6f, 1.5f), accent);
                    Part(PrimitiveType.Cylinder, "Engine", model, new Vector3(0f, 0f, -3.5f), new Vector3(90f, 0f, 0f), new Vector3(0.9f, 0.3f, 0.9f), accent);
                    return new ShipLayout(new Vector3(0f, 0.1f, 0f), new Vector3(8.5f, 2.2f, 7.4f),
                        new Vector3(-4f, -0.15f, 1.1f), new Vector3(4f, -0.15f, 1.1f));

                default:
                    Part(PrimitiveType.Cube, "Body", model, Vector3.zero, Vector3.zero, new Vector3(1.4f, 1f, 6.4f), hull);
                    Part(PrimitiveType.Cube, "Wing L", model, new Vector3(-2.3f, 0f, 0.6f), new Vector3(0f, -25f, 0f), new Vector3(4.2f, 0.16f, 1.5f), hull);
                    Part(PrimitiveType.Cube, "Wing R", model, new Vector3(2.3f, 0f, 0.6f), new Vector3(0f, 25f, 0f), new Vector3(4.2f, 0.16f, 1.5f), hull);
                    Part(PrimitiveType.Cube, "Spine", model, new Vector3(0f, 0.7f, -1.2f), Vector3.zero, new Vector3(0.3f, 0.9f, 3.2f), hull);
                    Part(PrimitiveType.Sphere, "Eye", model, new Vector3(0f, 0.35f, 2.9f), Vector3.zero, new Vector3(0.8f, 0.5f, 0.9f), accent);
                    Part(PrimitiveType.Cylinder, "Engine", model, new Vector3(0f, 0f, -3.4f), new Vector3(90f, 0f, 0f), new Vector3(0.8f, 0.3f, 0.8f), accent);
                    return new ShipLayout(new Vector3(0f, 0.2f, 0f), new Vector3(8.4f, 1.8f, 7f),
                        new Vector3(-1.1f, 0f, 3.3f), new Vector3(1.1f, 0f, 3.3f));
            }
        }

        // A visual-only primitive: the collider is removed (ships use one box collider on their root).
        private static void Part(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 euler, Vector3 scale, Material material)
        {
            GameObject part = SolidPart(type, name, parent, position, euler, scale, material);
            Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        private static GameObject SolidPart(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 euler, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localEulerAngles = euler;
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return part;
        }

        // ---- Scene building blocks --------------------------------------------------------------------

        private static void CreateSun()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.color = new Color(1f, 0.95f, 0.88f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(28f, -35f, 0f);
        }

        private static PlayerFlightController CreatePlayerFleet(GameObject flagshipPrefab, GameObject wingmanPrefab)
        {
            var flagship = (GameObject)PrefabUtility.InstantiatePrefab(flagshipPrefab);
            flagship.name = "Flagship";
            flagship.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            SetupUtility.RecordInstance(flagship, flagship.transform);
            var leader = flagship.GetComponent<Ship>();

            for (int slot = 0; slot < WingmanCount; slot++)
            {
                var wingman = (GameObject)PrefabUtility.InstantiatePrefab(wingmanPrefab);
                wingman.name = $"Wingman {slot + 1}";
                wingman.transform.position = Formation.SlotOffset(slot, FormationSpacing);
                var brain = wingman.GetComponent<WingmanBrain>();
                brain.SetLeader(leader, slot);
                SetupUtility.RecordInstance(wingman, wingman.transform, brain);
            }
            return flagship.GetComponent<PlayerFlightController>();
        }

        private static Camera CreateChaseCamera(PlayerFlightController player, GameObject dustPrefab)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 5f, -22f), Quaternion.identity);

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 65f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 8000f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<ChaseCameraRig>().SetTarget(player);
            PrefabUtility.InstantiatePrefab(dustPrefab, cameraObject.transform);
            return camera;
        }

        // Spawn points face the player start, so ships arrive pointing at the action.
        private static Transform[] CreateSpawnPoints(Transform parent, Vector3[] positions)
        {
            Transform root = new GameObject("Spawn Points").transform;
            root.SetParent(parent, false);

            var points = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                points[i] = new GameObject($"Spawn {i + 1}").transform;
                points[i].SetParent(root, false);
                points[i].SetPositionAndRotation(positions[i], Quaternion.LookRotation(-positions[i]));
            }
            return points;
        }

        private static PatrolRoute[] CreatePatrolRoutes(Transform parent, Transform[] spawnPoints)
        {
            Vector3[] corners =
            {
                new Vector3(-250f, 0f, -150f),
                new Vector3(-150f, 60f, 200f),
                new Vector3(250f, -30f, 150f),
                new Vector3(150f, 40f, -200f),
            };

            Transform root = new GameObject("Patrol Routes").transform;
            root.SetParent(parent, false);
            var routes = new PatrolRoute[spawnPoints.Length];
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                var route = new GameObject($"Route {i + 1}");
                route.transform.SetParent(root, false);
                route.transform.position = spawnPoints[i].position;
                for (int c = 0; c < corners.Length; c++)
                {
                    Transform point = new GameObject($"Point {c + 1}").transform;
                    point.SetParent(route.transform, false);
                    point.localPosition = corners[c];
                }
                routes[i] = route.AddComponent<PatrolRoute>();
            }
            return routes;
        }

        // Deterministic belt of asteroids around the arena, keeping the player start and the given points clear.
        private static void CreateAsteroidField(Mesh[] meshes, Material material, int seed, Vector3[] keepClear)
        {
            Transform root = new GameObject("Asteroid Field").transform;
            var random = new Random(seed);
            int placed = 0;
            for (int attempt = 0; attempt < 2000 && placed < AsteroidCount; attempt++)
            {
                Vector3 direction = RandomDirection(random);
                float distance = Mathf.Lerp(260f, 1600f, (float)random.NextDouble());
                Vector3 position = Vector3.Scale(direction * distance, new Vector3(1f, 0.45f, 1f));
                if (position.magnitude < 220f || IsNear(position, keepClear, 160f)) continue;

                float size = Mathf.Lerp(8f, 60f, Mathf.Pow((float)random.NextDouble(), 2.2f));
                var asteroid = new GameObject($"Asteroid {placed + 1}");
                asteroid.transform.SetParent(root, false);
                asteroid.transform.SetPositionAndRotation(position, Quaternion.Euler(Angle(random), Angle(random), Angle(random)));
                asteroid.transform.localScale = new Vector3(size * Jitter(random), size * Jitter(random), size * Jitter(random));

                Mesh mesh = meshes[placed % meshes.Length];
                asteroid.AddComponent<MeshFilter>().sharedMesh = mesh;
                asteroid.AddComponent<MeshRenderer>().sharedMaterial = material;
                var collider = asteroid.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = true;
                GameObjectUtility.SetStaticEditorFlags(asteroid, StaticEditorFlags.BatchingStatic);
                placed++;
            }
        }

        private static Vector3 RandomDirection(Random random)
        {
            while (true)
            {
                var point = new Vector3((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f);
                float sqr = point.sqrMagnitude;
                if (sqr > 1e-3f && sqr <= 1f) return point / Mathf.Sqrt(sqr);
            }
        }

        private static bool IsNear(Vector3 position, Vector3[] points, float distance)
        {
            foreach (Vector3 point in points)
            {
                if ((point - position).sqrMagnitude < distance * distance) return true;
            }
            return false;
        }

        private static float Angle(Random random)
        {
            return (float)random.NextDouble() * 360f;
        }

        private static float Jitter(Random random)
        {
            return 0.75f + (float)random.NextDouble() * 0.5f;
        }

        private static Shader FindShader(string name)
        {
            Shader shader = Shader.Find(name);
            if (shader == null) throw new System.InvalidOperationException($"Shader '{name}' was not found. Is URP installed?");
            return shader;
        }

        /// <summary>The player's side of a generated combat scene.</summary>
        public readonly struct CombatStage
        {
            public readonly PlayerFlightController Player;
            public readonly Camera Camera;

            public CombatStage(PlayerFlightController player, Camera camera)
            {
                Player = player;
                Camera = camera;
            }
        }

        private readonly struct ShipLayout
        {
            public readonly Vector3 ColliderCenter;
            public readonly Vector3 ColliderSize;
            public readonly Vector3[] Muzzles;

            public ShipLayout(Vector3 colliderCenter, Vector3 colliderSize, params Vector3[] muzzles)
            {
                ColliderCenter = colliderCenter;
                ColliderSize = colliderSize;
                Muzzles = muzzles;
            }
        }
    }
}
