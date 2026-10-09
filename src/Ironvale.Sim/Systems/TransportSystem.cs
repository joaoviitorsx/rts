namespace Ironvale.Sim.Systems;

/// <summary>
/// Carriers pick up output from producers and walk it to storage, cell by cell. While carried, the
/// resource exists only as a <see cref="Shipment"/>. Planning happens on hour starts; walking every tick.
/// </summary>
public sealed class TransportSystem : ISimSystem
{
    public string Name => "transport";
    public Phase Phase => Phase.Transport;
    public Frequency Frequency => Frequency.Tick;

    public void Run(World w, in Calendar cal)
    {
        var carriers = w.Carriers.ToArray();   // carriers may be removed while iterating
        foreach (var c in carriers) StepCarrier(w, c, cal);
    }

    private static void StepCarrier(World w, Carrier c, in Calendar cal)
    {
        var bal = w.Content.Balance;
        switch (c.Phase)
        {
            case CarrierPhase.Idle:
            case CarrierPhase.Returning:
                if (c.Retiring)
                {
                    w.RemoveCarrier(c);
                    return;
                }
                if (cal.IsHourStart && TryPlan(w, c)) return;
                if (c.Phase == CarrierPhase.Returning && Move(w, c)) c.Phase = CarrierPhase.Idle;
                return;

            case CarrierPhase.ToPickup:
                if (Move(w, c))
                {
                    c.Phase = CarrierPhase.Loading;
                    c.WaitTicks = bal.CarrierLoadTicks;
                }
                return;

            case CarrierPhase.Loading:
                if (--c.WaitTicks > 0) return;
                var pickup = w.GetBuilding(c.PickupId)!;
                var dropoff = w.GetBuilding(c.DropoffId)!;
                pickup.Stock.TakeReserved(c.Resource, c.Amount);
                c.ShipmentId = w.AddShipment(c.Resource, c.Amount, pickup.Id, dropoff.Id, c.Id).Id;
                c.Phase = CarrierPhase.ToDropoff;
                c.Target = dropoff.Center;
                return;

            case CarrierPhase.ToDropoff:
                if (Move(w, c))
                {
                    c.Phase = CarrierPhase.Unloading;
                    c.WaitTicks = bal.CarrierLoadTicks;
                }
                return;

            case CarrierPhase.Unloading:
                if (--c.WaitTicks > 0) return;
                var shipment = w.GetShipment(c.ShipmentId)!;
                w.GetBuilding(c.DropoffId)!.Stock.CompleteIncoming(shipment.Resource, shipment.Amount);
                w.RemoveShipment(shipment);
                w.Telemetry.OnDelivery();
                c.ShipmentId = 0;
                c.PickupId = 0;
                c.DropoffId = 0;
                c.Resource = -1;
                c.Amount = Qty.Zero;
                if (c.Retiring)
                {
                    w.RemoveCarrier(c);
                    return;
                }
                var home = w.GetBuilding(c.BaseId);
                c.Target = home?.Center ?? c.Pos;
                c.Phase = c.Pos == c.Target ? CarrierPhase.Idle : CarrierPhase.Returning;
                return;
        }
    }

    /// <summary>
    /// One tick of walking along the pathfinder's route: the next cell is chosen when a step starts, and entering
    /// it takes its terrain cost (road faster than open ground).
    /// </summary>
    /// <returns>true when arrived at <see cref="Carrier.Target"/>.</returns>
    private static bool Move(World w, Carrier c)
    {
        if (c.Pos == c.Target)
        {
            c.NextCell = c.Pos;
            return true;
        }
        if (c.StepTicks == 0) c.NextCell = w.Paths.NextStep(c.Pos, c.Target);
        c.StepTicks++;
        if (c.StepTicks >= w.StepTicksInto(c.NextCell, c.Target))
        {
            c.Pos = c.NextCell;
            c.StepTicks = 0;
        }
        return c.Pos == c.Target;
    }

    /// <summary>
    /// Picks what to haul. Priority: (1) producer output of resources a policy says are short (urgent),
    /// (2) materials for construction sites (oldest site first), (3) the fullest producer buffer (a full buffer
    /// stops production), then bigger load, nearest, lowest id/resource.
    /// </summary>
    private static bool TryPlan(World w, Carrier c)
    {
        var bal = w.Content.Balance;
        Span<bool> urgent = stackalloc bool[w.Content.ResourceCount];
        foreach (var p in w.Policies)
            if (p.Enabled && w.StorageStockIncludingTransit(p.Resource) < p.Min) urgent[p.Resource] = true;

        Building? best = null;
        int bestRes = -1;
        Qty bestAmount = Qty.Zero;
        (int urgent, long fill, long amount, int negDist) bestKey = default;

        foreach (var b in w.Buildings)
        {
            if (!b.IsActive || !b.IsProducer) continue;
            int dist = c.Pos.Manhattan(b.Center);
            long cap = Math.Max(1, b.Stock.Capacity.Milli);
            long fill = b.Stock.Total.Milli * Permille.One / cap;
            for (int r = 0; r < w.Content.ResourceCount; r++)
            {
                var free = b.Stock.Free(r);
                if (free < bal.MinPickup || !free.IsPositive) continue;
                var amount = Qty.Min(free, w.Content.Resources[r].CarryPerTrip);
                var key = (urgent[r] ? 1 : 0, fill, amount.Milli, -dist);
                if (best is null || key.CompareTo(bestKey) > 0)
                {
                    best = b;
                    bestRes = r;
                    bestAmount = amount;
                    bestKey = key;
                }
            }
        }
        bool urgentJob = best is not null && bestKey.urgent == 1;
        if (!urgentJob && TryPlanSiteDelivery(w, c)) return true;
        if (best is null) return false;

        var home = w.GetBuilding(c.BaseId);
        Building? dropoff = home is { IsActive: true } && home.Stock.Space >= bestAmount
            ? home
            : w.StoragesByDistance(best.Center).FirstOrDefault(s => s.Stock.Space >= bestAmount);
        if (dropoff is null) return false;

        best.Stock.TryReserve(bestRes, bestAmount);
        dropoff.Stock.TryReserveIncoming(bestAmount);
        c.PickupId = best.Id;
        c.DropoffId = dropoff.Id;
        c.Resource = bestRes;
        c.Amount = bestAmount;
        c.Target = best.Center;
        c.StepTicks = 0;
        c.Phase = CarrierPhase.ToPickup;
        return true;
    }

    /// <summary>Storage → construction site: the oldest site still missing a material, from the nearest storage.</summary>
    private static bool TryPlanSiteDelivery(World w, Carrier c)
    {
        foreach (var site in w.Buildings)
        {
            if (site.IsActive) continue;
            for (int r = 0; r < w.Content.ResourceCount; r++)
            {
                var need = w.SiteNeed(site, r);
                if (!need.IsPositive) continue;
                foreach (var storage in w.StoragesByDistance(site.Center))
                {
                    var free = storage.Stock.Free(r);
                    if (!free.IsPositive) continue;
                    var amount = Qty.Min(Qty.Min(need, free), w.Content.Resources[r].CarryPerTrip);
                    if (!site.Stock.TryReserveIncoming(amount)) break;
                    storage.Stock.TryReserve(r, amount);
                    c.PickupId = storage.Id;
                    c.DropoffId = site.Id;
                    c.Resource = r;
                    c.Amount = amount;
                    c.Target = storage.Center;
                    c.StepTicks = 0;
                    c.Phase = CarrierPhase.ToPickup;
                    return true;
                }
            }
        }
        return false;
    }
}
