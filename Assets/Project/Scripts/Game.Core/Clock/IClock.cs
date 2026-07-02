using System;

namespace Game.Core.Clock
{
    public interface IClock
    {
        float DeltaTime { get; }
        float UnscaledDeltaTime { get; }
        float GameSpeed { get; set; }
        float GameDeltaTime { get; }
        event Action<float> OnGameSpeedChanged;
        event Action OnTick;
        event Action OnFixedTick;
        ITimerHandle InvokeDelayed(Action callback, float delay, bool useUnscaledTime = false);
        ITimerHandle InvokeRepeat(Action callback, float interval, int repeatCount = -1, bool useUnscaledTime = false);
        void CancelTimer(ITimerHandle handle);
        void CancelAllTimers();
    }
}
