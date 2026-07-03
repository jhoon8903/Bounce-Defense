using System;
using Game.Core.Base;

namespace Game.Core.Mvc
{
    public abstract class BaseController : IDisposable
    {
        private bool IsInitialized { get; set; }
        private bool IsDisposed { get; set; }

        public void Initialize()
        {
            if (IsInitialized)
            {
                Verbose.W($"[{GetType().Name}] Already initialized.");
                return;
            }
            OnInitialize();
            IsInitialized = true;
        }

        public void Reset()
        {
            if (!IsInitialized) return;
            OnReset();
        }

        public void Tick(float deltaTime)
        {
            if (!IsInitialized || IsDisposed) return;
            OnTick(deltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (!IsInitialized || IsDisposed) return;
            OnFixedTick(fixedDeltaTime);
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            OnDispose();
            IsDisposed = true;
            IsInitialized = false;
        }

        protected abstract void OnInitialize();
        protected abstract void OnReset();
        protected abstract void OnTick(float deltaTime);
        protected abstract void OnFixedTick(float fixedDeltaTime);
        protected abstract void OnDispose();
    }
}
