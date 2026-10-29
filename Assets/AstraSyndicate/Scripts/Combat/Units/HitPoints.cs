using UnityEngine;

namespace AstraSyndicate.Combat
{
    /// <summary>Hull points of a unit. Plain C# so the damage rules can be unit-tested.</summary>
    public sealed class HitPoints
    {
        public HitPoints(float max)
        {
            Max = Mathf.Max(1f, max);
            Current = Max;
        }

        public float Max { get; }

        public float Current { get; private set; }

        public float Fraction => Current / Max;

        public bool IsDepleted => Current <= 0f;

        /// <summary>Removes up to <paramref name="amount"/> points and returns how many were actually removed.</summary>
        public float ApplyDamage(float amount)
        {
            if (IsDepleted || amount <= 0f) return 0f;

            float applied = Mathf.Min(amount, Current);
            Current -= applied;
            return applied;
        }

        /// <summary>Restores up to <paramref name="amount"/> points (never above max, never once depleted).</summary>
        public float Restore(float amount)
        {
            if (IsDepleted || amount <= 0f) return 0f;

            float applied = Mathf.Min(amount, Max - Current);
            Current += applied;
            return applied;
        }
    }
}
