using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// Unity-side driver for the mission state manager: ticks it every frame, runs game time only while a
    /// mission is in progress (or outside missions), and pauses the mission when the app is sent to the background.
    /// </summary>
    public sealed class MissionRunner : MonoBehaviour
    {
        private IMissionStateManager _missions;
        private IEventBus _events;

        public void Initialize(IMissionStateManager missions, IEventBus events)
        {
            _missions = missions;
            _events = events;
            _events.Subscribe<MissionStateChangedEvent>(OnMissionStateChanged);
        }

        private void Update()
        {
            _missions?.Tick(Time.deltaTime);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) _missions?.Pause();
        }

        private void OnDestroy()
        {
            _events?.Unsubscribe<MissionStateChangedEvent>(OnMissionStateChanged);
            Time.timeScale = 1f;
        }

        // The world waits for launch during the briefing and stops behind the pause and results screens.
        private static void OnMissionStateChanged(MissionStateChangedEvent evt)
        {
            Time.timeScale = evt.Current == MissionState.Running || evt.Current == MissionState.Idle ? 1f : 0f;
        }
    }
}
