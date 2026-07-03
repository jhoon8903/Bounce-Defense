using System.Collections.Generic;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Events;
using Game.Runtime.Enemy;

namespace Game.Runtime.Stage
{
    public enum StageState { Idle, Playing, Won, Lost }

    // 스테이지 흐름 진입점(개발플랜 §83). 웨이브 스폰 캐이던스 + 킬/방어선 해소 집계로 웨이브 진행.
    // 마지막 웨이브 전멸=승(§48) / 베이스 HP 0=패(§117). BaseController 미러(순수 DI, IClock 구독).
    public sealed class StageController : BaseController
    {
        private readonly EnemyController _enemies;
        private readonly CombatEventHub _hub;
        private readonly IClock _clock;
        private readonly StageDefinition _stage;
        private readonly BaseModel _base = new();

        private readonly List<EnemyDefinition> _spawnQueue = new();
        private int _spawnIndex;        // 다음 스폰할 큐 인덱스
        private int _resolvedThisWave;  // 이번 웨이브 해소된 적(킬+침범) 수
        private int _plannedThisWave;   // 이번 웨이브 총 스폰 수
        private float _spawnTimer;
        private float _spawnInterval;
        private int _waveIndex;
        private int _totalKills;
        private StageState _state = StageState.Idle;

        public BaseModel Base => _base;
        public StageState State => _state;
        public int WaveIndex => _waveIndex;         // 0-based
        public int WaveNumber => _waveIndex + 1;    // 1-based(HUD)
        public int WaveCount => _stage != null ? _stage.WaveCount : 0;
        public int TotalKills => _totalKills;
        public int AliveThisWave => _spawnIndex - _resolvedThisWave; // 현재 전장에 남은(스폰됨-해소됨) 적

        public StageController(EnemyController enemies, CombatEventHub hub, IClock clock, StageDefinition stage)
        {
            _enemies = enemies;
            _hub = hub;
            _clock = clock;
            _stage = stage;
        }

        protected override void OnInitialize()
        {
            _hub.OnKill += OnKill;
            _hub.OnBreach += OnBreach;
            _clock.OnFixedTick += OnClockFixedTick;
            StartStage();
        }

        protected override void OnDispose()
        {
            _hub.OnKill -= OnKill;
            _hub.OnBreach -= OnBreach;
            _clock.OnFixedTick -= OnClockFixedTick;
        }

        protected override void OnReset() => StartStage();
        protected override void OnTick(float deltaTime) { }

        private void OnClockFixedTick() => FixedTick(_clock.GameDeltaTime);

        // 전체 리셋(§117 재시작): 적 전부 제거 + 베이스/웨이브 초기화 후 웨이브0부터.
        public void StartStage()
        {
            _enemies.DespawnAll();
            _base.Initialize(_stage != null ? _stage.BaseHp : 300);
            _waveIndex = 0;
            _totalKills = 0;
            _state = _stage != null && _stage.WaveCount > 0 ? StageState.Playing : StageState.Won;
            if (_state == StageState.Playing) BeginWave(0);
        }

        public void Restart() => StartStage();

        private void BeginWave(int index)
        {
            WaveDefinition wave = _stage.GetWave(index);
            _spawnQueue.Clear();
            wave?.BuildSpawnQueue(_spawnQueue);
            _spawnIndex = 0;
            _resolvedThisWave = 0;
            _plannedThisWave = _spawnQueue.Count;
            _spawnInterval = wave != null ? wave.SpawnInterval : 0.8f;
            _spawnTimer = _spawnInterval; // 첫 적 즉시 스폰
            if (_plannedThisWave == 0) AdvanceWave(); // 빈 웨이브면 즉시 진행
        }

        protected override void OnFixedTick(float fixedDeltaTime)
        {
            if (_state != StageState.Playing) return;

            // 스폰 캐이던스: 큐가 남았으면 interval마다 1마리. 상단 자리 없으면 다음 틱 재시도(과밀 대기).
            if (_spawnIndex < _spawnQueue.Count)
            {
                _spawnTimer += fixedDeltaTime;
                if (_spawnTimer >= _spawnInterval)
                {
                    if (_enemies.Spawn(_spawnQueue[_spawnIndex]) != null)
                    {
                        _spawnIndex++;
                        _spawnTimer = 0f;
                    }
                    else
                    {
                        _spawnTimer = _spawnInterval; // 자리 없음 → 다음 틱 재시도
                    }
                }
            }

            // 웨이브 전멸 = 계획된 전부 스폰 + 전부 해소(킬/침범).
            if (_spawnIndex >= _plannedThisWave && _resolvedThisWave >= _plannedThisWave)
                AdvanceWave();
        }

        private void AdvanceWave()
        {
            if (_state != StageState.Playing) return;
            if (_waveIndex + 1 >= _stage.WaveCount)
            {
                _state = StageState.Won; // 마지막 웨이브 클리어 → 성공
                return;
            }
            _waveIndex++;
            // Phase 3 훅: 여기서 스폰 일시정지 + 3택 카드 드래프트(XP 레벨업과 조율). 지금은 바로 다음 웨이브.
            BeginWave(_waveIndex);
        }

        private void OnKill(EnemyKillInfo info)
        {
            if (_state != StageState.Playing) return;
            _resolvedThisWave++;
            _totalKills++;
        }

        private void OnBreach(EnemyBreachInfo info)
        {
            if (_state != StageState.Playing) return;
            _resolvedThisWave++;                 // 침범도 웨이브 해소로 집계
            _base.TakeDamage(info.BreachDamage);
            if (_base.IsDead) _state = StageState.Lost; // 베이스 0 → 실패
        }
    }
}
