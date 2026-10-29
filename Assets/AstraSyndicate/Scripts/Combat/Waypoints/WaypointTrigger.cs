using System;
using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.Combat
{
    /// <summary>Trigger volume that publishes WaypointReachedEvent when a player ship flies into it.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class WaypointTrigger : MonoBehaviour
    {
        [Tooltip("Id that reach_waypoint objectives refer to (their targetId).")]
        [SerializeField] private string waypointId = "waypoint";

        [Tooltip("Only player ships with this unit tag count. Empty = any player ship.")]
        [SerializeField] private string requiredUnitTag = "flagship";

        [SerializeField] private bool triggerOnce = true;

        private IEventBus _events;

        public event Action<WaypointTrigger> Reached;

        public string WaypointId => waypointId;

        public bool HasBeenReached { get; private set; }

        public void Configure(string id, string unitTagFilter, bool once)
        {
            waypointId = id;
            requiredUnitTag = unitTagFilter;
            triggerOnce = once;
        }

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            Services.TryGet(out _events);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (HasBeenReached && triggerOnce) return;

            Ship ship = other.GetComponentInParent<Ship>();
            if (ship == null || !ship.IsAlive || ship.Faction != Faction.Player) return;
            if (!string.IsNullOrEmpty(requiredUnitTag) && ship.UnitTag != requiredUnitTag) return;

            HasBeenReached = true;
            Reached?.Invoke(this);
            _events?.Publish(new WaypointReachedEvent(waypointId));
        }
    }
}
