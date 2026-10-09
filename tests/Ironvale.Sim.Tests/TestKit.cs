namespace Ironvale.Sim.Tests;

/// <summary>Shared fixtures: real content from godot/data and invariant checks.</summary>
internal static class TestKit
{
    private static readonly Lazy<(ContentDb Content, ScenarioDef Scenario)> Mvp =
        new(() => DataPaths.LoadWithScenario(DataPaths.FindDataDirectory(), "mvp_start"));

    public static ContentDb Content => Mvp.Value.Content;
    public static ScenarioDef Scenario => Mvp.Value.Scenario;

    public static World NewWorld(ulong seed = 42, bool opening = false)
    {
        var w = World.Create(Content, Scenario, seed);
        w.CollectEvents = true;
        if (opening) MvpOpening.Apply(w);
        return w;
    }

    public static int Res(string id) => Content.Resource(id).Index;

    /// <summary>Real content with balance.json fields overridden (e.g. ("autoBuilders", "0")).</summary>
    public static ContentDb ContentWith(params (string Key, string Json)[] balance)
    {
        string dir = DataPaths.FindDataDirectory();
        var files = ContentLoader.RequiredFiles.ToDictionary(f => f, f => File.ReadAllText(Path.Combine(dir, f)));
        var text = files[ContentLoader.BalanceFile];
        foreach (var (key, json) in balance)
        {
            var m = System.Text.RegularExpressions.Regex.Match(text, $"\"{key}\"\\s*:\\s*[^,\\n}}]+");
            Assert.True(m.Success, $"balance key {key} not found");
            text = text.Remove(m.Index, m.Length).Insert(m.Index, $"\"{key}\": {json}");
        }
        files[ContentLoader.BalanceFile] = text;
        return ContentLoader.Load(files);
    }

    public static World NewWorld(ContentDb content, ulong seed = 42)
    {
        var scenario = ContentLoader.LoadScenario(
            File.ReadAllText(Path.Combine(DataPaths.FindDataDirectory(), DataPaths.ScenarioDir, "mvp_start.json")), content);
        var w = World.Create(content, scenario, seed);
        w.CollectEvents = true;
        return w;
    }

    public static Building AddActive(World w, string defId, Cell origin) =>
        w.AddBuilding(Content.Building(defId), origin, 0, active: true);

    /// <summary>Conservation per resource + nothing negative + references consistent.</summary>
    public static void AssertInvariants(World w)
    {
        int n = w.Content.ResourceCount;
        for (int r = 0; r < n; r++)
        {
            long actual = 0;
            foreach (var b in w.Buildings)
            {
                long amount = b.Stock.Get(r).Milli, reserved = b.Stock.Reserved(r).Milli;
                Assert.True(amount >= 0, $"{w.DescribeBuilding(b)} negative {w.Content.Resources[r].Id}: {amount}");
                Assert.True(reserved >= 0 && reserved <= amount, $"{w.DescribeBuilding(b)} bad reservation");
                actual += amount;
            }
            foreach (var s in w.Shipments)
            {
                Assert.True(s.Amount.IsPositive, "empty shipment");
                if (s.Resource == r) actual += s.Amount.Milli;
            }
            Assert.True(w.Ledger.ExpectedOf(r).Milli == actual,
                $"conservation broken for {w.Content.Resources[r].Id} at {w.Calendar}: ledger {w.Ledger.ExpectedOf(r)} vs world {new Qty(actual)}");
        }
        foreach (var b in w.Buildings)
        {
            Assert.True(b.Stock.Incoming.Milli >= 0, "negative incoming");
            Assert.True(b.Stock.Total <= b.Stock.Capacity || b.Stock.Capacity.IsZero, $"{w.DescribeBuilding(b)} over capacity");
        }
        foreach (var h in w.Households)
        {
            Assert.InRange(h.ToolCondition, 0, Permille.One);
            Assert.InRange(h.ProductivityPermille, 0, Permille.One);
            Assert.True(h.FoodDeficitDays >= 0 && h.ColdDeficitDays >= 0);
        }
        foreach (var c in w.Carriers)
            if (c.ShipmentId != 0) Assert.NotNull(w.GetShipment(c.ShipmentId));
    }
}
