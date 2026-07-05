using Game.Combat;

namespace Game.Runtime.Combat
{
    // 볼 한 슬롯의 스폰 사양. SkillRuntime이 로드아웃에서 만들어 BallController.SetRoster로 넘긴다.
    //  - Normal 슬롯 = { Normal, 노멀 baseDamage, module=null }.
    //  - 액티브 슬롯 = { 볼 타입, 스킬 레벨 baseDamage, 볼 모듈 }.
    // penetratesEnemies = 적 관통(Ghost). 스폰 시 모터 passThroughMask 결정에 쓰이는 스폰-타임 속성
    // (온-히트 모듈로 표현 불가) — 타입 지식은 SkillModuleFactory가 소유, BallController는 플래그만 본다.
    public readonly struct BallSpawnSpec
    {
        public readonly BallSourceType SourceType;
        public readonly float BaseDamage;
        public readonly IBallModule Module;
        public readonly bool PenetratesEnemies;
        // 직격 데미지 종류. Direct = 크리·모디파이어 대상(로스터 볼). Cluster 특수볼 등 2차볼은 ClusterSpawn 등으로 무크리·무버프.
        public readonly DamageKind DamageKind;

        public BallSpawnSpec(BallSourceType sourceType, float baseDamage, IBallModule module,
            bool penetratesEnemies = false, DamageKind damageKind = DamageKind.Direct)
        {
            SourceType = sourceType;
            BaseDamage = baseDamage;
            Module = module;
            PenetratesEnemies = penetratesEnemies;
            DamageKind = damageKind;
        }
    }
}
