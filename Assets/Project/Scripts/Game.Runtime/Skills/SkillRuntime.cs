using System.Collections.Generic;
using Game.Combat;
using Game.Core.Observer;
using Game.Roguelike;
using Game.Runtime.Combat;
using Game.Runtime.Enemy;
using Game.Skills;

namespace Game.Runtime.Skills
{
    // 로드아웃(데이터) → 전투(볼 로스터 + 패시브 모디파이어) 브리지. asmdef 경계를 넘어 배선하는 오케스트레이터.
    //  - PlayerLoadout 변경 구독 → 볼 로스터 재구성(노멀 5 + 획득 액티브당 1) → BallController.SetRoster.
    //  - 패시브 IDamageModifier 재등록(ModifierRegistry.Clear 후 소유 패시브만).
    // 코어 루프·리졸버·볼컨트롤러는 이 배선을 모른 채 동작(개방-폐쇄) — 스킬 효과는 로스터/레지스트리로만 주입.
    public sealed class SkillRuntime : IObserver
    {
        private const int NormalBallCount = 5;   // 기본 노멀 볼(스펙 정정: 기본 5 + 카드당 +1).
        private const float NormalBallDamage = 8f; // 노멀 볼 기본 뎀(§535). 단일 출처 — 액티브볼 뎀은 SkillDefinition.

        private readonly PlayerLoadout _loadout;
        private readonly BallController _balls;
        private readonly ModifierRegistry _modifiers;
        private readonly EnemyController _enemies; // Last Match(반경 폭발) 파라미터 주입 대상

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
            Rebuild(); // 초기(스킬 0) = 노멀 5
        }

        public void Dispose() => _loadout.RemoveObserver(this);

        // 로드아웃이 바뀔 때마다(카드 획득/업그레이드) 로스터·모디파이어 재구성.
        public void OnChanged(IObservable observable) => Rebuild();

        private void Rebuild()
        {
            RebuildRoster();
            RebuildModifiers();
            RebuildPassiveControllers();
        }

        // 컨트롤러 내부에서 처리되는 패시브(모디파이어 아님) 값 주입: Magic Mirror%→BallController, Last Match→EnemyController.
        // 미보유면 0(비활성). self-gating이 아니라 오너가 신호(벽튕김·킬)를 직접 갖는 패시브라 여기서 푸시.
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
            const float normalDmg = NormalBallDamage; // 노멀 볼 기본 뎀(단일 상수). 액티브볼은 SkillDefinition에서.
            BallSpawnSpec normal = new BallSpawnSpec(BallSourceType.Normal, normalDmg, null);
            for (int i = 0; i < NormalBallCount; i++) _roster.Add(normal);

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
            // 패시브(WarmTin·단검) + 액티브 기여(Ice 상시 추가뎀) 모두 스캔. self-gating이라 순서 무관.
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
