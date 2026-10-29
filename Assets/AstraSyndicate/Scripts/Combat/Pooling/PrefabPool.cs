using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace AstraSyndicate.Combat
{
    /// <summary>
    /// Pools of prefab instances, keyed by prefab. Instances live under a container object in the active
    /// scene, so they are destroyed with that scene, and the pools reset themselves when that happens.
    /// </summary>
    public static class PrefabPool<T> where T : Component
    {
        private const int MaxPooledPerPrefab = 512;

        private static readonly Dictionary<T, ObjectPool<T>> Pools = new Dictionary<T, ObjectPool<T>>();
        private static readonly Dictionary<T, T> PrefabOfInstance = new Dictionary<T, T>();
        private static PoolContainer _container;

        public static T Get(T prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));

            EnsureContainer();
            if (!Pools.TryGetValue(prefab, out ObjectPool<T> pool))
            {
                pool = new ObjectPool<T>(
                    () => CreateInstance(prefab),
                    instance => instance.gameObject.SetActive(true),
                    instance => instance.gameObject.SetActive(false),
                    DestroyInstance,
                    collectionCheck: false,
                    defaultCapacity: 16,
                    maxSize: MaxPooledPerPrefab);
                Pools.Add(prefab, pool);
            }
            return pool.Get();
        }

        /// <summary>Returns an instance to its pool; instances that did not come from a pool are destroyed.</summary>
        public static void Release(T instance)
        {
            if (instance == null) return;

            if (PrefabOfInstance.TryGetValue(instance, out T prefab) && Pools.TryGetValue(prefab, out ObjectPool<T> pool))
            {
                pool.Release(instance);
            }
            else
            {
                Object.Destroy(instance.gameObject);
            }
        }

        private static T CreateInstance(T prefab)
        {
            T instance = Object.Instantiate(prefab, _container.transform);
            PrefabOfInstance[instance] = prefab;
            return instance;
        }

        private static void DestroyInstance(T instance)
        {
            PrefabOfInstance.Remove(instance);
            if (instance != null) Object.Destroy(instance.gameObject);
        }

        private static void EnsureContainer()
        {
            if (_container != null) return;

            Clear();
            _container = new GameObject($"[{typeof(T).Name} Pool]").AddComponent<PoolContainer>();
            _container.Destroyed += Clear;
        }

        private static void Clear()
        {
            Pools.Clear();
            PrefabOfInstance.Clear();
            _container = null;
        }
    }
}
