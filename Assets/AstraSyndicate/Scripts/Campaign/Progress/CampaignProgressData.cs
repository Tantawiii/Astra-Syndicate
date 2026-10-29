using System;
using System.Collections.Generic;

namespace AstraSyndicate.Campaign
{
    /// <summary>Everything the offline campaign saves. Unlock state is derived, not stored.</summary>
    [Serializable]
    public class CampaignProgressData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public string lastMissionId;
        public List<MissionRecord> missions = new List<MissionRecord>();
    }
}
