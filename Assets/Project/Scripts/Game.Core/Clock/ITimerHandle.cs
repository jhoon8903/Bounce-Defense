using System;

namespace Game.Core.Clock
{
    public interface ITimerHandle : IDisposable
    {
        bool IsRunning { get; }
        void Cancel();
    }
}
