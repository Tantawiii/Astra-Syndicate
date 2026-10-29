using UnityEngine;

namespace AstraSyndicate.SceneFlow
{
    /// <summary>
    /// What the player sees while scenes change. Swap in another implementation for a branded
    /// loading screen; the scene loader only depends on this interface.
    /// </summary>
    public interface ISceneTransitionView
    {
        /// <summary>Covers the screen (e.g. fade to black).</summary>
        Awaitable ShowAsync();

        /// <summary>Reveals the newly loaded scene.</summary>
        Awaitable HideAsync();

        void SetProgress(float progress01);
    }
}
