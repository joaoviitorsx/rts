using Ironvale.Sim.Scripting;
namespace Ironvale.Sim.Tests;

/// <summary>RTS opening, briefing step 2 (GDD v0.3 §3–§9): units, orders, gathering, hunting, the ox, rain, wolves.</summary>
public class OpeningUnitsTests
{
    private static readonly Lazy<ScenarioDef> Wild =
        new(() => DataPaths.LoadWithScenario(DataPaths.FindDataDirectory(), "wild_start").Scenario);

    private static World NewWild(ulong seed = 42, ContentDb? content = null)
    {
        content ??= TestKit.Content;
        var scenario = content == TestKit.Content ? Wild.Value
            : ContentLoader.LoadScenario(File.ReadAllText(Path.Combine(DataPaths.FindDataDirectory(), DataPaths.ScenarioDir, "wild_start.json")), content);
        var w = World.Create(content, scenario, seed);
        w.CollectEvents = true;
        return w;
    }

    /// <summary>Colonists never leave (tests that need summer or autumn without feeding the band).</summary>
    private static readonly Lazy<ContentDb> Patient = new(() => TestKit.ContentWith(("leaveAfterDeficitDays", "100000")));

    private static int Res(World w, string id) => w.Content.Resource(id).Index;
    private static Building Pile(World w) => w.Buildings.First(b => b.Def.Id == "pile");
    private static List<Unit> Colonists(World w) => w.Units.Where(u => u.IsColonist).ToList();
    private static Unit Ox(World w) => w.Units.Single(u => u.Kind == UnitKind.Ox);

    private static bool StepUntil(World w, Func<bool> done, int maxTicks)
    {
        for (int i = 0; i < maxTicks; i++)
        {
            if (done()) return true;
            w.Step();
        }
        return done();
    }

    private static Cell NearestTree(World w, Cell from) =>
        w.NearestNode(NodeKind.Tree, from, 40, Colonists(w)[0]) ?? throw new InvalidOperationException("no tree");

    [Fact]
    public void The_band_starts_in_the_clearing_with_a_pile_on_the_ground()
    {
        var w = NewWild();
        Assert.Equal(8, Colonists(w).Count);
        Assert.Single(w.Units, u => u.Kind == UnitKind.Ox);
        Assert.All(w.Units, u => Assert.True(World.Chebyshev(u.Pos, w.Terrain!.Start) <= 4));
        Assert.True(Pile(w).Def.Uncovered);
        Assert.Equal(80_000, Pile(w).Stock.Get(Res(w, "food")).Milli);
        Assert.Empty(w.Households);
        Assert.NotEmpty(w.Animals);
        Assert.False(w.IsCollapsed);
    }

