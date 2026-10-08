using System.Collections.Generic;
using Godot;

namespace Ironvale.Game.Visual;

/// <summary>
/// Candidate material treatments to reconcile the hand-painted Stylized Nature pack with the PBR trim-sheet
/// packs (Medieval Village, Fantasy Props, Outfits). Runtime overrides only: vendor files are untouched.
/// The owner picks one in Etapa 3; the winner will then be baked into the asset scenes.
/// </summary>
public static class MaterialStyle
{
    public enum Style
    {
        /// <summary>A — as imported: PBR (normal + ORM, metallic from texture).</summary>
        Original,
        /// <summary>B — cozy matte: no metal, rough, normal maps at 40%.</summary>
        CozyMatte,
        /// <summary>C — painted: B + no normal maps, toon diffuse bands, warm tint, brighter foliage.</summary>
        Painted,
    }

    public static string Describe(Style style) => style switch
    {
        Style.Original => "A — Original (PBR importado)",
        Style.CozyMatte => "B — Cozy fosco (sem metal, rugoso, normal 40%)",
        _ => "C — Pintado (B + sem normal, difuso toon, tom quente, folhagem clara)",
    };

    private static readonly Dictionary<ulong, Material?[]> Originals = new();

    public static void Apply(Node root, Style style, Environment? environment = null)
    {
        foreach (var node in root.FindChildren("*", "MeshInstance3D", true, false))
        {
            var mesh = (MeshInstance3D)node;
            if (mesh.Mesh is null) continue;
            int count = mesh.Mesh.GetSurfaceCount();
            if (!Originals.ContainsKey(mesh.GetInstanceId()))
            {
                var saved = new Material?[count];
                for (int i = 0; i < count; i++) saved[i] = mesh.GetSurfaceOverrideMaterial(i);
                Originals[mesh.GetInstanceId()] = saved;
            }
            var originals = Originals[mesh.GetInstanceId()];
            for (int i = 0; i < count; i++)
            {
                var baseMaterial = originals[i] ?? mesh.Mesh.SurfaceGetMaterial(i);
                mesh.SetSurfaceOverrideMaterial(i, style == Style.Original || baseMaterial is not StandardMaterial3D std
                    ? originals[i]
                    : Treat(std, style));
            }
        }
        if (environment is not null)
        {
            environment.AdjustmentEnabled = style == Style.Painted;
            environment.AdjustmentSaturation = 1.12f;
            environment.AdjustmentBrightness = 1.03f;
        }
    }

    private static StandardMaterial3D Treat(StandardMaterial3D source, Style style)
    {
        var m = (StandardMaterial3D)source.Duplicate();
        bool foliage = m.Transparency != BaseMaterial3D.TransparencyEnum.Disabled;
        m.Metallic = 0f;
        m.MetallicTexture = null;
        m.RoughnessTexture = null;
        m.Roughness = 0.9f;
        m.NormalScale = 0.4f;
        if (style == Style.Painted)
        {
            m.NormalEnabled = false;
            m.DiffuseMode = BaseMaterial3D.DiffuseModeEnum.Toon;
            m.SpecularMode = BaseMaterial3D.SpecularModeEnum.Toon;
            m.AlbedoColor *= new Color(1.04f, 1.0f, 0.94f);
            if (foliage) m.VertexColorUseAsAlbedo = false;   // Nature's greyscale vertex mask darkens leaves
        }
        return m;
    }
}
