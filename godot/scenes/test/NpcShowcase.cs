using Godot;
using Ironvale.Game.Visual;

namespace Ironvale.Game.Test;

/// <summary>
/// Dev showcase (not exported): the cozy villager touches side by side — carried loads with walk_carry, the four emotion
/// balloons, home idles — and the head-proportion study (1.0 · 1.15 · 1.3). Args after "--": --shot=PATH.
/// </summary>
public partial class NpcShowcase : Node3D
{
    private const string Male = "res://assets/characters/CHR_Villager_Base.tscn";
    private const string Female = "res://assets/characters/CHR_Villager_Base_F.tscn";

    public override void _Ready()
    {
        CozyLight();
        string[] loads = { "wood", "food", "stone", "tools" };
        VillagerLook.Emote[] emotes = { VillagerLook.Emote.Hunger, VillagerLook.Emote.Cold, VillagerLook.Emote.Tired, VillagerLook.Emote.Happy };
        string[] idles = { "idle", "talk", "sit", "idle" };
        for (int i = 0; i < 4; i++)
        {
            var carrier = Spawn(i % 2 == 0 ? Male : Female, new Vector3(-4.5f + i * 3f, 0, -3f), i, 1f);
            carrier.Character.Play("walk_carry");
            carrier.Look.Carry(loads[i]);
            var feeling = Spawn(i % 2 == 0 ? Female : Male, new Vector3(-4.5f + i * 3f, 0, 1.5f), i + 10, 1f);
            feeling.Character.Play(idles[i]);
            feeling.Look.ShowEmote(emotes[i]);
        }
        float[] heads = { 1f, 1.15f, 1.3f };
        for (int i = 0; i < 3; i++)
        {
            var a = Spawn(Male, new Vector3(-3f + i * 3f, 0, 6f), 20 + i, heads[i]);
            a.Character.Play("idle");
            var b = Spawn(Female, new Vector3(-1.6f + i * 3f, 0, 6.6f), 30 + i, heads[i]);
            b.Character.Play("idle");
        }
        var cam = new Camera3D { Fov = 32, Current = true };
        AddChild(cam);
        cam.Position = new Vector3(0, 13f, 19f);
        cam.LookAt(new Vector3(0, 0.8f, 1.6f), Vector3.Up);
        var ground = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(40, 40) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.62f, 0.78f, 0.45f) } };
        AddChild(ground);
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg.StartsWith("--shot=")) AddChild(new DevShot { Name = "DevShot", Path = arg[7..] });
            if (arg == "--heads")   // close-up of the proportion study row
            {
                cam.Position = new Vector3(0.7f, 3.4f, 13.5f);
                cam.LookAt(new Vector3(0.7f, 1.3f, 6.3f), Vector3.Up);
            }
        }
    }

    private (ModularCharacter Character, VillagerLook Look) Spawn(string scene, Vector3 at, int seed, float head)
    {
        var root = GD.Load<PackedScene>(scene).Instantiate<Node3D>();
        AddChild(root);
        root.Position = at;
        var c = (ModularCharacter)root;
        c.ApplyHairColor(ModularCharacter.HairPalette[seed % ModularCharacter.HairPalette.Length]);
        if (!Mathf.IsEqualApprox(head, 1f) && c.Skeleton is { } sk) sk.AddChild(new HeadScaleModifier { HeadScale = head });
        return (c, new VillagerLook(root, seed));
    }

    private void CozyLight()
    {
        var sun = new DirectionalLight3D { LightColor = new Color(1f, 0.93f, 0.82f), LightEnergy = 1.2f, ShadowEnabled = true };
        sun.RotationDegrees = new Vector3(-50, -35, 0);
        AddChild(sun);
        AddChild(new WorldEnvironment { Environment = new Environment
        {
            BackgroundMode = Environment.BGMode.Color, BackgroundColor = new Color(0.62f, 0.78f, 0.45f),
            AmbientLightSource = Environment.AmbientSource.Color, AmbientLightColor = new Color(0.75f, 0.78f, 0.85f), AmbientLightEnergy = 0.6f,
        } });
    }
}
