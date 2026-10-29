using System;

namespace AstraSyndicate.Campaign
{
    /// <summary>Saved progress for one mission.</summary>
    [Serializable]
    public class MissionRecord
    {
        public string missionId;
        public bool completed;

        /// <summary>Times the mission was launched (past the briefing).</summary>
        public int attempts;

        public int completions;

        /// <summary>Fastest completion in seconds; only meaningful once <see cref="completions"/> is above 0.</summary>
        public float bestTimeSeconds;

        public MissionRecord Clone()
        {
            return (MissionRecord)MemberwiseClone();
        }
    }
}
