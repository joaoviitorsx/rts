using Godot;

namespace Ironvale.Game.UI;

/// <summary>
/// HUD v2 (docs/ui/HUD_v2_spec.md §3): the zones of the screen, laid out in hud_v2.tscn with anchors and containers
/// only — top wood bar (settlement · population · resources · season · speed), overlay rail, left column (objective,
/// reeve, alerts, feed), right panel (building), build tray + hover card, mode banner, bottom wood bar (ledger and
/// options · categories · CA and reeve). Plan §7 step 2: structure only; components fill the slots in step 3.
/// Enabled in the game with the dev flag --hud=v2 until it replaces the 2A HUD.
/// </summary>
public partial class HudV2 : CanvasLayer
{
    public Control Root { get; private set; } = null!;
    public HBoxContainer Settlement { get; private set; } = null!;
    public HBoxContainer Population { get; private set; } = null!;
    public HBoxContainer Resources { get; private set; } = null!;
    public VBoxContainer Season { get; private set; } = null!;
    public HBoxContainer Speed { get; private set; } = null!;
    public VBoxContainer OverlayButtons { get; private set; } = null!;
    public VBoxContainer LeftColumn { get; private set; } = null!;
    public VBoxContainer RightPanel { get; private set; } = null!;
    public PanelContainer ModeBanner { get; private set; } = null!;
    public PanelContainer BuildTray { get; private set; } = null!;
    public PanelContainer BuildHover { get; private set; } = null!;
    public HBoxContainer BottomLeft { get; private set; } = null!;
    public HBoxContainer Categories { get; private set; } = null!;
    public HBoxContainer BottomRight { get; private set; } = null!;

    public override void _Ready()
    {
        Root = GetNode<Control>("Root");
        Settlement = GetNode<HBoxContainer>("Root/TopBar/Row/Settlement");
        Population = GetNode<HBoxContainer>("Root/TopBar/Row/Population");
        Resources = GetNode<HBoxContainer>("Root/TopBar/Row/Resources");
        Season = GetNode<VBoxContainer>("Root/TopBar/Row/Season");
        Speed = GetNode<HBoxContainer>("Root/TopBar/Row/Speed");
        OverlayButtons = GetNode<VBoxContainer>("Root/OverlayRail/Buttons");
        LeftColumn = GetNode<VBoxContainer>("Root/LeftColumn");
        RightPanel = GetNode<VBoxContainer>("Root/RightPanel");
        ModeBanner = GetNode<PanelContainer>("Root/ModeBanner");
        BuildTray = GetNode<PanelContainer>("Root/BuildTray");
        BuildHover = GetNode<PanelContainer>("Root/BuildHover");
        BottomLeft = GetNode<HBoxContainer>("Root/BottomBar/Row/Left");
        Categories = GetNode<HBoxContainer>("Root/BottomBar/Row/Categories");
        BottomRight = GetNode<HBoxContainer>("Root/BottomBar/Row/Right");
        UiSettings.RegisterTheme(Root.Theme);
    }

    /// <summary>In the game: UI scale from the settings (and the font floor) on the real window.</summary>
    public void Init() => UiSettings.Load(GetTree().Root);

    /// <summary>
    /// Dev view of the empty structure (step 2 captures): every zone gets a caption with its name and size, so the
    /// anchors can be checked at several window sizes. Uses theme variations only; no game data.
    /// </summary>
    public void ShowZones()
    {
        Settlement.AddChild(Caption("Brasão + vila", "LabelOnWood"));
        Population.AddChild(Caption("População", "LabelOnWood"));
        Resources.AddChild(Caption("Recursos (3 grupos)", "LabelOnWood"));
        Season.AddChild(Caption("Estação + linha do tempo", "LabelOnWood"));
        Speed.AddChild(Caption("Velocidade", "LabelOnWood"));
        foreach (var key in new[] { "F1", "F2", "F3", "F4", "F5" })
            OverlayButtons.AddChild(new Button { Text = key, ThemeTypeVariation = "ButtonOverlay", CustomMinimumSize = new Vector2(52, 52), Disabled = true });
        foreach (var zone in new[] { "Próximo objetivo", "O reeve sugere", "Alertas (≤ 3)", "Na vila" })
            LeftColumn.AddChild(Zone(zone, "coluna esquerda · 344 px"));
        RightPanel.AddChild(Zone("Painel do edifício", "424 px · abas"));
        BottomLeft.AddChild(Caption("Livro de contas · Opções", "LabelOnWood"));
        Categories.AddChild(Caption("6 categorias de construção (108 × 76)", "LabelOnWood"));
        BottomRight.AddChild(Caption("Capacidade Adm. · Reeve", "LabelOnWood"));
        ModeBanner.AddChild(Caption("Faixa de modo (Posicionando X / Mapa: Y)", "LabelOnWood"));
        ModeBanner.Visible = true;
        BuildTray.AddChild(Caption("Bandeja de construção (112 px acima da base)", "LabelBodyBold"));
        BuildTray.Visible = true;
    }

    private static Label Caption(string text, string variation) => new() { Text = text, ThemeTypeVariation = variation };

    private static PanelContainer Zone(string title, string detail)
    {
        var panel = new PanelContainer { ThemeTypeVariation = "PanelParchment" };
        var col = new VBoxContainer { ThemeTypeVariation = "ColumnTight" };
        col.AddChild(Caption(title, "LabelTitle"));
        col.AddChild(Caption(detail, "LabelCaption"));
        panel.AddChild(col);
        return panel;
    }
}
