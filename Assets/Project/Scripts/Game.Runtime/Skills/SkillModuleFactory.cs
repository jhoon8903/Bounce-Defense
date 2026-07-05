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

        // 적 관통 여부(스폰-타임 모터 속성). Ghost만 true — BallController가 passThroughMask를 켜는 근거.
        // 온-히트 모듈로 표현 불가한 유일한 효과라 spec 플래그로 전달(타입 지식은 여기 팩토리에만).
        public static bool PenetratesEnemies(SkillEffectKind kind) => kind == SkillEffectKind.GhostBall;

        // 액티브 볼 모듈(온-히트 동작). 수치는 skill(SkillDefinition)에서 읽는다(§11-17). 미구현 스킬은 null(순수 데미지 볼).
        public static IBallModule CreateBallModule(SkillDefinition skill, int level)
        {
            if (skill == null) return null;
            switch (skill.EffectKind)
            {
                case SkillEffectKind.FireBall: return new FireBallModule(skill, level);
                case SkillEffectKind.IceBall: return new IceBallModule(skill, level);
                case SkillEffectKind.LaserBall: return new LaserBallModule(skill, level);
                case SkillEffectKind.ClusterBall: return new ClusterBallModule(skill, level);
                default: return null; // GhostBall = 관통(스폰타임 spec 플래그, 모듈 없음)
            }
        }

        // 패시브 데미지 모디파이어(SkillRuntime이 패시브+액티브 모두 스캔). 미구현/기여없음은 null.
        public static IDamageModifier CreatePassiveModifier(SkillDefinition skill, int level)
        {
            if (skill == null) return null;
            switch (skill.EffectKind)
            {
                case SkillEffectKind.WarmTin: return new WarmTinModifier(level);
                case SkillEffectKind.AmethystDagger: return new AmethystDaggerModifier(level); // 전면 크리
                case SkillEffectKind.EmeraldDagger: return new EmeraldDaggerModifier(level);   // 후면 크리
                case SkillEffectKind.IceBall: return new IceBonusModifier(skill, level);       // Ice 상시 추가뎀(액티브 기여)
                default: return null; // MagicMirror/LastMatch = fan-out
            }
        }
    }
}
