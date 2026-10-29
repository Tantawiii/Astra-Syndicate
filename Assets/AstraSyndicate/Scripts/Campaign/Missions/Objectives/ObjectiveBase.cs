using System;
using AstraSyndicate.Core;

namespace AstraSyndicate.Campaign
{
    /// <summary>Shared lifecycle for objectives; subclasses only implement their own tracking rule.</summary>
    public abstract class ObjectiveBase : IObjective
    {
        protected ObjectiveBase(ObjectiveData data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public event Action<IObjective> Changed;

        public string Id => Data.id;

        public string Description => Data.description;

        public bool IsOptional => Data.optional;

        public ObjectiveStatus Status { get; private set; } = ObjectiveStatus.Inactive;

        public abstract float Progress01 { get; }

        public abstract string ProgressText { get; }

        protected ObjectiveData Data { get; }

        /// <summary>The bus passed to <see cref="Activate"/>; null while inactive.</summary>
        protected IEventBus Events { get; private set; }

        public void Activate(IEventBus events)
        {
            if (Status != ObjectiveStatus.Inactive) return;

            Events = events ?? throw new ArgumentNullException(nameof(events));
            Status = ObjectiveStatus.Active;
            OnActivated();
            NotifyChanged();
        }

        public void Deactivate()
        {
            if (Events == null) return;

            OnDeactivated();
            Events = null;
        }

        public virtual void Tick(float deltaTime)
        {
        }

        protected virtual void OnActivated()
        {
        }

        protected virtual void OnDeactivated()
        {
        }

        protected void MarkCompleted()
        {
            if (Status != ObjectiveStatus.Active) return;

            Status = ObjectiveStatus.Completed;
            NotifyChanged();
        }

        protected void MarkFailed()
        {
            if (Status != ObjectiveStatus.Active) return;

            Status = ObjectiveStatus.Failed;
            NotifyChanged();
        }

        protected void NotifyChanged()
        {
            Changed?.Invoke(this);
        }
    }
}
