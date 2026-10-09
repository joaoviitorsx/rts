namespace Ironvale.Sim.Map;

/// <summary>
/// Integer value noise in 16.16 fixed point (world generation). No floating point anywhere, so a seed gives the same
/// map on every machine and .NET version. Values are 0..<see cref="One"/>−1.
/// </summary>
public static class Noise
{
    public const int Shift = 16;
    public const int One = 1 << Shift;

    /// <summary>Lattice value 0..65535 for an integer point.</summary>
    public static int Lattice(int x, int y, uint seed)
    {
        unchecked
        {
            uint h = seed ^ ((uint)x * 0x8DA6B343u) ^ ((uint)y * 0xD8163841u);
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            h ^= h >> 15;
            return (int)(h & 0xFFFF);
        }
    }

    /// <summary>Hash of a cell as a 0..65535 value (independent per <paramref name="seed"/>): per-cell dice rolls.</summary>
    public static int Roll(int x, int y, uint seed) => Lattice(x, y, seed ^ 0xA511E9B3u);

    /// <summary>Smoothstep 3t² − 2t³ in fixed point.</summary>
    private static long Fade(long t)
    {
        long t2 = t * t >> Shift;
        return t2 * (3L * One - 2 * t) >> Shift;
    }

    /// <summary>Smooth value noise at a fixed-point position (cells × 65536).</summary>
    public static int Value(long fx, long fy, uint seed)
    {
        int ix = (int)(fx >> Shift), iy = (int)(fy >> Shift);
        long sx = Fade(fx & (One - 1)), sy = Fade(fy & (One - 1));
        long a = Lattice(ix, iy, seed), b = Lattice(ix + 1, iy, seed);
        long c = Lattice(ix, iy + 1, seed), d = Lattice(ix + 1, iy + 1, seed);
        long ab = a + ((b - a) * sx >> Shift);
        long cd = c + ((d - c) * sx >> Shift);
        return (int)(ab + ((cd - ab) * sy >> Shift));
    }

    /// <summary>
    /// Fractal sum: <paramref name="octaves"/> layers, the first with features <paramref name="period"/> cells wide,
    /// each next one half the size and half the weight. Position in fixed point (cells × 65536).
    /// </summary>
    public static int Fbm(long fx, long fy, int period, int octaves, uint seed)
    {
        long sum = 0, total = 0, amp = One;
        for (int o = 0; o < octaves; o++)
        {
            int p = Math.Max(1, period >> o);
            sum += Value(fx / p, fy / p, seed + (uint)o * 0x9E3779B9u) * amp;
            total += amp;
            amp >>= 1;
        }
        return (int)(sum / total);
    }

    public static int Fbm(int x, int y, int period, int octaves, uint seed) =>
        Fbm((long)x << Shift, (long)y << Shift, period, octaves, seed);
}
