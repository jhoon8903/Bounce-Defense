namespace Game.Core.Observer
{
    public interface IObserver
    {
        void OnChanged(IObservable observable);
    }
}
