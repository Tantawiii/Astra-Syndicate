using AstraSyndicate.Combat;
using AstraSyndicate.Core;
using NUnit.Framework;

namespace AstraSyndicate.Tests
{
    public class ProjectileRulesTests
    {
        [Test]
        public void Scenery_StopsShots()
        {
            Assert.IsTrue(ProjectileRules.CanHit(null, Faction.Player, null));
        }

        [Test]
        public void HostileShips_AreHit()
        {
            Assert.IsTrue(ProjectileRules.CanHit(null, Faction.Player, new FakeTarget(Faction.Enemy)));
        }

        [Test]
        public void SameFaction_IsPassedThrough()
        {
            Assert.IsFalse(ProjectileRules.CanHit(null, Faction.Player, new FakeTarget(Faction.Player)));
        }

        [Test]
        public void NeutralDamageables_AreHit()
        {
            Assert.IsTrue(ProjectileRules.CanHit(null, Faction.Enemy, new FakeTarget(Faction.Neutral)));
        }

        [Test]
        public void Owner_IsPassedThrough()
        {
            var owner = new FakeTarget(Faction.Enemy);

            Assert.IsFalse(ProjectileRules.CanHit(owner, Faction.Player, owner));
        }

        [Test]
        public void Wrecks_ArePassedThrough()
        {
            Assert.IsFalse(ProjectileRules.CanHit(null, Faction.Player, new FakeTarget(Faction.Enemy, alive: false)));
        }

        private sealed class FakeTarget : IDamageable
        {
            public FakeTarget(Faction faction, bool alive = true)
            {
                Faction = faction;
                IsAlive = alive;
            }

            public Faction Faction { get; }

            public bool IsAlive { get; }

            public void ApplyDamage(DamageInfo damage)
            {
            }
        }
    }
}
