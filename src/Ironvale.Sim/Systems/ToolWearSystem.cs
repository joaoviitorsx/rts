namespace Ironvale.Sim.Systems;

/// <summary>Tools wear with hours worked; a worn-out tool is replaced from storage if available.</summary>
public sealed class ToolWearSystem : ISimSystem
{
    public const string ToolsId = "tools";

    public string Name => "tool_wear";
    public Phase Phase => Phase.Consumption;
    public Frequency Frequency => Frequency.Daily;

    public void Run(World w, in Calendar cal)
    {
        var bal = w.Content.Balance;
        int tools = w.Content.Resource(ToolsId).Index;
        var oneTool = Qty.Units(1);

        foreach (var h in w.Households)
        {
            if (h.ToolHoursToday == 0) continue;
            int wear = bal.ToolWearPerWorkDayPermille * h.ToolHoursToday / SimTime.HoursPerDay;
            h.ToolCondition = Math.Max(0, h.ToolCondition - Math.Max(1, wear));
            h.ToolHoursToday = 0;

            if (h.ToolCondition == 0 && w.TryTakeExactFromStorages(tools, oneTool, w.HomeCellOf(h)))
            {
                w.RecordConsumed(tools, oneTool, fromStorage: true);
                h.ToolCondition = Permille.One;
            }
        }
    }
}
