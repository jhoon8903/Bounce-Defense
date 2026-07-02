namespace Game.Core.Pool
{
    /// <summary>Reflection 없이 풀을 관리하기 위한 비제네릭 인터페이스.</summary>
    public interface IPoolInternal
    {
        int ActiveCount { get; }
        int TotalCount { get; }
        void ReleaseAll();
        void Clear();
    }
}
