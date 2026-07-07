using Game.Combat;

namespace Game.Runtime.Combat
{
    public readonly struct BallSpawnSpec
    {
        public readonly BallSourceType SourceType;
        public readonly float BaseDamage;
        public readonly IBallModule Module;
        public readonly bool PenetratesEnemies;
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
