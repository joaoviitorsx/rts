using Godot;

namespace Ironvale.Game.UI;

/// <summary>
/// Icons by logical id (UI_UX_guide §8.1 rule 9) — code asks for "res.food", never for a file path.
/// Empty until the game-icons.net set is added (F3); Get() returns null and components show text only.
/// </summary>
[GlobalClass]
public partial class IconRegistry : Resource
{
    private const string Path = "res://ui/icons/icon_registry.tres";
    private static IconRegistry? _instance;

    [Export] public Godot.Collections.Dictionary<string, Texture2D> Icons { get; set; } = new();

    public static Texture2D? Get(string id)
    {
        _instance ??= ResourceLoader.Exists(Path) ? GD.Load<IconRegistry>(Path) : new IconRegistry();
        return _instance.Icons.TryGetValue(id, out var t) ? t : null;
    }
}
