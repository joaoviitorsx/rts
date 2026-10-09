namespace Ironvale.Sim.Tests;

/// <summary>
/// Frozen simulation outcomes of the playtest-2A candidate (tag playtest-2A-candidate). Branches that only touch the
/// view/UI must keep these hashes. Update them only on purpose, when a simulation rule or balance value changes.
/// </summary>
public class GoldenHashTests
{
    [Theory]
    [InlineData("optimal", 42UL, "0287c46541624b8f")]
    [InlineData("optimal", 7UL, "156f8c3d87bbc807")]
    [InlineData("naive", 123UL, "5281c0541cd1226b")]
    public void Ten_years_of_a_scripted_player_match_the_candidate(string player, ulong seed, string expected)
    {
        var w = World.Create(TestKit.Content, TestKit.Scenario, seed);
        var p = ScriptedPlayers.Create(player);
        p.Start(w);
        for (int year = 0; year < 10 && !w.IsCollapsed; year++)   // same loop as the CLI (stops after a collapsed year)
            for (int d = 0; d < SimTime.DaysPerYear; d++)
            {
                w.StepDays(1);
                p.Daily(w);
            }
        Assert.Equal(expected, SaveSerializer.StateHashHex(w));
    }
}
