using AstraSyndicate.Combat;
using NUnit.Framework;
using UnityEngine;

namespace AstraSyndicate.Tests
{
    public class FlightMathTests
    {
        private const float MaxRate = 2f;
        private const float Gain = 3f;

        [Test]
        public void TargetAhead_NeedsNoTurn()
        {
            Vector3 rates = FlightMath.SteeringRates(Quaternion.identity, Vector3.forward, MaxRate, MaxRate, Gain);

            Assert.AreEqual(0f, rates.x, 1e-4f);
            Assert.AreEqual(0f, rates.y, 1e-4f);
        }

        [Test]
        public void TargetToTheRight_YawsTowardIt()
        {
            var desired = new Vector3(1f, 0f, 1f);
            Vector3 rates = FlightMath.SteeringRates(Quaternion.identity, desired, MaxRate, MaxRate, Gain);

            Assert.Greater(rates.y, 0f);
            Assert.AreEqual(0f, rates.x, 1e-4f);
            Assert.Less(AngleAfterTurning(rates, desired), 45f, "Turning at the returned rate must reduce the heading error.");
        }

        [Test]
        public void TargetAbove_PitchesTheNoseUp()
        {
            var desired = new Vector3(0f, 1f, 1f);
            Vector3 rates = FlightMath.SteeringRates(Quaternion.identity, desired, MaxRate, MaxRate, Gain);

            Assert.Less(rates.x, 0f);
            Assert.Less(AngleAfterTurning(rates, desired), 45f, "Turning at the returned rate must reduce the heading error.");
        }

        [Test]
        public void Rates_AreClampedToTheShipsLimits()
        {
            Vector3 rates = FlightMath.SteeringRates(Quaternion.identity, new Vector3(0.01f, -0.2f, -1f), 0.5f, 0.7f, 100f);

            Assert.LessOrEqual(Mathf.Abs(rates.x), 0.5f + 1e-5f);
            Assert.AreEqual(0.7f, Mathf.Abs(rates.y), 1e-5f);
        }

        [Test]
        public void LevelShip_HasNoRollError()
        {
            Assert.AreEqual(0f, FlightMath.RollError(Quaternion.LookRotation(new Vector3(1f, 0.3f, 1f))), 1e-4f);
        }

        [Test]
        public void RolledShip_LevelingRateRollsItBackTowardLevel()
        {
            Quaternion rolled = Quaternion.AngleAxis(30f, Vector3.forward);
            float rate = FlightMath.LevelingRollRate(rolled, 1f);

            Quaternion afterHalfSecond = Quaternion.AngleAxis(rate * Mathf.Rad2Deg * 0.5f, rolled * Vector3.forward) * rolled;

            Assert.Less(Mathf.Abs(FlightMath.RollError(afterHalfSecond)), Mathf.Abs(FlightMath.RollError(rolled)));
        }

        [Test]
        public void LevelRoll_FullStepLevelsWithoutChangingHeading()
        {
            Quaternion rolled = Quaternion.LookRotation(new Vector3(1f, 0.2f, 1f)) * Quaternion.AngleAxis(40f, Vector3.forward);

            Quaternion level = FlightMath.LevelRoll(rolled, 1f);

            Assert.AreEqual(0f, FlightMath.RollError(level), 1e-3f);
            Assert.Less(Vector3.Angle(rolled * Vector3.forward, level * Vector3.forward), 0.01f);
        }

        [Test]
        public void PointingStraightUp_SkipsLeveling()
        {
            Assert.AreEqual(0f, FlightMath.RollError(Quaternion.LookRotation(Vector3.up, Vector3.back)));
        }

        // Heading error after turning for 0.1 s at the given local rates from the identity rotation.
        private static float AngleAfterTurning(Vector3 localRates, Vector3 desired)
        {
            Quaternion turned = Quaternion.AngleAxis(localRates.magnitude * Mathf.Rad2Deg * 0.1f, localRates.normalized);
            return Vector3.Angle(turned * Vector3.forward, desired);
        }
    }
}
