using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace AstraSyndicate.Tests
{
    internal static class TestScenes
    {
        private static int _created;

        /// <summary>
        /// Replaces every loaded scene with a new empty one, so ships and encounters left over from earlier tests
        /// (e.g. a mission scene with its fleet) cannot interfere.
        /// </summary>
        public static IEnumerator LoadEmpty()
        {
            _created++;
            Scene empty = SceneManager.CreateScene($"Empty Test Scene {_created}");
            SceneManager.SetActiveScene(empty);

            var others = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene != empty) others.Add(scene);
            }
            foreach (Scene scene in others) yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
