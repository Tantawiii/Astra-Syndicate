using UnityEngine;

namespace AstraSyndicate.Combat
{
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 Point;

        /// <summary>The ship that caused the damage; null for environment damage.</summary>
        public readonly Ship Source;

        public DamageInfo(float amount, Vector3 point, Ship source)
        {
            Amount = amount;
            Point = point;
            Source = source;
        }
    }
}
