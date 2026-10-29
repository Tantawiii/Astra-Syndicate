using System;
using System.Collections.Generic;

namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// Creates runtime objectives from config data. New objective types are added by registering a
    /// creator under a new type key; no existing code has to change.
    /// </summary>
    public sealed class ObjectiveFactory
    {
        private readonly Dictionary<string, Func<ObjectiveData, IObjective>> _creators =
            new Dictionary<string, Func<ObjectiveData, IObjective>>(StringComparer.Ordinal);

        public IEnumerable<string> RegisteredTypes => _creators.Keys;

        /// <summary>A factory with the built-in objective types registered.</summary>
        public static ObjectiveFactory CreateDefault()
        {
            var factory = new ObjectiveFactory();
            factory.Register(ObjectiveTypes.DestroyEnemies, data => new DestroyEnemiesObjective(data));
            factory.Register(ObjectiveTypes.Survive, data => new SurviveObjective(data));
            factory.Register(ObjectiveTypes.ReachWaypoint, data => new ReachWaypointObjective(data));
            return factory;
        }

        public void Register(string type, Func<ObjectiveData, IObjective> creator)
        {
            if (string.IsNullOrEmpty(type)) throw new ArgumentException("An objective type key is required.", nameof(type));
            _creators[type] = creator ?? throw new ArgumentNullException(nameof(creator));
        }

        public bool IsRegistered(string type)
        {
            return !string.IsNullOrEmpty(type) && _creators.ContainsKey(type);
        }

        public bool TryCreate(ObjectiveData data, out IObjective objective)
        {
            objective = null;
            if (data == null || !IsRegistered(data.type)) return false;

            objective = _creators[data.type](data);
            return objective != null;
        }
    }
}
