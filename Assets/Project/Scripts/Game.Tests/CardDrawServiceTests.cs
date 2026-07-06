using System;
using System.Collections.Generic;
using Game.Core.Random;
using Game.Roguelike;
using Game.Skills;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class CardDrawServiceTests
    {
        private SkillDefinition a1, a2, a3, a4, a5, p1, p2, p3;
        private SkillDatabase db;
        private PlayerLoadout loadout;
        private CardDrawService draw;

        [SetUp]
        public void Setup()
        {
            a1 = SkillTestFactory.Skill("a1", SkillCategory.Active, 3, new[] { 10, 20, 30 });
            a2 = SkillTestFactory.Skill("a2", SkillCategory.Active, 3, new[] { 11, 21, 31 });
            a3 = SkillTestFactory.Skill("a3", SkillCategory.Active, 3, new[] { 12, 22, 32 });
            a4 = SkillTestFactory.Skill("a4", SkillCategory.Active, 3, new[] { 13, 23, 33 });
            a5 = SkillTestFactory.Skill("a5", SkillCategory.Active, 3, new[] { 14, 24, 34 });
            p1 = SkillTestFactory.Skill("p1", SkillCategory.Passive);
            p2 = SkillTestFactory.Skill("p2", SkillCategory.Passive);
            p3 = SkillTestFactory.Skill("p3", SkillCategory.Passive);
            db = SkillTestFactory.Database(new[] { a1, a2, a3, a4, a5 }, new[] { p1, p2, p3 });
            loadout = new PlayerLoadout();
            draw = new CardDrawService(db, loadout, new SystemRandom(1));
        }

        private static bool Has(List<SkillCard> cards, SkillDefinition s) => cards.Exists(c => c.Definition == s);
        private static SkillCard Of(List<SkillCard> cards, SkillDefinition s) => cards.Find(c => c.Definition == s);

        [Test]
        public void AllUnowned_OffersEveryNewAtLevel1()
        {
            List<SkillCard> all = draw.Draw(100);
            Assert.AreEqual(8, all.Count);
            foreach (SkillCard c in all)
            {
                Assert.IsTrue(c.IsNew, "unowned should be new: " + c.Definition.SkillId);
                Assert.AreEqual(1, c.Level);
            }
        }

        [Test]
        public void OwnedSkill_OffersUpgradeNotNew()
        {
            loadout.Acquire(a1); // L1
            SkillCard card = Of(draw.Draw(100), a1);
            Assert.IsTrue(card.IsValid);
            Assert.IsFalse(card.IsNew);
            Assert.AreEqual(2, card.Level); // owned+1
        }

        [Test]
        public void MaxedSkill_Excluded()
        {
            loadout.Acquire(a1); loadout.Upgrade(a1); loadout.Upgrade(a1); // L3 = max
            Assert.IsFalse(Has(draw.Draw(100), a1));
        }

        [Test]
        public void NoDuplicateSkillPerDraw()
        {
            List<SkillCard> all = draw.Draw(100);
            HashSet<SkillDefinition> seen = new HashSet<SkillDefinition>();
            foreach (SkillCard c in all)
                Assert.IsTrue(seen.Add(c.Definition), "duplicate skill: " + c.Definition.SkillId);
        }

        [Test]
        public void ActiveCapReached_NoNewActive_UpgradesAndPassivesOnly()
        {
            loadout.Acquire(a1); loadout.Acquire(a2); loadout.Acquire(a3); loadout.Acquire(a4); // 4 = cap
            List<SkillCard> all = draw.Draw(100);
            Assert.IsFalse(Has(all, a5), "capped: unowned 5th active must not appear");
            Assert.IsTrue(Has(all, a1) && !Of(all, a1).IsNew, "owned active offered as upgrade");
            Assert.IsTrue(Has(all, p1) && Of(all, p1).IsNew, "unowned passive still new");
        }

        [Test]
        public void PassiveCapReached_NoNewPassive_UpgradesOnly()
        {
            loadout.Acquire(p1); loadout.Acquire(p2);
            List<SkillCard> all = draw.Draw(100);
            Assert.IsFalse(Has(all, p3), "capped: unowned 3rd passive must not appear");
            Assert.IsTrue(Has(all, p1) && !Of(all, p1).IsNew, "owned passive offered as upgrade");
            Assert.IsTrue(Has(all, a1) && Of(all, a1).IsNew, "unowned active still new");
        }

        [Test]
        public void FewerThanThreeValid_ReturnsWhatExists()
        {
            SkillDatabase tiny = SkillTestFactory.Database(new[] { a1 }, new[] { p1 });
            CardDrawService d = new CardDrawService(tiny, new PlayerLoadout(), new SystemRandom(1));
            Assert.AreEqual(2, d.Draw(3).Count);
        }

        [Test]
        public void ZeroValid_ReturnsEmpty()
        {
            PlayerLoadout lo = new PlayerLoadout();
            SkillDatabase tiny = SkillTestFactory.Database(new[] { a1 }, Array.Empty<SkillDefinition>());
            lo.Acquire(a1); lo.Upgrade(a1); lo.Upgrade(a1);
            CardDrawService d = new CardDrawService(tiny, lo, new SystemRandom(1));
            Assert.AreEqual(0, d.Draw(3).Count);
        }

        [Test]
        public void DrawThree_CapsAtThree()
        {
            Assert.AreEqual(3, draw.Draw(3).Count);
        }

        [Test]
        public void SameSeed_IsDeterministic()
        {
            CardDrawService d1 = new CardDrawService(db, new PlayerLoadout(), new SystemRandom(42));
            CardDrawService d2 = new CardDrawService(db, new PlayerLoadout(), new SystemRandom(42));
            List<SkillCard> r1 = d1.Draw(3);
            List<SkillCard> r2 = d2.Draw(3);
            Assert.AreEqual(r1.Count, r2.Count);
            for (int i = 0; i < r1.Count; i++) Assert.AreEqual(r1[i].Definition, r2[i].Definition);
        }
    }
}
