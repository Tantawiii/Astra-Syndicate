using System;
using System.Collections.Generic;
using UnityEngine;

namespace AstraSyndicate.Core
{
    /// <summary>
    /// Minimal service registry. Only the composition root (GameBootstrapper) registers services,
    /// and only MonoBehaviours, which cannot use constructor injection, should resolve from it.
    /// Plain C# classes receive their dependencies through their constructors.
    /// </summary>
    public static class Services
    {
        private static readonly Dictionary<Type, object> Registry = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            Registry[typeof(T)] = service;
        }

        public static void Unregister<T>() where T : class
        {
            Registry.Remove(typeof(T));
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Registry.TryGetValue(typeof(T), out object instance))
            {
                service = (T)instance;
                return true;
            }

            service = null;
            return false;
        }

        public static T Get<T>() where T : class
        {
            if (TryGet(out T service)) return service;
            throw new InvalidOperationException(
                $"Service {typeof(T).Name} is not registered. The game must be started through GameBootstrapper.");
        }

        public static void Clear()
        {
            Registry.Clear();
        }

        // Keeps the registry clean when "Enter Play Mode Options" skip the domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeEnter()
        {
            Clear();
        }
    }
}
