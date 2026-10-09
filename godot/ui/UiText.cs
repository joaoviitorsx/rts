using Godot;

namespace Ironvale.Game.UI;

/// <summary>All UI text goes through translation keys (UI_UX_guide §8.1 rule 8).</summary>
public static class UiText
{
    public static string T(string key) => TranslationServer.Translate(key);

    public static string T(string key, params object[] args) => string.Format(TranslationServer.Translate(key), args);
}
