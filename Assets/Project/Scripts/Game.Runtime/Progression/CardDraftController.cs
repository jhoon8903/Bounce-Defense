using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Events;
using Game.Roguelike;
using Game.Runtime.UI;

namespace Game.Runtime.Progression
{
    // 킬 기반 XP 카드 드래프트 오케스트레이터(Phase 3, 결정 B, 스펙 §3).
    //  - CombatEventHub.OnKill → LevelModel.AddKill.
    //  - LevelModel.OnLevelUp → 전투 일시정지(IClock.GameSpeed=0) → CardDrawService.Draw(3) → CardSelectView.Show.
    //  - 카드 선택 → PlayerLoadout.Apply → 대기 레벨업 있으면 연속 드래프트, 없으면 재개(GameSpeed=1).
    //  - <3 유효 카드는 있는 만큼, 0장이면 스킵(레벨업만 소비하고 재개, §261).
    // 뷰 참조를 소유(DI RegisterComponent)하되 뷰는 순수 표현 — 흐름/일시정지/규칙은 전부 여기.
    public sealed class CardDraftController : BaseController
    {
        private const int DrawCount = 3;

        private readonly LevelModel _level;
        private readonly PlayerLoadout _loadout;
        private readonly CardDrawService _draw;
        private readonly CombatEventHub _hub;
        private readonly IClock _clock;
        private readonly LevelProgressView _levelView;
        private readonly CardSelectView _cardView;

        private bool _drafting;

        public CardDraftController(LevelModel level, PlayerLoadout loadout, CardDrawService draw,
            CombatEventHub hub, IClock clock, LevelProgressView levelView, CardSelectView cardView)
        {
            _level = level;
            _loadout = loadout;
            _draw = draw;
            _hub = hub;
            _clock = clock;
            _levelView = levelView;
            _cardView = cardView;
        }

        protected override void OnInitialize()
        {
            _levelView?.Bind(_level);
            if (_cardView != null)
            {
                _cardView.BindLoadout(_loadout);
                _cardView.Hide();
            }
            _hub.OnKill += OnKill;
            _level.OnLevelUp += OnLevelUp;
        }

        protected override void OnDispose()
        {
            _hub.OnKill -= OnKill;
            _level.OnLevelUp -= OnLevelUp;
        }

        // 재시작(§264): 드래프트 강제 종료 + 스킬 0 + 레벨 1 + 재개. (현재 라이브 재시작 트리거는 미배선 — Phase 5)
        protected override void OnReset()
        {
            _drafting = false;
            _clock.GameSpeed = 1f;
            _cardView?.Hide();
            _loadout.ResetLoadout();
            _level.ResetProgression();
        }

        protected override void OnTick(float deltaTime) { }
        protected override void OnFixedTick(float fixedDeltaTime) { }

        private void OnKill() => _level.AddKill();

        private void OnLevelUp()
        {
            if (_drafting) return; // 드래프트 중이면 대기 큐가 픽 후 처리
            ProcessNextDraft();
        }

        // 대기 레벨업을 하나씩 소비하며 드래프트. 유효 카드 0이면 스킵, 더 없으면 재개.
        private void ProcessNextDraft()
        {
            while (_level.TryConsumeLevelUp())
            {
                System.Collections.Generic.List<SkillCard> cards = _draw.Draw(DrawCount);
                if (cards.Count > 0)
                {
                    _drafting = true;
                    _clock.GameSpeed = 0f; // 일시정지(볼·적 하강 모두 GameDeltaTime 기반이라 정지)
                    _cardView.Show(cards, OnCardPicked);
                    return;
                }
                // 0장 → 스킵하고 다음 대기 레벨업 처리
            }
            EndDraft();
        }

        private void OnCardPicked(SkillCard card)
        {
            _loadout.Apply(card);
            _drafting = false;
            _cardView.Hide();
            ProcessNextDraft(); // 다음 대기 레벨업 or 재개
        }

        private void EndDraft()
        {
            _drafting = false;
            _clock.GameSpeed = 1f; // 재개
            _cardView?.Hide();
        }
    }
}
