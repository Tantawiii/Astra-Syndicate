using UnityEngine;

namespace AstraSyndicate.AI
{
    /// <summary>A looping patrol path. Its waypoints are this object's children, in hierarchy order.</summary>
    public sealed class PatrolRoute : MonoBehaviour
    {
        private Transform[] _points;

        public int Count => Points.Length;

        private Transform[] Points
        {
            get
            {
                if (_points == null || _points.Length != transform.childCount) _points = CollectPoints();
                return _points;
            }
        }

        /// <summary>Waypoint position; the index wraps around the loop.</summary>
        public Vector3 GetPoint(int index)
        {
            int count = Count;
            return Points[((index % count) + count) % count].position;
        }

        public int NearestIndex(Vector3 position)
        {
            int nearest = 0;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                float sqr = (Points[i].position - position).sqrMagnitude;
                if (sqr >= nearestSqr) continue;
                nearest = i;
                nearestSqr = sqr;
            }
            return nearest;
        }

        private Transform[] CollectPoints()
        {
            var points = new Transform[transform.childCount];
            for (int i = 0; i < points.Length; i++) points[i] = transform.GetChild(i);
            return points;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.8f);
            int count = transform.childCount;
            for (int i = 0; i < count; i++)
            {
                Vector3 point = transform.GetChild(i).position;
                Gizmos.DrawWireSphere(point, 8f);
                Gizmos.DrawLine(point, transform.GetChild((i + 1) % count).position);
            }
        }
    }
}
