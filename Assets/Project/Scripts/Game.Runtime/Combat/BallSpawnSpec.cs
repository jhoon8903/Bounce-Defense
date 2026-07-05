using Game.Combat;

namespace Game.Runtime.Combat
{
    // 볼 한 슬롯의 스폰 사양. SkillRuntime이 로드아웃에서 만들어 BallController.SetRoster로 넘긴다.
    //  - Normal 슬롯 = { Normal, 노멀 baseDamage, module=null }.
    //  - 액티브 슬롯 = { 볼 타입, 스킬 레벨 baseDamage, 볼 모듈 }.
    public readonly struct BallSpawnSpec
    {
        public readonly BallSourceType SourceType;
        public readonly float BaseDamage;
        public readonly IBallModule Module;

        public BallSpawnSpec(BallSourceType sourceType, float baseDamage, IBallModule module)
        {
            SourceType = sourceType;
            BaseDamage = baseDamage;
            Module = module;
        }
    }
}
