namespace Ironvale.Sim.Tests;

public class ProductionTests
{
    [Fact]
    public void Woodcutter_produces_per_worker_hour_into_its_own_stock()
    {
        var w = TestKit.NewWorld();
        var b = TestKit.AddActive(w, "woodcutter", new Cell(10, 10));
        var h = w.Households[0];
        w.Assign(h, b, AssignmentSource.Player, 0);

        // Align to an hour boundary, then one day = 10 production hours.
        w.StepTicks(SimTime.TicksPerHour - w.Tick % SimTime.TicksPerHour);
        var before = b.Stock.Get(TestKit.Res("wood"));
        w.StepDays(1);

        // 0.5 wood/worker/hour × 2 workers × 10 h × productivity (≈ 100%, slight tool wear) ≈ 10
        var produced = b.Stock.Get(TestKit.Res("wood")) - before;
        Assert.InRange(produced.AsDouble, 9.5, 10.0);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Production_stops_when_the_output_buffer_is_full()
    {
        var w = TestKit.NewWorld();
        var b = TestKit.AddActive(w, "woodcutter", new Cell(10, 10));
        w.Assign(w.Households[0], b, AssignmentSource.Player, 0);
        w.StepDays(30);   // no carriers: nothing leaves
        Assert.Equal(b.Def.OutputCapacity, b.Stock.Total);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Field_only_yields_at_harvest_after_a_work_season()
    {
        var w = TestKit.NewWorld();                      // starts in the last month of spring
        var field = TestKit.AddActive(w, "field", new Cell(10, 10));
        w.Assign(w.Households[0], field, AssignmentSource.Player, 0);
        int food = TestKit.Res("food");

        w.StepDays(SimTime.DaysPerMonth - 1);           // still spring: work accumulates, no food yet
        Assert.True(field.SeasonalWorkMilli > 0);
        Assert.Equal(Qty.Zero, field.Stock.Get(food));

        w.StepDays(2);                                  // summer starts → spring work is harvested
        Assert.True(field.Stock.Get(food).IsPositive);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Worn_tools_reduce_productivity_down_to_the_floor()
    {
        var w = TestKit.NewWorld();
        var b = TestKit.AddActive(w, "woodcutter", new Cell(10, 10));
        var h = w.Households[0];
        w.Assign(h, b, AssignmentSource.Player, 0);
        int tools = TestKit.Res("tools");
        w.SeatBuilding!.Stock.RemoveUpTo(tools, Qty.Units(1000));   // no spare tools
        w.Ledger.Consumed[tools] += Qty.Units(12).Milli;             // keep the ledger honest

        w.StepDays(40);
        Assert.True(h.ToolCondition < Permille.One);
        int partial = h.ProductivityPermille;
        Assert.True(partial < Permille.One);

        h.ToolCondition = 3;   // almost worn out (avoid waiting months: the family would starve first)
        w.StepDays(2);
        Assert.Equal(0, h.ToolCondition);
        Assert.Equal(w.Content.Balance.ToolFactorFloorPermille, h.ProductivityPermille);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Worn_out_tool_is_replaced_from_storage()
    {
        var w = TestKit.NewWorld();
        var b = TestKit.AddActive(w, "woodcutter", new Cell(10, 10));
        var h = w.Households[0];
        w.Assign(h, b, AssignmentSource.Player, 0);
        h.ToolCondition = 1;
        int tools = TestKit.Res("tools");
        var before = w.StorageStock(tools);

        w.StepDays(2);

        Assert.Equal(before - Qty.Units(1), w.StorageStock(tools));
        Assert.True(h.ToolCondition > 900);
        TestKit.AssertInvariants(w);
    }
}

public class TransportTests
{
    [Fact]
    public void Carrier_moves_output_to_storage_through_an_in_transit_shipment()
    {
        var w = TestKit.NewWorld();
        int wood = TestKit.Res("wood");
        var producer = TestKit.AddActive(w, "woodcutter", new Cell(20, 30));
        producer.Stock.AddUpTo(wood, Qty.Units(15));
        w.Ledger.Produced[wood] += Qty.Units(15).Milli;
        var hall = w.SeatBuilding!;
        var storedBefore = hall.Stock.Get(wood);

        w.Assign(w.Households[0], hall, AssignmentSource.Player, 0);
        var carrier = Assert.Single(w.Carriers);

        bool sawInTransit = false;
        for (int i = 0; i < 400 && hall.Stock.Get(wood) == storedBefore; i++)
        {
            w.Step();
            TestKit.AssertInvariants(w);
            if (w.Shipments.Count == 1)
            {
                sawInTransit = true;
                // While carried, the wood is neither at the producer nor at the hall.
                Assert.Equal(Qty.Zero, producer.Stock.Get(wood));
                Assert.Equal(storedBefore, hall.Stock.Get(wood));
            }
        }

        Assert.True(sawInTransit, "resource must exist as a shipment while carried");
        Assert.Equal(storedBefore + Qty.Units(15), hall.Stock.Get(wood));
        Assert.Empty(w.Shipments);
    }

    [Fact]
    public void Trip_takes_time_proportional_to_distance()
    {
        static int TicksToDeliver(int distanceCells)
        {
            var w = TestKit.NewWorld();
            int wood = TestKit.Res("wood");
            var hall = w.SeatBuilding!;
            var producer = TestKit.AddActive(w, "woodcutter", new Cell(hall.Center.X - distanceCells - 1, hall.Center.Y));
            producer.Stock.AddUpTo(wood, Qty.Units(10));
            w.Ledger.Produced[wood] += Qty.Units(10).Milli;
            w.StepTicks(SimTime.TicksPerHour - w.Tick % SimTime.TicksPerHour);  // plan on the next hour start
            w.Assign(w.Households[0], hall, AssignmentSource.Player, 0);
            var before = hall.Stock.Get(wood);
            int ticks = 0;
            while (hall.Stock.Get(wood) == before) { w.Step(); ticks++; }
            return ticks;
        }

        int near = TicksToDeliver(4);
        int far = TicksToDeliver(14);
        int perCell = TestKit.Content.Balance.CarrierTicksPerCell;
        Assert.Equal(2 * 10 * perCell, far - near);   // there and back, 10 extra cells each way
    }

    [Fact]
    public void Unassigning_a_loaded_carrier_still_delivers_the_cargo()
    {
        var w = TestKit.NewWorld();
        int wood = TestKit.Res("wood");
        var producer = TestKit.AddActive(w, "woodcutter", new Cell(20, 30));
        producer.Stock.AddUpTo(wood, Qty.Units(15));
        w.Ledger.Produced[wood] += Qty.Units(15).Milli;
        var hall = w.SeatBuilding!;
        var h = w.Households[0];
        w.Assign(h, hall, AssignmentSource.Player, 0);

        while (w.Shipments.Count == 0) w.Step();
        w.Unassign(h);
        Assert.True(w.Carriers.Single().Retiring);

        for (int i = 0; i < 400 && w.Carriers.Count > 0; i++) w.Step();
        Assert.Empty(w.Carriers);
        Assert.Empty(w.Shipments);
        TestKit.AssertInvariants(w);
    }
}

public class NeedsTests
{
    [Fact]
    public void Firewood_is_only_consumed_in_winter()
    {
        var w = TestKit.NewWorld();
        int firewood = TestKit.Res("firewood");
        var before = w.StorageStock(firewood);
        w.StepDays(30);                                  // late spring → summer
        Assert.Equal(before, w.StorageStock(firewood));

        // Jump straight to the start of winter (waiting would starve the families on 300 food first).
        w.Tick = SimTime.TicksPerMonth * 9;
        w.StepDays(2);
        Assert.True(w.StorageStock(firewood) < before);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Families_eat_daily_and_subsist_partially_without_work()
    {
        var w = TestKit.NewWorld();
        int food = TestKit.Res("food");
        var before = w.StorageStock(food);
        w.StepDays(1);
        var bal = w.Content.Balance;
        long members = w.Households.Sum(h => h.Members);
        var fromStorage = (bal.FoodPerMemberPerDay * members).MulPermille(Permille.One - bal.SubsistenceFoodCoverPermille);
        Assert.Equal(before - fromStorage, w.StorageStock(food));
    }

    [Fact]
    public void Starving_household_leaves_after_the_deficit_window()
    {
        var w = TestKit.NewWorld();
        int food = TestKit.Res("food");
        var removed = w.SeatBuilding!.Stock.RemoveUpTo(food, Qty.Units(10_000));
        w.Ledger.Consumed[food] += removed.Milli;

        w.StepDays(w.Content.Balance.LeaveAfterDeficitDays + 2);

        Assert.True(w.IsCollapsed);
        Assert.Contains(w.DrainEvents(), e => e is HouseholdLeft { Reason: "fome" });
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Homeless_households_move_into_finished_houses()
    {
        var w = TestKit.NewWorld();
        var house = TestKit.AddActive(w, "house", new Cell(10, 10));
        w.StepDays(1);
        Assert.Equal(house.Def.HousingCapacity, w.Households.Count(h => h.HomeId == house.Id));
    }
}

public class PolicyTests
{
    private static (World w, Building cutter, Policy p) Setup(long threshold)
    {
        var w = TestKit.NewWorld();
        var cutter = TestKit.AddActive(w, "woodcutter", new Cell(10, 10));
        w.Enqueue(new CreatePolicy("keep_above", "wood", threshold));
        w.Step();
        return (w, cutter, w.Policies.Single());
    }

    [Fact]
    public void Recruits_one_household_per_day_while_below_threshold()
    {
        var (w, cutter, p) = Setup(threshold: 1000);   // hall has 120 wood → short; created on a day start
        Assert.Equal(1, cutter.AssignedCount);           // first evaluation already ran that day
        w.StepDays(1);
        Assert.Equal(2, cutter.AssignedCount);
        Assert.All(w.Households.Where(h => h.HasJob), h =>
        {
            Assert.Equal(AssignmentSource.Policy, h.AssignedBy);
            Assert.Equal(p.Id, h.AssignedByPolicyId);
        });
        Assert.Contains(w.PolicyLog, e => e.Text.Contains("→ Lenhador"));
    }

    [Fact]
    public void Releases_households_above_threshold_plus_hysteresis()
    {
        var (w, cutter, p) = Setup(threshold: 1000);
        w.StepDays(1);
        Assert.Equal(2, cutter.AssignedCount);

        w.Enqueue(new SetPolicyThreshold(p.Id, 10));   // 120 wood > 12.5 → release
        w.StepDays(3);
        Assert.Equal(0, cutter.AssignedCount);
        Assert.Contains(w.PolicyLog, e => e.Text.Contains("liberada"));
    }

    [Fact]
    public void Never_takes_households_assigned_by_the_player()
    {
        var (w, cutter, _) = Setup(threshold: 1000);
        var field = TestKit.AddActive(w, "field", new Cell(20, 10));
        foreach (var h in w.Households.Take(2)) w.Enqueue(new AssignHousehold(h.Id, field.Id));
        foreach (var h in w.Households.Skip(2)) w.Enqueue(new AssignHousehold(h.Id, field.Id));  // field has 2 slots: rest rejected
        w.Step();
        var playerOwned = w.Households.Where(h => h.AssignedBy == AssignmentSource.Player).Select(h => h.Id).ToList();
        Assert.Equal(2, playerOwned.Count);

        w.StepDays(10);
        Assert.All(playerOwned, id => Assert.Equal(field.Id, w.GetHousehold(id)!.JobBuildingId));
    }

    [Fact]
    public void Switches_an_empty_producer_to_the_needed_recipe()
    {
        var w = TestKit.NewWorld();
        var cutter = TestKit.AddActive(w, "woodcutter", new Cell(10, 10));
        Assert.Equal("chop_wood", cutter.Recipe!.Id);
        w.Enqueue(new CreatePolicy("keep_above", "firewood", 1000));
        w.StepDays(1);
        Assert.Equal("split_firewood", cutter.Recipe!.Id);
        Assert.Equal(1, cutter.AssignedCount);
    }
}

public class CommandTests
{
    [Fact]
    public void Commands_apply_at_the_next_step_and_pay_the_cost()
    {
        var w = TestKit.NewWorld();
        int wood = TestKit.Res("wood");
        var before = w.StorageStock(wood);
        w.Enqueue(new PlaceBuilding("house", new Cell(10, 10), 0));
        Assert.Single(w.Buildings);
        w.Step();
        Assert.Equal(2, w.Buildings.Count);
        Assert.Equal(before - Qty.Units(15), w.StorageStock(wood));
        Assert.False(w.Buildings[1].IsActive);

        w.StepDays(w.Content.Building("house").BuildDays + 1);
        Assert.True(w.Buildings[1].IsActive);
        TestKit.AssertInvariants(w);
    }

    [Theory]
    [InlineData("house", 30, 30, "ocupado")]       // on top of the hall
    [InlineData("house", 63, 63, "fora do mapa")]
    [InlineData("hall", 5, 5, "não pode ser construído")]
    [InlineData("castle", 5, 5, "desconhecido")]
    public void Invalid_placement_is_rejected_with_a_reason(string def, int x, int y, string reason)
    {
        var w = TestKit.NewWorld();
        w.Enqueue(new PlaceBuilding(def, new Cell(x, y), 0));
        w.Step();
        var rejected = Assert.Single(w.DrainEvents().OfType<CommandRejected>());
        Assert.Contains(reason, rejected.Reason);
        Assert.Single(w.Buildings);
    }

    [Fact]
    public void Placement_without_enough_resources_is_rejected()
    {
        var w = TestKit.NewWorld();
        for (int i = 0; i < 5; i++) w.Enqueue(new PlaceBuilding("granary", new Cell(2 + i * 4, 2), 0));  // 5×30 wood > 120
        w.Step();
        Assert.Contains(w.DrainEvents().OfType<CommandRejected>(), e => e.Reason.Contains("faltam Madeira"));
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Cancelling_construction_refunds_the_cost()
    {
        var w = TestKit.NewWorld();
        int wood = TestKit.Res("wood");
        var before = w.StorageStock(wood);
        w.Enqueue(new PlaceBuilding("granary", new Cell(10, 10), 0));
        w.Step();
        w.Enqueue(new CancelConstruction(w.Buildings[1].Id));
        w.Step();
        Assert.Single(w.Buildings);
        Assert.Equal(before, w.StorageStock(wood));
        Assert.Equal(0, w.Map.BuildingAt(new Cell(10, 10)));
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Assigning_to_a_full_building_is_rejected()
    {
        var w = TestKit.NewWorld();
        var hall = w.SeatBuilding!;
        foreach (var h in w.Households.Take(3)) w.Enqueue(new AssignHousehold(h.Id, hall.Id));
        w.Step();
        Assert.Equal(2, w.Carriers.Count);
        Assert.Contains(w.DrainEvents().OfType<CommandRejected>(), e => e.Reason == "sem vagas");
    }
}
