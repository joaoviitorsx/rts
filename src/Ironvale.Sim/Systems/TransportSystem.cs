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
                if (c.Phase == CarrierPhase.Returning && Move(c, bal)) c.Phase = CarrierPhase.Idle;
                return;

            case CarrierPhase.ToPickup:
                if (Move(c, bal))
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
                if (Move(c, bal))
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

    /// <returns>true when arrived at <see cref="Carrier.Target"/>.</returns>
    private static bool Move(Carrier c, BalanceDef bal)
    {
        if (c.Pos == c.Target) return true;
        c.StepTicks++;
        if (c.StepTicks >= bal.CarrierTicksPerCell)
        {
            c.Pos = c.NextCell;
            c.StepTicks = 0;
        }
        return c.Pos == c.Target;
    }

    /// <summary>Picks the producer with most waiting output (ties: nearest, then lowest id/resource).</summary>
    private static bool TryPlan(World w, Carrier c)
    {
        var bal = w.Content.Balance;
        Building? best = null;
        int bestRes = -1;
        Qty bestAmount = Qty.Zero;
        int bestDist = int.MaxValue;

        foreach (var b in w.Buildings)
        {
            if (!b.IsActive || !b.IsProducer) continue;
            int dist = c.Pos.Manhattan(b.Center);
            for (int r = 0; r < w.Content.ResourceCount; r++)
            {
                var free = b.Stock.Free(r);
                if (free < bal.MinPickup || !free.IsPositive) continue;
                var amount = Qty.Min(free, w.Content.Resources[r].CarryPerTrip);
                if (amount > bestAmount || (amount == bestAmount && dist < bestDist))
                {
                    best = b;
                    bestRes = r;
                    bestAmount = amount;
                    bestDist = dist;
                }
            }
        }
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
}
