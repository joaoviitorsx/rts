namespace Ironvale.Sim.Tests;

/// <summary>Marco 2A.4: smithy turns wood + stone into tools; inputs are carried to it; tools decree; quarry.</summary>
public class SmithyTests
{
    private static void FillInputs(World w, Building smithy, int wood, int stone)
    {
        foreach (var (res, units) in new[] { ("wood", wood), ("stone", stone) })
        {
            int r = TestKit.Res(res);
            var taken = w.TakeFromStorages(r, Qty.Units(units), smithy.Center);   // moved, not created: ledger unchanged
            Assert.Equal(taken, smithy.InputStock.AddUpTo(r, taken));
        }
    }

    [Fact]
    public void Smithy_makes_tools_from_wood_and_stone()
    {
        var w = TestKit.NewWorld(TestKit.ContentWith(("commuteTicksPermille", "0")));
        var smithy = TestKit.AddActive(w, "smithy", new Cell(26, 30));
        FillInputs(w, smithy, wood: 10, stone: 5);
        w.Assign(w.Households[0], smithy, AssignmentSource.Player, 0);
        int tools = TestKit.Res("tools"), wood = TestKit.Res("wood"), stone = TestKit.Res("stone");
        w.StepDays(1);
        var made = smithy.Stock.Get(tools);
        Assert.True(made.IsPositive);
        // 2 wood + 1 stone per tool.
        Assert.Equal(Qty.Units(10) - made * 2, smithy.InputStock.Get(wood));
        Assert.Equal(Qty.Units(5) - made, smithy.InputStock.Get(stone));
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Without_inputs_the_smithy_makes_nothing()
    {
        var w = TestKit.NewWorld();
        var smithy = TestKit.AddActive(w, "smithy", new Cell(26, 30));
        w.Assign(w.Households[0], smithy, AssignmentSource.Player, 0);
        w.StepDays(3);   // no carriers: nothing arrives
        Assert.Equal(Qty.Zero, smithy.Stock.Total);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Carriers_keep_the_input_buffer_filled_and_tools_reach_storage()
    {
        var w = TestKit.NewWorld();
        int tools = TestKit.Res("tools");
        var smithy = TestKit.AddActive(w, "smithy", new Cell(26, 30));
        w.Assign(w.Households[0], smithy, AssignmentSource.Player, 0);
        w.Assign(w.Households[1], w.SeatBuilding!, AssignmentSource.Player, 0);
        w.Assign(w.Households[2], w.SeatBuilding!, AssignmentSource.Player, 0);
        var before = w.StorageStock(tools);
        for (int d = 0; d < 10; d++)
        {
            w.StepDays(1);
            TestKit.AssertInvariants(w);
            Assert.True(smithy.InputStock.Total <= smithy.Def.InputCapacity);
        }
        Assert.True(w.StorageStock(tools) > before, "tools should reach the hall");
    }

    [Fact]
    public void A_tools_decree_staffs_the_smithy()
    {
        var w = TestKit.NewWorld();
        var smithy = TestKit.AddActive(w, "smithy", new Cell(26, 30));
        w.Enqueue(new CreatePolicy("keep_above", "tools", 50, 60));   // 12 in stock: short
        w.StepDays(2);
        Assert.Equal(2, smithy.AssignedCount);
        Assert.All(w.Households.Where(h => h.JobBuildingId == smithy.Id), h => Assert.Equal(AssignmentSource.Policy, h.AssignedBy));
    }

    [Fact]
    public void Quarry_produces_stone()
    {
        var w = TestKit.NewWorld(TestKit.ContentWith(("commuteTicksPermille", "0")));
        var quarry = TestKit.AddActive(w, "quarry", new Cell(26, 30));
        w.Assign(w.Households[0], quarry, AssignmentSource.Player, 0);
        w.StepDays(1);
        Assert.True(quarry.Stock.Get(TestKit.Res("stone")) >= Qty.Units(4));
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Input_buffers_survive_save_and_load()
    {
        var w = TestKit.NewWorld();
        var smithy = TestKit.AddActive(w, "smithy", new Cell(26, 30));
        FillInputs(w, smithy, 6, 3);
        w.Assign(w.Households[0], smithy, AssignmentSource.Player, 0);
        w.Assign(w.Households[1], w.SeatBuilding!, AssignmentSource.Player, 0);
        w.StepTicks(SimTime.TicksPerDay + 9);
        var copy = SaveSerializer.Load(SaveSerializer.Save(w), w.Content).World;
        w.StepDays(4);
        copy.StepDays(4);
        Assert.Equal(SaveSerializer.StateHashHex(w), SaveSerializer.StateHashHex(copy));
    }
}
