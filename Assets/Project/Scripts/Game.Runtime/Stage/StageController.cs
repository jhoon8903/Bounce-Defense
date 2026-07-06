using System.Collections.Generic;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Events;
using Game.Runtime.Enemy;
using Game.Runtime.Grid;

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

        private readonly List<WaveDefinition.Placement> _groupBuf = new(); // 그룹 빌드 임시 버퍼
        private readonly List<WaveDefinition.Placement> _pending = new();  // 아직 스폰 안 된 배치(그룹 순). 빈 셀마다 롤링 스폰.
        private WaveDefinition _wave;   // 현재 웨이브
        private int _resolvedThisWave;  // 이번 웨이브 해소된 적(킬+침범) 수
        private int _plannedThisWave;   // 이번 웨이브 실제 스폰 수(누적)
        private int _waveIndex;
        private int _totalKills;
        private StageState _state = StageState.Idle;
        private bool _defeatPending;     // 실패 연출 진행 중(중복 발화·추가 침범 차단)

        public BaseModel Base => _base;
        public StageState State => _state;
        public event System.Action<StageState> StateChanged; // 승/패 전이 시 결과 팝업이 구독
        public event System.Action OnBaseDefeated; // 베이스 HP 0 → 캐릭터 분리 연출 게이트(끝나면 CompleteDefeat)
        public int WaveNumber => _waveIndex + 1;    // 1-based(HUD)
        public int WaveCount => _stage != null ? _stage.WaveCount : 0;
        public int TotalKills => _totalKills;
        public int AliveThisWave => _plannedThisWave - _resolvedThisWave; // 현재 전장에 남은(스폰됨-해소됨) 적

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
            _defeatPending = false;
            SetState(_stage != null && _stage.WaveCount > 0 ? StageState.Playing : StageState.Won);
            if (_state == StageState.Playing) BeginWave(0);
        }

        // 웨이브 = 서브그룹(1-1,1-2…) 배치도. 전체 배치를 group 순으로 대기열에 쌓고, 빈 셀마다 롤링 스폰한다.
        //  → group0은 시작 시 전부(빈 격자) 스폰, group1~은 각 셀이 비는 즉시(하강/처치) 채워져 연속 스트림.
        private void BeginWave(int index)
        {
            _wave = _stage.GetWave(index);
            _pending.Clear();
            _resolvedThisWave = 0;
            _plannedThisWave = 0;
            if (_wave != null)
            {
                int groups = _wave.GroupCount;
                for (int g = 0; g < groups; g++) { _wave.BuildGroup(g, _groupBuf); _pending.AddRange(_groupBuf); } // group 순 대기열
            }
            if (_pending.Count == 0) { AdvanceWave(); return; } // 빈 웨이브면 즉시 진행
            TrySpawnPending(); // 시작 스폰(빈 격자라 group0 전량 즉시)
        }

        // 대기열을 훑어 '지금 배치 가능한(셀이 빈)' 배치만 스폰. group 순 대기라 같은 셀은 앞 그룹이 먼저,
        // 그 적이 하강/처치로 셀을 비우면 뒤 그룹이 채운다 → "비는 데로" 연속 스폰.
        private void TrySpawnPending()
        {
            if (_pending.Count == 0) return;
            float hpScale = _stage != null ? _stage.WaveHpScale(_waveIndex) : 1f; // 웨이브 진행할수록 적 HP↑
            int w = 0;
            for (int i = 0; i < _pending.Count; i++)
            {
                WaveDefinition.Placement p = _pending[i];
                if (_enemies.CanSpawnAt(p.enemy, new CellCoord(p.col, p.row))
                    && _enemies.Spawn(p.enemy, new CellCoord(p.col, p.row), _plannedThisWave, hpScale) != null)
                {
                    _plannedThisWave++; // 스폰 성공 → 대기열에서 제외
                    continue;
                }
                _pending[w++] = _pending[i]; // 아직 못 스폰 → 유지(앞으로 압축)
            }
            _pending.RemoveRange(w, _pending.Count - w);
        }

        protected override void OnFixedTick(float fixedDeltaTime)
        {
            if (_state != StageState.Playing) return;

            TrySpawnPending(); // 빈 셀마다 롤링 스폰(연속 스트림)

            // 웨이브 클리어 = 대기열 소진 + 스폰된 전부 해소(킬/침범).
            if (_pending.Count == 0 && _plannedThisWave > 0 && _resolvedThisWave >= _plannedThisWave)
                AdvanceWave();
        }

        private void AdvanceWave()
        {
            if (_state != StageState.Playing) return;
            if (_waveIndex + 1 >= _stage.WaveCount)
            {
                SetState(StageState.Won); // 마지막 웨이브 클리어 → 성공
                return;
            }
            _waveIndex++;
            // Phase 3 훅: 여기서 스폰 일시정지 + 3택 카드 드래프트(XP 레벨업과 조율). 지금은 바로 다음 웨이브.
            BeginWave(_waveIndex);
        }

        // 상태 전이 단일 지점 — 변경 시에만 이벤트 발화(결과 팝업 트리거).
        private void SetState(StageState next)
        {
            if (_state == next) return;
            _state = next;
            StateChanged?.Invoke(next);
        }

        private void OnKill()
        {
            if (_state != StageState.Playing) return;
            _resolvedThisWave++;
            _totalKills++;
        }

        private void OnBreach(int breachDamage)
        {
            if (_state != StageState.Playing || _defeatPending) return;
            _resolvedThisWave++;                 // 침범도 웨이브 해소로 집계
            _base.TakeDamage(breachDamage);
            if (!_base.IsDead) return;
            // 베이스 0 → 실패. 캐릭터 분리 시퀀스가 배선됐으면 연출 후 CompleteDefeat, 아니면 즉시 Lost.
            if (OnBaseDefeated != null) { _defeatPending = true; OnBaseDefeated.Invoke(); }
            else SetState(StageState.Lost);
        }

        // 실패 시퀀스(캐릭터 분리) 완료 콜백 → 최종 Lost 전이(팝업 오픈).
        public void CompleteDefeat()
        {
            if (_defeatPending && _state == StageState.Playing) SetState(StageState.Lost);
        }
    }
}
