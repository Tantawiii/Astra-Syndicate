using AstraSyndicate.Core;

namespace AstraSyndicate.Campaign
{
    /// <summary>Reach the waypoint whose id equals <c>targetId</c>.</summary>
    public sealed class ReachWaypointObjective : ObjectiveBase
    {
        public ReachWaypointObjective(ObjectiveData data) : base(data)
        {
        }

        public override float Progress01 => Status == ObjectiveStatus.Completed ? 1f : 0f;

        public override string ProgressText => Status == ObjectiveStatus.Completed ? "Reached" : "En route";

        protected override void OnActivated()
        {
            Events.Subscribe<WaypointReachedEvent>(OnWaypointReached);
        }

        protected override void OnDeactivated()
        {
            Events.Unsubscribe<WaypointReachedEvent>(OnWaypointReached);
        }

        private void OnWaypointReached(WaypointReachedEvent evt)
        {
            if (Status == ObjectiveStatus.Active && evt.WaypointId == Data.targetId) MarkCompleted();
        }
    }
}
