using System.Linq;
using Godot;

namespace Ironvale.Game.Test;

/// <summary>
/// HUD v2 step 1 (docs/ui/HUD_v2_spec.md §7): every token, font and Type Variation of the single Theme side by side.
/// Only theme variations are used — no colour, font or StyleBox set on the nodes (token swatches read the theme).
/// The UI is drawn in a SubViewport at the requested size with the 1920×1080 base stretched into it (same as the
/// project's canvas_items stretch), so 1920×1080 and 1280×720 captures are exact whatever the window size.
/// Args after "--": --size=WxH (default 1920x1080) · --shot=PATH (save and quit).
/// </summary>
public partial class ThemeTest : Node
{
    private const string ThemePath = "res://ui/theme/main_theme.tres";
    private SubViewport _viewport = null!;
    private string? _shot;
    private int _frames;

    public override void _Ready()
    {
        var size = new Vector2I(1920, 1080);
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--size=") && arg[7..].Split('x') is [var w, var h] && int.TryParse(w, out int wi) && int.TryParse(h, out int hi))
                size = new Vector2I(wi, hi);
            if (arg.StartsWith("--shot=")) _shot = arg[7..];
        }

        _viewport = new SubViewport
        {
            Size = size,
            Size2DOverride = new Vector2I(1920, 1080),
            Size2DOverrideStretch = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Linear,
        };
        AddChild(_viewport);
        var ui = new Control { Theme = GD.Load<Theme>(ThemePath) };
        ui.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _viewport.AddChild(ui);
        Build(ui);

        var view = new TextureRect
        {
            Texture = _viewport.GetTexture(),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        view.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var layer = new CanvasLayer();
        AddChild(layer);
        layer.AddChild(view);
    }

    public override void _Process(double delta)
    {
        if (_shot is null || ++_frames != 30) return;
        if (OS.GetCmdlineUserArgs().Contains("--dump")) Dump(_viewport, 0);
        var image = _viewport.GetTexture().GetImage();
        GD.Print(image.SavePng(_shot) == Error.Ok ? $"SHOT {_shot} {image.GetSize()}" : "SHOT FAILED");
        GetTree().Quit();
    }

    private static void Dump(Node n, int depth)
    {
        if (depth > 6) return;
        if (n is Control c) GD.Print($"{new string(' ', depth * 2)}{c.GetType().Name} {c.ThemeTypeVariation} pos={c.GlobalPosition} size={c.Size} vis={c.Visible}");
        foreach (var child in n.GetChildren()) Dump(child, depth + 1);
    }

    // ------------------------------------------------------------------------------------------------ layout

    private static Label L(string text, string variation)
    {
        var l = new Label { Text = text, ThemeTypeVariation = variation };
        return l;
    }

    private static T V<T>(T node, string variation) where T : Control
    {
        node.ThemeTypeVariation = variation;
        return node;
    }

    private static PanelContainer Panel(string variation, Control child)
    {
        var p = new PanelContainer { ThemeTypeVariation = variation };
        if (variation.StartsWith("PanelMarker") || variation.StartsWith("Chip") || variation.StartsWith("PanelPill") || variation == "PanelWoodInset")
        {
            p.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            p.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        }
        p.AddChild(child);
        return p;
    }

