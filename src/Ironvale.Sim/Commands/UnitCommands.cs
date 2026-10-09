namespace Ironvale.Sim.Commands;

/// <summary>
/// Right-click order to selected units (GDD v0.3 §4.2). A group sent to gather spreads over the nearest free nodes of
/// the same kind around the clicked one; <paramref name="Queue"/> (Shift) appends instead of replacing. Units that
/// cannot do it (the ox does not chop, hunt or build) are left alone; if none can, the order is rejected with the reason.
/// </summary>
public sealed record OrderUnits(int[] UnitIds, OrderKind Kind, Cell Cell, int TargetId = 0, bool Queue = false) : SimCommand
{
    private const int GroupSpread = 6;

    internal override string? Apply(World w)
    {
        var units = UnitIds.Distinct().Select(w.GetUnit).Where(u => u is { Controllable: true }).Select(u => u!)
            .OrderBy(u => u.Id).ToList();
        if (units.Count == 0) return "nenhuma unidade selecionada";
        if (Validate(w) is { } error) return error;
        var able = units.Where(u => CanDo(w, u)).ToList();
        if (able.Count == 0) return Kind switch
        {
            OrderKind.Pickup => "o boi só arrasta toras e pedra",
            _ => "o boi só anda e arrasta cargas",
        };

        var taken = new HashSet<Cell>();
        foreach (var u in able)
        {
            var cell = Cell;
            if (Kind == OrderKind.Gather)
            {
                // First unit takes the clicked node; the others spread to the nearest free ones of the same kind.
                var kind = w.Nature!.At(Cell).Kind;
                if (taken.Contains(Cell) || w.Reserved(Cell, u))
                    cell = w.NearestNode(kind, Cell, GroupSpread, u, taken) ?? Cell;
                taken.Add(cell);
            }
            var order = new UnitOrder(Kind, cell, TargetId);
            if (Queue && (u.Order is not null || u.Queue.Count > 0)) u.Queue.Add(order);
            else
            {
                u.Queue.Clear();
                Ironvale.Sim.Systems.UnitSystem.Start(u, order);
            }
        }
        return null;
    }

    private string? Validate(World w)
    {
        switch (Kind)
        {
            case OrderKind.Move:
                if (!w.Map.InBounds(Cell) || w.Terrain is { } t && t.IsWater(Cell)) return "não dá para ir até lá";
                return null;
            case OrderKind.Gather:
                return w.Gatherable(Cell) ? null : "nada para coletar aqui (ou fora de época)";
            case OrderKind.Pickup:
                return w.GetGroundItem(TargetId) is null ? "não há nada no chão aqui" : null;
            case OrderKind.Hunt:
                return w.GetAnimal(TargetId) is { Huntable: true } ? null : "só cervos e coelhos podem ser caçados";
            case OrderKind.Scare:
                return w.GetAnimal(TargetId) is { Kind: FaunaKind.Wolf } ? null : "só lobos podem ser espantados";
            case OrderKind.Build:
                return w.GetBuilding(TargetId) is { IsActive: false } ? null : "não há obra aqui";
            case OrderKind.Deposit:
            case OrderKind.Split:
                return w.GetBuilding(TargetId) is { IsActive: true, IsStorage: true } ? null : "não é um depósito";
        }
        return null;
    }

    private bool CanDo(World w, Unit u) => u.Kind == UnitKind.Colonist || Kind switch
    {
        OrderKind.Move or OrderKind.Deposit => true,
        OrderKind.Pickup => w.GetGroundItem(TargetId) is { } item && w.CarryCapacity(u, item.Resource).IsPositive,
        _ => false,
    };
}

/// <summary>"Parar": drops the orders (the load stays in the hands).</summary>
public sealed record StopUnits(int[] UnitIds) : SimCommand
{
    internal override string? Apply(World w)
    {
        foreach (var u in UnitIds.Distinct().Select(w.GetUnit).Where(u => u is { Controllable: true }).Select(u => u!))
        {
            u.Queue.Clear();
            Ironvale.Sim.Systems.UnitSystem.Finish(u);
            u.Confused = false;
        }
        return null;
    }
}
