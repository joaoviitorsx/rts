using System;
using Godot;
using Ironvale.Sim;
using Ironvale.Sim.Population;

namespace Ironvale.Game.Visual;

/// <summary>
/// Cozy touches on one villager (view only, reads the world): emotion balloon over the head, the load it carries,
/// and varied idles at home. Never changes the simulation.
/// </summary>
public sealed class VillagerLook
{
    public enum Emote { None, Hunger, Cold, Tired, Happy }

    private static readonly string[] IdleClips = { "idle", "talk", "idle", "sit" };
    private static readonly Texture2D?[] EmoteTextures = new Texture2D?[5];

    private readonly Node3D _root;
    private Sprite3D? _balloon;
    private Emote _shown;
    private Node3D? _carry;
    private string _carryKind = "";
    private readonly float _idlePhase;

    public VillagerLook(Node3D root, int seed)
    {
        _root = root;
        _idlePhase = Mathf.PosMod(seed * 0.6180339f, 1f);
    }

    /// <summary>What this family feels, most urgent first (hunger > cold > tired > happy).</summary>
    public static Emote Feeling(World w, Household h)
    {
        if (h.FoodDeficitDays > 0) return Emote.Hunger;
        if (h.ColdDeficitDays > 0) return Emote.Cold;
        if (w.CommutePermille(h) >= 250 || h.ToolCondition < 300) return Emote.Tired;
        if (h.HomeId != 0 && h.GardenFoodToday.IsPositive) return Emote.Happy;
        return Emote.None;
    }

    public void ShowEmote(Emote e)
    {
        if (e == _shown) return;
        _shown = e;
        if (e == Emote.None)
        {
            if (_balloon is not null) _balloon.Visible = false;
            return;
        }
        _balloon ??= CreateBalloon();
        _balloon.Texture = Texture(e);
        _balloon.Visible = true;
        _balloon.Scale = Vector3.One * 0.4f;
        var tw = _balloon.CreateTween().SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tw.TweenProperty(_balloon, "scale", Vector3.One, 0.22f);
    }

    /// <summary>Gentle bob so balloons feel alive.</summary>
    public void Animate(double time)
    {
        if (_balloon is { Visible: true })
            _balloon.Position = new Vector3(0, 2.75f + 0.08f * Mathf.Sin((float)time * 3f + _idlePhase * 6f), 0);
    }

    private Sprite3D CreateBalloon()
    {
        var s = new Sprite3D
        {
            Name = "Emote",
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = 0.011f,
            NoDepthTest = true,
            RenderPriority = 10,
            Shaded = false,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
            Position = new Vector3(0, 2.75f, 0),
        };
        _root.AddChild(s);
        return s;
    }

    private static Texture2D? Texture(Emote e)
    {
        int i = (int)e;
        return EmoteTextures[i] ??= GD.Load<Texture2D>($"res://assets/ui/emotes/EMO_{e.ToString().ToLowerInvariant()}.png");
    }

    /// <summary>The load matches what is carried (log on the shoulder, basket, sack, crate); none when empty.</summary>
    public void Carry(string resourceId)
    {
        string kind = resourceId switch
        {
            "wood" or "firewood" => "Log",
            "food" => "Basket",
            "tools" => "Crate",
            "" => "",
            _ => "Sack",
        };
        if (kind == _carryKind) return;
        _carryKind = kind;
        _carry?.QueueFree();
        _carry = null;
        if (kind.Length == 0) return;
        var prop = GD.Load<PackedScene>($"res://assets/props/PROP_Carry_{kind}.tscn").Instantiate<Node3D>();
        // Character faces +Z: in front of the chest, or across the right shoulder for logs. The props' pivots are
        // wherever the vendor put them, so the mesh bounds are centred on the carry point.
        _carry = new Node3D { Name = "Carry" };
        _carry.Transform = kind == "Log"
            ? new Transform3D(new Basis(Vector3.Up, Mathf.Pi / 2), new Vector3(0.24f, 1.52f, -0.05f))
            : new Transform3D(Basis.Identity, new Vector3(0, 0.95f, 0.34f));
        _carry.AddChild(prop);
        _root.AddChild(_carry);
        prop.Position = -BoundsCenter(prop);
    }

    /// <summary>Centre of the meshes' bounds in the prop's own space.</summary>
    private static Vector3 BoundsCenter(Node3D prop)
    {
        Aabb? total = null;
        var inv = prop.GlobalTransform.AffineInverse();
        foreach (var n in prop.FindChildren("*", "VisualInstance3D", true, false))
        {
            var v = (VisualInstance3D)n;
            var box = inv * v.GlobalTransform * v.GetAabb();
            total = total is { } t ? t.Merge(box) : box;
        }
        return total?.GetCenter() ?? Vector3.Zero;
    }

    /// <summary>A home idle that changes every few seconds per villager (idle · talk · sit).</summary>
    public string IdleClip(double time) => IdleClips[(int)Math.Floor(time / 7.0 + _idlePhase * IdleClips.Length) % IdleClips.Length];
}
