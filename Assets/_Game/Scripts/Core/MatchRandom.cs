public sealed class MatchRandom
{
    private uint state;

    public MatchRandom(int seed)
    {
        Seed = seed;
        state = unchecked((uint)seed);
    }

    public int Seed { get; }

    public uint NextUInt()
    {
        unchecked
        {
            state += 0x6D2B79F5u;
            uint z = state;
            z = (z ^ (z >> 15)) * (z | 1u);
            z ^= z + (z ^ (z >> 7)) * (z | 61u);
            return z ^ (z >> 14);
        }
    }

    public float Value()
    {
        return (NextUInt() >> 8) * (1f / 16777216f);
    }

    public float Range(float min, float max)
    {
        return min + (max - min) * Value();
    }

    public float Sign()
    {
        return (NextUInt() & 0x80000000u) == 0u ? -1f : 1f;
    }
}
