using System.Collections.Generic;
using Game.Core.Clock;
using Game.Core.Mvc;
using Game.Events;
using Game.Runtime.Enemy;
using Game.Runtime.Grid;

namespace Game.Runtime.Stage
{
    public enum StageState { Idle, Playing, Won, Lost }

    public sealed class StageController : BaseController
    {
        private readonly EnemyController _enemies;
        private readonly CombatEventHub _hub;
        private readonly IClock _clock;
        private readonly StageDefinition _stage;
        private readonly BaseModel _base = new();

        private readonly List<WaveDefinition.Placement> _groupBuf = new();
        private readonly List<WaveDefinition.Placement> _pending = new();
        private WaveDefinition _wave;
        private int _resolvedThisWave;
        private int _plannedThisWave;
        private int _waveIndex;
        private int _totalKills;
        private StageState _state = StageState.Idle;
        private bool _defeatPending;

        public BaseModel Base => _base;
        public StageState State => _state;
        public event System.Action<StageState> StateChanged;
        public event System.Action OnBaseDefeated;
        public int WaveNumber => _waveIndex + 1;
        public int WaveCount => _stage != null ? _stage.WaveCount : 0;
        public int TotalKills => _totalKills;
        public int AliveThisWave => _plannedThisWave - _resolvedThisWave;

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

        private void BeginWave(int index)
        {
            _wave = _stage.GetWave(index);
            _pending.Clear();
            _resolvedThisWave = 0;
            _plannedThisWave = 0;
            if (_wave != null)
            {
                int groups = _wave.GroupCount;
                for (int g = 0; g < groups; g++)
                {
                    _wave.BuildGroup(g, _groupBuf);
                    _pending.AddRange(_groupBuf);
                }
            }
            if (_pending.Count == 0)
            {
                AdvanceWave();
                return;
            }
            TrySpawnPending();
        }

        private void TrySpawnPending()
        {
            if (_pending.Count == 0) return;
            float hpScale = _stage != null ? _stage.WaveHpScale(_waveIndex) : 1f;
            int w = 0;
            for (int i = 0; i < _pending.Count; i++)
            {
                WaveDefinition.Placement p = _pending[i];
                if (_enemies.CanSpawnAt(p.enemy, new CellCoord(p.col, p.row))
                    && _enemies.Spawn(p.enemy, new CellCoord(p.col, p.row), _plannedThisWave, hpScale) != null)
                {
                    _plannedThisWave++;
                    continue;
                }
                _pending[w++] = _pending[i];
            }
            _pending.RemoveRange(w, _pending.Count - w);
        }

        protected override void OnFixedTick(float fixedDeltaTime)
        {
            if (_state != StageState.Playing) return;

            TrySpawnPending();

            if (_pending.Count == 0 && _plannedThisWave > 0 && _resolvedThisWave >= _plannedThisWave)
                AdvanceWave();
        }

        private void AdvanceWave()
        {
            if (_state != StageState.Playing) return;
            if (_waveIndex + 1 >= _stage.WaveCount)
            {
                SetState(StageState.Won);
                return;
            }
            _waveIndex++;
            BeginWave(_waveIndex);
        }

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
            _resolvedThisWave++;
            _base.TakeDamage(breachDamage);
            if (!_base.IsDead) return;
            if (OnBaseDefeated != null)
            {
                _defeatPending = true;
                OnBaseDefeated.Invoke();
            }
            else SetState(StageState.Lost);
        }

        public void CompleteDefeat()
        {
            if (_defeatPending && _state == StageState.Playing) SetState(StageState.Lost);
        }
    }
}
