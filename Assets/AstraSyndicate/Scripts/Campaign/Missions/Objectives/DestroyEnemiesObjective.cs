using AstraSyndicate.Core;
using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>Destroy <c>targetCount</c> enemies, optionally only those whose tag equals <c>targetId</c>.</summary>
    public sealed class DestroyEnemiesObjective : ObjectiveBase
    {
        private int _destroyed;

        public DestroyEnemiesObjective(ObjectiveData data) : base(data)
        {
        }

        public override float Progress01 => Mathf.Clamp01((float)_destroyed / Required);

        public override string ProgressText => $"{Mathf.Min(_destroyed, Required)}/{Required}";

        private int Required => Mathf.Max(1, Data.targetCount);

        protected override void OnActivated()
        {
            Events.Subscribe<UnitDestroyedEvent>(OnUnitDestroyed);
        }

        protected override void OnDeactivated()
        {
            Events.Unsubscribe<UnitDestroyedEvent>(OnUnitDestroyed);
        }

        private void OnUnitDestroyed(UnitDestroyedEvent evt)
        {
            if (Status != ObjectiveStatus.Active || evt.Faction != Faction.Enemy) return;
            if (!string.IsNullOrEmpty(Data.targetId) && evt.Tag != Data.targetId) return;

            _destroyed++;
            if (_destroyed >= Required) MarkCompleted();
            else NotifyChanged();
        }
    }
}
