using System.Collections.Generic;

namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// Runtime state of the current mission. Every change is also published on the event bus
    /// (MissionStateChangedEvent, ObjectiveUpdatedEvent, MissionEndedEvent) for UI and other modules.
    /// </summary>
    public interface IMissionStateManager
    {
        MissionState State { get; }

        /// <summary>The mission being played, or null when Idle.</summary>
        MissionData CurrentMission { get; }

        IReadOnlyList<IObjective> Objectives { get; }

        /// <summary>Mission time in seconds; does not advance during the briefing or while paused.</summary>
        float ElapsedSeconds { get; }

        /// <summary>Seconds left before the time limit fails the mission; null if the mission has no limit.</summary>
        float? TimeRemaining { get; }

        /// <summary>Result of the most recently finished mission.</summary>
        MissionResult? LastResult { get; }

        /// <summary>Idle → Briefing. Returns false if another mission is still loaded.</summary>
        bool BeginMission(MissionData mission);

        /// <summary>Briefing → Running: objectives start tracking.</summary>
        bool Launch();

        bool Pause();

        bool Resume();

        /// <summary>Ends the mission successfully (scripted events, debug tools).</summary>
        bool Complete();

        bool Fail(string reason);

        /// <summary>Leaves the current mission from any state without recording a result.</summary>
        void Exit();

        /// <summary>Advances mission time; called once per frame by MissionRunner.</summary>
        void Tick(float deltaTime);
    }
}
