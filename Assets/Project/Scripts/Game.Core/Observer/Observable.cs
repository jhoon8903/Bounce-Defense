using System;
using System.Collections.Generic;

namespace Game.Core.Observer
{
    [Serializable]
    public abstract class Observable : IObservable
    {
        [NonSerialized] private List<IObserver> _observers;
        private int _observerCount;
        public bool IsChanged { get; private set; }
        public int ObserverCount => _observerCount;

        private List<IObserver> Observers => _observers ??= new List<IObserver>();

        public void Raise()
        {
            IsChanged = true;
            if (_observerCount == 0) return;
            bool hasNulls = false;
            int count = Observers.Count;
            for (int i = 0; i < count; i++)
            {
                IObserver observer = Observers[i];
                if (observer != null) observer.OnChanged(this);
                else hasNulls = true;
            }
            if (!hasNulls) return;
            for (int i = Observers.Count - 1; i >= 0; i--)
            {
                if (Observers[i] == null) Observers.RemoveAt(i);
            }
            _observerCount = Observers.Count;
        }

        public void AddObserver(IObserver observer)
        {
            if (observer == null) return;
            if (Observers.Contains(observer)) return;
            Observers.Add(observer);
            _observerCount = Observers.Count;
        }

        public void RemoveObserver(IObserver observer)
        {
            if (observer == null) return;
            int index = Observers.IndexOf(observer);
            if (index < 0) return;
            Observers[index] = null;
            _observerCount--;
        }

        public void Commit() => IsChanged = false;

        public void NotifyWithCommit()
        {
            Raise();
            Commit();
        }

        public void Dispose()
        {
            Observers.Clear();
            _observerCount = 0;
        }
    }
}
