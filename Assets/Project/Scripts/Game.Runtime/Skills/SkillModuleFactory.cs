using Game.Combat;
using Game.Runtime.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
    // SkillDefinition(데이터) → 런타임 전투 객체 매핑의 단일 지점. asmdef 경계를 넘는 유일한 곳:
    // Game.Skills는 Game.Combat를 못 보므로 Assembly-CSharp인 여기서 EffectKind를 볼 타입/모듈로 번역.
    //   11번째 스킬 = SkillEffectKind 값 + 모듈 클래스 + 아래 case 1줄 (BallController·DamageResolver 무수정).
    // 슬라이스 구현 = FireBall(모듈) + WarmTin(모디파이어). 나머지 8개는 null(fan-out) — 순수 데미지 볼로 동작.
    public static class SkillModuleFactory
    {
        // 액티브 스킬의 볼 타입. None/패시브면 Normal.
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

        // 액티브 볼 모듈(온-히트 동작). 미구현 스킬은 null(효과 없는 순수 데미지 볼).
        public static IBallModule CreateBallModule(SkillEffectKind kind, int level)
        {
            switch (kind)
            {
                case SkillEffectKind.FireBall: return new FireBallModule(level);
                default: return null; // IceBall/LaserBall/GhostBall/ClusterBall = fan-out
            }
        }

        // 패시브 데미지 모디파이어. 미구현 패시브는 null.
        public static IDamageModifier CreatePassiveModifier(SkillEffectKind kind, int level)
        {
            switch (kind)
            {
                case SkillEffectKind.WarmTin: return new WarmTinModifier(level);
                default: return null; // MagicMirror/AmethystDagger/EmeraldDagger/LastMatch = fan-out
            }
        }
    }
}
