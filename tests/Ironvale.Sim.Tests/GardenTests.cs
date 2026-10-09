namespace Ironvale.Sim.Tests;

/// <summary>Marco 2A.3: the yard garden feeds the family in its free time; commute eats free time.</summary>
public class GardenTests
{
    private static (World w, Household h) Family(Cell workAt, int startMonth = -1)
    {
        var w = TestKit.NewWorld();
        if (startMonth >= 0) w.Tick = (long)startMonth * SimTime.TicksPerMonth;   // jump the clock (nothing to simulate yet)
        var home = TestKit.AddActive(w, "house", new Cell(30, 26));
        var cutter = TestKit.AddActive(w, "woodcutter", workAt);
        var h = w.Households[0];
        h.HomeId = home.Id;
        w.Assign(h, cutter, AssignmentSource.Player, 0);
        return (w, h);
    }

    [Fact]
    public void Living_next_to_work_leaves_more_garden_time_than_living_far()
    {
        var (wn, near) = Family(new Cell(30, 22));
        var (wf, far) = Family(new Cell(30, 4));
        wn.StepDays(1);
        wf.StepDays(1);
        Assert.True(near.GardenFoodToday.IsPositive);
        Assert.True(far.GardenFoodToday < near.GardenFoodToday, $"far {far.GardenFoodToday} vs near {near.GardenFoodToday}");
        Assert.True(wf.FreeMilliHours(far) < wn.FreeMilliHours(near));
        TestKit.AssertInvariants(wn);
        TestKit.AssertInvariants(wf);
    }

    [Fact]
    public void No_garden_in_winter_or_without_a_house()
    {
        var (w, h) = Family(new Cell(30, 22), startMonth: 9);   // first month of winter
        w.StepDays(1);
        Assert.Equal(Qty.Zero, h.GardenFoodToday);

        var w2 = TestKit.NewWorld();
        w2.StepDays(1);                                       // no houses at all
        Assert.All(w2.Households, x => Assert.Equal(Qty.Zero, x.GardenFoodToday));
    }

    [Fact]
    public void The_garden_saves_food_from_storage()
    {
        long StorageFoodUsed(string freeHours)
        {
            var w = TestKit.NewWorld(TestKit.ContentWith(("gardenFreeHours", freeHours)));
            var home = TestKit.AddActive(w, "house", new Cell(30, 26));
            foreach (var h in w.Households.Take(2)) h.HomeId = home.Id;
            int food = TestKit.Res("food");
            var before = w.StorageStock(food);
            w.StepDays(10);
            TestKit.AssertInvariants(w);
            return (before - w.StorageStock(food)).Milli;
        }
        Assert.True(StorageFoodUsed("2") < StorageFoodUsed("0"));
    }
}
