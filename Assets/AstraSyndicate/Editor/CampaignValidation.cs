using System.IO;
using AstraSyndicate.App;
using AstraSyndicate.Campaign;
using UnityEditor;
using UnityEngine;

namespace AstraSyndicate.EditorTools
{
    public static class CampaignValidation
    {
        [MenuItem("Tools/Astra Syndicate/Validate Campaign", priority = 20)]
        private static void ValidateFromMenu()
        {
            var config = Resources.Load<GameConfig>(GameConfig.ResourcesPath);
            if (config == null || config.Catalog == null)
            {
                Debug.LogError("[Campaign] No GameConfig or Campaign Catalog found. Run Tools > Astra Syndicate > Setup Sprint 1 Content first.");
                return;
            }
            ValidateAndLog(config.Catalog);
        }

        /// <summary>Loads the catalog exactly as the game does, validates it and logs the result.</summary>
        public static bool ValidateAndLog(CampaignCatalog catalog)
        {
            var campaign = new CampaignRepository(catalog.CreateSources());
            ValidationReport report = campaign.Report;
            report.Merge(CampaignValidator.Validate(campaign, ObjectiveFactory.CreateDefault(), IsSceneInBuildSettings));
            report.Log("[Campaign]", catalog);

            string summary = $"{campaign.Chapters.Count} chapters, {campaign.MissionsInOrder.Count} missions, " +
                             $"{report.Errors.Count} errors, {report.Warnings.Count} warnings";
            if (report.HasErrors) Debug.LogError($"[Campaign] Validation FAILED: {summary}", catalog);
            else Debug.Log($"[Campaign] Validation OK: {summary}", catalog);
            return !report.HasErrors;
        }

        private static bool IsSceneInBuildSettings(string sceneName)
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && Path.GetFileNameWithoutExtension(scene.path) == sceneName) return true;
            }
            return false;
        }
    }
}
