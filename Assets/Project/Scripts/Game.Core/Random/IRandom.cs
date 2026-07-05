using System.Collections.Generic;

namespace Game.Core.Random
{
    public interface IRandom
    {
        float NextFloat();
        int NextInt(int minInclusive, int maxExclusive);
        bool NextBool(float probability);
        void Shuffle<T>(IList<T> list);
    }
}
