using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AstraSyndicate.EditorTools
{
    /// <summary>Player builds of the scenes in Build Settings (DEV-02 "playable build").</summary>
    public static class BuildTool
    {
        private const string WindowsPath = "Builds/Windows/AstraSyndicate.exe";

        [MenuItem("Tools/Astra Syndicate/Build/Windows Player", priority = 60)]
        private static void BuildWindowsFromMenu()
        {
            if (BuildWindows()) EditorUtility.RevealInFinder(WindowsPath);
        }

        /// <summary>
        /// Batch-mode entry point:
        /// Unity.exe -batchmode -quit -projectPath &lt;path&gt; -executeMethod AstraSyndicate.EditorTools.BuildTool.BuildWindowsBatch
        /// </summary>
        public static void BuildWindowsBatch()
        {
            bool succeeded = BuildWindows();
            if (Application.isBatchMode) EditorApplication.Exit(succeeded ? 0 : 1);
        }

        public static bool BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = WindowsPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Build] Succeeded: {summary.outputPath} ({summary.totalSize / (1024 * 1024)} MB) in {summary.totalTime:mm\\:ss}.");
                return true;
            }

            Debug.LogError($"[Build] {summary.result}: {summary.totalErrors} errors. See the build report in the Console.");
            return false;
        }
    }
}
