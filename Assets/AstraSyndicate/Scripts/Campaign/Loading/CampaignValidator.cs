using System;
using System.Collections.Generic;

namespace AstraSyndicate.Campaign
{
    /// <summary>Checks loaded campaign content for mistakes that would break a mission at runtime.</summary>
    public static class CampaignValidator
    {
        /// <param name="sceneExists">Optional check that a mission's scene can be loaded (e.g. is in Build Settings).</param>
        public static ValidationReport Validate(ICampaignRepository campaign, ObjectiveFactory objectives, Func<string, bool> sceneExists = null)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            if (objectives == null) throw new ArgumentNullException(nameof(objectives));

            var report = new ValidationReport();
            if (campaign.Chapters.Count == 0) report.AddError("The campaign has no chapters.");

            foreach (ChapterData chapter in campaign.Chapters)
            {
                if (string.IsNullOrWhiteSpace(chapter.title)) report.AddWarning($"Chapter '{chapter.id}' has no title.");
                if (chapter.missions.Count == 0) report.AddWarning($"Chapter '{chapter.id}' has no missions.");

                foreach (MissionData mission in chapter.missions)
                {
                    ValidateMission(mission, objectives, sceneExists, report);
                }
            }
            return report;
        }

        private static void ValidateMission(MissionData mission, ObjectiveFactory objectives, Func<string, bool> sceneExists, ValidationReport report)
        {
            string where = $"Mission '{mission.id}'";
            if (string.IsNullOrWhiteSpace(mission.title)) report.AddWarning($"{where} has no title.");

            if (string.IsNullOrEmpty(mission.sceneName)) report.AddError($"{where} has no sceneName.");
            else if (sceneExists != null && !sceneExists(mission.sceneName)) report.AddError($"{where}: scene '{mission.sceneName}' is not in Build Settings.");

            if (mission.objectives.Count == 0)
            {
                report.AddError($"{where} has no objectives.");
                return;
            }

            var ids = new HashSet<string>();
            int required = 0;
            foreach (ObjectiveData objective in mission.objectives)
            {
                string objectiveName = string.IsNullOrEmpty(objective.id) ? "(no id)" : objective.id;
                if (string.IsNullOrEmpty(objective.id)) report.AddError($"{where} has an objective without an id.");
                else if (!ids.Add(objective.id)) report.AddError($"{where} has two objectives with id '{objective.id}'.");

                if (!objectives.IsRegistered(objective.type))
                {
                    report.AddError($"{where}, objective '{objectiveName}': unknown type '{objective.type}'.");
                    continue;
                }
                if (!objective.optional) required++;

                switch (objective.type)
                {
                    case ObjectiveTypes.DestroyEnemies when objective.targetCount <= 0:
                        report.AddError($"{where}, objective '{objectiveName}': targetCount must be greater than 0.");
                        break;
                    case ObjectiveTypes.Survive when objective.durationSeconds <= 0f:
                        report.AddError($"{where}, objective '{objectiveName}': durationSeconds must be greater than 0.");
                        break;
                    case ObjectiveTypes.Survive when !objective.optional && mission.timeLimitSeconds > 0f && objective.durationSeconds >= mission.timeLimitSeconds:
                        report.AddError($"{where}, objective '{objectiveName}': surviving {objective.durationSeconds}s is impossible within the {mission.timeLimitSeconds}s time limit.");
                        break;
                    case ObjectiveTypes.ReachWaypoint when string.IsNullOrEmpty(objective.targetId):
                        report.AddError($"{where}, objective '{objectiveName}': targetId must name the waypoint.");
                        break;
                }
            }

            if (required == 0) report.AddError($"{where} needs at least one required (non-optional) objective.");
        }
    }
}
