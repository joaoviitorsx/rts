using System.Collections.Generic;
using Godot;

namespace Ironvale.Game.UI;

/// <summary>Minimal line chart: each series is normalised to its own max (labels show the max).</summary>
public partial class SeriesChart : Control
{
    public sealed record Series(string Name, Color Color, IReadOnlyList<double> Values);

    private IReadOnlyList<Series> _series = new List<Series>();

    public void SetSeries(IReadOnlyList<Series> series)
    {
        _series = series;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Size);
        DrawRect(rect, new Color(0, 0, 0, 0.25f));
        var font = ThemeDB.FallbackFont;
        float legendY = 14;
        foreach (var s in _series)
        {
            if (s.Values.Count < 2) continue;
            double max = 0;
            foreach (var v in s.Values) max = System.Math.Max(max, v);
            if (max <= 0) max = 1;
            var points = new Vector2[s.Values.Count];
            for (int i = 0; i < s.Values.Count; i++)
            {
                float x = rect.Size.X * i / (s.Values.Count - 1);
                float y = rect.Size.Y - 4 - (float)(s.Values[i] / max) * (rect.Size.Y - 22);
                points[i] = new Vector2(x, y);
            }
            DrawPolyline(points, s.Color, 2f, true);
            DrawString(font, new Vector2(6, legendY), $"{s.Name} (máx {max:0.#})", HorizontalAlignment.Left, -1, 12, s.Color);
            legendY += 14;
        }
    }
}
