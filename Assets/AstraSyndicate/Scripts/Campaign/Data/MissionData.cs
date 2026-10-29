using System;
using System.Collections.Generic;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    [Serializable]
    public class MissionData
    {
        [Tooltip("Unique across the whole campaign, e.g. \"ch01_m01\". Save files reference missions by this id.")]
        public string id;

        public string title;

        [TextArea(2, 6)]
        public string briefing;

        [Tooltip("Scene loaded for this mission. Must be in Build Settings. Several missions may share a scene.")]
        public string sceneName;

        [Tooltip("The mission fails when this many seconds have passed. 0 = no limit.")]
        [Min(0f)]
        public float timeLimitSeconds;

        public List<ObjectiveData> objectives = new List<ObjectiveData>();
    }
}
