using Xunit.Abstractions;
using System.Diagnostics;

namespace Ironvale.Sim.Tests;

public class DeterminismTests
{
    [Fact]
    public void Same_seed_and_commands_give_the_same_state_after_5_years()
    {
        var a = TestKit.NewWorld(seed: 1234, opening: true);
        var b = TestKit.NewWorld(seed: 1234, opening: true);
        a.StepYears(5);
        b.StepYears(5);
        Assert.Equal(SaveSerializer.StateHash(a), SaveSerializer.StateHash(b));
    }

    [Fact]
    public void Different_seeds_diverge()
    {
        var a = TestKit.NewWorld(seed: 1, opening: true);
        var b = TestKit.NewWorld(seed: 2, opening: true);
        a.StepYears(2);
        b.StepYears(2);
        Assert.NotEqual(SaveSerializer.StateHash(a), SaveSerializer.StateHash(b));
    }
}

public class SaveLoadTests
{
    [Fact]
    public void Save_load_and_continue_equals_never_saving()
    {
        var straight = TestKit.NewWorld(seed: 99, opening: true);
        straight.StepYears(5);

        var first = TestKit.NewWorld(seed: 99, opening: true);
        first.StepYears(2);
        first.StepTicks(17);                               // mid-day, carriers mid-trip
        var bytes = SaveSerializer.Save(first);
        var loaded = SaveSerializer.Load(bytes, TestKit.Content);
        Assert.Empty(loaded.Warnings);
        var resumed = loaded.World;
        resumed.StepTicks(SimTime.TicksPerYear * 3 - 17);

        Assert.Equal(straight.Tick, resumed.Tick);
        Assert.Equal(SaveSerializer.StateHash(straight), SaveSerializer.StateHash(resumed));
        TestKit.AssertInvariants(resumed);
    }

    [Fact]
    public void Roundtrip_is_byte_identical()
    {
        var w = TestKit.NewWorld(seed: 5, opening: true);
        w.StepDays(200);
        var when = new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc);
        var once = SaveSerializer.Save(w, when);
        var twice = SaveSerializer.Save(SaveSerializer.Load(once, TestKit.Content).World, when);
        Assert.Equal(once, twice);
    }

    [Fact]
    public void Rejects_foreign_and_future_saves()
    {
        Assert.Throws<SaveException>(() => SaveSerializer.Load("{\"format\":\"other\"}"u8.ToArray(), TestKit.Content));
        Assert.Throws<SaveException>(() =>
            SaveSerializer.Load("{\"format\":\"ironvale-save\",\"saveVersion\":999}"u8.ToArray(), TestKit.Content));
    }

    [Fact]
    public void Telemetry_survives_a_save()
    {
        var w = TestKit.NewWorld(opening: true);
        w.StepDays(90);
        var loaded = SaveSerializer.Load(SaveSerializer.Save(w), TestKit.Content).World;
        Assert.Equal(w.Telemetry.Daily.Count(), loaded.Telemetry.Daily.Count());
        Assert.Equal(w.Telemetry.Monthly.Count, loaded.Telemetry.Monthly.Count);
    }
}

[Trait("Category", "Soak")]
public class SoakTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(42UL)]
    [InlineData(7UL)]
    public void Fifty_years_headless_without_deadlock_or_invalid_values(ulong seed)
    {
        var w = TestKit.NewWorld(seed, opening: true);
        w.CollectEvents = false;
        var sw = Stopwatch.StartNew();
        for (int day = 0; day < 50 * SimTime.DaysPerYear; day++)
        {
            w.StepDays(1);
            TestKit.AssertInvariants(w);          // conservation + no negatives, every single day
            Assert.False(w.Telemetry.Deadlocked, $"deadlock detected on day {w.Telemetry.DeadlockDay} ({w.Calendar})");
            Assert.False(w.IsCollapsed, $"population reached zero at {w.Calendar}");
        }
        sw.Stop();

        var food = TestKit.Res("food");
        output.WriteLine($"seed {seed}: 50 years in {sw.Elapsed.TotalSeconds:0.00}s, pop {w.Households.Count}, " +
                         $"food {w.StorageStock(food)}, hash {SaveSerializer.StateHashHex(w)}");
        // Every year the economy must have produced something (not frozen)
        Assert.All(w.Telemetry.Monthly.Chunk(12), year => Assert.True(year.Sum(m => m.Produced.Sum()) > 0));
    }
}
