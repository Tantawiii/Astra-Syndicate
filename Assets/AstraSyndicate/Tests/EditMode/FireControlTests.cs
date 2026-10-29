using AstraSyndicate.Combat;
using NUnit.Framework;

namespace AstraSyndicate.Tests
{
    public class FireControlTests
    {
        [Test]
        public void FirstShot_IsAllowedImmediately()
        {
            Assert.IsTrue(new FireControl(10f).TryFire(0f));
        }

        [Test]
        public void Shots_AreLimitedToTheFireRate()
        {
            var trigger = new FireControl(10f);

            Assert.IsTrue(trigger.TryFire(0f));
            Assert.IsFalse(trigger.TryFire(0.05f));
            Assert.IsTrue(trigger.TryFire(0.1f));
        }

        [Test]
        public void HeldTrigger_KeepsItsCadenceThroughLateFrames()
        {
            var trigger = new FireControl(10f);

            Assert.IsTrue(trigger.TryFire(0f));
            Assert.IsTrue(trigger.TryFire(0.13f));
            Assert.IsTrue(trigger.TryFire(0.2f), "A late frame must not push every later shot back.");
        }

        [Test]
        public void AfterAPause_TheWeaponDoesNotBurst()
        {
            var trigger = new FireControl(10f);
            trigger.TryFire(0f);

            Assert.IsTrue(trigger.TryFire(5f));
            Assert.IsFalse(trigger.TryFire(5.01f));
        }
    }
}
