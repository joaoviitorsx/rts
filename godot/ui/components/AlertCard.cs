using System;
using Godot;

namespace Ironvale.Game.UI;

/// <summary>Alert in the left stack (guide §3.4): severity · text · [Ir] [✕]. Severity is text + colour, never colour alone.</summary>
public partial class AlertCard : PanelContainer
{
    public event Action? Dismissed;
    public event Action? Go;

    public string Key { get; private set; } = "";
    private Label _text = null!;
    private Label _glyph = null!;
    private Button _go = null!;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        var row = new HBoxContainer();
        AddChild(row);
        _glyph = UiNodes.Label("");
        row.AddChild(_glyph);
        _text = UiNodes.Label("", wrap: true);
        row.AddChild(_text);
        _go = UiNodes.Button(UiText.T("ui.alert.go"), () => Go?.Invoke());
        row.AddChild(_go);
        row.AddChild(UiNodes.Button("✕", () => Dismissed?.Invoke(), UiText.T("ui.alert.dismiss")));
    }

    public void Bind(string key, AlertSeverity severity, string text, bool canGo)
    {
        Key = key;
        _text.Text = text;
        _go.Visible = canGo;
        (ThemeTypeVariation, _glyph.Text) = severity switch
        {
            AlertSeverity.Critical => ("AlertCritical", "‼"),
            AlertSeverity.Warning => ("AlertWarning", "⚠"),
            _ => ("AlertInfo", "ℹ"),
        };
    }
}
