namespace Ironvale.Sim.Tests;

/// <summary>Marco 2A.1: construction consumes materials carried to the site, and needs builders.</summary>
public class ConstructionTests
{
    private static Building Place(World w, string def, Cell at)
    {
        w.Enqueue(new PlaceBuilding(def, at, 0));
        w.Step();
        return w.Buildings.Last();
    }

    /// <summary>Moves material from storage onto a site without carriers (ledger unchanged: it only moves).</summary>
    private static void Deliver(World w, Building site, string res, int units)
    {
        int r = TestKit.Res(res);
        var taken = w.TakeFromStorages(r, Qty.Units(units), site.Center);
        Assert.Equal(taken, site.Stock.AddUpTo(r, taken));
    }

    [Fact]
    public void A_site_without_materials_does_not_progress_even_with_builders()
    {
        var w = TestKit.NewWorld();                     // no carriers: nothing reaches the site
        var site = Place(w, "house", new Cell(26, 30));
        w.StepDays(10);
        Assert.False(site.IsActive);
        Assert.Equal(0, site.BuildWorkMilli);
        Assert.All(w.Households, h => Assert.NotEqual(HouseholdState.Building, h.State));   // helpers don't idle there
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Work_cannot_run_ahead_of_the_materials_on_site()
    {
        var w = TestKit.NewWorld();
        var site = Place(w, "woodcutter", new Cell(24, 25));   // 10 wood + 2 stone
        Deliver(w, site, "wood", 10);
        Deliver(w, site, "stone", 1);                            // half the stone
        w.StepDays(10);
        Assert.False(site.IsActive);
        Assert.Equal(500, site.MaterialPermille);
        Assert.Equal(site.RequiredBuildWorkMilli / 2, site.BuildWorkMilli);

        Deliver(w, site, "stone", 1);
        w.StepDays(3);
        Assert.True(site.IsActive);
        Assert.Equal(Qty.Zero, site.Stock.Total);               // materials consumed, not kept as output
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Carriers_bring_materials_from_storage_to_the_site()
    {
        var w = TestKit.NewWorld();
        int wood = TestKit.Res("wood"), stone = TestKit.Res("stone");
        var woodBefore = w.StorageStock(wood);
        var site = Place(w, "woodcutter", new Cell(24, 25));
        w.Enqueue(new AssignHousehold(w.Households[0].Id, w.SeatBuilding!.Id));
        bool sawShipment = false;
        for (int i = 0; i < SimTime.TicksPerDay * 6 && !site.IsActive; i++)
        {
            w.Step();
            sawShipment |= w.Shipments.Any(s => s.ToId == site.Id);
            TestKit.AssertInvariants(w);
            Assert.True(site.IsActive || (site.Stock.Get(wood) <= Qty.Units(10) && site.Stock.Get(stone) <= Qty.Units(2)),
                "over-delivered");
        }
        Assert.True(sawShipment);
        Assert.True(site.IsActive);
        Assert.Equal(woodBefore - Qty.Units(10), w.StorageStock(wood));
    }

    [Fact]
    public void More_builders_finish_sooner_and_the_cap_per_site_holds()
    {
        int DaysToBuild(int builders)
        {
            var w = TestKit.NewWorld(TestKit.ContentWith(("autoBuilders", "0")));
            var site = Place(w, "house", new Cell(26, 30));
            Deliver(w, site, "wood", 15);
            foreach (var h in w.Households.Take(builders)) w.Enqueue(new AssignHousehold(h.Id, site.Id));
            int days = 0;
            while (!site.IsActive && days < 50) { w.StepDays(1); days++; }
            TestKit.AssertInvariants(w);
            return days;
        }
        int one = DaysToBuild(1), three = DaysToBuild(3);
        Assert.InRange(one, TestKit.Content.Building("house").BuildDays, TestKit.Content.Building("house").BuildDays + 1);
        Assert.True(three < one, $"3 builders ({three} d) should beat 1 ({one} d)");

        var w2 = TestKit.NewWorld();
        var site2 = Place(w2, "granary", new Cell(30, 36));
        Deliver(w2, site2, "wood", 30);
        Deliver(w2, site2, "stone", 5);
        w2.StepTicks(SimTime.TicksPerHour * 2);
        Assert.Equal(w2.Content.Balance.MaxBuildersPerSite, w2.BuildersAt(site2));   // 6 idle families, cap 3
    }

    [Fact]
    public void Without_auto_builders_only_assigned_families_build_and_they_are_freed_at_the_end()
    {
        var w = TestKit.NewWorld(TestKit.ContentWith(("autoBuilders", "0")));
        var site = Place(w, "house", new Cell(26, 30));
        Deliver(w, site, "wood", 15);
        w.StepDays(2);
        Assert.Equal(0, site.BuildWorkMilli);

        var h = w.Households[0];
        w.Enqueue(new AssignHousehold(h.Id, site.Id));
        w.StepDays(w.Content.Building("house").BuildDays + 1);
        Assert.True(site.IsActive);
        Assert.False(h.HasJob);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Cancelling_mid_delivery_returns_everything_to_storage()
    {
        var w = TestKit.NewWorld();
        int wood = TestKit.Res("wood");
        var before = w.StorageStock(wood);
        var site = Place(w, "granary", new Cell(36, 30));
        w.Enqueue(new AssignHousehold(w.Households[0].Id, w.SeatBuilding!.Id));
        w.Enqueue(new AssignHousehold(w.Households[1].Id, w.SeatBuilding!.Id));
        for (int i = 0; i < 2000 && !(site.Stock.Get(wood).IsPositive && w.Shipments.Any(s => s.ToId == site.Id)); i++) w.Step();
        Assert.True(site.Stock.Get(wood).IsPositive, "some wood should be on site");
        Assert.Contains(w.Shipments, s => s.ToId == site.Id);

        w.Enqueue(new CancelConstruction(site.Id));
        w.Step();
        Assert.DoesNotContain(w.Buildings, b => b.Id == site.Id);
        TestKit.AssertInvariants(w);
        w.StepDays(1);                                    // redirected cargo arrives at storage
        Assert.Empty(w.Shipments);
        Assert.Equal(before, w.StorageStock(wood));
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void A_site_survives_save_and_load_identically()
    {
        var w = TestKit.NewWorld();
        Place(w, "granary", new Cell(36, 30));
        w.Enqueue(new AssignHousehold(w.Households[0].Id, w.SeatBuilding!.Id));
        w.StepTicks(SimTime.TicksPerDay + 17);   // mid-delivery, mid-hour
        var copy = SaveSerializer.Load(SaveSerializer.Save(w), w.Content).World;
        w.StepDays(5);
        copy.StepDays(5);
        Assert.Equal(SaveSerializer.StateHashHex(w), SaveSerializer.StateHashHex(copy));
    }

    [Fact]
    public void Old_saves_are_rejected_with_a_clear_message()
    {
        var e = Assert.Throws<SaveException>(() =>
            SaveSerializer.Load("{\"format\":\"ironvale-save\",\"saveVersion\":1}"u8.ToArray(), TestKit.Content));
        Assert.Contains("versão anterior", e.Message);
    }
}
