using Godot;

namespace Ironvale.Game.Visual;

/// <summary>Small view-only effects (UI_UX_guide §5.3): dust puffs and scale "pops". Never touch the simulation.</summary>
public static class Fx
{
    private static StandardMaterial3D? _dustMaterial;

    private static StandardMaterial3D DustMaterial => _dustMaterial ??= new StandardMaterial3D
    {
        AlbedoColor = new Color(0.78f, 0.69f, 0.55f, 0.6f),
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        VertexColorUseAsAlbedo = true,
    };

    /// <summary>A dust emitter around a footprint: one-shot puff, or a light continuous haze while builders work.</summary>
    public static CpuParticles3D Dust(Node3D parent, Vector2 footprintMeters, bool continuous)
    {
        var p = new CpuParticles3D
        {
            Name = continuous ? "SiteDust" : "Puff",
            Mesh = new SphereMesh { Radius = 0.45f, Height = 0.7f, RadialSegments = 8, Rings = 4, Material = DustMaterial },   // soft round puffs
            Amount = continuous ? 10 : 32,
            Lifetime = continuous ? 1.6f : 0.9f,
            OneShot = !continuous,
            Explosiveness = continuous ? 0f : 0.9f,
            EmissionShape = CpuParticles3D.EmissionShapeEnum.Box,
            EmissionBoxExtents = new Vector3(footprintMeters.X * 0.45f, 0.2f, footprintMeters.Y * 0.45f),
            Direction = Vector3.Up,
            Spread = 70f,
            InitialVelocityMin = 0.6f,
            InitialVelocityMax = continuous ? 1.2f : 2.4f,
            Gravity = new Vector3(0, -0.6f, 0),
            ScaleAmountMin = 0.6f,
            ScaleAmountMax = 1.4f,
            ColorRamp = new Gradient { Colors = new[] { new Color(1, 1, 1, 0.8f), new Color(1, 1, 1, 0f) } },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Position = new Vector3(0, 0.3f, 0),
            Emitting = true,
        };
        parent.AddChild(p);
        if (!continuous) p.Finished += p.QueueFree;
        return p;
    }

    /// <summary>Scale pop: squash in, overshoot, settle (≈ 0.25 s).</summary>
    public static void Pop(Node3D target)
    {
        target.Scale = new Vector3(0.88f, 0.88f, 0.88f);
        var tw = target.CreateTween().SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tw.TweenProperty(target, "scale", Vector3.One, 0.25f);
    }
}
