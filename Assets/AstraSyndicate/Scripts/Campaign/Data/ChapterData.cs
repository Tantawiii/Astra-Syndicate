using System;
using System.Collections.Generic;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// One campaign chapter. Shared schema for ChapterConfig assets and JSON chapter files, so field
    /// names are the JSON keys.
    /// </summary>
    [Serializable]
    public class ChapterData
    {
        [Tooltip("Unique chapter id, e.g. \"ch01\".")]
        public string id;

        public string title;

        [TextArea(2, 6)]
        public string synopsis;

        [Tooltip("Chapters are played in ascending order.")]
        public int order;

        public List<MissionData> missions = new List<MissionData>();
    }
}