    [Fact]
    public void A_colonist_fells_a_tree_carries_the_log_home_and_keeps_going_nearby()
    {
        var w = NewWild();
        var c = Colonists(w)[0];
        var tree = NearestTree(w, c.Pos);
        w.Enqueue(new OrderUnits(new[] { c.Id }, OrderKind.Gather, tree));
        int wood = Res(w, "wood");
        Assert.True(StepUntil(w, () => Pile(w).Stock.Get(wood).Milli >= 12_000, 4000), "log not brought home");
        w.StepTicks(2);   // the next order is chosen on the following tick
        Assert.Equal(TreeStage.Stump, Nature.StageOf(w.Nature!.At(tree), w.Tick, w.Content.Balance));
        Assert.Contains(w.DrainEvents(), e => e is TreeFelled f && f.Cell == tree);
        // Auto-continue: another tree close to the first, or "?" when there is none.
        Assert.True(c.Order is { Kind: OrderKind.Gather } o && World.Chebyshev(o.Cell, tree) <= w.Content.Balance.AutoContinueCells
                    || c.Confused);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void The_ox_drags_a_whole_log_in_one_trip()
    {
        var w = NewWild();
        var c = Colonists(w)[0];
        var tree = NearestTree(w, c.Pos);
        w.Enqueue(new OrderUnits(new[] { c.Id }, OrderKind.Gather, tree));
        Assert.True(StepUntil(w, () => w.GroundItems.Any(g => g.Cell == tree), 400));
        w.Enqueue(new StopUnits(new[] { c.Id }));
        var log = w.GroundItems.Single(g => g.Cell == tree);
        var ox = Ox(w);
        w.Enqueue(new OrderUnits(new[] { ox.Id }, OrderKind.Pickup, tree, log.Id));
        Assert.True(StepUntil(w, () => ox.IsCarrying, 2000));
        Assert.Equal(12_000, ox.CarryAmount.Milli);
        Assert.True(StepUntil(w, () => !ox.IsCarrying && ox.Order is null, 2000));
        Assert.Equal(12_000, Pile(w).Stock.Get(Res(w, "wood")).Milli);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void The_ox_does_not_chop_hunt_or_build()
    {
        var w = NewWild();
        var ox = Ox(w);
        w.Enqueue(new OrderUnits(new[] { ox.Id }, OrderKind.Gather, NearestTree(w, ox.Pos)));
        w.ApplyPendingCommands();
        Assert.Contains(w.DrainEvents(), e => e is CommandRejected);
        Assert.Null(ox.Order);
    }

    [Fact]
    public void A_group_sent_to_one_tree_spreads_over_the_nearest_trees()
    {
        var w = NewWild();
        var group = Colonists(w).Take(3).Select(u => u.Id).ToArray();
        var tree = NearestTree(w, w.Terrain!.Start);
        w.Enqueue(new OrderUnits(group, OrderKind.Gather, tree));
        w.ApplyPendingCommands();
        var targets = group.Select(id => w.GetUnit(id)!.Order!.Value.Cell).ToList();
        Assert.Equal(3, targets.Distinct().Count());
        Assert.Contains(tree, targets);
    }

    [Fact]
    public void Bushes_give_food_only_in_summer_and_autumn()
    {
        var w = NewWild(content: Patient.Value);
        var c = Colonists(w)[0];
        var bush = w.NearestNode(NodeKind.Bush, c.Pos, 60, c);
        Assert.Null(bush);   // spring: no fruit, so no gatherable bush
        w.StepDays(SimTime.DaysPerMonth * SimTime.MonthsPerSeason);   // summer
        bush = w.NearestNode(NodeKind.Bush, c.Pos, 60, c);
        Assert.NotNull(bush);
        int food = Res(w, "food");
        long before = w.Ledger.ProducedOf(food).Milli;
        w.Enqueue(new OrderUnits(new[] { c.Id }, OrderKind.Gather, bush!.Value));
        Assert.True(StepUntil(w, () => w.Ledger.ProducedOf(food).Milli > before, 2000));
        Assert.False(w.Gatherable(bush.Value));   // picked for this season
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Hunting_a_deer_brings_meat_and_hides()
    {
        var w = NewWild(content: Patient.Value);
        var hunters = Colonists(w).Take(2).Select(u => u.Id).ToArray();
        var deer = w.Animals.Where(a => a.Kind == FaunaKind.Deer).OrderBy(a => a.Pos.Manhattan(w.Terrain!.Start)).First();
        int deerCount = w.Animals.Count(a => a.Kind == FaunaKind.Deer);
        w.Enqueue(new OrderUnits(hunters, OrderKind.Hunt, deer.Pos, deer.Id));
        int hides = Res(w, "hides");
        Assert.True(StepUntil(w, () => Pile(w).Stock.Get(hides).IsPositive, 90 * SimTime.TicksPerDay), "no hides home");   // 4 trips of meat first
        Assert.Equal(deerCount - 1, w.Animals.Count(a => a.Kind == FaunaKind.Deer));
        Assert.Contains(w.DrainEvents(), e => e is AnimalKilled k && k.AnimalId == deer.Id);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Rain_is_forecast_and_spoils_the_uncovered_pile()
    {
        var w = NewWild();
        int food = Res(w, "food");
        bool forecast = false;
        Assert.True(StepUntil(w, () =>
        {
            if (w.WeatherTomorrow == Weather.Rain) forecast = true;
            return w.WeatherToday == Weather.Rain;
        }, (w.Content.Balance.FirstRainDay + 2) * SimTime.TicksPerDay), "no rain by the guaranteed day");
        Assert.True(forecast, "rain came without the day-ahead warning");
        w.StepTicks(SimTime.TicksPerDay);
        Assert.Contains(w.DrainEvents(), e => e is Spoiled s && s.Resource == food);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Colonists_fetch_material_and_build()
    {
        var w = NewWild();
        int wood = Res(w, "wood");
        Pile(w).Stock.AddUpTo(wood, Qty.Units(20));
        w.Ledger.Initial[wood] += 20_000;
        var house = w.Content.Building("house");
        var spot = w.FreeCellsAround(new Cell(w.Terrain!.Start.X + 4, w.Terrain.Start.Y), 60)
            .First(c => w.CanPlace(house, c, 0));
        w.Enqueue(new PlaceBuilding("house", spot, 0));
        w.ApplyPendingCommands();
        var site = w.Buildings.Single(b => b.Def.Id == "house");
        w.Enqueue(new OrderUnits(Colonists(w).Take(2).Select(u => u.Id).ToArray(), OrderKind.Build, site.Center, site.Id));
        Assert.True(StepUntil(w, () => site.IsActive, 20 * SimTime.TicksPerDay), "house not built");
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void A_wolf_scares_a_lone_colonist_and_backs_off_from_a_group()
    {
        var w = NewWild(content: Patient.Value);
        w.StepDays(SimTime.DaysPerMonth * SimTime.MonthsPerSeason * 2);   // autumn
        var colonists = Colonists(w);
        var lone = colonists[0];
        var wolf = w.Animals.First(a => a.Kind == FaunaKind.Wolf);
        // Put the lone colonist far from the others, carrying stone, with the wolf next to them.
        var far = w.FreeCellsAround(new Cell(w.Terrain!.Start.X + 25, w.Terrain.Start.Y + 25), 3);
        lone.Pos = lone.Next = far[0];
        lone.CarryResource = Res(w, "stone");
        lone.CarryAmount = Qty.Units(3);
        w.Ledger.Initial[Res(w, "stone")] += 3_000;
        wolf.Pos = wolf.Next = far[1];
        wolf.State = AnimalState.Grazing;
        Assert.True(StepUntil(w, () => lone.Step == UnitStep.Fleeing, 40));
        Assert.False(lone.IsCarrying);
        Assert.Contains(w.GroundItems, g => g.Resource == Res(w, "stone"));
        Assert.Equal(AnimalState.Retreating, wolf.State);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Hungry_colonists_leave_and_their_load_stays_on_the_ground()
    {
        var content = TestKit.ContentWith(("leaveAfterDeficitDays", "3"));
        var w = NewWild(content: content);
        var food = content.Resource("food").Index;
        Pile(w).Stock.RemoveUpTo(food, Pile(w).Stock.Get(food));
        w.Ledger.Initial[food] -= 80_000;
        w.StepDays(6);
        Assert.Empty(Colonists(w));
        Assert.True(w.IsCollapsed);
        Assert.Equal(8, w.DrainEvents().Count(e => e is UnitLeft));
    }

    [Fact]
    public void The_opening_is_deterministic_through_save_and_load()
    {
        static void Drive(World w, int days, int salt)
        {
            var rng = new Random(salt);
            for (int d = 0; d < days; d++)
            {
                var ids = w.Units.Select(u => u.Id).ToArray();
                if (ids.Length > 0 && w.NearestNode(NodeKind.Tree, w.Terrain!.Start, 30, w.Units[0]) is { } tree)
                    w.Enqueue(new OrderUnits(ids.Where((_, i) => rng.Next(2) == 0).ToArray(), OrderKind.Gather, tree, 0, rng.Next(2) == 0));
                w.StepDays(1);
            }
        }
        var a = NewWild(7);
        var b = NewWild(7);
        Drive(a, 20, 1);
        Drive(b, 20, 1);
        Assert.Equal(SaveSerializer.StateHashHex(a), SaveSerializer.StateHashHex(b));
        var c = SaveSerializer.Load(SaveSerializer.Save(a), TestKit.Content).World;
        Assert.Equal(SaveSerializer.StateHashHex(a), SaveSerializer.StateHashHex(c));
        Drive(a, 20, 2);
        Drive(c, 20, 2);
        Assert.Equal(SaveSerializer.StateHashHex(a), SaveSerializer.StateHashHex(c));
        TestKit.AssertInvariants(a);
    }

    // ------------------------------------------------------------------ step 3: campfire, covered depot, tent

    private static Building PlaceAndBuild(World w, string id, Cell near, IEnumerable<Unit> builders)
    {
        var def = w.Content.Building(id);
        var spot = w.FreeCellsAround(near, 80).First(c => w.CanPlace(def, c, 0));
        w.Enqueue(new PlaceBuilding(id, spot, 0));
        w.ApplyPendingCommands();
        var site = w.Buildings.Last(b => b.Def.Id == id);
        w.Enqueue(new OrderUnits(builders.Select(u => u.Id).ToArray(), OrderKind.Build, site.Center, site.Id));
        return site;
    }

    [Fact]
    public void Campfire_depot_and_tent_are_built_and_do_their_job()
    {
        var w = NewWild(42, Patient.Value);
        int wood = Res(w, "wood"), hides = Res(w, "hides"), food = Res(w, "food");
        Pile(w).Stock.AddUpTo(wood, Qty.Units(40));
        w.Ledger.Initial[wood] += 40_000;
        Pile(w).Stock.AddUpTo(hides, Qty.Units(4));
        w.Ledger.Initial[hides] += 4_000;
        var start = w.Terrain!.Start;
        var band = Colonists(w);
        var fire = PlaceAndBuild(w, "campfire", new Cell(start.X + 3, start.Y + 3), band.Take(2));
        var depot = PlaceAndBuild(w, "depot", new Cell(start.X - 4, start.Y), band.Skip(2).Take(3));
        var tent = PlaceAndBuild(w, "tent", new Cell(start.X, start.Y + 5), band.Skip(5).Take(3));
        Assert.True(StepUntil(w, () => fire.IsActive && depot.IsActive && tent.IsActive, 30 * SimTime.TicksPerDay),
            $"not built: fire {fire.IsActive} depot {depot.IsActive} tent {tent.IsActive}");

        // Tent: two colonists sleep in it (the next daily needs pass).
        w.StepDays(1);
        Assert.Equal(2, w.Units.Count(u => u.ShelterId == tent.Id));

        // Covered depot: food moved into it does not spoil on rainy days, the pile's does.
        var moved = Pile(w).Stock.RemoveUpTo(food, Qty.Units(30));
        depot.Stock.AddUpTo(food, moved);
        w.DrainEvents();
        Assert.True(StepUntil(w, () => w.WeatherToday == Weather.Rain, 60 * SimTime.TicksPerDay), "no rain");
        w.StepTicks(SimTime.TicksPerDay);
        var spoiled = w.DrainEvents().OfType<Spoiled>().ToList();
        Assert.DoesNotContain(spoiled, e => e.BuildingId == depot.Id);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void Opening_buildings_are_not_offered_on_the_flat_map()
    {
        var w = World.Create(TestKit.Content, TestKit.Scenario, 1);
        Assert.NotNull(w.PlacementError(w.Content.Building("campfire"), new Cell(5, 5), 0));
    }

    // ------------------------------------------------------------------ step 3b: colonists → families

    private static Building BuildHouse(World w)
    {
        int wood = Res(w, "wood");
        Pile(w).Stock.AddUpTo(wood, Qty.Units(20));
        w.Ledger.Initial[wood] += 20_000;
        var house = PlaceAndBuild(w, "house", new Cell(w.Terrain!.Start.X + 4, w.Terrain.Start.Y), Colonists(w).Take(3));
        Assert.True(StepUntil(w, () => house.IsActive, 20 * SimTime.TicksPerDay), "house not built");
        return house;
    }

    [Fact]
    public void A_finished_house_turns_the_two_nearest_colonists_into_a_family()
    {
        var w = NewWild(42, Patient.Value);
        int before = Colonists(w).Count;
        var house = BuildHouse(w);
        var formed = Assert.Single(w.DrainEvents().OfType<FamilyFormed>());
        Assert.Equal(before - 2, Colonists(w).Count);
        var family = w.Households.Single();
        Assert.Equal(house.Id, family.HomeId);
        Assert.Equal(2, family.Members);
        Assert.All(formed.UnitIds, id => Assert.DoesNotContain(w.Units, u => u.Id == id));
        // The family is in the 2A model: it can be designated to a workplace.
        w.StepDays(2);
        Assert.Equal(house.Id, w.Households.Single().HomeId);
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void A_family_arrives_only_while_a_house_stands_empty_and_food_lasts()
    {
        // No colonists move in (familyFromColonists 0), so the finished house stays empty: an invitation.
        var content = TestKit.ContentWith(("leaveAfterDeficitDays", "100000"), ("familyArrivalFoodDays", "1"), ("familyFromColonists", "0"));
        var w = NewWild(42, content);
        w.StepDays(12);
        Assert.Empty(w.Households);   // no house, nobody comes
        var house = BuildHouse(w);
        Assert.Empty(w.Households);
        Assert.True(StepUntil(w, () => w.Households.Count == 1, 25 * SimTime.TicksPerDay), "no family arrived");
        var arrived = w.DrainEvents().OfType<FamilyArrived>().Single();
        Assert.InRange(arrived.Members, 2, 4);
        Assert.Equal(house.Id, arrived.HouseId);
        // The house is taken now: nobody else comes.
        w.StepDays(25);
        Assert.Single(w.Households);
        Assert.Empty(w.DrainEvents().OfType<FamilyArrived>());
        TestKit.AssertInvariants(w);
    }

    // ------------------------------------------------------------------ step 3c: huts of the delegation ladder

    private static Building HutWithFamily(World w, string id, Cell near)
    {
        BuildHouse(w);
        int wood = Res(w, "wood");
        Pile(w).Stock.AddUpTo(wood, Qty.Units(20));
        w.Ledger.Initial[wood] += 20_000;
        var hut = PlaceAndBuild(w, id, near, Colonists(w).Take(3));
        Assert.True(StepUntil(w, () => hut.IsActive, 20 * SimTime.TicksPerDay), $"{id} not built");
        w.Enqueue(new AssignHousehold(w.Households.Single().Id, hut.Id));
        w.StepDays(1);
        return hut;
    }

    [Fact]
    public void A_gatherer_family_picks_the_bushes_around_its_hut_in_summer()
    {
        var w = NewWild(42, Patient.Value);
        // Summer, so the bushes bear fruit.
        w.StepTicks(SimTime.TicksPerSeason - w.Calendar.Tick % SimTime.TicksPerSeason);
        var start = w.Terrain!.Start;
        var bush = w.NearestNode(NodeKind.Bush, start, 40, Colonists(w)[0]) ?? throw new InvalidOperationException("no bush");
        var hut = HutWithFamily(w, "gatherer", bush);
        w.StepDays(5);
        Assert.True(hut.Stock.Get(Res(w, "food")).IsPositive, "the gatherer produced nothing (no carriers: it stays in the hut)");
        Assert.False(w.Gatherable(bush), "the nearest bush was not picked");
        TestKit.AssertInvariants(w);
    }

    [Fact]
    public void A_hunting_camp_never_takes_the_last_animals_of_a_herd()
    {
        var w = NewWild(42, Patient.Value);
        var start = w.Terrain!.Start;
        var deer = w.Animals.Where(a => a.Kind == FaunaKind.Deer).OrderBy(a => a.Pos.Manhattan(start)).First();
        var camp = HutWithFamily(w, "hunting_camp", deer.Home);
        int herdBefore = w.Animals.Count(a => a.Herd == deer.Herd);
        w.StepDays(40);
        int keep = w.Content.Balance.HuntKeepPerHerd;
        Assert.True(w.Animals.Count(a => a.Herd == deer.Herd) >= Math.Min(keep, herdBefore), "herd hunted out");
        Assert.True(camp.Stock.Get(Res(w, "food")).IsPositive || w.Animals.Count(a => a.Herd == deer.Herd) < herdBefore, "the camp hunted nothing");
        TestKit.AssertInvariants(w);
    }

    // ------------------------------------------------------------------ step 3 gate

    [Fact]
    public void The_scripted_band_reaches_the_first_winter_with_families_and_the_passive_one_does_not()
    {
        static World Play(string player)
        {
            var w = NewWild(42);
            var p = ScriptedPlayers.Create(player);
            p.Start(w);
            while (!w.Calendar.IsWinter)
            {
                w.StepTicks(SimTime.TicksPerDay / 4);
                p.Daily(w);
            }
            return w;
        }
        var rts = Play("rts");
        Assert.True(rts.Households.Count >= 3, $"rts reached winter with {rts.Households.Count} families");
        Assert.True(rts.StorageStock(Res(rts, "firewood")).WholeUnits >= 60, "no firewood for the winter");
        Assert.All(rts.Buildings.Where(b => !b.IsActive), b =>
            Assert.All(Enumerable.Range(0, rts.Content.ResourceCount), r => Assert.True(b.Stock.Get(r) <= b.Def.Cost[r], $"site {b.Def.Id} overfilled")));
        TestKit.AssertInvariants(rts);
        var passive = Play("passive");
        Assert.Empty(passive.Households);
        Assert.Empty(passive.Units.Where(u => u.IsColonist));
    }
}
