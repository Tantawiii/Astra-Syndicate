using UnityEngine;

namespace AstraSyndicate.Combat
{
    /// <summary>Rate-of-fire gate. Takes the time as a parameter so it can be unit-tested.</summary>
    public sealed class FireControl
    {
        private float _nextShotTime = float.NegativeInfinity;

        public FireControl(float shotsPerSecond)
        {
            Interval = 1f / Mathf.Max(0.01f, shotsPerSecond);
        }

        public float Interval { get; }

        public bool IsReady(float time)
        {
            return time >= _nextShotTime;
        }

        public bool TryFire(float time)
        {
            if (time < _nextShotTime) return false;

            // Holding the trigger keeps a steady cadence; after a pause the cadence restarts from now.
            _nextShotTime = time - _nextShotTime <= Interval ? _nextShotTime + Interval : time + Interval;
            return true;
        }
    }
}
