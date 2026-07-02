namespace Game.Core.Pool
{
    public interface IPoolable
    {
        void OnActive();
        void OnInactive();
    }
}
