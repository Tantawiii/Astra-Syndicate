using System;
using System.Collections.Generic;
using AstraSyndicate.Campaign;
using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.App
{
    /// <summary>
    /// Placeholder mission HUD (IMGUI): briefing, objectives, pause menu and results, plus debug
    /// buttons that publish the gameplay events combat will publish from Task 1.2 on.
    /// </summary>
    public sealed class DevMissionHud : MonoBehaviour
    {
        private const float PanelWidth = 380f;

        [Tooltip("Show the buttons that simulate gameplay events. Toggle in play with F1.")]
        [SerializeField] private bool showDebugPanel;

        private readonly List<string> _notices = new List<string>();
        private IMissionStateManager _missions;
        private ICampaignFlow _flow;
        private ICampaignRepository _campaign;
        private IEventBus _events;
        private Action _pending;
        private int _debugUnitCount;

        private void Start()
        {
            Services.TryGet(out _missions);
            Services.TryGet(out _flow);
            Services.TryGet(out _campaign);
            if (!Services.TryGet(out _events)) return;

            _events.Subscribe<MissionStateChangedEvent>(OnMissionStateChanged);
            _events.Subscribe<MissionUnlockedEvent>(OnMissionUnlocked);
            _events.Subscribe<ChapterUnlockedEvent>(OnChapterUnlocked);
        }

        private void OnDestroy()
        {
            if (_events == null) return;

            _events.Unsubscribe<MissionStateChangedEvent>(OnMissionStateChanged);
            _events.Unsubscribe<MissionUnlockedEvent>(OnMissionUnlocked);
            _events.Unsubscribe<ChapterUnlockedEvent>(OnChapterUnlocked);
        }

        // Button actions run here instead of inside OnGUI, so state never changes between IMGUI's
        // layout and repaint passes.
        private void Update()
        {
            Action action = _pending;
            _pending = null;
            action?.Invoke();
        }

        private void OnGUI()
        {
            DevGui.Begin();
            if (_missions == null || _flow == null || _events == null)
            {
                GUI.Label(new Rect(16f, 16f, DevGui.Width - 32f, 60f), "Mission services are missing. Run Tools > Astra Syndicate > Setup Sprint 1 Content.", DevGui.Body);
                return;
            }

            MissionData mission = _missions.CurrentMission;
            if (mission == null) return;

            HandleShortcuts();
            GUI.enabled = !_flow.IsBusy;
            switch (_missions.State)
            {
                case MissionState.Briefing:
                    DrawBriefing(mission);
                    break;
                case MissionState.Running:
                    DrawStatus(mission);
                    if (showDebugPanel) DrawDebugPanel(mission);
                    break;
                case MissionState.Paused:
                    DrawStatus(mission);
                    DrawPauseMenu();
                    break;
                case MissionState.Completed:
                case MissionState.Failed:
                    DrawStatus(mission);
                    DrawResults(mission);
                    break;
            }
            GUI.enabled = true;
        }

        private void HandleShortcuts()
        {
            Event current = Event.current;
            if (current.type != EventType.KeyDown) return;

            if (current.keyCode == KeyCode.Escape)
            {
                current.Use();
                Defer(() =>
                {
                    if (!_missions.Pause()) _missions.Resume();
                });
            }
            else if (current.keyCode == KeyCode.F1)
            {
                current.Use();
                Defer(() => showDebugPanel = !showDebugPanel);
            }
        }

        private void DrawBriefing(MissionData mission)
        {
            ChapterData chapter = _campaign?.GetChapterOfMission(mission.id);
            GUILayout.BeginArea(DevGui.Centered(560f, 440f), GUI.skin.box);
            if (chapter != null) GUILayout.Label(chapter.title, DevGui.Small);
            GUILayout.Label(mission.title, DevGui.Title);
            GUILayout.Label(mission.briefing, DevGui.Body);

            GUILayout.Space(10f);
            GUILayout.Label("Objectives", DevGui.Header);
            foreach (IObjective objective in _missions.Objectives)
            {
                GUILayout.Label($"- {objective.Description}{OptionalSuffix(objective)}", DevGui.Body);
            }
            if (mission.timeLimitSeconds > 0f) GUILayout.Label($"Time limit: {DevGui.FormatTime(mission.timeLimitSeconds)}", DevGui.Body);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Launch", GUILayout.Height(40f))) Defer(() => _missions.Launch());
            if (GUILayout.Button("Main Menu", GUILayout.Width(140f), GUILayout.Height(40f))) Defer(() => _flow.ReturnToMenuAsync().Forget());
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawStatus(MissionData mission)
        {
            GUILayout.BeginArea(new Rect(12f, 12f, PanelWidth, 280f), GUI.skin.box);
            GUILayout.Label(mission.title, DevGui.Header);

            string time = DevGui.FormatTime(_missions.ElapsedSeconds);
            float? remaining = _missions.TimeRemaining;
            if (remaining.HasValue) time += $"  ({DevGui.FormatTime(remaining.Value)} left)";
            GUILayout.Label($"{_missions.State}   {time}", DevGui.Small);

            foreach (IObjective objective in _missions.Objectives)
            {
                GUILayout.Label($"{StatusMark(objective.Status)} {objective.Description}{OptionalSuffix(objective)}: {objective.ProgressText}", DevGui.Body);
            }
            if (_missions.State == MissionState.Running) GUILayout.Label("Esc: pause   F1: debug tools", DevGui.Small);
            GUILayout.EndArea();
        }

        private void DrawDebugPanel(MissionData mission)
        {
            GUILayout.BeginArea(new Rect(DevGui.Width - PanelWidth - 12f, 12f, PanelWidth, 420f), GUI.skin.box);
            GUILayout.Label("DEBUG: simulate gameplay", DevGui.Header);
            GUILayout.Label("Publishes the same events real combat does. Hold Alt to use the mouse.", DevGui.Small);

            if (GUILayout.Button("Enemy destroyed")) Defer(() => PublishEnemyDestroyed(null));
            foreach (ObjectiveData objective in mission.objectives)
            {
                if (string.IsNullOrEmpty(objective.targetId)) continue;

                string target = objective.targetId;
                if (objective.type == ObjectiveTypes.DestroyEnemies && GUILayout.Button($"Enemy '{target}' destroyed"))
                {
                    Defer(() => PublishEnemyDestroyed(target));
                }
                if (objective.type == ObjectiveTypes.ReachWaypoint && GUILayout.Button($"Waypoint '{target}' reached"))
                {
                    Defer(() => _events.Publish(new WaypointReachedEvent(target)));
                }
            }
            if (GUILayout.Button("Player fleet destroyed")) Defer(() => _events.Publish(new PlayerDefeatedEvent("Fleet destroyed (debug)")));

            GUILayout.Space(8f);
            if (GUILayout.Button("Pause  [Esc]")) Defer(() => _missions.Pause());
            if (GUILayout.Button("Force complete")) Defer(() => _missions.Complete());
            GUILayout.EndArea();
        }

        private void DrawPauseMenu()
        {
            GUILayout.BeginArea(DevGui.Centered(320f, 230f), GUI.skin.box);
            GUILayout.Label("PAUSED", DevGui.Title);
            if (GUILayout.Button("Resume", GUILayout.Height(36f))) Defer(() => _missions.Resume());
            if (GUILayout.Button("Restart Mission", GUILayout.Height(36f))) Defer(() => _flow.RetryAsync().Forget());
            if (GUILayout.Button("Quit to Main Menu", GUILayout.Height(36f))) Defer(() => _flow.ReturnToMenuAsync().Forget());
            GUILayout.EndArea();
        }

        private void DrawResults(MissionData mission)
        {
            bool succeeded = _missions.State == MissionState.Completed;
            GUILayout.BeginArea(DevGui.Centered(480f, 360f), GUI.skin.box);
            GUILayout.Label(succeeded ? "MISSION COMPLETE" : "MISSION FAILED", DevGui.Title);

            MissionResult? result = _missions.LastResult;
            if (result.HasValue)
            {
                MissionResult value = result.Value;
                if (!succeeded) GUILayout.Label(value.FailureReason, DevGui.Body);
                GUILayout.Label($"Time: {DevGui.FormatTime(value.ElapsedSeconds)}", DevGui.Body);
                if (value.OptionalObjectivesTotal > 0)
                {
                    GUILayout.Label($"Optional objectives: {value.OptionalObjectivesCompleted}/{value.OptionalObjectivesTotal}", DevGui.Body);
                }
            }
            foreach (string notice in _notices)
            {
                GUILayout.Label(notice, DevGui.Header);
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (succeeded)
            {
                bool hasNext = _campaign?.GetNextMission(mission.id) != null;
                if (GUILayout.Button(hasNext ? "Next Mission" : "Finish", GUILayout.Height(40f))) Defer(() => _flow.NextMissionAsync().Forget());
            }
            if (GUILayout.Button("Retry", GUILayout.Height(40f))) Defer(() => _flow.RetryAsync().Forget());
            if (GUILayout.Button("Main Menu", GUILayout.Height(40f))) Defer(() => _flow.ReturnToMenuAsync().Forget());
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void PublishEnemyDestroyed(string tag)
        {
            _debugUnitCount++;
            _events.Publish(new UnitDestroyedEvent($"debug_enemy_{_debugUnitCount}", Faction.Enemy, tag));
        }

        private void OnMissionStateChanged(MissionStateChangedEvent evt)
        {
            if (evt.Current == MissionState.Briefing) _notices.Clear();
        }

        private void OnMissionUnlocked(MissionUnlockedEvent evt)
        {
            MissionData unlocked = _campaign?.GetMission(evt.MissionId);
            _notices.Add($"Unlocked: {unlocked?.title ?? evt.MissionId}");
        }

        private void OnChapterUnlocked(ChapterUnlockedEvent evt)
        {
            ChapterData chapter = _campaign?.GetChapter(evt.ChapterId);
            _notices.Add($"New chapter: {chapter?.title ?? evt.ChapterId}");
        }

        private void Defer(Action action)
        {
            _pending += action;
        }

        private static string StatusMark(ObjectiveStatus status)
        {
            switch (status)
            {
                case ObjectiveStatus.Completed:
                    return "[x]";
                case ObjectiveStatus.Failed:
                    return "[!]";
                default:
                    return "[ ]";
            }
        }

        private static string OptionalSuffix(IObjective objective)
        {
            return objective.IsOptional ? " (optional)" : string.Empty;
        }
    }
}
