using AstraSyndicate.Combat;
using NUnit.Framework;

namespace AstraSyndicate.Tests
{
    public class HitPointsTests
    {
        [Test]
        public void Damage_ReducesHullAndReturnsTheAmountApplied()
        {
            var hull = new HitPoints(100f);

            Assert.AreEqual(30f, hull.ApplyDamage(30f));
            Assert.AreEqual(70f, hull.Current);
            Assert.AreEqual(0.7f, hull.Fraction, 1e-5f);
        }

        [Test]
        public void Damage_NeverGoesBelowZero()
        {
            var hull = new HitPoints(50f);

            Assert.AreEqual(50f, hull.ApplyDamage(80f));
            Assert.AreEqual(0f, hull.Current);
            Assert.IsTrue(hull.IsDepleted);
        }

        [Test]
        public void DepletedHull_IgnoresFurtherDamageAndRepairs()
        {
            var hull = new HitPoints(100f);
            hull.ApplyDamage(100f);

            Assert.AreEqual(0f, hull.ApplyDamage(10f));
            Assert.AreEqual(0f, hull.Restore(10f));
            Assert.AreEqual(0f, hull.Current);
        }

        [Test]
        public void Restore_IsCappedAtMax()
        {
            var hull = new HitPoints(100f);
            hull.ApplyDamage(20f);

            Assert.AreEqual(20f, hull.Restore(50f));
            Assert.AreEqual(100f, hull.Current);
        }

        [Test]
        public void NegativeAmounts_AreIgnored()
        {
            var hull = new HitPoints(100f);

            Assert.AreEqual(0f, hull.ApplyDamage(-5f));
            Assert.AreEqual(0f, hull.Restore(-5f));
            Assert.AreEqual(100f, hull.Current);
        }

        [Test]
        public void Max_IsAtLeastOne()
        {
            Assert.AreEqual(1f, new HitPoints(0f).Max);
        }
    }
}
