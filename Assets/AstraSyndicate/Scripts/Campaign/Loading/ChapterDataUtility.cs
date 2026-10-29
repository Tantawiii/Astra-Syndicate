using System.Collections.Generic;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    internal static class ChapterDataUtility
    {
        public static ChapterData Clone(ChapterData source)
        {
            return JsonUtility.FromJson<ChapterData>(JsonUtility.ToJson(source));
        }

        /// <summary>Replaces missing lists with empty ones, drops null entries and trims identifiers.</summary>
        public static ChapterData Normalize(ChapterData chapter)
        {
            chapter.id = Trim(chapter.id);
            chapter.missions ??= new List<MissionData>();
            chapter.missions.RemoveAll(mission => mission == null);

            foreach (MissionData mission in chapter.missions)
            {
                mission.id = Trim(mission.id);
                mission.sceneName = Trim(mission.sceneName);
                mission.objectives ??= new List<ObjectiveData>();
                mission.objectives.RemoveAll(objective => objective == null);

                foreach (ObjectiveData objective in mission.objectives)
                {
                    objective.id = Trim(objective.id);
                    objective.type = Trim(objective.type);
                    objective.targetId = Trim(objective.targetId);
                }
            }
            return chapter;
        }

        private static string Trim(string value)
        {
            return value?.Trim() ?? string.Empty;
        }
    }
}
