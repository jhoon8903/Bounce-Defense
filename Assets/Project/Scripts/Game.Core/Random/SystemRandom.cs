using System.Collections.Generic;

namespace Game.Core.Random
{
    // 시드 결정론 RNG(플랜 §265). EditMode 테스트·재현 영상용. 크리/냉동/클러스터/셔플 단일 소스.
    public sealed class SystemRandom : IRandom
    {
        private readonly System.Random _random;

        public int Seed { get; }

        public SystemRandom(int seed)
        {
            Seed = seed;
            _random = new System.Random(seed);
        }

        public float NextFloat() => (float)_random.NextDouble();

        public int NextInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

        public bool NextBool(float probability)
        {
            if (probability <= 0f) return false;
            if (probability >= 1f) return true;
            return NextFloat() < probability;
        }

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = NextInt(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
