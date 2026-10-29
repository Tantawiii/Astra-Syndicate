using AstraSyndicate.AI;
using NUnit.Framework;
using UnityEngine;

namespace AstraSyndicate.Tests
{
    public class FormationTests
    {
        [Test]
        public void FirstTwoSlots_AreMirroredBehindTheLeader()
        {
            Assert.AreEqual(new Vector3(-10f, 0f, -10f), Formation.SlotOffset(0, 10f));
            Assert.AreEqual(new Vector3(10f, 0f, -10f), Formation.SlotOffset(1, 10f));
        }

        [Test]
        public void LaterRanks_AreFurtherOutAndFurtherBack()
        {
            Assert.AreEqual(new Vector3(-20f, 0f, -20f), Formation.SlotOffset(2, 10f));
            Assert.AreEqual(new Vector3(20f, 0f, -20f), Formation.SlotOffset(3, 10f));
        }

        [Test]
        public void NegativeSlot_IsTreatedAsTheFirstSlot()
        {
            Assert.AreEqual(Formation.SlotOffset(0, 10f), Formation.SlotOffset(-3, 10f));
        }
    }
}
