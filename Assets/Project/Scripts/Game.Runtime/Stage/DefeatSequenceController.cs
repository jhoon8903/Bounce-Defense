using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Runtime.Combat;

namespace Game.Runtime.Stage
{
    // 실패 시퀀스 오케스트레이터(스펙 #4). 베이스 HP 0 → StageController.OnBaseDefeated 수신 →
    // 게임 정지(GameSpeed=0) + 캐릭터 분리 연출 재생 → 연출 끝나면 StageController.CompleteDefeat() →
    // StateChanged(Lost) 발화 → (Daniel) 실패 팝업 오픈. 뷰 미배선이면 즉시 완료(연출 스킵).
    public sealed class DefeatSequenceController : BaseController
    {
        private readonly StageController _stage;
        private readonly IClock _clock;
        private readonly CharDeathView _charDeath; // 씬 컴포넌트(nullable)

        public DefeatSequenceController(StageController stage, IClock clock, CharDeathView charDeath)
        {
            _stage = stage;
            _clock = clock;
            _charDeath = charDeath;
        }

        protected override void OnInitialize() => _stage.OnBaseDefeated += OnBaseDefeated;
        protected override void OnDispose() => _stage.OnBaseDefeated -= OnBaseDefeated;
        protected override void OnReset() { }
        protected override void OnTick(float deltaTime) { }
        protected override void OnFixedTick(float fixedDeltaTime) { }

        private void OnBaseDefeated()
        {
            _clock.GameSpeed = 0f; // 볼·적 전부 정지(캐릭터 분리만 unscaled로 재생)
            if (_charDeath != null) _charDeath.Play(_stage.CompleteDefeat);
            else _stage.CompleteDefeat();
        }
    }
}
