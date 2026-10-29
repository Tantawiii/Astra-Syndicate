using System;
using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// Scene content (encounters, waypoints, set pieces) that belongs to specific missions. Its children stay
    /// inactive until one of those missions launches, so several missions can share a scene and each brings
    /// only its own enemies and goals.
    /// </summary>
    public sealed class MissionContentGroup : MonoBehaviour
    {
        [Tooltip("Missions this content belongs to. Empty = every mission played in this scene.")]
        [SerializeField] private string[] missionIds = Array.Empty<string>();

        private IEventBus _events;

        public bool IsActive { get; private set; }

        public void Configure(params string[] ids)
        {
            missionIds = ids ?? Array.Empty<string>();
        }

        public bool AppliesTo(string missionId)
        {
            return missionIds.Length == 0 || Array.IndexOf(missionIds, missionId) >= 0;
        }

        private void Awake()
        {
            SetContentActive(false);
            if (Services.TryGet(out _events)) _events.Subscribe<MissionStateChangedEvent>(OnMissionStateChanged);
        }

        private void Start()
        {
            // Covers a mission that was already launched before this object woke up.
            if (Services.TryGet(out IMissionStateManager missions) && missions.State == MissionState.Running && missions.CurrentMission != null)
            {
                Activate(missions.CurrentMission.id);
            }
        }

        private void OnDestroy()
        {
            _events?.Unsubscribe<MissionStateChangedEvent>(OnMissionStateChanged);
        }

        private void OnMissionStateChanged(MissionStateChangedEvent evt)
        {
            if (evt.Previous == MissionState.Briefing && evt.Current == MissionState.Running) Activate(evt.MissionId);
        }

        private void Activate(string missionId)
        {
            if (!IsActive && AppliesTo(missionId)) SetContentActive(true);
        }

        private void SetContentActive(bool active)
        {
            IsActive = active;
            foreach (Transform child in transform) child.gameObject.SetActive(active);
        }
    }
}
