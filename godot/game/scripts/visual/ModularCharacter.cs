using Godot;

namespace Ironvale.Game.Visual;

/// <summary>
/// Quaternius modular character: head-only base body + outfit + hair, each imported with its own copy of
/// the same 65-bone rig. On ready, every skinned mesh is moved onto the body's skeleton (skins bind by bone
/// name), so one AnimationPlayer drives everything with the shared villager library.
/// </summary>
public partial class ModularCharacter : Node3D
{
    [Export] public NodePath BodyPath { get; set; } = "Body";
    [Export] public NodePath[] PartPaths { get; set; } = { "Outfit", "Hair" };
    [Export] public NodePath AnimationPlayerPath { get; set; } = "AnimationPlayer";
    [Export] public float BlendTime { get; set; } = 0.2f;
    /// <summary>Quaternius hair/eyebrow textures are greyscale: the colour comes from this tint.</summary>
    [Export] public Color HairColor { get; set; } = new(0.36f, 0.24f, 0.15f);

    public static readonly Color[] HairPalette =
    {
        new(0.36f, 0.24f, 0.15f), new(0.18f, 0.12f, 0.08f), new(0.62f, 0.42f, 0.22f),
        new(0.75f, 0.6f, 0.35f), new(0.55f, 0.25f, 0.12f), new(0.12f, 0.1f, 0.09f),
    };

    private AnimationPlayer? _player;

    public Skeleton3D? Skeleton { get; private set; }

    public override void _Ready()
    {
        Skeleton = FindSkeleton(GetNode(BodyPath));
        if (Skeleton is null)
        {
            GD.PushError($"{Name}: body has no Skeleton3D");
            return;
        }
        foreach (var path in PartPaths)
        {
            var part = GetNodeOrNull(path);
            if (part is null) continue;
            foreach (var node in part.FindChildren("*", "MeshInstance3D", true, false))
            {
                var mesh = (MeshInstance3D)node;
                mesh.Reparent(Skeleton, keepGlobalTransform: false);
                mesh.Skeleton = "..";
            }
            part.QueueFree();
        }
        _player = GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);
        ApplyHairColor(HairColor);
    }

    public void ApplyHairColor(Color color)
    {
        HairColor = color;
        if (Skeleton is null) return;
        foreach (var node in Skeleton.FindChildren("*", "MeshInstance3D", true, false))
        {
            var mesh = (MeshInstance3D)node;
            for (int i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
            {
                if (mesh.Mesh.SurfaceGetMaterial(i) is not StandardMaterial3D mat) continue;
                bool isHair = mat.ResourceName.Contains("Hair") || mesh.Name.ToString().Contains("Eyebrow");
                if (!isHair) continue;
                var tinted = (StandardMaterial3D)mat.Duplicate();
                tinted.AlbedoColor = color;
                mesh.SetSurfaceOverrideMaterial(i, tinted);
            }
        }
    }

    /// <summary>Plays a clip from the shared library (idle, walk, walk_carry, idle_carry, harvest, …).</summary>
    public void Play(string animation, float speed = 1f)
    {
        if (_player is null || !_player.HasAnimation(animation)) return;
        if (_player.CurrentAnimation == animation)
        {
            _player.SpeedScale = speed;
            return;
        }
        _player.Play(animation, BlendTime, speed);
    }

    /// <summary>Starts the current clip at a different phase so a crowd doesn't move in lockstep.</summary>
    public void Desync(float fraction)
    {
        if (_player is null || string.IsNullOrEmpty(_player.CurrentAnimation)) return;
        _player.Seek(_player.CurrentAnimationLength * Mathf.PosMod(fraction, 1f), true);
    }

    private static Skeleton3D? FindSkeleton(Node root) =>
        root.FindChildren("*", "Skeleton3D", true, false) is { Count: > 0 } found ? (Skeleton3D)found[0] : null;
}
