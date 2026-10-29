using System;
using AstraSyndicate.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AstraSyndicate.SceneFlow
{
    /// <summary>
    /// Loads scenes one at a time behind an <see cref="ISceneTransitionView"/>: cover the screen,
    /// load in the background while reporting progress, activate, then reveal.
    /// </summary>
    public sealed class SceneLoader : ISceneLoader
    {
        // Unity reports 0.9 once a scene is loaded and only waiting for activation.
        private const float ActivationThreshold = 0.9f;

        private readonly ISceneTransitionView _view;
        private readonly IEventBus _events;

        public SceneLoader(ISceneTransitionView view, IEventBus events)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public bool IsLoading { get; private set; }

        public string ActiveSceneName => SceneManager.GetActiveScene().name;

        public async Awaitable<bool> LoadSceneAsync(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneFlow] Ignoring request to load '{sceneName}' while another scene is loading.");
                return false;
            }
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneFlow] Scene '{sceneName}' is not in Build Settings.");
                return false;
            }

            IsLoading = true;
            try
            {
                _events.Publish(new SceneLoadStartedEvent(sceneName));
                _view.SetProgress(0f);
                await _view.ShowAsync();

                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                operation.allowSceneActivation = false;
                while (operation.progress < ActivationThreshold)
                {
                    _view.SetProgress(operation.progress / ActivationThreshold);
                    await Awaitable.NextFrameAsync();
                }

                _view.SetProgress(1f);
                operation.allowSceneActivation = true;
                while (!operation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }

                _events.Publish(new SceneLoadCompletedEvent(sceneName));
                await _view.HideAsync();
                return true;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
