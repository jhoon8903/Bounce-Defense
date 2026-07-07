using System.Collections.Generic;
using Game.Combat;
using Game.Core.Observer;
using Game.Roguelike;
using Game.Runtime.Combat;
using Game.Runtime.Enemy;
using Game.Skills;

namespace Game.Runtime.Skills
{
    public sealed class SkillRuntime : IObserver
    {
        private const int NormalBallCount = 5;
        private const float NormalBallDamage = 8f;

        private readonly PlayerLoadout _loadout;
        private readonly BallController _balls;
        private readonly ModifierRegistry _modifiers;
        private readonly EnemyController _enemies;

        private readonly List<BallSpawnSpec> _roster = new();
        private readonly List<KeyValuePair<SkillDefinition, int>> _buffer = new();

        public SkillRuntime(PlayerLoadout loadout, BallController balls, ModifierRegistry modifiers, EnemyController enemies)
        {
            _loadout = loadout;
            _balls = balls;
            _modifiers = modifiers;
            _enemies = enemies;
        }

        public void Initialize()
        {
            _loadout.AddObserver(this);
            Rebuild();
        }

        public void Dispose() => _loadout.RemoveObserver(this);

        public void OnChanged(IObservable observable) => Rebuild();

        private void Rebuild()
        {
            RebuildRoster();
            RebuildModifiers();
            RebuildPassiveControllers();
        }

        private void RebuildPassiveControllers()
        {
            float mirrorPercent = 0f;
            float lastMatchDamage = 0f, lastMatchRadius = 0f;
            _loadout.CopyOwned(SkillCategory.Passive, _buffer);
            for (int i = 0; i < _buffer.Count; i++)
            {
                SkillDefinition s = _buffer[i].Key;
                int lv = _buffer[i].Value;
                if (s.EffectKind == SkillEffectKind.MagicMirror) mirrorPercent = s.GetMirrorBonus(lv);
                else if (s.EffectKind == SkillEffectKind.LastMatch)
                {
                    lastMatchDamage = s.GetLastMatchDamage(lv);
                    lastMatchRadius = s.GetLastMatchRadius(lv);
                }
            }
            _balls.SetMirrorPercent(mirrorPercent);
            _enemies?.SetLastMatch(lastMatchDamage, lastMatchRadius);
        }

        private void RebuildRoster()
        {
            _roster.Clear();
            const float normalDmg = NormalBallDamage;
            BallSpawnSpec normal = new BallSpawnSpec(BallSourceType.Normal, normalDmg, null);
            for (int i = 0; i < NormalBallCount; i++)
            {
                _roster.Add(normal);
            }

            _loadout.CopyOwned(SkillCategory.Active, _buffer);
            for (int i = 0; i < _buffer.Count; i++)
            {
                SkillDefinition skill = _buffer[i].Key;
                int level = _buffer[i].Value;
                BallSourceType type = SkillModuleFactory.BallTypeOf(skill.EffectKind);
                float dmg = skill.HasBallDamage ? skill.GetBallDamage(level) : normalDmg;
                IBallModule module = SkillModuleFactory.CreateBallModule(skill, level);
                bool penetrates = SkillModuleFactory.PenetratesEnemies(skill.EffectKind);
                _roster.Add(new BallSpawnSpec(type, dmg, module, penetrates));
            }
            _balls.SetRoster(_roster);
        }

        private void RebuildModifiers()
        {
            _modifiers.Clear();
            RegisterModifiersOf(SkillCategory.Passive);
            RegisterModifiersOf(SkillCategory.Active);
        }

        private void RegisterModifiersOf(SkillCategory category)
        {
            _loadout.CopyOwned(category, _buffer);
            for (int i = 0; i < _buffer.Count; i++)
            {
                IDamageModifier mod = SkillModuleFactory.CreatePassiveModifier(_buffer[i].Key, _buffer[i].Value);
                if (mod != null) _modifiers.Register(mod);
            }
        }
    }
}
