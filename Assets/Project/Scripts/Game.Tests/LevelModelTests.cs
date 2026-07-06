using Game.Roguelike;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class LevelModelTests
    {
        [Test]
        public void AddKill_AccumulatesXpWithoutLevelUp()
        {
            LevelModel m = new LevelModel(1, 5, 0);
            for (int i = 0; i < 4; i++) m.AddKill();
            Assert.AreEqual(1, m.Level);
            Assert.AreEqual(4, m.Xp);
            Assert.AreEqual(0.8f, m.Progress, 0.0001f);
        }

        [Test]
        public void AddKill_LevelsUpAtThreshold_FiresEvent()
        {
            LevelModel m = new LevelModel(1, 5, 0);
            int fired = 0;
            m.OnLevelUp += () => fired++;
            for (int i = 0; i < 5; i++) m.AddKill();
            Assert.AreEqual(2, m.Level);
            Assert.AreEqual(0, m.Xp);
            Assert.AreEqual(1, m.PendingLevelUps);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void AddKill_CarriesRemainder()
        {
            LevelModel m = new LevelModel(1, 5, 0);
            for (int i = 0; i < 6; i++) m.AddKill();
            Assert.AreEqual(2, m.Level);
            Assert.AreEqual(1, m.Xp);
        }

        [Test]
        public void Growth_IncreasesNextThreshold()
        {
            LevelModel m = new LevelModel(1, 5, 2);
            for (int i = 0; i < 12; i++) m.AddKill();
            Assert.AreEqual(3, m.Level);
            Assert.AreEqual(0, m.Xp);
            Assert.AreEqual(2, m.PendingLevelUps);
        }

        [Test]
        public void TryConsumeLevelUp_DrainsQueue()
        {
            LevelModel m = new LevelModel(1, 5, 2);
            for (int i = 0; i < 12; i++) m.AddKill(); // 2 pending
            Assert.IsTrue(m.TryConsumeLevelUp());
            Assert.IsTrue(m.TryConsumeLevelUp());
            Assert.IsFalse(m.TryConsumeLevelUp());
            Assert.AreEqual(0, m.PendingLevelUps);
        }

        [Test]
        public void ResetProgression_BackToLevel1()
        {
            LevelModel m = new LevelModel(1, 5, 0);
            for (int i = 0; i < 7; i++) m.AddKill();
            m.ResetProgression();
            Assert.AreEqual(1, m.Level);
            Assert.AreEqual(0, m.Xp);
            Assert.AreEqual(0, m.PendingLevelUps);
        }
    }
}