    private static VBoxContainer Col(int gap, params Control[] children)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", gap);   // test layout only (spacing, not style)
        foreach (var c in children) box.AddChild(c);
        return box;
    }

    private static HBoxContainer Row(int gap, params Control[] children)
    {
        var box = new HBoxContainer();
        box.AddThemeConstantOverride("separation", gap);
        foreach (var c in children) box.AddChild(c);
        return box;
    }

    private static Control Caption(string name, Control sample) => Col(4, L(name, "LabelCaption"), sample);

    private void Build(Control ui)
    {
        var world = new ColorRect { Color = new Color("7d8a5c") };   // stands in for the 3D world behind the HUD
        world.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        ui.AddChild(world);

        var root = new VBoxContainer();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 0);
        ui.AddChild(root);

        // ---- top bar (BarWood, 72)
        var top = V(new PanelContainer { CustomMinimumSize = new Vector2(0, 72) }, "BarWood");
        var topRow = Row(16,
            Col(0, L("Ironvale", "LabelTitleOnWood"), L("Aldeia · ano 1", "LabelOnWoodDim")),
            V(new VSeparator(), "SeparatorWood"),
            Panel("PanelPillOnWood", Row(8, L("24", "LabelValueOnWood"), L("aldeões · 6 famílias", "LabelOnWoodDim"))),
            V(new VSeparator(), "SeparatorWood"),
            Chip("Madeira", "120", "▲ +4/dia", "LabelPositive", alert: false),
            Chip("Lenha", "18", "▼ −6/dia", "LabelNegative", alert: true),
            new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill },
            Col(2, L("Outono · dia 12", "LabelOnWood"),
                V(new ProgressBar { Value = 40, ShowPercentage = false, CustomMinimumSize = new Vector2(220, 8) }, "ProgressOnWood")),
            Panel("PanelWoodInset", Row(4, Wood("||", "ButtonWoodAlert"), Wood("1x", "ButtonWood", on: true), Wood("2x", "ButtonWood"),
                Wood("4x", "ButtonWood"), Wood("8x", "ButtonWood"))));
        Center(topRow);
        top.AddChild(topRow);
        root.AddChild(top);

        // ---- body: four columns of samples
        var body = new MarginContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        foreach (var side in new[] { "left", "top", "right", "bottom" }) body.AddThemeConstantOverride($"margin_{side}", 16);
        var columns = Row(16);
        body.AddChild(columns);
        root.AddChild(body);

        columns.AddChild(Col(12, Caption("PanelParchment · TitleXL · Quote · Body · Value · Caption", Panel("PanelParchment", Col(8,
                L("Próximo objetivo", "LabelTitleXL"),
                L("“A lenha não chega ao inverno, senhor.”", "LabelQuote"),
                L("Corte lenha suficiente para o inverno: 150 unidades.", "LabelBody"),
                Row(8, V(new ProgressBar { Value = 53, ShowPercentage = false, CustomMinimumSize = new Vector2(200, 10), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter }, "ProgressFire"),
                    L("80/150", "LabelValue")),
                L("PRAZO · 24 DIAS", "LabelCaption")))),
            Caption("PanelReeveHeader + PanelDecree (cartão do reeve)", Panel("PanelParchmentFlush", Col(0,
                Panel("PanelReeveHeader", Row(10, L("O reeve sugere", "LabelOnWood"))),
                Panel("PanelParchmentInner", Col(8,
                    L("“Mandaste lenhadores três vezes. Que eu cuide disso?”", "LabelQuote"),
                    Panel("PanelDecree", L("Manter lenha entre 80 e 200", "LabelBodyBold")),
                    Row(8, V(new Button { Text = "Criar decreto", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }, "ButtonPrimary"),
                        V(new Button { Text = "Agora não" }, "ButtonSecondary"), V(new Button { Text = "Nunca" }, "ButtonGhost"))))))),
            Caption("TooltipPanel", Panel("TooltipPanel", Col(6, L("Lenha", "LabelTitle"),
                Row(16, Col(2, L("PRODUÇÃO", "LabelCaption"), L("Lenhador +8/dia", "LabelPositive")),
                    Col(2, L("CONSUMO", "LabelCaption"), L("Casas −14/dia", "LabelNegative"))),
                L("Previsão: acaba em 3 dias", "LabelWarning"), L("Alt fixa este tooltip", "LabelCaption"))))));

        columns.AddChild(Col(12,
            Caption("ParchmentFlush + ParchmentHeader + Tab + Cell + WarningBox", Panel("PanelParchmentFlush", Col(0,
                Panel("PanelParchmentHeader", Row(12, Col(0, L("Lenhador", "LabelTitleOnWood"), L("Encosta norte", "LabelOnWoodDim")),
                    new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }, V(new Button { Text = "✕", CustomMinimumSize = new Vector2(32, 32) }, "ButtonWoodOutline"))),
                Panel("PanelTabStrip", Tabs()),
                Panel("PanelParchmentInner", Col(8,
                    Row(8, Panel("PanelCell", Col(2, L("PRODUÇÃO", "LabelCaption"), L("12/dia", "LabelValue"))),
                        Panel("PanelCell", Col(2, L("ESTOQUE", "LabelCaption"), L("34", "LabelValue"))),
                        Panel("PanelCell", Col(2, L("EFICIÊNCIA", "LabelCaption"), L("62%", "LabelValue")))),
                    Panel("PanelWarningBox", Col(6, L("Por que 62%?", "LabelBodyBold"), L("Machados desgastados −25%", "LabelBody"),
                        L("Trajeto longo −13%", "LabelBody"), V(new Button { Text = "Ir ao ferreiro" }, "ButtonSecondary"))),
                    V(new HSeparator(), "SeparatorParchment"),
                    L("Trabalhadores 2/2", "LabelSecondary")))))),
            Caption("PanelAlertCard + ButtonSmall + ButtonGhost", Panel("PanelAlertCard", Row(10,
                Col(0, L("Pouca lenha", "LabelBodyBold"), L("Inverno em 24 dias", "LabelSecondary")),
                new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill },
                V(new Button { Text = "Ir" }, "ButtonSmall"), V(new Button { Text = "✕" }, "ButtonGhost"))))));

        columns.AddChild(Col(12,
            Caption("Primary · Secondary · Ghost · Small (+ desabilitado)", Col(8,
                Row(8, V(new Button { Text = "Criar decreto" }, "ButtonPrimary"), V(new Button { Text = "Criar decreto", Disabled = true }, "ButtonPrimary")),
                Row(8, V(new Button { Text = "Agora não" }, "ButtonSecondary"), V(new Button { Text = "Agora não", Disabled = true }, "ButtonSecondary")),
                Row(8, V(new Button { Text = "Nunca" }, "ButtonGhost"), V(new Button { Text = "Ir" }, "ButtonSmall"), V(new Button { Text = "Ir", Disabled = true }, "ButtonSmall")))),
            Caption("ButtonOverlay (desligado / ligado)", Row(8, Overlay("Fluxo\nF1", false), Overlay("Trab.\nF3", true), Overlay("Fert.\nF5", false, disabled: true))),
            Caption("Progress: Brass · Fire · Positive", Col(6,
                V(new ProgressBar { Value = 70, ShowPercentage = false, CustomMinimumSize = new Vector2(260, 10) }, "ProgressBrass"),
                V(new ProgressBar { Value = 45, ShowPercentage = false, CustomMinimumSize = new Vector2(260, 10) }, "ProgressFire"),
                V(new ProgressBar { Value = 85, ShowPercentage = false, CustomMinimumSize = new Vector2(260, 10) }, "ProgressPositive"))),
            Caption("PanelMarker · Warning · Positive", Col(6,
                Panel("PanelMarker", L("Lenhador", "LabelBodyBold")),
                Panel("PanelMarkerWarning", L("Machados gastos", "LabelBodyBold")),
                Panel("PanelMarkerPositive", L("Colheita pronta", "LabelBodyBold")))),
            Caption("PanelDark (faixa de modo)", Panel("PanelDark", Col(2, L("Posicionando Lenhador", "LabelOnWood"), L("Clique direito cancela", "LabelOnWoodDim"))))));

        columns.AddChild(Col(12,
            Caption("Rótulos semânticos", Col(4, L("LabelPositive ▲ +4/dia", "LabelPositive"), L("LabelWarning ◆ aviso", "LabelWarning"),
                L("LabelNegative ▼ falta", "LabelNegative"), L("LabelInfo · reeve", "LabelInfo"), L("LabelSecondary · rótulo", "LabelSecondary"),
                L("LabelCaption · 12 PX MÍNIMO", "LabelCaption"), L("LabelTitle · Outono", "LabelTitle"))),
            Caption("Tokens (Theme › Tokens/colors)", Swatches())));

        // ---- bottom bar (BarWoodBottom, 96) with the six categories
        var bottom = V(new PanelContainer { CustomMinimumSize = new Vector2(0, 96) }, "BarWoodBottom");
        var cats = Row(8);
        cats.Alignment = BoxContainer.AlignmentMode.Center;
        string[] names = { "Habitação\nH", "Comida\nC", "Recursos\nR", "Ofícios\nO", "Armazéns\nA", "Estradas\nE" };
        for (int i = 0; i < names.Length; i++)
            cats.AddChild(V(new Button { Text = names[i], ToggleMode = true, ButtonPressed = i == 2, CustomMinimumSize = new Vector2(108, 76) }, "ButtonCategory"));
        var bottomRow = Row(16, V(new Button { Text = "Livro de contas" }, "ButtonWoodOutline"), new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill },
            cats, new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill },
            Col(2, L("CAPACIDADE ADM.", "LabelOnWoodDim"), L("3/4", "LabelValueOnWood")));
        Center(bottomRow);
        foreach (var c in cats.GetChildren()) ((Control)c).SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        bottom.AddChild(bottomRow);
        root.AddChild(bottom);
    }

    /// <summary>Bar rows: children keep their own height, centred on the bar.</summary>
    private static void Center(HBoxContainer row)
    {
        foreach (var c in row.GetChildren())
            if (c is Control control && control is not VSeparator) control.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
    }

    private static Control Chip(string name, string value, string trend, string trendVariation, bool alert) =>
        Panel(alert ? "ChipResourceAlert" : "ChipResource", Row(8, L(name, "LabelBodyBold"), L(value, "LabelValue"), L(trend, trendVariation)));

    private static Button Wood(string text, string variation, bool on = false) =>
        V(new Button { Text = text, ToggleMode = true, ButtonPressed = on, CustomMinimumSize = new Vector2(40, 40) }, variation);

    private static Button Overlay(string text, bool on, bool disabled = false) =>
        V(new Button { Text = text, ToggleMode = true, ButtonPressed = on, Disabled = disabled, CustomMinimumSize = new Vector2(52, 52) }, "ButtonOverlay");

    private static TabBar Tabs()
    {
        var tabs = V(new TabBar(), "Tab");
        tabs.AddTab("Visão geral");
        tabs.AddTab("Trabalhadores");
        tabs.AddTab("Histórico");
        return tabs;
    }

    private Control Swatches()
    {
        var theme = GD.Load<Theme>(ThemePath);
        var grid = new GridContainer { Columns = 4 };
        foreach (var name in theme.GetColorList("Tokens").OrderBy(n => n))
        {
            grid.AddChild(new ColorRect { Color = theme.GetColor(name, "Tokens"), CustomMinimumSize = new Vector2(28, 16) });
            grid.AddChild(L(name, "LabelCaption"));
        }
        return Panel("PanelParchment", grid);
    }
}
