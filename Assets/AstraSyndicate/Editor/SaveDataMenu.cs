using System.IO;
using AstraSyndicate.App;
using UnityEditor;
using UnityEngine;

namespace AstraSyndicate.EditorTools
{
    public static class SaveDataMenu
    {
        [MenuItem("Tools/Astra Syndicate/Save Data/Open Save Folder", priority = 40)]
        private static void OpenSaveFolder()
        {
            string directory = SaveDirectory();
            Directory.CreateDirectory(directory);
            EditorUtility.RevealInFinder(directory);
        }

        [MenuItem("Tools/Astra Syndicate/Save Data/Delete Save Data", priority = 41)]
        private static void DeleteSaveData()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Save] Exit Play Mode before deleting save data.");
                return;
            }

            string directory = SaveDirectory();
            if (!Directory.Exists(directory))
            {
                Debug.Log("[Save] There is no save data to delete.");
                return;
            }
            if (!EditorUtility.DisplayDialog("Delete save data", $"Delete all save files in\n{directory}?", "Delete", "Cancel")) return;

            Directory.Delete(directory, recursive: true);
            Debug.Log($"[Save] Deleted {directory}");
        }

        private static string SaveDirectory()
        {
            var config = Resources.Load<GameConfig>(GameConfig.ResourcesPath);
            return config != null ? config.SaveDirectory : Path.Combine(Application.persistentDataPath, "saves");
        }
    }
}
