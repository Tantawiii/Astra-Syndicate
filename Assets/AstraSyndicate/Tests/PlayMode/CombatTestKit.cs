using System;
using System.Reflection;
using AstraSyndicate.Combat;
using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.Tests
{
    /// <summary>Builds minimal combat objects in code, configured before they wake up.</summary>
    internal static class CombatTestKit
    {
        public static ShipConfig ShipConfig(float hull = 100f)
        {
            var config = ScriptableObject.CreateInstance<ShipConfig>();
            SetField(config, "maxHull", hull);
            return config;
        }

        public static WeaponConfig WeaponConfig(Projectile projectile, float shotsPerSecond, float damage)
        {
            var config = ScriptableObject.CreateInstance<WeaponConfig>();
            SetField(config, "projectilePrefab", projectile);
            SetField(config, "shotsPerSecond", shotsPerSecond);
            SetField(config, "damage", damage);
            SetField(config, "spreadDegrees", 0f);
            SetField(config, "gimbalDegrees", 0f);
            return config;
        }

        /// <summary>Sets a private serialized config field, failing loudly if it was renamed.</summary>
        public static void SetField(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (info == null) throw new ArgumentException($"{target.GetType().Name} has no field '{field}'.");
            info.SetValue(target, value);
        }

        /// <summary>A 6 x 2 x 6 m box ship. <paramref name="addComponents"/> runs before the ship is activated.</summary>
        public static Ship CreateShip(string name, Faction faction, Vector3 position, ShipConfig config,
            string unitTag = null, bool withMotor = false, Action<GameObject> addComponents = null)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.position = position;

            var body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            go.AddComponent<BoxCollider>().size = new Vector3(6f, 2f, 6f);

            var ship = go.AddComponent<Ship>();
            ship.Configure(config, faction, unitTag);
            if (withMotor) go.AddComponent<ShipMotor>();
            addComponents?.Invoke(go);

            go.SetActive(true);
            return ship;
        }

        /// <summary>An inactive projectile used as the pool's prefab.</summary>
        public static Projectile CreateProjectileTemplate()
        {
            var go = new GameObject("TestBolt");
            go.SetActive(false);
            return go.AddComponent<Projectile>();
        }

        public static Projectile Launch(Projectile template, Vector3 from, Vector3 velocity, float damage, Faction faction)
        {
            Projectile shot = PrefabPool<Projectile>.Get(template);
            shot.Launch(from, velocity, damage, 500f, null, faction, null);
            return shot;
        }
    }
}
