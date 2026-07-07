using Game.Roguelike;
using Game.Skills;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class PlayerLoadoutTests
    {
        private PlayerLoadout lo;
        private SkillDefinition active;
        private SkillDefinition passive;

        [SetUp]
        public void Setup()
        {
            lo = new PlayerLoadout();
            active = SkillTestFactory.Skill("act", SkillCategory.Active, 3);
            passive = SkillTestFactory.Skill("pas", SkillCategory.Passive, 3);
        }

        [Test]
        public void Acquire_AddsAtLevel1()
        {
            lo.Acquire(active);
            Assert.IsTrue(lo.Owns(active));
            Assert.AreEqual(1, lo.LevelOf(active));
            Assert.AreEqual(1, lo.ActiveCount);
        }

        [Test]
        public void Acquire_Twice_NoDuplicate()
        {
            lo.Acquire(active); lo.Acquire(active);
            Assert.AreEqual(1, lo.ActiveCount);
            Assert.AreEqual(1, lo.LevelOf(active));
        }

        [Test]
        public void Acquire_RespectsActiveCap()
        {
            for (int i = 0; i < 6; i++)
            {
                lo.Acquire(SkillTestFactory.Skill("a" + i, SkillCategory.Active, 3));
            }
            Assert.AreEqual(PlayerLoadout.ActiveCap, lo.ActiveCount);
            Assert.IsTrue(lo.IsFull(SkillCategory.Active));
        }

        [Test]
        public void Acquire_RespectsPassiveCap()
        {
            for (int i = 0; i < 4; i++)
            {
                lo.Acquire(SkillTestFactory.Skill("p" + i, SkillCategory.Passive, 3));
            }
            Assert.AreEqual(PlayerLoadout.PassiveCap, lo.PassiveCount);
            Assert.IsTrue(lo.IsFull(SkillCategory.Passive));
        }

        [Test]
        public void Upgrade_Increments()
        {
            lo.Acquire(active); lo.Upgrade(active);
            Assert.AreEqual(2, lo.LevelOf(active));
        }

        [Test]
        public void Upgrade_ClampsAtMaxLevel()
        {
            lo.Acquire(active);
            for (int i = 0; i < 5; i++)
            {
                lo.Upgrade(active);
            }
            Assert.AreEqual(active.MaxLevel, lo.LevelOf(active));
        }

        [Test]
        public void Upgrade_UnownedNoOp()
        {
            lo.Upgrade(active);
            Assert.IsFalse(lo.Owns(active));
        }

        [Test]
        public void Apply_NewAcquires_UpgradeUpgrades()
        {
            lo.Apply(new SkillCard(active, 1, isNew: true));
            Assert.AreEqual(1, lo.LevelOf(active));
            lo.Apply(new SkillCard(active, 2, isNew: false));
            Assert.AreEqual(2, lo.LevelOf(active));
        }

        [Test]
        public void ResetLoadout_Clears()
        {
            lo.Acquire(active); lo.Acquire(passive);
            lo.ResetLoadout();
            Assert.AreEqual(0, lo.ActiveCount);
            Assert.AreEqual(0, lo.PassiveCount);
            Assert.IsFalse(lo.Owns(active));
        }
    }
}
