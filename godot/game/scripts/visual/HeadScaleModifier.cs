using Godot;

namespace Ironvale.Game.Visual;

/// <summary>
/// Scales the "Head" bone after animation (proportion study for the cozy look — not on by default; dev switch
/// --head-scale=N). Children of the skinned head follow, so hair and eyebrows scale too.
/// </summary>
public partial class HeadScaleModifier : SkeletonModifier3D
{
    public float HeadScale { get; set; } = 1f;
    private int _bone = -2;

    public override void _ProcessModificationWithDelta(double delta)
    {
        var skeleton = GetSkeleton();
        if (skeleton is null) return;
        if (_bone == -2) _bone = skeleton.FindBone("Head");
        if (_bone < 0) return;
        skeleton.SetBonePoseScale(_bone, Vector3.One * HeadScale);
    }
}
