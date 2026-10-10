namespace ParkSort.Core
{
    public struct Rng
    {
        private uint _state;

        public Rng(uint seed)
        {
            _state = seed == 0 ? 0x9E3779B9u : seed;
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        public int Range(int maxExclusive) => maxExclusive <= 1 ? 0 : (int)(NextUInt() % (uint)maxExclusive);
        public int Range(int minInclusive, int maxExclusive) => minInclusive + Range(maxExclusive - minInclusive);
        public float Value() => (NextUInt() & 0xFFFFFF) / 16777216f;
        public bool Chance(float probability) => Value() < probability;

        public void Shuffle<T>(T[] items, int count)
        {
            for (int i = count - 1; i > 0; i--)
            {
                int j = Range(i + 1);
                T tmp = items[i];
                items[i] = items[j];
                items[j] = tmp;
            }
        }
    }
}
