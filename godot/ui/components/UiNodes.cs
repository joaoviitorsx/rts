using System;
using Godot;

namespace Ironvale.Game.UI;

/// <summary>Small factory helpers so components stay readable. No styling here: only theme type variations.</summary>
public static class UiNodes
{
    public static Label Label(string text, string? variation = null, bool expand = false, bool wrap = false)
    {
        var l = new Label { Text = text };
        if (variation is not null) l.ThemeTypeVariation = variation;
        if (expand) l.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        if (wrap)
        {
            l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            l.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        }
        return l;
    }

    public static Button Button(string text, Action onPressed, string? tooltip = null)
    {
        var b = new Button { Text = text, FocusMode = Control.FocusModeEnum.None, TooltipText = tooltip ?? "" };
        b.Pressed += onPressed;
        return b;
    }

    public static Control Spacer() => new Control
    {
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    public static void Clear(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            node.RemoveChild(child);
            child.QueueFree();
        }
    }

    /// <summary>Panel header: title + close button (top-right, guide heuristic 4).</summary>
    public static HBoxContainer Header(string title, Action onClose)
    {
        var row = new HBoxContainer();
        row.AddChild(Label(title, "HeaderLabel", expand: true));
        row.AddChild(Button("✕", onClose, UiText.T("ui.close")));
        return row;
    }
}
