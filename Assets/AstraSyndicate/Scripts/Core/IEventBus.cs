using System;

namespace AstraSyndicate.Core
{
    /// <summary>
    /// Type-safe publish/subscribe channel for cross-module communication. Modules publish facts
    /// ("unit destroyed", "mission ended") without knowing who listens, which keeps gameplay,
    /// campaign, UI and security code decoupled from each other.
    /// </summary>
    /// <remarks>Events are structs so publishing does not allocate.</remarks>
    public interface IEventBus
    {
        void Subscribe<T>(Action<T> handler) where T : struct;

        void Unsubscribe<T>(Action<T> handler) where T : struct;

        void Publish<T>(T evt) where T : struct;
    }
}
