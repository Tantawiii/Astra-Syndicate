using UnityEngine;

namespace AstraSyndicate.AI
{
    public static class Formation
    {
        /// <summary>
        /// Offset of a wingman slot from its leader, in the leader's local space: a V behind the leader that
        /// alternates left and right, each rank further out and further back.
        /// </summary>
        public static Vector3 SlotOffset(int slot, float spacing)
        {
            slot = Mathf.Max(0, slot);
            int rank = slot / 2 + 1;
            float side = slot % 2 == 0 ? -1f : 1f;
            return new Vector3(side * spacing * rank, 0f, -spacing * rank);
        }
    }
}
