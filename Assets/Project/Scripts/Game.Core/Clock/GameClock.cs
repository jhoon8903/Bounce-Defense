using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Clock
{
    public sealed class GameClock : IClock, IDisposable
    {
        private const float MaxGameSpeed = 3f;
        private readonly HashSet<TimerHandle> _activeTimers = new();
        private readonly List<TimerHandle> _timersToRemove = new();
        private ClockRunner _runner;
        public event Action OnTick;
        public event Action OnFixedTick;
        public event Action<float> OnGameSpeedChanged;
        public float DeltaTime => Time.deltaTime;
        public float UnscaledDeltaTime => Time.unscaledDeltaTime;
        private float _gameSpeed = 1f;
        public float GameSpeed
        {
            get => _gameSpeed;
            set
            {
                float newSpeed = Mathf.Clamp(value, 0f, MaxGameSpeed);
                if (Mathf.Approximately(_gameSpeed, newSpeed)) return;
                _gameSpeed = newSpeed;
                OnGameSpeedChanged?.Invoke(_gameSpeed);
            }
        }

        public float GameDeltaTime => Time.deltaTime * _gameSpeed;

        public GameClock()
        {
            Application.targetFrameRate = 60;
            GameObject runnerObject = new GameObject("[GameClock]");
            _runner = runnerObject.AddComponent<ClockRunner>();
            _runner.Bind(this);
            UnityEngine.Object.DontDestroyOnLoad(runnerObject);
        }

        public void Dispose()
        {
            CancelAllTimers();
            OnTick = null;
            OnFixedTick = null;
            OnGameSpeedChanged = null;
            if (!_runner) return;
            UnityEngine.Object.Destroy(_runner.gameObject);
            _runner = null;
        }

        private void InvokeUpdate()
        {
            ProcessTimers();
            OnTick?.Invoke();
        }

        private void InvokeFixedUpdate() => OnFixedTick?.Invoke();

        private void ProcessTimers()
        {
            if (_activeTimers.Count == 0) return;
            _timersToRemove.Clear();
            foreach (TimerHandle timer in _activeTimers)
            {
                if (!timer.IsRunning)
                {
                    _timersToRemove.Add(timer);
                    continue;
                }

                timer.Tick();
                if (!timer.IsRunning) _timersToRemove.Add(timer);
            }

            for (int i = 0; i < _timersToRemove.Count; i++)
            {
                TimerHandle timer = _timersToRemove[i];
                _activeTimers.Remove(timer);
            }
        }

        public ITimerHandle InvokeDelayed(Action callback, float delay, bool useUnscaledTime = false)
        {
            if (callback == null) return null;
            TimerHandle handle = new TimerHandle(callback, delay, 1, useUnscaledTime);
            _activeTimers.Add(handle);
            return handle;
        }

        public ITimerHandle InvokeRepeat(Action callback, float interval, int repeatCount = -1, bool useUnscaledTime = false)
        {
            if (callback == null || interval <= 0f) return null;
            TimerHandle handle = new TimerHandle(callback, interval, repeatCount, useUnscaledTime);
            _activeTimers.Add(handle);
            return handle;
        }

        public void CancelTimer(ITimerHandle handle) => handle?.Cancel();

        public void CancelAllTimers()
        {
            foreach (TimerHandle timer in _activeTimers) timer.Cancel();
            _activeTimers.Clear();
        }

        private sealed class ClockRunner : MonoBehaviour
        {
            private GameClock _clock;
            public void Bind(GameClock clock) => _clock = clock;
            private void Update() => _clock?.InvokeUpdate();
            private void FixedUpdate() => _clock?.InvokeFixedUpdate();
            private void OnDestroy() => _clock = null;
        }

        private sealed class TimerHandle : ITimerHandle
        {
            private Action _callback;
            private readonly float _interval;
            private readonly int _totalCount;
            private readonly bool _useUnscaledTime;
            private float _nextTime;
            private int _currentCount;
            public bool IsRunning { get; private set; }

            public TimerHandle(Action callback, float interval, int totalCount, bool useUnscaledTime)
            {
                _callback = callback;
                _interval = interval;
                _totalCount = totalCount;
                _useUnscaledTime = useUnscaledTime;
                IsRunning = true;
                _nextTime = CurrentTime() + interval;
            }

            public void Tick()
            {
                if (!IsRunning) return;
                if (CurrentTime() < _nextTime) return;

                try
                {
                    _callback?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                _currentCount++;
                if (_totalCount != -1 && _currentCount >= _totalCount) Cancel();
                else _nextTime = CurrentTime() + _interval;
            }

            private float CurrentTime() => _useUnscaledTime ? Time.unscaledTime : Time.time;

            public void Cancel()
            {
                IsRunning = false;
                _callback = null;
            }

            public void Dispose() => Cancel();
        }
    }
}
