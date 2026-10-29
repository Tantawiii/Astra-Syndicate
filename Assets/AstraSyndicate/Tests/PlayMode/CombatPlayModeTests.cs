using System;
using System.Collections;
using System.Collections.Generic;
using AstraSyndicate.AI;
using AstraSyndicate.Combat;
using AstraSyndicate.Core;
using AstraSyndicate.PlayerControl;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static AstraSyndicate.Tests.CombatTestKit;
using Object = UnityEngine.Object;

namespace AstraSyndicate.Tests
{
    /// <summary>Combat systems with real physics: projectiles, weapons, flight, AI, orders and waypoints.</summary>
    public class CombatPlayModeTests
    {
        private const float Timeout = 3f;

        private readonly List<Object> _created = new List<Object>();
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
            foreach (Object created in _created)
            {
                if (created == null) continue;
                Object.DestroyImmediate(created is Component component ? component.gameObject : created);
            }
            _created.Clear();
            Services.Unregister<IEventBus>();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Projectiles_DestroyHostileShip_AndPublishUnitDestroyed()
        {
            Ship target = Track(CreateShip("Target", Faction.Enemy, new Vector3(0f, 0f, 40f), Track(ShipConfig(hull: 10f)), "raider"));
            Projectile template = Track(CreateProjectileTemplate());
            yield return new WaitForFixedUpdate();

            Launch(template, Vector3.zero, Vector3.forward * 200f, 6f, Faction.Player);
            Launch(template, new Vector3(0f, 0f, -2f), Vector3.forward * 200f, 6f, Faction.Player);
            yield return WaitUntil(() => target == null);

            Assert.IsTrue(target == null, "Two 6-damage hits should destroy a 10-hull ship.");
            Assert.AreEqual(1, _destroyed.Count);
            Assert.AreEqual(Faction.Enemy, _destroyed[0].Faction);
            Assert.AreEqual("raider", _destroyed[0].Tag);
        }

        [UnityTest]
        public IEnumerator Projectiles_PassThroughFriendlyShips()
        {
            ShipConfig config = Track(ShipConfig(hull: 100f));
            Ship friendly = Track(CreateShip("Friendly", Faction.Player, new Vector3(0f, 0f, 30f), config));
            Ship enemy = Track(CreateShip("Enemy", Faction.Enemy, new Vector3(0f, 0f, 60f), config));
            Projectile template = Track(CreateProjectileTemplate());
            yield return new WaitForFixedUpdate();

            Launch(template, Vector3.zero, Vector3.forward * 200f, 10f, Faction.Player);
            yield return WaitUntil(() => enemy.Hull < enemy.MaxHull);

            Assert.AreEqual(friendly.MaxHull, friendly.Hull, "Shots must not damage their own faction.");
            Assert.AreEqual(enemy.MaxHull - 10f, enemy.Hull, 1e-3f);
        }

        [UnityTest]
        public IEnumerator Projectiles_AreStoppedByScenery()
        {
            GameObject rock = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            rock.transform.position = new Vector3(0f, 0f, 30f);
            rock.transform.localScale = Vector3.one * 6f;
            Ship enemy = Track(CreateShip("Enemy", Faction.Enemy, new Vector3(0f, 0f, 60f), Track(ShipConfig())));
            Projectile template = Track(CreateProjectileTemplate());
            yield return new WaitForFixedUpdate();

            Projectile shot = Launch(template, Vector3.zero, Vector3.forward * 200f, 10f, Faction.Player);
            yield return WaitUntil(() => !shot.InFlight);

            Assert.IsFalse(shot.InFlight);
            Assert.AreEqual(enemy.MaxHull, enemy.Hull, "The asteroid should have absorbed the shot.");
        }

        [UnityTest]
        public IEnumerator WeaponSystem_RespectsFireRate_AndItsShotsHit()
        {
            Projectile template = Track(CreateProjectileTemplate());
            WeaponConfig weapon = Track(WeaponConfig(template, shotsPerSecond: 10f, damage: 5f));

            ShipConfig config = Track(ShipConfig());
            Ship shooter = Track(CreateShip("Shooter", Faction.Player, Vector3.zero, config, addComponents: go =>
            {
                Transform muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(go.transform, false);
                muzzle.localPosition = new Vector3(0f, 0f, 4f);
                go.AddComponent<WeaponSystem>().Configure(weapon, muzzle);
            }));
            Ship target = Track(CreateShip("Target", Faction.Enemy, new Vector3(0f, 0f, 80f), config));
            yield return new WaitForFixedUpdate();

            var guns = shooter.GetComponent<WeaponSystem>();
            Assert.IsTrue(guns.TryFire());
            Assert.IsFalse(guns.TryFire(), "The second shot must wait for the fire-rate interval.");
            yield return WaitUntil(() => target.Hull < target.MaxHull);

            Assert.AreEqual(target.MaxHull - 5f, target.Hull, 1e-3f);
        }

        [UnityTest]
        public IEnumerator ShipMotor_TurnsTowardHeading_ReachesSpeed_AndStaysLevel()
        {
            Ship ship = Track(CreateShip("Pilot", Faction.Player, Vector3.zero, Track(ShipConfig()), withMotor: true));
            var motor = ship.GetComponent<ShipMotor>();
            motor.SetHeading(Vector3.right);
            motor.SetThrottle(1f);

            yield return new WaitForSeconds(3f);

            Assert.Less(Vector3.Angle(ship.transform.forward, Vector3.right), 10f, "The nose should have turned to the requested heading.");
            Assert.Greater(Vector3.Dot(motor.Velocity, ship.transform.forward), 0.8f * motor.MaxSpeed, "The ship should be near full speed.");
            Assert.Greater(Vector3.Dot(ship.transform.up, Vector3.up), 0.9f, "The ship should stay level.");
        }

