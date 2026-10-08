namespace Ironvale.Sim.Content;

/// <summary>Locates godot/data from tools running outside Godot (tests, CLI).</summary>
public static class DataPaths
{
    public const string ScenarioDir = "scenarios";

    public static string FindDataDirectory(string? start = null)
    {
        var dir = new DirectoryInfo(start ?? AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "godot", "data");
            if (File.Exists(Path.Combine(candidate, ContentLoader.ResourcesFile))) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("godot/data not found above " + (start ?? AppContext.BaseDirectory));
    }

    public static (ContentDb Content, ScenarioDef Scenario) LoadWithScenario(string dataDir, string scenarioId)
    {
        var content = ContentLoader.LoadFromDirectory(dataDir);
        string file = Path.Combine(dataDir, ScenarioDir, scenarioId + ".json");
        var scenario = ContentLoader.LoadScenario(File.ReadAllText(file), content, Path.GetFileName(file));
        return (content, scenario);
    }
}
