using AstraSyndicate.Combat;
using NUnit.Framework;
using UnityEngine;

namespace AstraSyndicate.Tests
{
    public class BallisticsTests
    {
        [Test]
        public void StationaryTarget_IsAimedAtDirectly()
        {
            Assert.IsTrue(Ballistics.TryPredictIntercept(Vector3.zero, Vector3.zero, 100f, new Vector3(0f, 0f, 200f), Vector3.zero, out Vector3 aim));

            AssertNear(new Vector3(0f, 0f, 200f), aim);
        }

        [Test]
        public void MovingTarget_IsLed_SoShotAndTargetArriveTogether()
        {
            var target = new Vector3(0f, 0f, 300f);
            var targetVelocity = new Vector3(40f, 0f, 0f);
            const float projectileSpeed = 200f;

            Assert.IsTrue(Ballistics.TryPredictIntercept(Vector3.zero, Vector3.zero, projectileSpeed, target, targetVelocity, out Vector3 aim));

            float flightTime = aim.magnitude / projectileSpeed;
            AssertNear(target + targetVelocity * flightTime, aim);
            Assert.Greater(aim.x, 0f, "The aim point must lead in the direction the target moves.");
        }

        [Test]
        public void ShooterVelocity_IsInheritedByTheProjectile()
        {
            // Shooter and target fly in formation: no relative motion, so no lead is needed.
            var sharedVelocity = new Vector3(0f, 0f, 50f);

            Assert.IsTrue(Ballistics.TryPredictIntercept(Vector3.zero, sharedVelocity, 100f, new Vector3(100f, 0f, 0f), sharedVelocity, out Vector3 aim));

            AssertNear(new Vector3(100f, 0f, 0f), aim);
        }

        [Test]
        public void TargetOutrunningTheProjectile_HasNoIntercept()
        {
            var target = new Vector3(0f, 0f, 100f);

            Assert.IsFalse(Ballistics.TryPredictIntercept(Vector3.zero, Vector3.zero, 50f, target, new Vector3(0f, 0f, 80f), out Vector3 aim));
            AssertNear(target, aim);
        }

        private static void AssertNear(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), 0.05f, $"Expected {expected} but was {actual}.");
        }
    }
}
