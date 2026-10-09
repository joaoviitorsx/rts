#!/usr/bin/env python3
"""Builds the single UI Theme (HUD v2, docs/ui/HUD_v2_spec.md §1–§2) from the tokens below.

Writes (all generated — edit this script, not the outputs):
  godot/ui/theme/main_theme.tres        the Theme: tokens (type "Tokens"), Type Variations, defaults
  godot/ui/theme/textures/*.png         9-slice textures: parchment panel (border + brass filete + solid shadow),
                                        wood bars with stripes, chips/buttons with the bottom "base" (inset −3 px)
  godot/assets/ui/fonts/*.tres          FontVariations over the OFL fonts in godot/assets/vendor/google_fonts/
                                        (Nunito variable: weight + tabular figures; Alegreya SC; Alegreya Italic)

Styles StyleBoxFlat cannot draw (double border, inset base, wood stripes) are textures drawn here with PIL at 4×
and downsampled, so 2-px lines stay exact at 1× (base 1920×1080).

The 2A HUD variations (PanelPrimary, TopBar, ChipPanel, Alert*, …) are kept unchanged in a "legacy" block until the
HUD v2 screens replace them (plan §7 steps 2–3); the default text colour stays light for them meanwhile.

Usage: python3 tools/ui/build_theme.py
"""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
THEME_DIR = ROOT / "godot" / "ui" / "theme"
TEX_DIR = THEME_DIR / "textures"
FONT_DIR = ROOT / "godot" / "assets" / "ui" / "fonts"
VENDOR_FONTS = "res://assets/vendor/google_fonts"
SS = 4  # supersampling

# ------------------------------------------------------------------------------------------------ tokens (spec §2.1)
C = {
    "wood": "#6B4630", "wood_dark": "#3E2A1C", "wood_mid": "#5A3D28", "wood_line": "#8A6042",
    "parchment": "#F6EBD6", "parchment_chip": "#F2E6CF", "parchment_alt": "#E6D5B5",
    "parchment_light": "#FFF9EC", "parchment_line": "#C9AE86",
    "brass": "#C48C39", "brass_dark": "#9B6B26", "brass_light": "#F2D08A",
    "text": "#3B2A1E", "text_secondary": "#6E5A48", "text_on_wood": "#F6EBD6", "text_on_wood_dim": "#E6D5B5",
    "positive": "#5E8A45", "warning": "#D9952B", "warning_dark": "#9A6418", "negative": "#B04A3A",
    "info": "#49637A", "winter": "#9DB5C8", "fire": "#D9652B",
    # extra values read from the mockup HTML (docs/ui/mockups/HUD_Principal_v2.dc.html)
    "base_light": "#E0CBA6",        # inset base of light chips/buttons
    "alert_fill": "#FBE3D2", "alert_base": "#EBC3A8",
    "warning_fill": "#FBEBD0",
    "selected_fill": "#FFF1D2",     # build card being placed
    "ink": "#2A1C10",               # text on brass
    "shadow": "#1E1208",            # rgba(30,18,8,a)
}


def rgba(hex_: str, a: float = 1.0) -> tuple[int, int, int, int]:
    h = hex_.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), round(a * 255)


def gd_color(hex_: str, a: float = 1.0) -> str:
    r, g, b, _ = rgba(hex_)
    return f"Color({r / 255:.4g}, {g / 255:.4g}, {b / 255:.4g}, {a:.4g})"


def lighten(hex_: str, k: float) -> str:
    r, g, b, _ = rgba(hex_)
    f = (lambda v: round(v + (255 - v) * k)) if k > 0 else (lambda v: round(v * (1 + k)))
    return "#%02X%02X%02X" % (f(r), f(g), f(b))


# ------------------------------------------------------------------------------------------------ texture drawing

def rounded(draw: ImageDraw.ImageDraw, box, r, fill):
    x0, y0, x1, y1 = box
    if x1 <= x0 or y1 <= y0:
        return
    draw.rounded_rectangle([x0, y0, x1 - 1, y1 - 1], radius=max(0, r), fill=fill)


