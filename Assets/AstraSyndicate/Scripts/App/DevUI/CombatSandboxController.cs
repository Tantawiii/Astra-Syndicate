using System;
using AstraSyndicate.AI;
using AstraSyndicate.Campaign;
using AstraSyndicate.Core;
using AstraSyndicate.PlayerControl;
using AstraSyndicate.SceneFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AstraSyndicate.App
{
    /// <summary>
    /// Runs the DEV-02 combat sandbox: endless enemy waves, a kill/wave readout, pause menu (Esc) and the
    /// defeat screen. Placeholder IMGUI like the other dev screens.
    /// </summary>
    public sealed class CombatSandboxController : MonoBehaviour
    {
        private const float BannerSeconds = 3f;

        [SerializeField] private PlayerFlightController player;
        [SerializeField] private EnemyWaveSpawner spawner;

        private ISceneLoader _scenes;
        private ICampaignFlow _flow;
        private IEventBus _events;
        private Action _pending;
        private bool _paused;
        private bool _defeated;
        private int _kills;
        private string _banner;
        private float _bannerUntil;

        public void Configure(PlayerFlightController flight, EnemyWaveSpawner waves)
        {
            player = flight;
            spawner = waves;
        }

        private void Start()
        {
            Services.TryGet(out _scenes);
            Services.TryGet(out _flow);
            if (!Services.TryGet(out _events)) return;

            _events.Subscribe<UnitDestroyedEvent>(OnUnitDestroyed);
            _events.Subscribe<PlayerDefeatedEvent>(OnPlayerDefeated);
            _events.Subscribe<WaypointReachedEvent>(OnWaypointReached);
            _events.Subscribe<WaveStartedEvent>(OnWaveStarted);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (_events == null) return;

            _events.Unsubscribe<UnitDestroyedEvent>(OnUnitDestroyed);
            _events.Unsubscribe<PlayerDefeatedEvent>(OnPlayerDefeated);
            _events.Unsubscribe<WaypointReachedEvent>(OnWaypointReached);
            _events.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
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
            Event current = Event.current;
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape && !_defeated)
            {
                current.Use();
                Defer(() => SetPaused(!_paused));
            }

            DrawStatusLine();
            if (_defeated) DrawDefeatPanel();
            else if (_paused) DrawPausePanel();
            if (Time.unscaledTime < _bannerUntil) DevGui.HudLabel(new Rect(0f, DevGui.Height * 0.22f, DevGui.Width, 24f), _banner, new Color(1f, 0.85f, 0.4f), centered: true);
        }

        private void DrawStatusLine()
        {
            string wave = spawner == null ? string.Empty
                : spawner.AliveCount > 0 ? $"Wave {spawner.Wave}: {spawner.AliveCount} hostiles"
                : $"Next wave in {Mathf.CeilToInt(spawner.NextWaveIn)}s";
            DevGui.HudLabel(new Rect(12f, 30f, 600f, 22f), $"COMBAT SANDBOX (DEV-02)   {wave}   Kills: {_kills}   Esc: menu", Color.white);
        }

        private void DrawPausePanel()
        {
            GUILayout.BeginArea(DevGui.Centered(320f, 230f), GUI.skin.box);
            GUILayout.Label("PAUSED", DevGui.Title);
            if (GUILayout.Button("Resume", GUILayout.Height(36f))) Defer(() => SetPaused(false));
            if (GUILayout.Button("Restart Sandbox", GUILayout.Height(36f))) Defer(Restart);
            if (GUILayout.Button("Main Menu", GUILayout.Height(36f))) Defer(ReturnToMenu);
            GUILayout.EndArea();
        }

        private void DrawDefeatPanel()
        {
            GUILayout.BeginArea(DevGui.Centered(360f, 220f), GUI.skin.box);
            GUILayout.Label("FLAGSHIP DESTROYED", DevGui.Title);
            GUILayout.Label($"Enemies destroyed: {_kills}", DevGui.Body);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Restart Sandbox", GUILayout.Height(36f))) Defer(Restart);
            if (GUILayout.Button("Main Menu", GUILayout.Height(36f))) Defer(ReturnToMenu);
            GUILayout.EndArea();
        }

        private void SetPaused(bool paused)
        {
            _paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (player != null) player.InputEnabled = !paused;
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            string scene = SceneManager.GetActiveScene().name;
            if (_scenes != null) _scenes.LoadSceneAsync(scene).Forget();
            else SceneManager.LoadScene(scene);
        }

        private void ReturnToMenu()
        {
            Time.timeScale = 1f;
            _flow?.ReturnToMenuAsync().Forget();
        }

        private void OnUnitDestroyed(UnitDestroyedEvent evt)
        {
            if (evt.Faction == Faction.Enemy) _kills++;
        }

        private void OnPlayerDefeated(PlayerDefeatedEvent evt)
        {
            _defeated = true;
            _paused = false;
        }

        private void OnWaypointReached(WaypointReachedEvent evt)
        {
            ShowBanner($"Waypoint '{evt.WaypointId}' reached");
        }

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            ShowBanner($"Wave {evt.Wave}: {evt.ShipCount} raiders inbound");
        }

        private void ShowBanner(string text)
        {
            _banner = text;
            _bannerUntil = Time.unscaledTime + BannerSeconds;
        }

        private void Defer(Action action)
        {
            _pending += action;
        }
    }
}
