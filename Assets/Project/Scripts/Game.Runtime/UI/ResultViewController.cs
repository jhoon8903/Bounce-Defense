using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Runtime.Combat;
using Game.Runtime.Stage;
using Game.Skills;

namespace Game.Runtime.UI
{
    // 결과 뷰 오케스트레이터: StageController.StateChanged → 성공(ClearView)/실패(DefeatedView) 표시 + 게임 정지.
    //  - Won: 마지막 웨이브 클리어 즉시. 잔여HP% 전달.
    //  - Lost: DefeatSequenceController의 캐릭터 분리 연출이 끝난 뒤 발화(이미 GameSpeed 0) → 실패 팝업.
    // 뷰는 씬 컴포넌트(nullable) — 미배선이면 스킵(코어 루프 보존).
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
                if (_clearView != null) _clearView.Show(_stage.Base.RemainingPercent, _damageStats, _skillDatabase); // ClearView가 자식 DTResult까지 채움
            }
            else if (state == StageState.Lost)
            {
                _clock.GameSpeed = 0f; // 방어(캐릭터 분리 시퀀스가 이미 0으로 둠)
                if (_defeatedView != null) _defeatedView.Show();
            }
        }
    }
}
