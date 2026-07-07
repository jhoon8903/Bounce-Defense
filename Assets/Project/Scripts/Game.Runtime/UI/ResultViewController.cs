using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Runtime.Combat;
using Game.Runtime.Stage;
using Game.Skills;

namespace Game.Runtime.UI
{
    public sealed class ResultViewController : BaseController
    {
        private readonly StageController _stage;
        private readonly IClock _clock;
        private readonly ClearView _clearView;
        private readonly DefeatedView _defeatedView;
        private readonly DamageStats _damageStats;
        private readonly SkillDatabase _skillDatabase;

        public ResultViewController(StageController stage, IClock clock, ClearView clearView, DefeatedView defeatedView, DamageStats damageStats, SkillDatabase skillDatabase)
        {
            _stage = stage;
            _clock = clock;
            _clearView = clearView;
            _defeatedView = defeatedView;
            _damageStats = damageStats;
            _skillDatabase = skillDatabase;
        }

        protected override void OnInitialize()
        {
            if (_clearView != null) _clearView.HideImmediate();
            if (_defeatedView != null) _defeatedView.HideImmediate();
            _stage.StateChanged += OnStateChanged;
        }

        protected override void OnDispose() => _stage.StateChanged -= OnStateChanged;
        protected override void OnReset() { }
        protected override void OnTick(float deltaTime) { }
        protected override void OnFixedTick(float fixedDeltaTime) { }

        private void OnStateChanged(StageState state)
        {
            if (state == StageState.Won)
            {
                _clock.GameSpeed = 0f;
                if (_clearView != null) _clearView.Show(_stage.Base.RemainingPercent, _damageStats, _skillDatabase);
            }
            else if (state == StageState.Lost)
            {
                _clock.GameSpeed = 0f;
                if (_defeatedView != null) _defeatedView.Show();
            }
        }
    }
}
