using System;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    [Serializable]
    public class ObjectiveData
    {
        [Tooltip("Unique within the mission, e.g. \"destroy_scouts\".")]
        public string id;

        [Tooltip("Objective type key: destroy_enemies, survive or reach_waypoint.")]
        [ObjectiveType]
        public string type = ObjectiveTypes.DestroyEnemies;

        public string description;

        [Tooltip("destroy_enemies: how many enemies must be destroyed.")]
        [Min(0)]
        public int targetCount;

        [Tooltip("survive: how many seconds the fleet must survive.")]
        [Min(0f)]
        public float durationSeconds;

        [Tooltip("destroy_enemies: optional unit tag filter. reach_waypoint: the waypoint id.")]
        public string targetId;

        [Tooltip("Optional objectives are tracked and rewarded but not needed to complete the mission.")]
        public bool optional;
    }
}