def box_texture(name: str, w: int, h: int, radius: int, fill: str, border=None, filete=None, base=None,
                solid_shadow: int = 0, soft_shadow: int = 0, pad: int = 0, fill_alpha: float = 1.0) -> dict:
    """Rounded box: outer border, optional brass filete just inside it, optional bottom base band, shadows below.
    Returns the 9-slice margins (in 1× px) for the StyleBoxTexture."""
    W, H = (w + 2 * pad) * SS, (h + 2 * pad + solid_shadow) * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ox, oy = pad * SS, pad * SS
    bw, bh = w * SS, h * SS
    if soft_shadow:
        sh = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        rounded(ImageDraw.Draw(sh), (ox, oy + 8 * SS, ox + bw, oy + bh + 8 * SS), radius * SS, rgba(C["shadow"], 0.25))
        sh = sh.filter(ImageFilter.GaussianBlur(soft_shadow * SS / 2))
        img = Image.alpha_composite(img, sh)
    d = ImageDraw.Draw(img)
    if solid_shadow:
        rounded(d, (ox, oy + solid_shadow * SS, ox + bw, oy + bh + solid_shadow * SS), radius * SS, rgba(C["shadow"], 0.3))
    inset = 0
    if border:
        color, width = border
        rounded(d, (ox, oy, ox + bw, oy + bh), radius * SS, rgba(color))
        inset = width
    if filete:
        color, width = filete
        rounded(d, (ox + inset * SS, oy + inset * SS, ox + bw - inset * SS, oy + bh - inset * SS),
                (radius - inset) * SS, rgba(color))
        inset += width
    r_in = max(0, radius - inset) * SS
    inner = (ox + inset * SS, oy + inset * SS, ox + bw - inset * SS, oy + bh - inset * SS)
    if base:
        color, height = base
        rounded(d, inner, r_in, rgba(color, fill_alpha))
        rounded(d, (inner[0], inner[1], inner[2], inner[3] - height * SS), r_in, rgba(fill, fill_alpha))
    else:
        if border or filete:
            # clear the centre before filling, so a translucent fill does not show the border colour
            rounded(d, inner, r_in, (0, 0, 0, 0))
        rounded(d, inner, r_in, rgba(fill, fill_alpha))
    img = img.resize((W // SS, H // SS), Image.LANCZOS)
    TEX_DIR.mkdir(parents=True, exist_ok=True)
    img.save(TEX_DIR / f"{name}.png")
    corner = max(radius, inset + (base[1] if base else 0)) + 2
    return {
        "texture": f"res://ui/theme/textures/{name}.png",
        "margin": (pad + corner, pad + corner, pad + corner, pad + solid_shadow + corner),
        "expand": (pad, pad, pad, pad + solid_shadow),
    }


def wood_texture(name: str, height: int, stripe_every: int, edge: str) -> dict:
    """Tileable wood: vertical stripes rgba(0,0,0,.05) 2 px every `stripe_every`; edge = 'bottom' (top bar:
    brass filete then dark border at the bottom), 'top' (bottom bar) or 'header' (2 px brass at the bottom)."""
    soft = 12 if edge in ("bottom", "top") else 0
    w = stripe_every
    img = Image.new("RGBA", (w, height + soft), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    body = (0, soft, w, height + soft) if edge == "top" else (0, 0, w, height)
    d.rectangle([body[0], body[1], body[2] - 1, body[3] - 1], fill=rgba(C["wood"]))
    d.rectangle([0, body[1], 1, body[3] - 1], fill=rgba(lighten(C["wood"], -0.05)))   # rgba(0,0,0,.05) over wood
    by0, by1 = body[1], body[3]
    if edge == "bottom":
        d.rectangle([0, by1 - 3, w - 1, by1 - 1], fill=rgba(C["wood_dark"]))
        d.rectangle([0, by1 - 5, w - 1, by1 - 4], fill=rgba(C["brass"]))
        for i in range(soft):   # 0 6px 16px shadow under the bar
            d.line([0, by1 + i, w - 1, by1 + i], fill=rgba(C["shadow"], 0.35 * (1 - i / soft) ** 2))
    elif edge == "top":
        d.rectangle([0, by0, w - 1, by0 + 2], fill=rgba(C["wood_dark"]))
        d.rectangle([0, by0 + 3, w - 1, by0 + 4], fill=rgba(C["brass"]))
        for i in range(soft):
            d.line([0, by0 - 1 - i, w - 1, by0 - 1 - i], fill=rgba(C["shadow"], 0.35 * (1 - i / soft) ** 2))
    else:
        d.rectangle([0, by1 - 2, w - 1, by1 - 1], fill=rgba(C["brass"]))
    img.save(TEX_DIR / f"{name}.png")
    if edge == "bottom":
        return {"texture": f"res://ui/theme/textures/{name}.png", "margin": (0, 2, 0, 5 + soft), "expand": (0, 0, 0, soft), "tile": True}
    if edge == "top":
        return {"texture": f"res://ui/theme/textures/{name}.png", "margin": (0, 5 + soft, 0, 2), "expand": (0, soft, 0, 0), "tile": True}
    return {"texture": f"res://ui/theme/textures/{name}.png", "margin": (0, 2, 0, 2), "expand": (0, 0, 0, 0), "tile": True}


# ------------------------------------------------------------------------------------------------ .tres writer

class Tres:
    def __init__(self):
        self.ext: list[tuple[str, str, str]] = []       # (type, path, id)
        self.sub: list[tuple[str, str, dict]] = []      # (type, id, props)
        self.props: list[tuple[str, str]] = []

    def ext_res(self, type_: str, path: str) -> str:
        for t, p, i in self.ext:
            if p == path:
                return f'ExtResource("{i}")'
        i = f"{len(self.ext) + 1}_{Path(path).stem.replace('[', '').replace(']', '')}"
        self.ext.append((type_, path, i))
        return f'ExtResource("{i}")'

    def sub_res(self, type_: str, id_: str, props: dict) -> str:
        self.sub.append((type_, id_, props))
        return f'SubResource("{id_}")'

    def set(self, key: str, value: str):
        self.props.append((key, value))

    def write(self, path: Path, header_type: str):
        out = [f'[gd_resource type="{header_type}" load_steps={len(self.ext) + len(self.sub) + 1} format=3]', ""]
        for t, p, i in self.ext:
            out.append(f'[ext_resource type="{t}" path="{p}" id="{i}"]')
        if self.ext:
            out.append("")
        for t, i, props in self.sub:
            out.append(f'[sub_resource type="{t}" id="{i}"]')
            out += [f"{k} = {v}" for k, v in props.items()]
            out.append("")
        out.append("[resource]")
        out += [f"{k} = {v}" for k, v in self.props]
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("\n".join(out) + "\n", encoding="utf-8")


TAG_WGHT = int.from_bytes(b"wght", "big")
TAG_TNUM = int.from_bytes(b"tnum", "big")


def font_variation(name: str, base_ttf: str, weight: int | None = None, tabular: bool = True, spacing: int = 0) -> str:
    t = Tres()
    t.set("base_font", t.ext_res("FontFile", f"{VENDOR_FONTS}/{base_ttf}"))
    if weight is not None:
        t.set("variation_opentype", "{%d: %d}" % (TAG_WGHT, weight))
    if tabular:
        t.set("opentype_features", "{%d: 1}" % TAG_TNUM)
    if spacing:
        t.set("spacing_glyph", str(spacing))
    t.write(FONT_DIR / f"{name}.tres", "FontVariation")
    return f"res://assets/ui/fonts/{name}.tres"


# ------------------------------------------------------------------------------------------------ theme

def main():
    fonts = {
        "body": font_variation("nunito_semibold", "nunito/Nunito[wght].ttf", 600),
        "bold": font_variation("nunito_bold", "nunito/Nunito[wght].ttf", 700),
        "xbold": font_variation("nunito_extrabold", "nunito/Nunito[wght].ttf", 800),
        "caption": font_variation("nunito_caption", "nunito/Nunito[wght].ttf", 800, spacing=1),   # +6 % at 12 px
        "title": font_variation("alegreya_sc_bold", "alegreyasc/AlegreyaSC-Bold.ttf", tabular=False),
        "title_medium": font_variation("alegreya_sc_medium", "alegreyasc/AlegreyaSC-Medium.ttf", tabular=False),
        "quote": font_variation("alegreya_italic", "alegreya/Alegreya-Italic[wght].ttf", 400, tabular=False),
    }

    T = Tres()
    F = {k: T.ext_res("FontVariation", v) for k, v in fonts.items()}

    def tex_style(id_: str, spec: dict, content=(0, 0, 0, 0), modulate: str | None = None) -> str:
        props = {"content_margin_left": f"{content[0]:.1f}", "content_margin_top": f"{content[1]:.1f}",
                 "content_margin_right": f"{content[2]:.1f}", "content_margin_bottom": f"{content[3]:.1f}",
                 "texture": T.ext_res("Texture2D", spec["texture"])}
        for side, v in zip(("left", "top", "right", "bottom"), spec["margin"]):
            props[f"texture_margin_{side}"] = f"{v:.1f}"
        for side, v in zip(("left", "top", "right", "bottom"), spec["expand"]):
            if v:
                props[f"expand_margin_{side}"] = f"{v:.1f}"
        if spec.get("tile"):
            props["axis_stretch_horizontal"] = "1"
        if modulate:
            props["modulate_color"] = modulate
        return T.sub_res("StyleBoxTexture", id_, props)

    def flat(id_: str, bg: str | None, radius=0, border: tuple[str, int] | None = None, content=(0, 0, 0, 0),
             bg_alpha=1.0, borders: tuple[int, int, int, int] | None = None, shadow: tuple[float, int, int] | None = None,
             corners: tuple[int, int, int, int] | None = None, expand: int = 0) -> str:
        props = {"content_margin_left": f"{content[0]:.1f}", "content_margin_top": f"{content[1]:.1f}",
                 "content_margin_right": f"{content[2]:.1f}", "content_margin_bottom": f"{content[3]:.1f}"}
        if bg is None:
            props["draw_center"] = "false"
        else:
            props["bg_color"] = gd_color(bg, bg_alpha)
        if border:
            widths = borders or (border[1],) * 4
            for side, v in zip(("left", "top", "right", "bottom"), widths):
                props[f"border_width_{side}"] = str(v)
            props["border_color"] = gd_color(border[0])
        cr = corners or (radius,) * 4
        for name, v in zip(("top_left", "top_right", "bottom_right", "bottom_left"), cr):
            props[f"corner_radius_{name}"] = str(v)
        if shadow:
            props["shadow_color"] = gd_color(C["shadow"], shadow[0])
            props["shadow_size"] = str(shadow[1])
            props["shadow_offset"] = f"Vector2(0, {shadow[2]})"
        if expand:
            for side in ("left", "top", "right", "bottom"):
                props[f"expand_margin_{side}"] = f"{expand:.1f}"
        props["anti_aliasing"] = "true"
        return T.sub_res("StyleBoxFlat", id_, props)

    def variation(name: str, base: str, **items):
        T.set(f"{name}/base_type", f'&"{base}"')
        for key, value in items.items():
            T.set(f"{name}/{key.replace('__', '/')}", value)

    def label(name: str, font: str, size: int, color: str, base="Label"):
        variation(name, base, fonts__font=F[font], font_sizes__font_size=str(size), colors__font_color=gd_color(C[color]))

    # ---- 9-slice textures
    parchment = box_texture("panel_parchment", 40, 40, 8, C["parchment"], border=(C["wood_mid"], 3),
                            filete=(C["brass"], 2), solid_shadow=6, soft_shadow=10, pad=12)
    parchment_plain = box_texture("panel_parchment_plain", 40, 40, 8, C["parchment"], border=(C["wood_mid"], 3),
                                  solid_shadow=4, soft_shadow=8, pad=10)
    chip = box_texture("chip_resource", 32, 32, 6, C["parchment_chip"], border=(C["wood_dark"], 2), base=(C["base_light"], 3))
    chip_alert = box_texture("chip_resource_alert", 32, 32, 6, C["alert_fill"], border=(C["negative"], 2), base=(C["alert_base"], 3))
    btn = {
        "primary": box_texture("button_primary", 32, 32, 6, C["brass"], border=(C["wood_mid"], 2), base=(C["brass_dark"], 3)),
        "primary_hover": box_texture("button_primary_hover", 32, 32, 6, lighten(C["brass"], 0.12), border=(C["wood_mid"], 2), base=(C["brass_dark"], 3)),
        "primary_pressed": box_texture("button_primary_pressed", 32, 32, 6, C["brass_dark"], border=(C["wood_mid"], 2)),
        "secondary": box_texture("button_secondary", 32, 32, 6, C["parchment"], border=(C["wood_mid"], 2), base=(C["base_light"], 3)),
        "secondary_hover": box_texture("button_secondary_hover", 32, 32, 6, C["parchment_light"], border=(C["wood_mid"], 2), base=(C["base_light"], 3)),
        "secondary_pressed": box_texture("button_secondary_pressed", 32, 32, 6, C["parchment_alt"], border=(C["wood_mid"], 2)),
        "category": box_texture("button_category", 32, 32, 6, C["parchment_chip"], border=(C["wood_dark"], 2), base=(C["base_light"], 3)),
        "category_hover": box_texture("button_category_hover", 32, 32, 6, C["parchment_light"], border=(C["wood_dark"], 2), base=(C["base_light"], 3)),
        "category_on": box_texture("button_category_on", 32, 32, 6, C["brass"], border=(C["wood_dark"], 2), base=(C["brass_dark"], 3)),
    }
    bar_top = wood_texture("bar_wood_top", 32, 64, "bottom")
    bar_bottom = wood_texture("bar_wood_bottom", 32, 64, "top")
    header = wood_texture("panel_header_wood", 16, 48, "header")

    # ---- tokens (readable from code: theme.GetColor("wood", "Tokens"))
    for k in sorted(C):
        T.set(f"Tokens/colors/{k}", gd_color(C[k]))

    # ---- defaults: Nunito; size 16 and light text kept for the 2A HUD until it is replaced (see docstring)
    T.set("default_font", F["body"])
    T.set("default_font_size", "16")
    T.set("TooltipPanel/styles/panel", tex_style("tooltip", parchment_plain, (14, 10, 14, 10)))
    T.set("TooltipLabel/colors/font_color", gd_color(C["text"]))
    T.set("TooltipLabel/fonts/font", F["body"])
    T.set("TooltipLabel/font_sizes/font_size", "14")

    # ---- panels (spec §2.4)
    variation("PanelParchment", "PanelContainer", styles__panel=tex_style("panel_parchment", parchment, (16, 12, 16, 12)))
    variation("PanelParchmentFlush", "PanelContainer", styles__panel=tex_style("panel_parchment_flush", parchment, (5, 5, 5, 5)))
    variation("PanelParchmentInner", "PanelContainer", styles__panel=flat("panel_inner", None, content=(16, 12, 16, 14)))
    variation("PanelParchmentHeader", "PanelContainer", styles__panel=tex_style("panel_header", header, (16, 10, 10, 12)))
    variation("PanelReeveHeader", "PanelContainer", styles__panel=flat("panel_reeve", C["info"], content=(14, 8, 14, 8),
                                                                        border=(C["brass"], 2), borders=(0, 0, 0, 2)))
    variation("BarWood", "PanelContainer", styles__panel=tex_style("bar_wood_top", bar_top, (16, 0, 16, 0)))
    variation("BarWoodBottom", "PanelContainer", styles__panel=tex_style("bar_wood_bottom", bar_bottom, (16, 0, 16, 0)))
    variation("PanelCell", "PanelContainer", styles__panel=flat("panel_cell", C["parchment_light"], 6, (C["parchment_line"], 1), (10, 8, 10, 8)))
    variation("PanelWarningBox", "PanelContainer", styles__panel=flat("panel_warning", C["warning_fill"], 6, (C["warning"], 2), (12, 12, 12, 12)))
    variation("PanelDecree", "PanelContainer", styles__panel=flat("panel_decree", C["parchment_alt"], 4, (C["wood_line"], 1), (12, 8, 12, 8)))
    variation("PanelDark", "PanelContainer", styles__panel=flat("panel_dark", C["wood_dark"], 8, (C["brass"], 2), (16, 8, 16, 8), shadow=(0.4, 16, 6)))
    variation("PanelWoodInset", "PanelContainer", styles__panel=flat("panel_wood_inset", C["wood_dark"], 8, content=(4, 4, 4, 4)))
    variation("PanelPillOnWood", "PanelContainer", styles__panel=flat("panel_pill_wood", C["wood_dark"], 18, content=(10, 0, 10, 0)))
    variation("PanelMarker", "PanelContainer", styles__panel=flat("panel_marker", C["parchment"], 16, (C["wood_mid"], 2), (4, 4, 12, 4), shadow=(0.35, 10, 4)))
    variation("PanelMarkerWarning", "PanelContainer", styles__panel=flat("panel_marker_warn", C["parchment"], 16, (C["warning_dark"], 2), (4, 4, 12, 4), shadow=(0.35, 10, 4)))
    variation("PanelMarkerPositive", "PanelContainer", styles__panel=flat("panel_marker_pos", C["parchment"], 16, (C["positive"], 2), (4, 4, 12, 4), shadow=(0.35, 10, 4)))
    variation("PanelAlertCard", "PanelContainer", styles__panel=flat("panel_alert_card", C["parchment"], 8, (C["wood_mid"], 3), (10, 8, 8, 8), shadow=(0.3, 8, 4)))
    variation("ChipResource", "PanelContainer", styles__panel=tex_style("chip_resource", chip, (12, 4, 12, 7)))
    variation("ChipResourceAlert", "PanelContainer", styles__panel=tex_style("chip_resource_alert", chip_alert, (12, 4, 12, 7)))

    # ---- buttons
    def button(name: str, normal: str, hover: str, pressed: str, font_color: str, size: int = 16, font: str = "xbold",
               disabled_alpha: float = 0.5, pressed_color: str | None = None, hover_color: str | None = None):
        focus = flat(f"{name}_focus", None, 7, (C["brass_light"], 2), expand=2)
        variation(name, "Button",
                  styles__normal=normal, styles__hover=hover, styles__pressed=pressed, styles__hover_pressed=pressed,
                  styles__disabled=normal, styles__focus=focus, fonts__font=F[font], font_sizes__font_size=str(size),
                  colors__font_color=gd_color(font_color), colors__font_hover_color=gd_color(hover_color or font_color),
                  colors__font_pressed_color=gd_color(pressed_color or font_color),
                  colors__font_hover_pressed_color=gd_color(pressed_color or font_color),
                  colors__font_focus_color=gd_color(font_color),
                  colors__font_disabled_color=gd_color(font_color, disabled_alpha),
                  colors__icon_normal_color=gd_color(font_color), colors__icon_pressed_color=gd_color(pressed_color or font_color),
                  colors__icon_hover_color=gd_color(hover_color or font_color),
                  colors__icon_hover_pressed_color=gd_color(pressed_color or font_color),
                  constants__h_separation="6")

    pad = (14, 6, 14, 9)
    pad_pressed = (14, 8, 14, 7)
    button("ButtonPrimary", tex_style("btn_primary", btn["primary"], pad), tex_style("btn_primary_hover", btn["primary_hover"], pad),
           tex_style("btn_primary_pressed", btn["primary_pressed"], pad_pressed), C["ink"])
    button("ButtonSecondary", tex_style("btn_secondary", btn["secondary"], pad), tex_style("btn_secondary_hover", btn["secondary_hover"], pad),
           tex_style("btn_secondary_pressed", btn["secondary_pressed"], pad_pressed), C["text"], size=14)
    button("ButtonGhost", flat("btn_ghost", None, content=(8, 6, 8, 6)), flat("btn_ghost_hover", C["parchment_alt"], 6, content=(8, 6, 8, 6)),
           flat("btn_ghost_pressed", C["parchment_alt"], 6, content=(8, 7, 8, 5)), C["text_secondary"], size=14, font="bold",
           hover_color=C["text"])
    cat_pad = (6, 6, 6, 9)
    button("ButtonCategory", tex_style("btn_category", btn["category"], cat_pad), tex_style("btn_category_hover", btn["category_hover"], cat_pad),
           tex_style("btn_category_on", btn["category_on"], cat_pad), C["text"], size=14, pressed_color=C["ink"])
    button("ButtonOverlay", flat("btn_overlay", C["parchment_light"], 6, (C["parchment_line"], 2), (4, 4, 4, 4)),
           flat("btn_overlay_hover", C["parchment_chip"], 6, (C["brass"], 2), (4, 4, 4, 4)),
           flat("btn_overlay_on", C["wood_dark"], 6, (C["brass"], 2), (4, 4, 4, 4)), C["text"], size=12, font="caption",
           pressed_color=C["brass_light"])
    button("ButtonWood", flat("btn_wood", C["wood_mid"], 5, content=(8, 4, 8, 4)), flat("btn_wood_hover", lighten(C["wood_mid"], 0.12), 5, content=(8, 4, 8, 4)),
           flat("btn_wood_on", C["brass"], 5, content=(8, 4, 8, 4)), C["text_on_wood"], size=14, pressed_color=C["ink"])
    button("ButtonWoodAlert", flat("btn_wood_alert", C["negative"], 5, content=(8, 4, 8, 4)), flat("btn_wood_alert_hover", lighten(C["negative"], 0.12), 5, content=(8, 4, 8, 4)),
           flat("btn_wood_alert_on", C["negative"], 5, content=(8, 4, 8, 4)), C["text_on_wood"], size=14)
    button("ButtonWoodOutline", flat("btn_wood_outline", C["wood_dark"], 5, (C["brass"], 2), (6, 4, 6, 4)),
           flat("btn_wood_outline_hover", C["wood_mid"], 5, (C["brass_light"], 2), (6, 4, 6, 4)),
           flat("btn_wood_outline_pressed", C["wood_dark"], 5, (C["brass"], 2), (6, 5, 6, 3)), C["text_on_wood"], size=14)
    button("ButtonSmall", flat("btn_small", C["parchment_alt"], 5, (C["wood_mid"], 2), (10, 4, 10, 4)),
           flat("btn_small_hover", C["parchment_chip"], 5, (C["wood_mid"], 2), (10, 4, 10, 4)),
           flat("btn_small_pressed", lighten(C["parchment_alt"], -0.08), 5, (C["wood_mid"], 2), (10, 5, 10, 3)), C["text"], size=13)

    # ---- tabs (TabBar) and progress bars
    tab_sel = flat("tab_selected", C["parchment"], 0, (C["wood_line"], 2), (12, 6, 12, 6), borders=(2, 2, 2, 0), corners=(6, 6, 0, 0))
    tab_un = flat("tab_unselected", C["parchment_alt"], 0, (C["wood_line"], 2), (12, 6, 12, 6), borders=(2, 2, 2, 0), corners=(6, 6, 0, 0))
    tab_hover = flat("tab_hovered", C["parchment_light"], 0, (C["wood_line"], 2), (12, 6, 12, 6), borders=(2, 2, 2, 0), corners=(6, 6, 0, 0))
    variation("Tab", "TabBar", styles__tab_selected=tab_sel, styles__tab_unselected=tab_un, styles__tab_hovered=tab_hover,
              styles__tab_disabled=tab_un, styles__tab_focus=flat("tab_focus", None, 6, (C["brass"], 2)),
              fonts__font=F["xbold"], font_sizes__font_size="14",
              colors__font_selected_color=gd_color(C["text"]), colors__font_unselected_color=gd_color(C["text_secondary"]),
              colors__font_hovered_color=gd_color(C["text"]), constants__h_separation="4")
    variation("PanelTabStrip", "PanelContainer", styles__panel=flat("panel_tabstrip", C["parchment_alt"], 0, (C["wood_line"], 2),
                                                                     (12, 8, 12, 0), borders=(0, 0, 0, 2)))

    def progress(name: str, track: str, fill: str, track_border: tuple[str, int] | None, radius: int):
        variation(name, "ProgressBar",
                  styles__background=flat(f"{name}_bg", track, radius, track_border),
                  styles__fill=flat(f"{name}_fill", fill, radius),
                  fonts__font=F["caption"], font_sizes__font_size="12", colors__font_color=gd_color(C["text"]))

    progress("ProgressBrass", C["parchment_alt"], C["brass"], (C["parchment_line"], 1), 5)
    progress("ProgressFire", C["parchment_alt"], C["fire"], (C["parchment_line"], 1), 5)
    progress("ProgressPositive", C["parchment_alt"], C["positive"], (C["parchment_line"], 1), 5)
    progress("ProgressOnWood", C["wood_dark"], C["brass_light"], None, 3)

    # ---- separators
    variation("SeparatorWood", "VSeparator", styles__separator=T.sub_res("StyleBoxLine", "sep_wood",
              {"color": gd_color(C["wood_line"]), "thickness": "1", "vertical": "true"}), constants__separation="16")
    variation("SeparatorParchment", "HSeparator", styles__separator=T.sub_res("StyleBoxLine", "sep_parchment",
              {"color": gd_color(C["parchment_line"]), "thickness": "1"}), constants__separation="8")

    # ---- labels (spec §2.2)
    label("LabelTitleXL", "title", 22, "text")
    label("LabelTitle", "title", 18, "text")
    label("LabelQuote", "quote", 15, "text")
    label("LabelValue", "xbold", 18, "text")
    label("LabelBodyBold", "xbold", 14, "text")
    label("LabelBody", "body", 14, "text")
    label("LabelSecondary", "body", 14, "text_secondary")
    label("LabelCaption", "caption", 12, "text_secondary")
    label("LabelTitleOnWood", "title", 22, "text_on_wood")
    label("LabelOnWood", "xbold", 14, "text_on_wood")
    label("LabelOnWoodDim", "body", 12, "text_on_wood_dim")
    label("LabelValueOnWood", "xbold", 18, "text_on_wood")
    label("LabelPositive", "xbold", 14, "positive")
    label("LabelWarning", "xbold", 14, "warning_dark")
    label("LabelNegative", "xbold", 14, "negative")
    label("LabelInfo", "xbold", 14, "info")

    write_theme(T)
    print(f"theme -> {THEME_DIR / 'main_theme.tres'} ({len(T.sub)} styles, {len(T.ext)} external resources)")


# The 2A HUD (dark panels, light text) — unchanged until HUD v2 replaces those screens.
LEGACY = """\
HBoxContainer/constants/separation = 8
VBoxContainer/constants/separation = 8
GridContainer/constants/h_separation = 12
GridContainer/constants/v_separation = 4
HeaderLabel/base_type = &"Label"
HeaderLabel/font_sizes/font_size = 22
SecondaryLabel/base_type = &"Label"
SecondaryLabel/colors/font_color = Color(0.72, 0.72, 0.72, 1)
SecondaryLabel/font_sizes/font_size = 14
TrendUp/base_type = &"Label"
TrendUp/colors/font_color = Color(0.37, 0.54, 0.27, 1)
TrendDown/base_type = &"Label"
TrendDown/colors/font_color = Color(0.69, 0.29, 0.23, 1)
WarningLabel/base_type = &"Label"
WarningLabel/colors/font_color = Color(0.85, 0.58, 0.17, 1)
PanelPrimary/base_type = &"PanelContainer"
PanelPrimary/styles/panel = SubResource("legacy_panel")
TopBar/base_type = &"PanelContainer"
TopBar/styles/panel = SubResource("legacy_topbar")
ChipPanel/base_type = &"PanelContainer"
ChipPanel/styles/panel = SubResource("legacy_chip")
AlertInfo/base_type = &"PanelContainer"
AlertInfo/styles/panel = SubResource("legacy_alert_info")
AlertWarning/base_type = &"PanelContainer"
AlertWarning/styles/panel = SubResource("legacy_alert_warning")
AlertCritical/base_type = &"PanelContainer"
AlertCritical/styles/panel = SubResource("legacy_alert_critical")
ScreenMargin/base_type = &"MarginContainer"
ScreenMargin/constants/margin_left = 12
ScreenMargin/constants/margin_top = 8
ScreenMargin/constants/margin_right = 12
ScreenMargin/constants/margin_bottom = 12
SeriesChart/base_type = &"Control"
SeriesChart/colors/background = Color(0.1, 0.1, 0.11, 1)
SeriesChart/colors/series_stock = Color(0.95, 0.85, 0.4, 1)
SeriesChart/colors/series_produced = Color(0.5, 0.9, 0.5, 1)
SeriesChart/colors/series_consumed = Color(0.95, 0.5, 0.45, 1)
SeriesChart/font_sizes/font_size = 12"""

LEGACY_SUBS = [
    ("legacy_panel", {"bg_color": "Color(0.16, 0.17, 0.18, 0.94)", "m": 12, "r": 4}),
    ("legacy_topbar", {"bg_color": "Color(0.12, 0.13, 0.14, 0.96)", "m": (12, 6), "r": 0}),
    ("legacy_chip", {"bg_color": "Color(0.22, 0.23, 0.24, 1)", "m": (8, 4), "r": 4}),
    ("legacy_alert_info", {"bg_color": "Color(0.16, 0.17, 0.18, 0.94)", "m": (12, 8, 8, 8), "border": "Color(0.29, 0.39, 0.48, 1)"}),
    ("legacy_alert_warning", {"bg_color": "Color(0.16, 0.17, 0.18, 0.94)", "m": (12, 8, 8, 8), "border": "Color(0.85, 0.58, 0.17, 1)"}),
    ("legacy_alert_critical", {"bg_color": "Color(0.16, 0.17, 0.18, 0.94)", "m": (12, 8, 8, 8), "border": "Color(0.69, 0.29, 0.23, 1)"}),
]


def write_theme(T: Tres):
    for id_, spec in LEGACY_SUBS:
        m = spec["m"]
        m = (m, m, m, m) if isinstance(m, int) else (m[0], m[1], m[0], m[1]) if len(m) == 2 else m
        props = {f"content_margin_{s}": f"{v:.1f}" for s, v in zip(("left", "top", "right", "bottom"), m)}
        props["bg_color"] = spec["bg_color"]
        if "border" in spec:
            props["border_width_left"] = "4"
            props["border_color"] = spec["border"]
        r = spec.get("r", 4)
        for name in ("top_left", "top_right", "bottom_right", "bottom_left"):
            props[f"corner_radius_{name}"] = str(r)
        T.sub_res("StyleBoxFlat", id_, props)
    for line in LEGACY.splitlines():
        k, v = line.split(" = ", 1)
        T.set(k, v)
    T.write(THEME_DIR / "main_theme.tres", "Theme")


if __name__ == "__main__":
    main()
