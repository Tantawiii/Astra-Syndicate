using System;
using AstraSyndicate.Core;
using AstraSyndicate.SceneFlow;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// Drives the offline campaign loop: menu, mission scene, result, next mission. It records attempts
    /// and results through the progress service, so mission scenes never touch the save system.
    /// </summary>
    public sealed class CampaignDirector : ICampaignFlow, IDisposable
    {
        private readonly ICampaignRepository _campaign;
        private readonly IProgressService _progress;
        private readonly IMissionStateManager _missions;
        private readonly ISceneLoader _scenes;
        private readonly IEventBus _events;
        private readonly string _menuScene;
        private MissionData _pendingMission;

        public CampaignDirector(
            ICampaignRepository campaign,
            IProgressService progress,
            IMissionStateManager missions,
            ISceneLoader scenes,
            IEventBus events,
            string menuSceneName)
        {
            _campaign = campaign ?? throw new ArgumentNullException(nameof(campaign));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _missions = missions ?? throw new ArgumentNullException(nameof(missions));
            _scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            if (string.IsNullOrEmpty(menuSceneName)) throw new ArgumentException("A main menu scene is required.", nameof(menuSceneName));
            _menuScene = menuSceneName;

            _events.Subscribe<MissionStateChangedEvent>(OnMissionStateChanged);
            _events.Subscribe<MissionEndedEvent>(OnMissionEnded);
        }

        public MissionData CurrentMission { get; private set; }

        public bool IsBusy => _scenes.IsLoading;

        public async Awaitable<bool> StartMissionAsync(string missionId)
        {
            MissionData mission = _campaign.GetMission(missionId);
            if (mission == null)
            {
                Debug.LogError($"[Campaign] Unknown mission '{missionId}'.");
                return false;
            }
            if (!_progress.IsMissionUnlocked(mission.id))
            {
                Debug.LogWarning($"[Campaign] Mission '{mission.id}' is locked.");
                return false;
            }
            return await LoadMissionAsync(mission);
        }

        public async Awaitable<bool> RetryAsync()
        {
            MissionData mission = CurrentMission;
            if (mission == null) return false;
            return await LoadMissionAsync(mission);
        }

        public async Awaitable<bool> NextMissionAsync()
        {
            MissionData next = CurrentMission != null
                ? _campaign.GetNextMission(CurrentMission.id)
                : _progress.GetContinueMission();

            if (next == null || !_progress.IsMissionUnlocked(next.id)) return await ReturnToMenuAsync();
            return await LoadMissionAsync(next);
        }

        public async Awaitable<bool> ReturnToMenuAsync()
        {
            if (_scenes.IsLoading) return false;

            _missions.Exit();
            _pendingMission = null;
            CurrentMission = null;
            return await _scenes.LoadSceneAsync(_menuScene);
        }

        public MissionData ConsumePendingMission()
        {
            MissionData mission = _pendingMission;
            _pendingMission = null;
            return mission;
        }

        public void Dispose()
        {
            _events.Unsubscribe<MissionStateChangedEvent>(OnMissionStateChanged);
            _events.Unsubscribe<MissionEndedEvent>(OnMissionEnded);
        }

        private async Awaitable<bool> LoadMissionAsync(MissionData mission)
        {
            if (_scenes.IsLoading) return false;

            _missions.Exit();
            _pendingMission = mission;
            CurrentMission = mission;

            bool loaded = await _scenes.LoadSceneAsync(mission.sceneName);
            if (!loaded) _pendingMission = null;
            return loaded;
        }

        private void OnMissionStateChanged(MissionStateChangedEvent evt)
        {
            // Also covers mission scenes started directly in the editor, which bypass StartMissionAsync.
            if (evt.Current == MissionState.Briefing)
            {
                CurrentMission = _missions.CurrentMission;
            }
            else if (evt.Previous == MissionState.Briefing && evt.Current == MissionState.Running)
            {
                _progress.MarkStarted(evt.MissionId);
            }
        }

        private void OnMissionEnded(MissionEndedEvent evt)
        {
            _progress.RecordResult(evt.Result);
        }
    }
}
