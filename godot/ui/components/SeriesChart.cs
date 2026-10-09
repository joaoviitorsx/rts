using System.Collections.Generic;
using Godot;

namespace Ironvale.Game.UI;

/// <summary>Minimal line chart; colours and font size come from the theme ("SeriesChart" type).</summary>
public partial class SeriesChart : Control
{
    public sealed record Series(string Name, string ColorName, IReadOnlyList<double> Values);

    private IReadOnlyList<Series> _series = new List<Series>();

    public override void _Ready() => ThemeTypeVariation = "SeriesChart";

    public void SetSeries(IReadOnlyList<Series> series)
    {
        _series = series;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Size);
        DrawRect(rect, GetThemeColor("background", "SeriesChart"));
        var font = GetThemeDefaultFont();
        int fontSize = GetThemeFontSize("font_size", "SeriesChart");
        float legendY = fontSize + 2;
        foreach (var s in _series)
        {
            if (s.Values.Count < 2) continue;
            var color = GetThemeColor(s.ColorName, "SeriesChart");
            double max = 0;
            foreach (var v in s.Values) max = System.Math.Max(max, v);
            if (max <= 0) max = 1;
            var points = new Vector2[s.Values.Count];
            for (int i = 0; i < s.Values.Count; i++)
                points[i] = new Vector2(rect.Size.X * i / (s.Values.Count - 1),
                    rect.Size.Y - 4 - (float)(s.Values[i] / max) * (rect.Size.Y - legendY * 3));
            DrawPolyline(points, color, 2f, true);
            DrawString(font, new Vector2(6, legendY), $"{s.Name} ({UiText.T("debug.max", max.ToString("0.#"))})",
                HorizontalAlignment.Left, -1, fontSize, color);
            legendY += fontSize + 2;
        }
    }
}
