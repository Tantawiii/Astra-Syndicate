using System;
using AstraSyndicate.Campaign;
using AstraSyndicate.Core;
using AstraSyndicate.Save;
using AstraSyndicate.SceneFlow;
using UnityEngine;

namespace AstraSyndicate.App
{
    /// <summary>
    /// Placeholder main menu (IMGUI): continue, chapter and mission select with unlock state, a progress reset,
    /// and a notice when saved progress was damaged or tampered with. The production UI replaces it and uses
    /// the same services and events.
    /// </summary>
    public sealed class DevMainMenu : MonoBehaviour
    {
        private const float MissionButtonWidth = 260f;

        [Tooltip("Scene opened by the Combat Sandbox button. The button is hidden if the scene is not in Build Settings.")]
        [SerializeField] private string sandboxScene = "CombatSandbox";

        private ICampaignRepository _campaign;
        private IProgressService _progress;
        private ICampaignFlow _flow;
        private ISceneLoader _scenes;
        private IEventBus _events;
        private Action _pending;
        private Vector2 _scroll;
        private bool _confirmingReset;
        private string _saveNotice;

        private void Start()
        {
            Services.TryGet(out _campaign);
            Services.TryGet(out _progress);
            Services.TryGet(out _flow);
            Services.TryGet(out _scenes);
            if (Services.TryGet(out _events))
            {
                _events.Subscribe<SaveLoadFailedEvent>(OnSaveLoadFailed);
                _events.Subscribe<SaveFailedEvent>(OnSaveFailed);
            }

            // Load progress now, so a damaged-save notice exists before the first frame is drawn.
            if (_progress != null) _ = _progress.HasProgress;
        }

        private void OnDestroy()
        {
            if (_events == null) return;

            _events.Unsubscribe<SaveLoadFailedEvent>(OnSaveLoadFailed);
            _events.Unsubscribe<SaveFailedEvent>(OnSaveFailed);
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
            if (_campaign == null || _progress == null || _flow == null)
            {
                GUI.Label(new Rect(16f, 16f, DevGui.Width - 32f, 60f), "Campaign services are missing. Run Tools > Astra Syndicate > Setup Sprint 1 Content.", DevGui.Body);
                return;
            }

            float width = Mathf.Min(720f, DevGui.Width - 32f);
            GUILayout.BeginArea(new Rect((DevGui.Width - width) * 0.5f, 16f, width, DevGui.Height - 32f), GUI.skin.box);
            GUI.enabled = !_flow.IsBusy;

            GUILayout.Label("ASTRA SYNDICATE: FLEET WARS", DevGui.Title);
            GUILayout.Label("Offline campaign: development menu (placeholder UI)", DevGui.Small);
            GUILayout.Space(8f);
            DrawSaveNotice();
            DrawContinueButton();
            DrawSandboxButton();

            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (ChapterData chapter in _campaign.Chapters)
            {
                DrawChapter(chapter);
            }
            GUILayout.EndScrollView();

            DrawResetControls();
            GUI.enabled = true;
            GUILayout.EndArea();
        }

        private void DrawContinueButton()
        {
            MissionData mission = _progress.GetContinueMission();
            if (mission == null) return;

            string label = _progress.HasProgress ? $"Continue: {mission.title}" : $"New Campaign: {mission.title}";
            if (GUILayout.Button(label, GUILayout.Height(44f))) Defer(() => _flow.StartMissionAsync(mission.id).Forget());
        }

        private void DrawSaveNotice()
        {
            if (string.IsNullOrEmpty(_saveNotice)) return;

            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label(_saveNotice, DevGui.Body);
            if (GUILayout.Button("Dismiss", GUILayout.Width(90f), GUILayout.Height(28f))) Defer(() => _saveNotice = null);
            GUILayout.EndHorizontal();
        }

        private void DrawSandboxButton()
        {
            if (_scenes == null || string.IsNullOrEmpty(sandboxScene) || !Application.CanStreamedLevelBeLoaded(sandboxScene)) return;

            if (GUILayout.Button("Combat Sandbox (flight & shooting prototype)", GUILayout.Height(32f)))
            {
                Defer(() => _scenes.LoadSceneAsync(sandboxScene).Forget());
            }
        }

        private void DrawChapter(ChapterData chapter)
        {
            GUILayout.Space(12f);
            string state = !_progress.IsChapterUnlocked(chapter.id) ? "  [LOCKED]"
                : _progress.IsChapterCompleted(chapter.id) ? "  [COMPLETE]"
                : string.Empty;
            GUILayout.Label(chapter.title + state, DevGui.Header);
            if (!string.IsNullOrEmpty(chapter.synopsis)) GUILayout.Label(chapter.synopsis, DevGui.Small);

            foreach (MissionData mission in chapter.missions)
            {
                bool unlocked = _progress.IsMissionUnlocked(mission.id);
                GUILayout.BeginHorizontal();

                bool enabled = GUI.enabled;
                GUI.enabled = enabled && unlocked;
                if (GUILayout.Button(mission.title, GUILayout.Width(MissionButtonWidth), GUILayout.Height(32f)))
                {
                    Defer(() => _flow.StartMissionAsync(mission.id).Forget());
                }
                GUI.enabled = enabled;

                GUILayout.Label(DescribeMission(unlocked, _progress.GetRecord(mission.id)), DevGui.Body);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawResetControls()
        {
            GUILayout.Space(8f);
            if (!_confirmingReset)
            {
                if (GUILayout.Button("Reset Progress", GUILayout.Height(28f))) Defer(() => _confirmingReset = true);
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Erase all campaign progress?", DevGui.Body);
            if (GUILayout.Button("Erase", GUILayout.Width(100f), GUILayout.Height(28f)))
            {
                Defer(() =>
                {
                    _progress.Reset();
                    _confirmingReset = false;
                });
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(100f), GUILayout.Height(28f))) Defer(() => _confirmingReset = false);
            GUILayout.EndHorizontal();
        }

        private void Defer(Action action)
        {
            _pending += action;
        }

        private void OnSaveLoadFailed(SaveLoadFailedEvent evt)
        {
            _saveNotice = evt.RecoveredFromBackup
                ? "Your saved progress was damaged and has been restored from its backup."
                : "Your saved progress could not be read (damaged or modified), so the campaign starts fresh.";
        }

        private void OnSaveFailed(SaveFailedEvent evt)
        {
            _saveNotice = $"Progress could not be saved: {evt.Error}";
        }

        private static string DescribeMission(bool unlocked, MissionRecord record)
        {
            if (!unlocked) return "Locked";
            if (record == null) return "Available";
            if (!record.completed) return $"Available ({record.attempts} attempt{(record.attempts == 1 ? string.Empty : "s")})";
            return $"Completed, best time {DevGui.FormatTime(record.bestTimeSeconds)}";
        }
    }
}
