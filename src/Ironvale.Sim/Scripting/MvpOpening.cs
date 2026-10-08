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
        ("house", new Cell(26, 30)),
        ("house", new Cell(26, 33)),
        ("house", new Cell(34, 30)),
        ("woodcutter", new Cell(24, 25)),
        ("woodcutter", new Cell(27, 25)),
        ("field", new Cell(35, 35)),
        ("field", new Cell(39, 35)),
        ("granary", new Cell(30, 35)),
    };

    public static void Apply(World w, long foodThreshold = 1500, long firewoodThreshold = 300, long woodThreshold = 40)
    {
        foreach (var (def, origin) in Layout) w.Enqueue(new PlaceBuilding(def, origin, 0));

        var hall = w.SeatBuilding ?? throw new InvalidOperationException("scenario has no seat building");
        foreach (var h in w.Households.Take(2)) w.Enqueue(new AssignHousehold(h.Id, hall.Id));

        w.Enqueue(new CreatePolicy("keep_above", "food", foodThreshold));
        w.Enqueue(new CreatePolicy("keep_above", "firewood", firewoodThreshold));
        w.Enqueue(new CreatePolicy("keep_above", "wood", woodThreshold));
    }
}
