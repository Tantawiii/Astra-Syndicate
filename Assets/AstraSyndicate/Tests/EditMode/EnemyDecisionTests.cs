using AstraSyndicate.AI;
using NUnit.Framework;

namespace AstraSyndicate.Tests
{
    public class EnemyDecisionTests
    {
        private static readonly EnemyRules Rules = new EnemyRules(retreatBelowHull: 0.3f, recoverAtHull: 0.75f, leashDistance: 1000f);

        [Test]
        public void Patrol_WithoutTarget_KeepsPatrolling()
        {
            Assert.AreEqual(EnemyState.Patrol, Next(EnemyState.Patrol));
        }

        [Test]
        public void Patrol_SpottingATarget_Attacks()
        {
            Assert.AreEqual(EnemyState.Attack, Next(EnemyState.Patrol, hasTarget: true));
        }

        [Test]
        public void Patrol_NearTheLeashEdge_DoesNotStartAChase()
        {
            Assert.AreEqual(EnemyState.Patrol, Next(EnemyState.Patrol, hasTarget: true, fromHome: 900f));
        }

        [Test]
        public void Patrol_BadlyDamaged_Retreats()
        {
            Assert.AreEqual(EnemyState.Retreat, Next(EnemyState.Patrol, hull: 0.2f));
        }

        [Test]
        public void Attack_BadlyDamaged_Retreats()
        {
            Assert.AreEqual(EnemyState.Retreat, Next(EnemyState.Attack, hull: 0.3f, hasTarget: true));
        }

        [Test]
        public void Attack_LosingTheTarget_ReturnsToPatrol()
        {
            Assert.AreEqual(EnemyState.Patrol, Next(EnemyState.Attack, hasTarget: false));
        }

        [Test]
        public void Attack_BeyondTheLeash_ReturnsToPatrol()
        {
            Assert.AreEqual(EnemyState.Patrol, Next(EnemyState.Attack, hasTarget: true, fromHome: 1001f));
        }

        [Test]
        public void Attack_InsideTheLeash_KeepsAttacking()
        {
            Assert.AreEqual(EnemyState.Attack, Next(EnemyState.Attack, hasTarget: true, fromHome: 900f));
        }

        [Test]
        public void Retreat_ContinuesUntilRepaired()
        {
            Assert.AreEqual(EnemyState.Retreat, Next(EnemyState.Retreat, hull: 0.5f, hasTarget: true));
        }

        [Test]
        public void Retreat_OnceRepaired_ReturnsToPatrol()
        {
            Assert.AreEqual(EnemyState.Patrol, Next(EnemyState.Retreat, hull: 0.75f));
        }

        private static EnemyState Next(EnemyState state, float hull = 1f, bool hasTarget = false, float fromHome = 0f)
        {
            return EnemyDecision.Next(new EnemySituation(state, hull, hasTarget, fromHome), Rules);
        }
    }
}
