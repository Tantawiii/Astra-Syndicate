namespace AstraSyndicate.SceneFlow
{
    public readonly struct SceneLoadStartedEvent
    {
        public readonly string SceneName;

        public SceneLoadStartedEvent(string sceneName)
        {
            SceneName = sceneName;
        }
    }

    /// <summary>The new scene is active; published before the transition view is hidden.</summary>
    public readonly struct SceneLoadCompletedEvent
    {
        public readonly string SceneName;

        public SceneLoadCompletedEvent(string sceneName)
        {
            SceneName = sceneName;
        }
    }
}
