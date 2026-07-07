using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Events;
using Game.Roguelike;
using Game.Runtime.UI;

namespace Game.Runtime.Progression
{
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
            if (_drafting) return;
            ProcessNextDraft();
        }

        private void ProcessNextDraft()
        {
            while (_level.TryConsumeLevelUp())
            {
                System.Collections.Generic.List<SkillCard> cards = _draw.Draw(DrawCount);
                if (cards.Count <= 0) continue;
                _drafting = true;
                _clock.GameSpeed = 0f;
                _cardView.Show(cards, OnCardPicked);
                return;
            }
            EndDraft();
        }

        private void OnCardPicked(SkillCard card)
        {
            _loadout.Apply(card);
            _drafting = false;
            _cardView.Hide();
            ProcessNextDraft();
        }

        private void EndDraft()
        {
            _drafting = false;
            _clock.GameSpeed = 1f;
            _cardView?.Hide();
        }
    }
}
