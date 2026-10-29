using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// Placed in every mission scene. Starts the mission the campaign requested, or the fallback
    /// mission when the scene is played directly in the editor.
    /// </summary>
    public sealed class MissionSceneController : MonoBehaviour
    {
        [Tooltip("Mission to run when this scene is played directly instead of being loaded by the campaign.")]
        [SerializeField] private string fallbackMissionId;

        [Tooltip("Skip the briefing and start the mission immediately.")]
        [SerializeField] private bool skipBriefing;

        private IMissionStateManager _missions;

        public string FallbackMissionId
        {
            get => fallbackMissionId;
            set => fallbackMissionId = value;
        }

        private void Start()
        {
            if (!Services.TryGet(out ICampaignFlow flow)
                || !Services.TryGet(out ICampaignRepository campaign)
                || !Services.TryGet(out _missions))
            {
                Debug.LogError("[Mission] Campaign services are missing. Run Tools > Astra Syndicate > Setup Sprint 1 Content.", this);
                return;
            }

            MissionData mission = flow.ConsumePendingMission() ?? campaign.GetMission(fallbackMissionId);
            if (mission == null)
            {
                Debug.LogError($"[Mission] Nothing to run: no mission was requested and fallback '{fallbackMissionId}' does not exist.", this);
                return;
            }

            if (_missions.BeginMission(mission) && skipBriefing) _missions.Launch();
        }

        private void OnDestroy()
        {
            // Leaving the scene by any route other than the campaign flow must not leave a mission running.
            if (_missions != null && _missions.State != MissionState.Idle) _missions.Exit();
        }
    }
}
