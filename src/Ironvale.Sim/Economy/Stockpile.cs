namespace Ironvale.Sim.Economy;

/// <summary>
/// Physical stock of one building. Amounts never go negative: removals fail instead of clamping.
/// <para>Reserved = promised to a carrier for pickup. Incoming = space promised to a carrier dropoff.</para>
/// </summary>
public sealed class Stockpile
{
    private readonly long[] _amount;
    private readonly long[] _reserved;
    private long _incoming;

    public Qty Capacity { get; }

    public Stockpile(int resourceCount, Qty capacity)
    {
        _amount = new long[resourceCount];
        _reserved = new long[resourceCount];
        Capacity = capacity;
    }

    public int ResourceCount => _amount.Length;
    public Qty Get(int r) => new(_amount[r]);
    public Qty Reserved(int r) => new(_reserved[r]);
    public Qty Free(int r) => new(_amount[r] - _reserved[r]);
    public Qty Incoming => new(_incoming);

    public Qty Total
    {
        get
        {
            long sum = 0;
            foreach (long a in _amount) sum += a;
            return new Qty(sum);
        }
    }

    /// <summary>Space left once promised incoming deliveries arrive.</summary>
    public Qty Space => Qty.Max(Qty.Zero, Capacity - Total - Incoming);

    internal Qty AddUpTo(int r, Qty q)
    {
        var added = Qty.Min(q, Space);
        if (added.IsPositive) _amount[r] += added.Milli;
        return Qty.Max(added, Qty.Zero);
    }

    internal Qty RemoveUpTo(int r, Qty q)
    {
        var taken = Qty.Min(q, Free(r));
        if (taken.IsPositive) _amount[r] -= taken.Milli;
        return Qty.Max(taken, Qty.Zero);
    }

    internal bool TryRemove(int r, Qty q)
    {
        if (q.IsNegative || Free(r) < q) return false;
        _amount[r] -= q.Milli;
        return true;
    }

    internal bool TryReserve(int r, Qty q)
    {
        if (q.IsNegative || Free(r) < q) return false;
        _reserved[r] += q.Milli;
        return true;
    }

    internal void Unreserve(int r, Qty q) => _reserved[r] -= q.Milli;

    /// <summary>Removes a previously reserved amount.</summary>
    internal void TakeReserved(int r, Qty q)
    {
        _reserved[r] -= q.Milli;
        _amount[r] -= q.Milli;
    }

    internal bool TryReserveIncoming(Qty q)
    {
        if (Space < q) return false;
        _incoming += q.Milli;
        return true;
    }

    internal void CancelIncoming(Qty q) => _incoming -= q.Milli;

    /// <summary>Delivers a promised incoming amount (space was already reserved).</summary>
    internal void CompleteIncoming(int r, Qty q)
    {
        _incoming -= q.Milli;
        _amount[r] += q.Milli;
    }

    internal void Restore(long[] amount, long[] reserved, long incoming)
    {
        Array.Copy(amount, _amount, _amount.Length);
        Array.Copy(reserved, _reserved, _reserved.Length);
        _incoming = incoming;
    }

    internal long[] AmountsRaw => _amount;
    internal long[] ReservedRaw => _reserved;
}
