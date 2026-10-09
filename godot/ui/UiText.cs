using Godot;
using Ironvale.Sim.Content;

namespace Ironvale.Game.UI;

/// <summary>All UI text goes through translation keys (UI_UX_guide §8.1 rule 8).</summary>
public static class UiText
{
    public static string T(string key) => TranslationServer.Translate(key);

    public static string T(string key, params object[] args) => string.Format(TranslationServer.Translate(key), args);

    /// <summary>
    /// Content names by id ("res.wood", "bld.house", "rcp.chop_wood" in ui.csv); the sim's own (Portuguese) name is the
    /// fallback, so new content shows up before it is translated (P5).
    /// </summary>
    public static string Content(string prefix, string id, string fallback)
    {
        string key = $"{prefix}.{id}";
        string text = TranslationServer.Translate(key);
        return text == key ? fallback : text;
    }

    public static string Res(ResourceDef r) => Content("res", r.Id, r.Name);
    public static string Bld(BuildingDef b) => Content("bld", b.Id, b.Name);
    public static string Rcp(RecipeDef r) => Content("rcp", r.Id, r.Name);
}
