namespace Game.Core.Pool
{
    public interface IPoolInternal
    {
        int ActiveCount { get; }
        int TotalCount { get; }
        void ReleaseAll();
        void Clear();
    }
}
