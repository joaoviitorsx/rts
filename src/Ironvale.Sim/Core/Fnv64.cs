using System.Text;

namespace Ironvale.Sim.Core;

/// <summary>FNV-1a 64-bit. Stable across runs/platforms (unlike string.GetHashCode).</summary>
public static class Fnv64
{
    private const ulong Offset = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong Hash(ReadOnlySpan<byte> data, ulong hash = Offset)
    {
        foreach (byte b in data)
        {
            hash ^= b;
            hash = unchecked(hash * Prime);
        }
        return hash;
    }

    public static ulong Hash(string text) => Hash(Encoding.UTF8.GetBytes(text));

    public static string ToHex(ulong hash) => hash.ToString("x16");
}
