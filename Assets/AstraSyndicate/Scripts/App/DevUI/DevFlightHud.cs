using System;
using AstraSyndicate.AI;
using AstraSyndicate.Combat;
using AstraSyndicate.PlayerControl;
using UnityEngine;

namespace AstraSyndicate.App
{
    /// <summary>
    /// Placeholder flight HUD (IMGUI): crosshair and gun reticle, ship markers with off-screen indicators,
    /// target brackets with lead marker, hull/boost/speed and wingmen status. The UI team's HUD replaces it
    /// and reads the same PlayerFlightController, Ship and ShipMotor state.
    /// </summary>
    public sealed class DevFlightHud : MonoBehaviour
    {
        private const float MarkerRange = 2500f;
        private const float EdgeMargin = 28f;
        private const float OrderMessageSeconds = 2.5f;

        private static readonly Color HostileColor = new Color(1f, 0.38f, 0.28f, 0.95f);
        private static readonly Color FriendlyColor = new Color(0.45f, 0.85f, 1f, 0.9f);
        private static readonly Color LeadColor = new Color(1f, 0.9f, 0.35f, 1f);
        private static readonly Color CrosshairColor = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Color ReticleColor = new Color(1f, 1f, 1f, 0.45f);
        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Color HullColor = new Color(0.4f, 0.9f, 0.5f, 0.9f);
        private static readonly Color BoostColor = new Color(0.4f, 0.75f, 1f, 0.9f);
        private static readonly Color TextColor = new Color(1f, 1f, 1f, 0.9f);

        [SerializeField] private PlayerFlightController player;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private bool showControls = true;

        private WingmanBrain[] _wingmen = Array.Empty<WingmanBrain>();
        private PlayerFlightController _subscribed;
        private string _orderMessage;
        private float _orderMessageUntil;

        public void Configure(PlayerFlightController flight, Camera view)
        {
            player = flight;
            viewCamera = view;
        }

        private void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerFlightController>();
            if (viewCamera == null) viewCamera = Camera.main;
            _wingmen = FindObjectsByType<WingmanBrain>(FindObjectsSortMode.None);
            if (player == null) return;

            _subscribed = player;
            _subscribed.OrderIssued += OnOrderIssued;
        }

        private void OnDestroy()
        {
            if (_subscribed != null) _subscribed.OrderIssued -= OnOrderIssued;
        }

        private void OnGUI()
        {
            Event current = Event.current;
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.H)
            {
                showControls = !showControls;
                current.Use();
            }

            // Drawing only: no controls, so nothing to do outside the repaint pass.
            if (current.type != EventType.Repaint || player == null || viewCamera == null) return;