        [UnityTest]
        public IEnumerator EnemyBrain_AttacksHostileInRange_ThenRetreatsWhenBadlyDamaged()
        {
            AIConfig ai = Track(ScriptableObject.CreateInstance<AIConfig>());
            ShipConfig config = Track(ShipConfig(hull: 100f));
            Ship player = Track(CreateShip("Player", Faction.Player, new Vector3(0f, 0f, 300f), config));
            Ship enemy = Track(CreateShip("Enemy", Faction.Enemy, Vector3.zero, config, withMotor: true,
                addComponents: go => go.AddComponent<EnemyBrain>().Configure(ai)));
            var brain = enemy.GetComponent<EnemyBrain>();

            yield return null;
            yield return null;
            Assert.AreEqual(EnemyState.Attack, brain.State);
            Assert.AreEqual(player, brain.Target);

            enemy.ApplyDamage(new DamageInfo(75f, enemy.Position, player));
            yield return null;
            Assert.AreEqual(EnemyState.Retreat, brain.State);
        }

        [UnityTest]
        public IEnumerator Wingman_FollowsAttackOrder_AndFormsUpAgain()
        {
            AIConfig ai = Track(ScriptableObject.CreateInstance<AIConfig>());
            ShipConfig config = Track(ShipConfig());
            Ship leader = Track(CreateShip("Leader", Faction.Player, Vector3.zero, config, withMotor: true));
            Ship wingman = Track(CreateShip("Wingman", Faction.Player, new Vector3(-14f, 0f, -14f), config, withMotor: true,
                addComponents: go => go.AddComponent<WingmanBrain>().Configure(ai, leader, 0)));
            // Far outside the defend radius, so the wingman only engages when ordered to.
            Ship enemy = Track(CreateShip("Enemy", Faction.Enemy, new Vector3(0f, 0f, 1500f), config));
            var brain = wingman.GetComponent<WingmanBrain>();

            yield return null;
            Assert.IsFalse(brain.IsEngaging);

            _events.Publish(new FleetOrderIssuedEvent(Faction.Player, FleetOrder.AttackTarget, enemy));
            yield return null;
            Assert.AreEqual(enemy, brain.Target);

            _events.Publish(new FleetOrderIssuedEvent(Faction.Player, FleetOrder.FormUp, null));
            yield return null;
            Assert.IsFalse(brain.IsEngaging);
        }

        [UnityTest]
        public IEnumerator Flagship_WhenDestroyed_PublishesPlayerDefeated()
        {
            var defeats = new List<PlayerDefeatedEvent>();
            _events.Subscribe<PlayerDefeatedEvent>(e => defeats.Add(e));
            Ship flagship = Track(CreateShip("Flagship", Faction.Player, Vector3.zero, Track(ShipConfig()), "flagship", withMotor: true,
                addComponents: go => go.AddComponent<PlayerFlightController>()));
            yield return null;

            flagship.ApplyDamage(new DamageInfo(1000f, flagship.Position, null));
            yield return null;

            Assert.AreEqual(1, defeats.Count);
            Assert.IsTrue(flagship == null);
        }

        [UnityTest]
        public IEnumerator WaypointTrigger_FiresOnlyForTheFlagship()
        {
            var reached = new List<string>();
            _events.Subscribe<WaypointReachedEvent>(e => reached.Add(e.WaypointId));

            GameObject gate = Track(new GameObject("Gate"));
            gate.transform.position = new Vector3(0f, 0f, 200f);
            gate.AddComponent<SphereCollider>().radius = 15f;
            gate.AddComponent<WaypointTrigger>().Configure("gate_a", "flagship", once: true);

            ShipConfig config = Track(ShipConfig());
            Ship wingman = Track(CreateShip("Wingman", Faction.Player, new Vector3(0f, 0f, 100f), config, "wingman"));
            Ship flagship = Track(CreateShip("Flagship", Faction.Player, Vector3.zero, config, "flagship"));
            yield return new WaitForFixedUpdate();

            wingman.GetComponent<Rigidbody>().position = gate.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0, reached.Count, "Only the flagship should trigger the waypoint.");

            flagship.GetComponent<Rigidbody>().position = gate.transform.position;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            CollectionAssert.AreEqual(new[] { "gate_a" }, reached);
        }

        [UnityTest]
        public IEnumerator TargetSelection_PicksTheHostileNearestTheAim_ThenCycles()
        {
            ShipConfig config = Track(ShipConfig());
            Ship ahead = Track(CreateShip("Ahead", Faction.Enemy, new Vector3(0f, 0f, 300f), config));
            Ship offAxis = Track(CreateShip("Off axis", Faction.Enemy, new Vector3(200f, 0f, 300f), config));
            Track(CreateShip("Friend", Faction.Player, new Vector3(0f, 0f, 150f), config));
            Track(CreateShip("Out of range", Faction.Enemy, new Vector3(0f, 0f, 5000f), config));
            yield return null;

            Ship first = TargetSelection.SelectByAim(Faction.Player, Vector3.zero, Vector3.forward, 1000f, null);
            Ship second = TargetSelection.SelectByAim(Faction.Player, Vector3.zero, Vector3.forward, 1000f, first);

            Assert.AreEqual(ahead, first);
            Assert.AreEqual(offAxis, second);
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }

        private static IEnumerator WaitUntil(Func<bool> condition)
        {
            float end = Time.time + Timeout;
            while (!condition() && Time.time < end) yield return null;
        }
    }
}
