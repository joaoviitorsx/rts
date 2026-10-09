namespace Ironvale.Sim.Tests;

/// <summary>Marco 2A.3: roads and walking routes (distance fields, terrain cost).</summary>
public class RoadTests
{
    private static Cell[] Line(int x0, int x1, int y) => Enumerable.Range(x0, x1 - x0 + 1).Select(x => new Cell(x, y)).ToArray();

    [Fact]
    public void Route_cost_matches_the_sum_of_cell_costs()
    {
        var w = TestKit.NewWorld();
        var from = new Cell(5, 5);
        var to = w.SeatBuilding!.Center;
        var route = w.Paths.Route(from, to);
        int target = w.Map.BuildingAt(to);
        Assert.Equal(to, route[^1]);
        Assert.Equal(w.Paths.Ticks(from, to), route.Sum(c => w.Paths.EnterCost(c, target)));
        Assert.Equal(from.Manhattan(to), route.Count);   // open ground: no detour
    }

    [Fact]
    public void A_road_makes_the_trip_faster_and_walkers_take_it()
    {
        var w = TestKit.NewWorld();
        var from = new Cell(4, 10);
        var to = new Cell(31, 10);
        int offroad = w.Paths.Ticks(from, to);
        w.Enqueue(new PlaceRoad(Line(4, 31, 11)));          // parallel road one row below (28 stone of 30)
        w.Step();
        Assert.Empty(w.DrainEvents().OfType<CommandRejected>());
        int withRoad = w.Paths.Ticks(from, to);
        Assert.True(withRoad < offroad, $"road {withRoad} should beat off-road {offroad}");
        Assert.True(w.Paths.Route(from, to).Count(w.Map.IsRoad) > 20, "walker should use the road");
    }

    [Fact]
    public void Walkers_go_around_other_buildings()
    {
        var w = TestKit.NewWorld();
        var wall = TestKit.AddActive(w, "granary", new Cell(20, 9));   // 3×2 across the straight line y = 10
        var route = w.Paths.Route(new Cell(10, 10), new Cell(30, 10));
        Assert.DoesNotContain(route, c => w.Map.BuildingAt(c) == wall.Id);
        Assert.Equal(new Cell(30, 10), route[^1]);
    }

    [Fact]
    public void Laying_road_costs_stone_per_cell_and_skips_occupied_cells()
    {
        var w = TestKit.NewWorld();
        int stone = TestKit.Res("stone");
        var before = w.StorageStock(stone);
        var hall = w.SeatBuilding!;
        w.Enqueue(new PlaceRoad(Line(hall.Origin.X - 3, hall.Origin.X + 1, hall.Origin.Y)));   // last 2 cells are the hall
        w.Step();
        Assert.Equal(3, w.Map.Roads.Count());
        Assert.Equal(before - w.Content.Balance.RoadStonePerCell * 3, w.StorageStock(stone));
        TestKit.AssertInvariants(w);

        w.Enqueue(new PlaceBuilding("house", new Cell(hall.Origin.X - 3, hall.Origin.Y), 0));   // on the road
        w.Step();
        Assert.Contains(w.DrainEvents().OfType<CommandRejected>(), e => e.Reason.Contains("ocupado"));
    }

    [Fact]
    public void Road_without_enough_stone_is_rejected()
    {
        var w = TestKit.NewWorld();
        w.Enqueue(new PlaceRoad(Line(0, 63, 2)));   // 64 cells > 30 stone
        w.Step();
        Assert.Contains(w.DrainEvents().OfType<CommandRejected>(), e => e.Reason.Contains("faltam Pedra"));
        Assert.Empty(w.Map.Roads);
    }

    [Fact]
    public void Carriers_walk_faster_on_road_and_roads_survive_save_and_load()
    {
        static (int ticks, World w) Deliver(bool road)
        {
            var w = TestKit.NewWorld();
            int wood = TestKit.Res("wood");
            var hall = w.SeatBuilding!;
            var producer = TestKit.AddActive(w, "woodcutter", new Cell(hall.Center.X - 16, hall.Center.Y));
            producer.Stock.AddUpTo(wood, Qty.Units(10));
            w.Ledger.Produced[wood] += Qty.Units(10).Milli;
            if (road) w.Enqueue(new PlaceRoad(Line(producer.Origin.X + 2, hall.Origin.X - 1, hall.Center.Y)));
            w.StepTicks(SimTime.TicksPerHour - w.Tick % SimTime.TicksPerHour);
            w.Assign(w.Households[0], hall, AssignmentSource.Player, 0);
            var before = hall.Stock.Get(wood);
            int ticks = 0;
            while (hall.Stock.Get(wood) == before && ticks < 2000) { w.Step(); ticks++; }
            return (ticks, w);
        }
        var (offroad, _) = Deliver(road: false);
        var (onRoad, w2) = Deliver(road: true);
        Assert.True(onRoad < offroad, $"road trip {onRoad} should beat {offroad}");

        var copy = SaveSerializer.Load(SaveSerializer.Save(w2), w2.Content).World;
        Assert.Equal(w2.Map.Roads, copy.Map.Roads);
        w2.StepDays(3);
        copy.StepDays(3);
        Assert.Equal(SaveSerializer.StateHashHex(w2), SaveSerializer.StateHashHex(copy));
    }
}
