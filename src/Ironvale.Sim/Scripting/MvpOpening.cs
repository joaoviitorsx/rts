namespace Ironvale.Sim.Scripting;

/// <summary>
/// A scripted "player" for the mvp_start scenario: the opening a person would do in the first minutes
/// (houses, woodcutters, fields, granary, two carriers, three policies). Used by the soak test and the CLI
/// so long runs exercise the whole economy without a human.
/// </summary>
public static class MvpOpening
{
    public static readonly (string Def, Cell Origin)[] Layout =
    {
        // Food first (the cart lasts ~40 days and fields only yield at harvest), then wood, then shelter.
        ("field", new Cell(35, 35)),
        ("field", new Cell(39, 35)),
        ("woodcutter", new Cell(24, 25)),
        ("woodcutter", new Cell(27, 25)),
        ("house", new Cell(26, 30)),
        ("house", new Cell(26, 33)),
        ("house", new Cell(34, 30)),
        ("granary", new Cell(30, 35)),
    };

    /// <summary>Roads of the optimal opening: west of the hall (houses, woodcutters, granary) and east to the fields.</summary>
    public static readonly Cell[] Roads =
        Enumerable.Range(27, 10).Select(y => new Cell(29, y))
            .Concat(Enumerable.Range(30, 9).Select(x => new Cell(x, 33))).ToArray();

    public static void Apply(World w, long foodThreshold = 1500, long firewoodThreshold = 300, long woodThreshold = 40,
        bool roads = false)
    {
        foreach (var (def, origin) in Layout) Placement.Place(w, def, origin);   // flat map: the layout as is
        if (roads)
        {
            if (w.Terrain is null) w.Enqueue(new PlaceRoad(Roads));
            else Placement.RoadsFromHall(w, Roads.Length);
        }

        var hall = w.SeatBuilding ?? throw new InvalidOperationException("scenario has no seat building");
        foreach (var h in w.Households.Take(2)) w.Enqueue(new AssignHousehold(h.Id, hall.Id));

        // Band = the old fixed 25 % hysteresis, so long runs stay comparable.
        w.Enqueue(new CreatePolicy("keep_above", "food", foodThreshold, foodThreshold * 5 / 4));
        w.Enqueue(new CreatePolicy("keep_above", "firewood", firewoodThreshold, firewoodThreshold * 5 / 4));
        w.Enqueue(new CreatePolicy("keep_above", "wood", woodThreshold, woodThreshold * 5 / 4));
    }
}