            DevGui.Begin();
            DrawShipMarkers();
            DrawTarget();
            DrawCrosshair();
            DrawStatus();
            DrawWingmen();
            DrawControlsHint();
        }

        private void DrawCrosshair()
        {
            Vector3 origin = player.transform.position;
            Vector3 aimPoint = player.AimPoint;
            if (DevGui.WorldToGui(viewCamera, aimPoint, out Vector2 aim))
            {
                const float arm = 12f;
                const float gap = 5f;
                const float thickness = 2f;
                DevGui.DrawRect(new Rect(aim.x - gap - arm, aim.y - thickness / 2f, arm, thickness), CrosshairColor);
                DevGui.DrawRect(new Rect(aim.x + gap, aim.y - thickness / 2f, arm, thickness), CrosshairColor);
                DevGui.DrawRect(new Rect(aim.x - thickness / 2f, aim.y - gap - arm, thickness, arm), CrosshairColor);
                DevGui.DrawRect(new Rect(aim.x - thickness / 2f, aim.y + gap, thickness, arm), CrosshairColor);
            }

            // Where the guns point right now; it chases the crosshair as the ship turns.
            Vector3 nosePoint = origin + player.transform.forward * Vector3.Distance(origin, aimPoint);
            if (DevGui.WorldToGui(viewCamera, nosePoint, out Vector2 nose))
            {
                DevGui.DrawFrame(new Rect(nose.x - 6f, nose.y - 6f, 12f, 12f), 1.5f, ReticleColor);
            }
        }

        private void DrawShipMarkers()
        {
            Ship flagship = player.Ship;
            foreach (Ship ship in ShipRegistry.All)
            {
                if (!ship.IsAlive || ship == flagship || ship == player.Target) continue;
                if (Vector3.Distance(ship.Position, flagship.Position) > MarkerRange) continue;

                bool hostile = FactionRelations.AreHostile(flagship.Faction, ship.Faction);
                Color color = hostile ? HostileColor : FriendlyColor;
                if (IsOnScreen(ship.Position, out Vector2 point)) DevGui.DrawFrame(new Rect(point.x - 5f, point.y - 5f, 10f, 10f), 1.5f, color);
                else if (hostile) DrawEdgeMarker(ship.Position, color, 6f);
            }
        }

        private void DrawTarget()
        {
            Ship target = player.Target;
            if (target == null) return;

            float distance = Vector3.Distance(player.transform.position, target.Position);
            if (IsOnScreen(target.Position, out Vector2 point))
            {
                float size = Mathf.Clamp(3000f / Mathf.Max(distance, 1f), 24f, 90f);
                var box = new Rect(point.x - size / 2f, point.y - size / 2f, size, size);
                DevGui.DrawBrackets(box, size * 0.3f, 2f, HostileColor);
                DevGui.DrawBar(new Rect(box.xMin, box.yMax + 4f, size, 4f), target.HullFraction, HostileColor, PanelColor);
                DevGui.HudLabel(new Rect(box.xMax + 6f, box.yMin, 240f, 60f), $"{target.DisplayName}\n{distance:0} m{AiState(target)}", HostileColor);
            }
            else
            {
                DrawEdgeMarker(target.Position, HostileColor, 12f);
            }

            if (player.LeadPoint.HasValue && IsOnScreen(player.LeadPoint.Value, out Vector2 lead))
            {
                DevGui.DrawFrame(new Rect(lead.x - 7f, lead.y - 7f, 14f, 14f), 2f, LeadColor);
                DevGui.DrawRect(new Rect(lead.x - 1.5f, lead.y - 1.5f, 3f, 3f), LeadColor);
            }
        }

        private void DrawStatus()
        {
            Ship ship = player.Ship;
            ShipMotor motor = player.Motor;
            var panel = new Rect(12f, DevGui.Height - 100f, 270f, 88f);
            DevGui.DrawRect(panel, PanelColor);

            DevGui.HudLabel(new Rect(panel.x + 8f, panel.y + 4f, 250f, 20f), $"HULL  {ship.Hull:0} / {ship.MaxHull:0}", TextColor);
            DevGui.DrawBar(new Rect(panel.x + 8f, panel.y + 24f, 254f, 8f), ship.HullFraction, HullColor, PanelColor);
            string boost = motor.IsBoosting ? "BOOSTING" : $"BOOST  {motor.BoostCharge * 100f:0}%";
            DevGui.HudLabel(new Rect(panel.x + 8f, panel.y + 36f, 250f, 20f), boost, TextColor);
            DevGui.DrawBar(new Rect(panel.x + 8f, panel.y + 56f, 254f, 6f), motor.BoostCharge, BoostColor, PanelColor);
            DevGui.HudLabel(new Rect(panel.x + 8f, panel.y + 64f, 250f, 20f), $"{motor.Speed:0} m/s   throttle {motor.Throttle * 100f:0}%", TextColor);
        }

        private void DrawWingmen()
        {
            const float rowHeight = 20f;
            int rows = _wingmen.Length + 1;
            var panel = new Rect(DevGui.Width - 282f, DevGui.Height - 16f - rows * rowHeight, 270f, rows * rowHeight + 4f);
            DevGui.DrawRect(panel, PanelColor);

            bool showOrder = Time.unscaledTime < _orderMessageUntil;
            DevGui.HudLabel(new Rect(panel.x + 8f, panel.y + 2f, 260f, rowHeight), showOrder ? _orderMessage : "WINGMEN", showOrder ? LeadColor : TextColor);
            for (int i = 0; i < _wingmen.Length; i++)
            {
                WingmanBrain wingman = _wingmen[i];
                var row = new Rect(panel.x + 8f, panel.y + 2f + (i + 1) * rowHeight, 260f, rowHeight);
                if (wingman == null)
                {
                    DevGui.HudLabel(row, "— lost —", HostileColor);
                    continue;
                }

                Ship ship = wingman.GetComponent<Ship>();
                string activity = wingman.IsEngaging ? $"engaging {wingman.Target.DisplayName}" : "in formation";
                DevGui.HudLabel(row, $"{ship.DisplayName}  {ship.HullFraction * 100f:0}%  {activity}", FriendlyColor);
            }
        }

        private void DrawControlsHint()
        {
            string text = showControls
                ? "Mouse aim · W/S throttle · A/D turn · Shift boost · LMB fire · T/RMB target · F wing attack · G form up · hold Alt: cursor · H: hide"
                : "H: show controls";
            DevGui.HudLabel(new Rect(0f, 8f, DevGui.Width, 22f), text, new Color(1f, 1f, 1f, 0.7f), centered: true);
        }

        private bool IsOnScreen(Vector3 world, out Vector2 point)
        {
            return DevGui.WorldToGui(viewCamera, world, out point)
                   && point.x >= 0f && point.x <= DevGui.Width
                   && point.y >= 0f && point.y <= DevGui.Height;
        }

        // Marker on the screen edge in the direction of something off-screen (works for points behind the camera too).
        private void DrawEdgeMarker(Vector3 world, Color color, float size)
        {
            Vector3 local = viewCamera.transform.InverseTransformPoint(world);
            var direction = new Vector2(local.x, -local.y);
            if (direction.sqrMagnitude < 1e-4f) direction = Vector2.down;
            direction.Normalize();

            Vector2 center = new Vector2(DevGui.Width, DevGui.Height) * 0.5f;
            float reach = Mathf.Min(
                (center.x - EdgeMargin) / Mathf.Max(Mathf.Abs(direction.x), 1e-4f),
                (center.y - EdgeMargin) / Mathf.Max(Mathf.Abs(direction.y), 1e-4f));
            Vector2 edge = center + direction * reach;
            DevGui.DrawRect(new Rect(edge.x - size / 2f, edge.y - size / 2f, size, size), color);
        }

        private void OnOrderIssued(FleetOrder order, Ship target)
        {
            _orderMessage = order == FleetOrder.AttackTarget ? $"ORDER: attack {target.DisplayName}" : "ORDER: form up";
            _orderMessageUntil = Time.unscaledTime + OrderMessageSeconds;
        }

        private static string AiState(Ship ship)
        {
            return ship.TryGetComponent(out EnemyBrain brain) ? $"\n{brain.State.ToString().ToUpperInvariant()}" : string.Empty;
        }
    }
}
