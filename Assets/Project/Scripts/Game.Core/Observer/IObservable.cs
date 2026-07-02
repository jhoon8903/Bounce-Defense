namespace Game.Core.Observer
{
    public interface IObservable : System.IDisposable
    {
        bool IsChanged { get; }
        int ObserverCount { get; }
        void Raise();
        void AddObserver(IObserver observer);
        void RemoveObserver(IObserver observer);
        void Commit();
        void NotifyWithCommit();
    }
}
