using AstraSyndicate.App;
using AstraSyndicate.Campaign;
using UnityEngine;

namespace AstraSyndicate.EditorTools
{
    /// <summary>
    /// Builds the Chapter 1 mission scenes: the shared combat stage plus each mission's encounter, grouped in
    /// MissionContentGroups so a scene can host several missions (Chapter 2's JSON mission reuses Into the Rift).
    /// </summary>
    internal static class MissionSceneSetup
    {
        /// <summary>ch01_m01 "First Contact": destroy the five raider scouts circling the relay.</summary>
        public static void PopulateFirstContact()
        {
            Vector3[] scouts = { new Vector3(-260f, 40f, 760f), new Vector3(300f, -30f, 820f) };
            CombatContentSetup.CombatStage stage = CombatContentSetup.BuildStage(101, scouts);
            AddMission("ch01_m01", stage);

            Transform group = ContentGroup("Scout Wing", "ch01_m01");
            CombatContentSetup.CreateEnemyWaves("Raider Scouts", group, scouts,
                firstWaveSize: 5, extraShipsPerWave: 0, maxWaveSize: 5, totalWaves: 1, firstWaveDelay: 1.5f, timeBetweenWaves: 0f);
            Seal(group);
        }

        /// <summary>ch01_m02 "Hold the Line": survive 60 seconds of raider waves at the relay; 10 kills is the bonus.</summary>
        public static void PopulateHoldTheLine()
        {
            Vector3[] attackers = { new Vector3(0f, 50f, 900f), new Vector3(-850f, -30f, 420f), new Vector3(850f, 20f, 460f) };
            var relay = new Vector3(0f, -30f, 90f);
            CombatContentSetup.CombatStage stage = CombatContentSetup.BuildStage(202, new[] { attackers[0], attackers[1], attackers[2], relay });
            CombatContentSetup.CreateRelayStation(relay);
            AddMission("ch01_m02", stage);

            Transform group = ContentGroup("Strike Group", "ch01_m02");
            CombatContentSetup.CreateEnemyWaves("Raider Strike Waves", group, attackers,
                firstWaveSize: 3, extraShipsPerWave: 1, maxWaveSize: 6, totalWaves: 0, firstWaveDelay: 3f, timeBetweenWaves: 5f);
            Seal(group);
        }

        /// <summary>
        /// ch01_m03 "Into the Rift": reach the rift gate past two raider pickets. The same scene hosts Chapter 2's
        /// ch02_m01 placeholder: eight listening-post escorts and the beacon drop point.
        /// </summary>
        public static void PopulateIntoTheRift()
        {
            var riftGate = new Vector3(0f, 80f, 2400f);
            var picketA = new Vector3(-320f, 0f, 1100f);
            var picketB = new Vector3(360f, 60f, 1800f);
            var beaconDrop = new Vector3(-950f, 40f, 1500f);
            var escortA = new Vector3(-700f, 0f, 1150f);
            var escortB = new Vector3(-1100f, 60f, 1250f);
            CombatContentSetup.CombatStage stage = CombatContentSetup.BuildStage(303, new[] { riftGate, picketA, picketB, beaconDrop, escortA, escortB });
            AddMission("ch01_m03", stage);

            Transform rift = ContentGroup("Rift Run", "ch01_m03");
            CombatContentSetup.CreateWaypointGate("rift_gate", riftGate, rift);
            CombatContentSetup.CreateEnemyWaves("Rift Picket A", rift, new[] { picketA },
                firstWaveSize: 3, extraShipsPerWave: 0, maxWaveSize: 3, totalWaves: 1, firstWaveDelay: 2f, timeBetweenWaves: 0f);
            CombatContentSetup.CreateEnemyWaves("Rift Picket B", rift, new[] { picketB },
                firstWaveSize: 3, extraShipsPerWave: 0, maxWaveSize: 3, totalWaves: 1, firstWaveDelay: 2f, timeBetweenWaves: 0f);
            Seal(rift);

            Transform listeningPost = ContentGroup("Listening Post", "ch02_m01");
            CombatContentSetup.CreateWaypointGate("beacon_drop", beaconDrop, listeningPost);
            CombatContentSetup.CreateEnemyWaves("Listening Post Escort", listeningPost, new[] { escortA, escortB },
                firstWaveSize: 4, extraShipsPerWave: 0, maxWaveSize: 4, totalWaves: 2, firstWaveDelay: 2f, timeBetweenWaves: 4f);
            Seal(listeningPost);
        }

        private static void AddMission(string fallbackMissionId, CombatContentSetup.CombatStage stage)
        {
            var mission = new GameObject("Mission");
            mission.AddComponent<MissionSceneController>().FallbackMissionId = fallbackMissionId;
            mission.AddComponent<DevMissionHud>();
            mission.AddComponent<MissionCombatBinder>().Configure(stage.Player);
        }

        private static Transform ContentGroup(string name, params string[] missionIds)
        {
            var group = new GameObject($"{name} (mission content)");
            group.AddComponent<MissionContentGroup>().Configure(missionIds);
            return group.transform;
        }

        // Saved inactive, so nothing in a group wakes up before its mission launches.
        private static void Seal(Transform group)
        {
            foreach (Transform child in group) child.gameObject.SetActive(false);
        }
    }
}
