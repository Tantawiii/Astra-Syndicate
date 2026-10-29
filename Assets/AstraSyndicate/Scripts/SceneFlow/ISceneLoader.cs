using UnityEngine;

namespace AstraSyndicate.SceneFlow
{
    public interface ISceneLoader
    {
        bool IsLoading { get; }

        string ActiveSceneName { get; }

        /// <summary>
        /// Replaces the current scene behind the transition view. Returns false if the scene is not in
        /// Build Settings or another load is already running.
        /// </summary>
        Awaitable<bool> LoadSceneAsync(string sceneName);
    }
}
