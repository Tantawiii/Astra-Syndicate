using UnityEngine;

namespace AstraSyndicate.App
{
    /// <summary>Shared layout helpers for the IMGUI development screens.</summary>
    internal static class DevGui
    {
        private const float ReferenceHeight = 900f;

        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.8f);

        private static GUIStyle _title;
        private static GUIStyle _header;
        private static GUIStyle _body;
        private static GUIStyle _small;
        private static GUIStyle _hudText;
        private static GUIStyle _hudTextCentered;

        public static float Scale => Mathf.Clamp(Screen.height / ReferenceHeight, 0.75f, 3f);

        /// <summary>Screen width in scaled GUI units.</summary>
        public static float Width => Screen.width / Scale;

        /// <summary>Screen height in scaled GUI units.</summary>
        public static float Height => Screen.height / Scale;

        public static GUIStyle Title => Style(ref _title, 26, FontStyle.Bold);

        public static GUIStyle Header => Style(ref _header, 18, FontStyle.Bold);

        public static GUIStyle Body => Style(ref _body, 14, FontStyle.Normal);

        public static GUIStyle Small => Style(ref _small, 12, FontStyle.Italic);

        // White base colour so GUI.color tints HUD text exactly.
        private static GUIStyle HudText => _hudText ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
        };

        private static GUIStyle HudTextCentered => _hudTextCentered ??= new GUIStyle(HudText) { alignment = TextAnchor.UpperCenter };

        /// <summary>Call first in OnGUI: scales the whole GUI with the screen height.</summary>
        public static void Begin()
        {
            GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1f));
        }

        public static Rect Centered(float width, float height)
        {
            width = Mathf.Min(width, Width - 32f);
            height = Mathf.Min(height, Height - 32f);
            return new Rect((Width - width) * 0.5f, (Height - height) * 0.5f, width, height);
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }

        /// <summary>Projects a world point into scaled GUI coordinates. Returns false if it is behind the camera.</summary>
        public static bool WorldToGui(Camera camera, Vector3 world, out Vector2 guiPoint)
        {
            Vector3 screen = camera.WorldToScreenPoint(world);
            guiPoint = new Vector2(screen.x / Scale, (Screen.height - screen.y) / Scale);
            return screen.z > 0f;
        }

        public static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        public static void DrawFrame(Rect rect, float thickness, Color color)
        {
            DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }

        /// <summary>Lock-on style corner brackets around <paramref name="rect"/>.</summary>
        public static void DrawBrackets(Rect rect, float length, float thickness, Color color)
        {
            DrawRect(new Rect(rect.xMin, rect.yMin, length, thickness), color);
            DrawRect(new Rect(rect.xMin, rect.yMin, thickness, length), color);
            DrawRect(new Rect(rect.xMax - length, rect.yMin, length, thickness), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, length), color);
            DrawRect(new Rect(rect.xMin, rect.yMax - thickness, length, thickness), color);
            DrawRect(new Rect(rect.xMin, rect.yMax - length, thickness, length), color);
            DrawRect(new Rect(rect.xMax - length, rect.yMax - thickness, length, thickness), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.yMax - length, thickness, length), color);
        }

        public static void DrawBar(Rect rect, float fraction, Color fill, Color background)
        {
            DrawRect(rect, background);
            DrawRect(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), fill);
        }

        /// <summary>HUD text with a drop shadow, readable over any background.</summary>
        public static void HudLabel(Rect rect, string text, Color color, bool centered = false)
        {
            GUIStyle style = centered ? HudTextCentered : HudText;
            Color previous = GUI.color;
            GUI.color = ShadowColor;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            GUI.color = color;
            GUI.Label(rect, text, style);
            GUI.color = previous;
        }

        private static GUIStyle Style(ref GUIStyle style, int fontSize, FontStyle fontStyle)
        {
            return style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                wordWrap = true,
            };
        }
    }
}
