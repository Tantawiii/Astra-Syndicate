using System;
using AstraSyndicate.Core;

namespace AstraSyndicate.Campaign
{
    /// <summary>A runtime mission objective. Each one listens to the gameplay events it cares about.</summary>
    public interface IObjective
    {
        string Id { get; }

        string Description { get; }

        bool IsOptional { get; }

        ObjectiveStatus Status { get; }

        float Progress01 { get; }

        /// <summary>Short progress label for HUDs, e.g. "3/5" or "42s left".</summary>
        string ProgressText { get; }

        /// <summary>Raised when status or progress changes.</summary>
        event Action<IObjective> Changed;

        /// <summary>Starts tracking. Subscriptions made here must be undone in <see cref="Deactivate"/>.</summary>
        void Activate(IEventBus events);

        void Deactivate();

        void Tick(float deltaTime);
    }
}
