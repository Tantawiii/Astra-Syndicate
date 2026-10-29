using UnityEngine;

namespace AstraSyndicate.App
{
    /// <summary>Shared layout helpers for the IMGUI development screens.</summary>
    internal static class DevGui
    {
        private const float ReferenceHeight = 900f;

        private static GUIStyle _title;
        private static GUIStyle _header;
        private static GUIStyle _body;
        private static GUIStyle _small;

        public static float Scale => Mathf.Clamp(Screen.height / ReferenceHeight, 0.75f, 3f);

        /// <summary>Screen width in scaled GUI units.</summary>
        public static float Width => Screen.width / Scale;

        /// <summary>Screen height in scaled GUI units.</summary>
        public static float Height => Screen.height / Scale;

        public static GUIStyle Title => Style(ref _title, 26, FontStyle.Bold);

        public static GUIStyle Header => Style(ref _header, 18, FontStyle.Bold);

        public static GUIStyle Body => Style(ref _body, 14, FontStyle.Normal);

        public static GUIStyle Small => Style(ref _small, 12, FontStyle.Italic);

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
