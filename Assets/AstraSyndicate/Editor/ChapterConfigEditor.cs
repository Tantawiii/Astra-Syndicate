using System.IO;
using AstraSyndicate.Campaign;
using UnityEditor;
using UnityEngine;

namespace AstraSyndicate.EditorTools
{
    /// <summary>Adds JSON export/import to ChapterConfig assets (both formats share one schema).</summary>
    [CustomEditor(typeof(ChapterConfig))]
    public sealed class ChapterConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Export JSON...")) ExportJson();
                if (GUILayout.Button("Import JSON...")) ImportJson();
            }
        }

        private void ExportJson()
        {
            var config = (ChapterConfig)target;
            string path = EditorUtility.SaveFilePanel("Export chapter as JSON", Application.dataPath, $"{config.Chapter.id}.json", "json");
            if (string.IsNullOrEmpty(path)) return;

            File.WriteAllText(path, JsonUtility.ToJson(config.Chapter, prettyPrint: true));
            AssetDatabase.Refresh();
            Debug.Log($"[Campaign] Exported '{config.Chapter.id}' to {path}");
        }

        private void ImportJson()
        {
            var config = (ChapterConfig)target;
            string path = EditorUtility.OpenFilePanel("Import chapter JSON", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;

            if (!JsonChapterSource.TryParse(File.ReadAllText(path), Path.GetFileName(path), out ChapterData chapter, out string error))
            {
                EditorUtility.DisplayDialog("Import failed", error, "OK");
                return;
            }

            Undo.RecordObject(config, "Import Chapter JSON");
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(chapter), config.Chapter);
            EditorUtility.SetDirty(config);
        }
    }
}
