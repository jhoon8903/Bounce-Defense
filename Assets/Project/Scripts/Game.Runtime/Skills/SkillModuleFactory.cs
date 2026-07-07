using Game.Combat;
using Game.Runtime.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
    public static class SkillModuleFactory
    {
        public static BallSourceType BallTypeOf(SkillEffectKind kind)
        {
            switch (kind)
            {
                case SkillEffectKind.FireBall: return BallSourceType.Fire;
                case SkillEffectKind.IceBall: return BallSourceType.Ice;
                case SkillEffectKind.LaserBall: return BallSourceType.Laser;
                case SkillEffectKind.GhostBall: return BallSourceType.Ghost;
                case SkillEffectKind.ClusterBall: return BallSourceType.Cluster;
                default: return BallSourceType.Normal;
            }
        }

        public static bool PenetratesEnemies(SkillEffectKind kind) => kind == SkillEffectKind.GhostBall;

        public static IBallModule CreateBallModule(SkillDefinition skill, int level)
        {
            if (skill == null) return null;
            switch (skill.EffectKind)
            {
                case SkillEffectKind.FireBall: return new FireBallModule(skill, level);
                case SkillEffectKind.IceBall: return new IceBallModule(skill, level);
                case SkillEffectKind.LaserBall: return new LaserBallModule(skill, level);
                case SkillEffectKind.ClusterBall: return new ClusterBallModule(skill, level);
                default: return null;
            }
        }

        public static IDamageModifier CreatePassiveModifier(SkillDefinition skill, int level)
        {
            if (skill == null) return null;
            switch (skill.EffectKind)
            {
                case SkillEffectKind.WarmTin: return new WarmTinModifier(level);
                case SkillEffectKind.AmethystDagger: return new AmethystDaggerModifier(level);
                case SkillEffectKind.EmeraldDagger: return new EmeraldDaggerModifier(level);
                case SkillEffectKind.IceBall: return new IceBonusModifier(skill, level);
                default: return null;
            }
        }
    }
}
