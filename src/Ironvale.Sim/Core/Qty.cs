using System.Globalization;

namespace Ironvale.Sim.Core;

/// <summary>
/// Fixed-point resource quantity: 1 unit = 1000 milli. The simulation state never stores floats,
/// so results are bit-identical across machines and NaN cannot exist.
/// </summary>
public readonly record struct Qty(long Milli) : IComparable<Qty>
{
    public const long Scale = 1000;
    public static readonly Qty Zero = new(0);

    public static Qty Units(long units) => new(units * Scale);

    /// <summary>Only for parsing content files; never call from simulation logic.</summary>
    public static Qty FromDouble(double value) =>
        new((long)Math.Round(value * Scale, MidpointRounding.AwayFromZero));

    public double AsDouble => Milli / (double)Scale;
    public long WholeUnits => Milli / Scale;
    public bool IsZero => Milli == 0;
    public bool IsPositive => Milli > 0;
    public bool IsNegative => Milli < 0;

    /// <summary>Multiplies by a permille factor (1000 = 1.0), truncating toward zero.</summary>
    public Qty MulPermille(int permille) => new(Milli * permille / Permille.One);

    public static Qty operator +(Qty a, Qty b) => new(a.Milli + b.Milli);
    public static Qty operator -(Qty a, Qty b) => new(a.Milli - b.Milli);
    public static Qty operator *(Qty a, long k) => new(a.Milli * k);
    public static bool operator <(Qty a, Qty b) => a.Milli < b.Milli;
    public static bool operator >(Qty a, Qty b) => a.Milli > b.Milli;
    public static bool operator <=(Qty a, Qty b) => a.Milli <= b.Milli;
    public static bool operator >=(Qty a, Qty b) => a.Milli >= b.Milli;

    public static Qty Min(Qty a, Qty b) => a.Milli <= b.Milli ? a : b;
    public static Qty Max(Qty a, Qty b) => a.Milli >= b.Milli ? a : b;

    public int CompareTo(Qty other) => Milli.CompareTo(other.Milli);

    public override string ToString() => AsDouble.ToString("0.###", CultureInfo.InvariantCulture);
}

/// <summary>Integer factors in thousandths (1000 = 100%).</summary>
public static class Permille
{
    public const int One = 1000;

    public static int Mul(int a, int b) => (int)((long)a * b / One);

    public static int Clamp(int value) => Math.Clamp(value, 0, One);
}
