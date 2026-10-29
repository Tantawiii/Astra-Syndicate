using AstraSyndicate.Campaign;
using AstraSyndicate.Core;
using AstraSyndicate.PlayerControl;
using UnityEngine;

namespace AstraSyndicate.App
{
    /// <summary>
    /// Connects a mission to the flight controls: the player flies only while the mission is running. During the
    /// briefing, pause and results screens the flagship ignores input and the cursor is free for menus.
    /// Lives in App because it is the only module that knows about both campaign and player control.
    /// </summary>
    public sealed class MissionCombatBinder : MonoBehaviour
    {
        [SerializeField] private PlayerFlightController player;

        private IEventBus _events;

        public void Configure(PlayerFlightController flight)
        {
            player = flight;
        }

        private void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerFlightController>();
            if (Services.TryGet(out _events)) _events.Subscribe<MissionStateChangedEvent>(OnMissionStateChanged);
            Apply(Services.TryGet(out IMissionStateManager missions) ? missions.State : MissionState.Idle);
        }

        private void OnDestroy()
        {
            _events?.Unsubscribe<MissionStateChangedEvent>(OnMissionStateChanged);
        }

        private void OnMissionStateChanged(MissionStateChangedEvent evt)
        {
            Apply(evt.Current);
        }

        private void Apply(MissionState state)
        {
            if (player != null) player.InputEnabled = state == MissionState.Running;
        }
    }
}
