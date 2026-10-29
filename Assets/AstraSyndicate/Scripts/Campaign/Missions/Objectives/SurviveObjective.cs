using UnityEngine;

namespace AstraSyndicate.Campaign
{
    /// <summary>Stay in the fight for <c>durationSeconds</c> of mission time.</summary>
    public sealed class SurviveObjective : ObjectiveBase
    {
        private float _elapsed;
        private int _lastWholeSecond;

        public SurviveObjective(ObjectiveData data) : base(data)
        {
        }

        public override float Progress01 => Mathf.Clamp01(_elapsed / Duration);

        public override string ProgressText => $"{Mathf.CeilToInt(Mathf.Max(0f, Duration - _elapsed))}s left";

        private float Duration => Mathf.Max(0.01f, Data.durationSeconds);

        public override void Tick(float deltaTime)
        {
            if (Status != ObjectiveStatus.Active) return;

            _elapsed += deltaTime;
            if (_elapsed >= Duration)
            {
                MarkCompleted();
                return;
            }

            // Notify once per second rather than every frame.
            int wholeSecond = Mathf.FloorToInt(_elapsed);
            if (wholeSecond == _lastWholeSecond) return;

            _lastWholeSecond = wholeSecond;
            NotifyChanged();
        }
    }
}
