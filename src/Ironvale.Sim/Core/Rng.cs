namespace Ironvale.Sim.Core;

/// <summary>
/// PCG32 (O'Neill). Own implementation so results never depend on the .NET version
/// (System.Random's algorithm is an implementation detail).
/// </summary>
public sealed class Pcg32
{
    public ulong State { get; private set; }
    public ulong Inc { get; private set; }

    public Pcg32(ulong seed, ulong sequence)
    {
        State = 0;
        Inc = (sequence << 1) | 1;
        NextUInt();
        State += seed;
        NextUInt();
    }

    private Pcg32() { }

    public static Pcg32 FromState(ulong state, ulong inc) => new() { State = state, Inc = inc };

    public uint NextUInt()
    {
        ulong old = State;
        State = unchecked(old * 6364136223846793005UL + Inc);
        uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
        int rot = (int)(old >> 59);
        return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
    }

    /// <summary>Unbiased integer in [0, maxExclusive).</summary>
    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        uint bound = (uint)maxExclusive;
        uint threshold = (uint)((0x1_0000_0000UL - bound) % bound);
        while (true)
        {
            uint r = NextUInt();
            if (r >= threshold) return (int)(r % bound);
        }
    }

    /// <summary>Integer in [minInclusive, maxInclusive].</summary>
    public int Range(int minInclusive, int maxInclusive) =>
        minInclusive + NextInt(maxInclusive - minInclusive + 1);
}

public static class SplitMix64
{
    public static ulong Next(ref ulong x)
    {
        ulong z = unchecked(x += 0x9E3779B97F4A7C15UL);
        z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
        z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
        return z ^ (z >> 31);
    }
}

/// <summary>
/// One independent RNG stream per system, derived from the world seed and the stream name.
/// Adding a new stream never shifts the sequence of existing ones.
/// </summary>
public sealed class RngStreams
{
    public const string Harvest = "harvest";

    private readonly SortedDictionary<string, Pcg32> _streams = new(StringComparer.Ordinal);

    public ulong WorldSeed { get; }

    public RngStreams(ulong worldSeed) => WorldSeed = worldSeed;

    public Pcg32 Get(string name)
    {
        if (!_streams.TryGetValue(name, out var rng))
        {
            ulong x = WorldSeed ^ Fnv64.Hash(name);
            ulong seed = SplitMix64.Next(ref x);
            ulong seq = SplitMix64.Next(ref x);
            rng = new Pcg32(seed, seq);
            _streams[name] = rng;
        }
        return rng;
    }

    public IEnumerable<KeyValuePair<string, Pcg32>> All => _streams;

    internal void Restore(string name, ulong state, ulong inc) => _streams[name] = Pcg32.FromState(state, inc);
}
