using System;
using System.Linq;
using Godot;
using Ironvale.Sim.Commands;

namespace Ironvale.Game.UI;

/// <summary>
/// Selected building (guide §3.2): one-line status with cause, workers, stock, recipe, assign/remove.
/// Rebuilt only when its data changes.
/// </summary>
public partial class BuildingPanel : PanelContainer
{
    public event Action? Closed;
    public Action<SimCommand> Send { get; set; } = _ => { };

    private VBoxContainer _body = null!;
    private string _signature = "";

    public override void _Ready()
    {
        ThemeTypeVariation = "PanelPrimary";
        MouseFilter = MouseFilterEnum.Stop;
        _body = new VBoxContainer();
        AddChild(_body);
    }

    public void Bind(BuildingSnap b, UiSnapshot snap)
    {
        string sig = $"{b.Id}|{b.Status}|{b.BuildProgressDays}|{b.RecipeId}|{string.Join(',', b.Slots.Select(s => s.HouseholdId))}|" +
                     $"{b.StockTotal}|{b.ExpectedHarvest}|{snap.Households.Count}|{string.Join(',', snap.Households.Select(h => h.JobBuildingId))}";
        if (sig == _signature) return;
        _signature = sig;
        UiNodes.Clear(_body);

        _body.AddChild(UiNodes.Header($"{b.Name} #{b.Id}", () => Closed?.Invoke()));
        _body.AddChild(UiNodes.Label(UiText.T("building.status." + b.Status, b.BuildProgressDays, b.BuildDays),
            b.Status is "no_workers" or "full" ? "WarningLabel" : null, wrap: true));

        if (!b.Active)
        {
            _body.AddChild(UiNodes.Button(UiText.T("building.cancel"), () => Send(new CancelConstruction(b.Id))));
            return;
        }
        if (b.IsHousing)
            _body.AddChild(UiNodes.Label(UiText.T("building.residents", b.Residents.Count, b.HousingCapacity,
                string.Join(", ", b.Residents)), wrap: true));

        if (b.Recipes.Count > 1)
        {
            var row = new HBoxContainer();
            row.AddChild(UiNodes.Label(UiText.T("building.produces"), "SecondaryLabel"));
            var pick = new OptionButton { FocusMode = FocusModeEnum.None };
            foreach (var r in b.Recipes) pick.AddItem(r.Name);
            pick.Select(b.Recipes.ToList().FindIndex(r => r.Id == b.RecipeId));
            pick.ItemSelected += i => Send(new SetRecipe(b.Id, b.Recipes[(int)i].Id));
            row.AddChild(pick);
            _body.AddChild(row);
        }

        if (b.Capacity > 0)
        {
            _body.AddChild(UiNodes.Label(UiText.T("building.stock", b.StockTotal, b.Capacity), "SecondaryLabel"));
            foreach (var (name, amount) in b.Stock) _body.AddChild(UiNodes.Label($"   {name}: {amount}"));
            if (b.SeasonalRecipe && b.ExpectedHarvest > 0)
                _body.AddChild(UiNodes.Label(UiText.T("building.harvest", b.ExpectedHarvest), "SecondaryLabel"));
        }

        if (b.Slots.Count == 0) return;
        _body.AddChild(new HSeparator());
        int used = b.Slots.Count(s => s.HouseholdId != 0);
        _body.AddChild(UiNodes.Label(UiText.T(b.IsStorage ? "building.carriers" : "building.workers", used, b.Slots.Count), "SecondaryLabel"));
        foreach (var slot in b.Slots)
        {
            var row = new HBoxContainer();
            row.AddChild(UiNodes.Label(slot.HouseholdId == 0 ? UiText.T("building.slot_free")
                : $"{slot.Name} — {UiText.T("source." + slot.Source)}", expand: true));
            if (slot.HouseholdId != 0)
            {
                int id = slot.HouseholdId;
                row.AddChild(UiNodes.Button(UiText.T("building.remove"), () => Send(new UnassignHousehold(id))));
            }
            _body.AddChild(row);
        }
        if (used < b.Slots.Count)
        {
            var row = new HBoxContainer();
            var pick = new OptionButton { FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            foreach (var h in snap.Households.Where(h => h.JobBuildingId != b.Id))
                pick.AddItem($"{h.Name} ({(h.JobBuildingId == 0 ? UiText.T("building.no_job") : h.JobName)})", h.Id);
            row.AddChild(pick);
            row.AddChild(UiNodes.Button(UiText.T("building.assign"), () =>
            {
                if (pick.ItemCount > 0) Send(new AssignHousehold(pick.GetSelectedId(), b.Id));
            }));
            _body.AddChild(row);
        }
    }
}
