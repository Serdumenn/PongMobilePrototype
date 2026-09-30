import math
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(__file__))
from generate_skins import body, face

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "ArtSource", "app_icon")
OUT = os.path.join(ROOT, "Assets", "_Game", "Art", "AppIcon")
INKSCAPE = r"C:\Program Files\Inkscape\bin\inkscape.exe"

SIZE = 432
CENTER = SIZE / 2
SAFE_RADIUS = SIZE * 33 / 108
VISIBLE = SIZE * 72 / 108

VARIANTS = {
    "cream": {"bg": "#FFF1E0", "blob_a": "#FFE2C4", "blob_b": "#FFDCE4"},
    "sun": {"bg": "#FFC93C", "blob_a": "#FFD868", "blob_b": "#FFB52E"},
}

BALL_RADIUS = 74
GAP = 14
PADDLE_WIDTH = 150


def ball_group(cx, cy, radius):
    s = radius / 78
    return f'<g transform="translate({cx - 100 * s:.2f} {cy - 100 * s:.2f}) scale({s:.4f})">{body("#FF6B57")}{face("happy")}</g>'


def paddle_group(cx, top, width):
    s = width / 430
    return (f'<g transform="translate({cx - width / 2:.2f} {top:.2f}) scale({s:.4f})">'
            '<rect x="0" y="21" width="430" height="110" rx="55" fill="#1E9C90"/>'
            '<rect x="0" y="0" width="430" height="110" rx="55" fill="#2EC4B6"/>'
            '<rect x="48" y="18" width="150" height="14" rx="7" fill="#FFFFFF" fill-opacity="0.3"/></g>')


def layout():
    paddle_height = 131 * PADDLE_WIDTH / 430
    total = BALL_RADIUS * 2 + GAP + paddle_height
    top = CENTER - total / 2
    ball_cy = top + BALL_RADIUS
    paddle_top = top + BALL_RADIUS * 2 + GAP
    return ball_cy, paddle_top, paddle_height


def foreground():
    ball_cy, paddle_top, _ = layout()
    shadow = f'<ellipse cx="{CENTER}" cy="{paddle_top - 4:.2f}" rx="44" ry="6" fill="#2B2D42" fill-opacity="0.12"/>'
    return shadow + ball_group(CENTER, ball_cy, BALL_RADIUS) + paddle_group(CENTER, paddle_top, PADDLE_WIDTH)


def background(v):
    return (f'<rect width="{SIZE}" height="{SIZE}" fill="{v["bg"]}"/>'
            f'<circle cx="{SIZE * 0.86:.1f}" cy="{SIZE * 0.14:.1f}" r="{SIZE * 0.36:.1f}" fill="{v["blob_a"]}"/>'
            f'<circle cx="{SIZE * 0.12:.1f}" cy="{SIZE * 0.9:.1f}" r="{SIZE * 0.4:.1f}" fill="{v["blob_b"]}"/>')


def svg(inner, view=f"0 0 {SIZE} {SIZE}", size=SIZE, defs=""):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="{view}">{defs}{inner}</svg>'


def composite(v, shape):
    crop = (SIZE - VISIBLE) / 2
    view = f"{crop:.2f} {crop:.2f} {VISIBLE:.2f} {VISIBLE:.2f}"
    if shape == "round":
        clip = f'<circle cx="{CENTER}" cy="{CENTER}" r="{VISIBLE / 2:.2f}"/>'
    else:
        clip = f'<rect x="{crop:.2f}" y="{crop:.2f}" width="{VISIBLE:.2f}" height="{VISIBLE:.2f}" rx="{VISIBLE * 0.22:.2f}"/>'
    defs = f'<defs><clipPath id="m">{clip}</clipPath></defs>'
    return svg(f'<g clip-path="url(#m)">{background(v)}{foreground()}</g>', view, 512, defs)


def check_safe_zone():
    ball_cy, paddle_top, paddle_height = layout()
    cap = paddle_height / 131 * 55
    points = [
        (CENTER, ball_cy - BALL_RADIUS),
        (CENTER + PADDLE_WIDTH / 2, paddle_top + cap),
        (CENTER + PADDLE_WIDTH / 2 - cap, paddle_top + paddle_height),
    ]
    worst = max(math.hypot(x - CENTER, y - CENTER) for x, y in points)
    return worst, SAFE_RADIUS


def export(svg_text, name, size, folder):
    os.makedirs(SRC, exist_ok=True)
    os.makedirs(folder, exist_ok=True)
    svg_path = os.path.join(SRC, name + ".svg")
    with open(svg_path, "w", encoding="utf-8") as f:
        f.write(svg_text)
    png_path = os.path.join(folder, name + ".png")
    subprocess.run([INKSCAPE, svg_path, "--export-type=png", f"--export-filename={png_path}",
                    f"--export-width={size}", f"--export-height={size}"], check=True, capture_output=True)
    return png_path


def build(variant, folder):
    v = VARIANTS[variant]
    return [
        export(svg(background(v)), "app_icon_bg", SIZE, folder),
        export(svg(foreground()), "app_icon_fg", SIZE, folder),
        export(composite(v, "round"), "app_icon_round", 512, folder),
        export(composite(v, "legacy"), "app_icon_legacy", 512, folder),
    ]


if __name__ == "__main__":
    variant = sys.argv[1] if len(sys.argv) > 1 else "sun"
    folder = sys.argv[2] if len(sys.argv) > 2 else OUT
    worst, safe = check_safe_zone()
    print(f"foreground reach {worst:.1f}px / safe radius {safe:.1f}px")
    for path in build(variant, folder):
        print(path)
