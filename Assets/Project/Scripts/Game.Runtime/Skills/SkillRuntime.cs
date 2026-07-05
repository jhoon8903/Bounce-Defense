using System.Collections.Generic;
using Game.Combat;
using Game.Core.Observer;
using Game.Roguelike;
using Game.Runtime.Combat;
using Game.Skills;

namespace Game.Runtime.Skills
{
    // 로드아웃(데이터) → 전투(볼 로스터 + 패시브 모디파이어) 브리지. asmdef 경계를 넘어 배선하는 오케스트레이터.
    //  - PlayerLoadout 변경 구독 → 볼 로스터 재구성(노멀 5 + 획득 액티브당 1) → BallController.SetRoster.
    //  - 패시브 IDamageModifier 재등록(ModifierRegistry.Clear 후 소유 패시브만).
    // 코어 루프·리졸버·볼컨트롤러는 이 배선을 모른 채 동작(개방-폐쇄) — 스킬 효과는 로스터/레지스트리로만 주입.
    public sealed class SkillRuntime : IObserver
    {
        private const int NormalBallCount = 5; // 기본 노멀 볼(스펙 정정: 기본 5 + 카드당 +1).

        private readonly PlayerLoadout _loadout;
        private readonly BallController _balls;
        private readonly BallConfig _ballConfig;
        private readonly ModifierRegistry _modifiers;

        private readonly List<BallSpawnSpec> _roster = new();
        private readonly List<KeyValuePair<SkillDefinition, int>> _buffer = new();

        public SkillRuntime(PlayerLoadout loadout, BallController balls, BallConfig ballConfig, ModifierRegistry modifiers)
        {
            _loadout = loadout;
            _balls = balls;
            _ballConfig = ballConfig;
            _modifiers = modifiers;
        }

        public void Initialize()
        {
            _loadout.AddObserver(this);
            Rebuild(); // 초기(스킬 0) = 노멀 5
        }

        public void Dispose() => _loadout.RemoveObserver(this);

        // 로드아웃이 바뀔 때마다(카드 획득/업그레이드) 로스터·모디파이어 재구성.
        public void OnChanged(IObservable observable) => Rebuild();

        private void Rebuild()
        {
            RebuildRoster();
            RebuildModifiers();
        }

        private void RebuildRoster()
        {
            _roster.Clear();
            float normalDmg = _ballConfig != null ? _ballConfig.GetDamage(1) : 0f;
            BallSpawnSpec normal = new BallSpawnSpec(BallSourceType.Normal, normalDmg, null);
            for (int i = 0; i < NormalBallCount; i++) _roster.Add(normal);

            _loadout.CopyOwned(SkillCategory.Active, _buffer);
            for (int i = 0; i < _buffer.Count; i++)
            {
                SkillDefinition skill = _buffer[i].Key;
                int level = _buffer[i].Value;
                BallSourceType type = SkillModuleFactory.BallTypeOf(skill.EffectKind);
                float dmg = skill.HasBallDamage ? skill.GetBallDamage(level) : normalDmg;
                IBallModule module = SkillModuleFactory.CreateBallModule(skill.EffectKind, level);
                _roster.Add(new BallSpawnSpec(type, dmg, module));
            }
            _balls.SetRoster(_roster);
        }

        private void RebuildModifiers()
        {
            _modifiers.Clear();
            _loadout.CopyOwned(SkillCategory.Passive, _buffer);
            for (int i = 0; i < _buffer.Count; i++)
            {
                IDamageModifier mod = SkillModuleFactory.CreatePassiveModifier(_buffer[i].Key.EffectKind, _buffer[i].Value);
                if (mod != null) _modifiers.Register(mod);
            }
        }
    }
}
