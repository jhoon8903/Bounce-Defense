using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Runtime.Combat;

namespace Game.Runtime.Stage
{
    public sealed class DefeatSequenceController : BaseController
    {
        private readonly StageController _stage;
        private readonly IClock _clock;
        private readonly CharDeathView _charDeath;

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
            _clock.GameSpeed = 0f;
            if (_charDeath != null) _charDeath.Play(_stage.CompleteDefeat);
            else _stage.CompleteDefeat();
        }
    }
}
