using System.Collections;
using System.Collections.Generic;
using AstraSyndicate.AI;
using AstraSyndicate.Combat;
using AstraSyndicate.Core;
using AstraSyndicate.PlayerControl;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AstraSyndicate.Tests
{
    /// <summary>
    /// Smoke test of the generated CombatSandbox scene with the real prefabs and configs: waves spawn and fly,
    /// wingmen are wired to the flagship, and the flagship's own guns can destroy a raider. Any error logged
    /// along the way (effects, pooling, AI) fails the test.
    /// </summary>
    public class CombatSandboxPlayModeTests
    {
        private const string SandboxScene = "CombatSandbox";

        private readonly List<UnitDestroyedEvent> _destroyed = new List<UnitDestroyedEvent>();
        private EventBus _events;

        [SetUp]
        public void SetUp()
        {
            _destroyed.Clear();
            _events = new EventBus();
            _events.Subscribe<UnitDestroyedEvent>(e => _destroyed.Add(e));
            Services.Register<IEventBus>(_events);
        }

        [TearDown]
        public void TearDown()
        {
            Services.Unregister<IEventBus>();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Sandbox_SpawnsFlyingWave_AndFlagshipGunsDestroyARaider()
        {
            if (!Application.CanStreamedLevelBeLoaded(SandboxScene)) Assert.Ignore("Run Tools > Astra Syndicate > Setup Sprint 1 Content first.");
            yield return SceneManager.LoadSceneAsync(SandboxScene);

            var flagship = Object.FindAnyObjectByType<PlayerFlightController>();
            var spawner = Object.FindAnyObjectByType<EnemyWaveSpawner>();
            Assert.IsNotNull(flagship, "The sandbox needs a player flagship.");
            Assert.IsNotNull(spawner, "The sandbox needs an enemy wave spawner.");
            foreach (WingmanBrain wingman in Object.FindObjectsByType<WingmanBrain>(FindObjectsSortMode.None))
            {
                Assert.AreEqual(flagship.Ship, wingman.Leader, $"{wingman.name} is not following the flagship.");
            }

            yield return WaitUntil(() => spawner.AliveCount > 0, 6f);
            Assert.AreEqual(3, ShipRegistry.CountAlive(Faction.Enemy), "The first wave should bring three raiders.");

            yield return new WaitForSeconds(1.5f);
            Ship raider = ShipRegistry.FindNearestHostile(Faction.Player, flagship.transform.position, 5000f);
            Assert.Greater(raider.Velocity.magnitude, 1f, "Spawned raiders should be flying their patrol.");

            // Park the flagship and put a passive raider in front of its guns.
            flagship.enabled = false;
            flagship.Motor.SetThrottle(0f);
            raider.GetComponent<EnemyBrain>().enabled = false;
            raider.GetComponent<ShipMotor>().enabled = false;
            var raiderBody = raider.GetComponent<Rigidbody>();
            raiderBody.linearVelocity = Vector3.zero;
            raiderBody.angularVelocity = Vector3.zero;
            raiderBody.position = flagship.transform.position + flagship.transform.forward * 150f;
            yield return new WaitForFixedUpdate();

            WeaponSystem[] guns = flagship.GetComponentsInChildren<WeaponSystem>();
            float end = Time.time + 10f;
            while (raider != null && Time.time < end)
            {
                foreach (WeaponSystem gun in guns) gun.TryFire(raider.Position);
                yield return null;
            }

            Assert.IsTrue(raider == null, "The flagship's cannons should destroy a raider parked in front of them.");
            Assert.IsTrue(_destroyed.Exists(e => e.Faction == Faction.Enemy && e.Tag == "raider"), "A UnitDestroyedEvent for the raider was expected.");

            // Let the impact and explosion effects play out and return to their pools.
            yield return new WaitForSeconds(2f);
        }

        // Real time, so the timeout still expires if game time is frozen.
        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        }
    }
}
