using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>
    /// High-level campaign navigation for menus and HUDs: start, retry and chain missions, or return
    /// to the main menu. Every method returns false if the request was rejected.
    /// </summary>
    public interface ICampaignFlow
    {
        /// <summary>The mission being played or just finished; null in menus.</summary>
        MissionData CurrentMission { get; }

        /// <summary>True while a scene transition is running; navigation requests are ignored meanwhile.</summary>
        bool IsBusy { get; }

        /// <summary>Loads the mission's scene. Locked missions are rejected.</summary>
        Awaitable<bool> StartMissionAsync(string missionId);

        Awaitable<bool> RetryAsync();

        /// <summary>Starts the next mission, or returns to the menu at the end of the campaign.</summary>
        Awaitable<bool> NextMissionAsync();

        Awaitable<bool> ReturnToMenuAsync();

        /// <summary>
        /// Called by a mission scene on start to take the mission it should run. Returns null when the
        /// scene was opened directly (e.g. by pressing Play on it in the editor).
        /// </summary>
        MissionData ConsumePendingMission();
    }
}
