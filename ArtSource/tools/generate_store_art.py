import os
import subprocess
import sys

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(__file__))
from generate_skins import body, face

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
INKSCAPE = r"C:\Program Files\Inkscape\bin\inkscape.exe"
OUT = os.path.join(ROOT, "docs", "release", "store")
SRC = os.path.join(ROOT, "ArtSource", "store")
FONT = os.path.join(ROOT, "Assets", "_Game", "Fonts", "fnt_fredoka_bold.ttf")

WIDTH, HEIGHT = 1024, 500
CREAM = "#FFF1E0"
PINGI = "#FF6B57"
TEAL = "#2EC4B6"
INK = (43, 45, 66)


def rasterize(svg_text, name, width, height):
    os.makedirs(SRC, exist_ok=True)
    os.makedirs(OUT, exist_ok=True)
    svg_path = os.path.join(SRC, name + ".svg")
    with open(svg_path, "w", encoding="utf-8") as f:
        f.write(svg_text)
    png_path = os.path.join(OUT, name + ".png")
    subprocess.run([INKSCAPE, svg_path, "--export-type=png", f"--export-filename={png_path}",
                    f"--export-width={width}", f"--export-height={height}"], check=True, capture_output=True)
    return png_path


def ball(cx, cy, radius, color, expr, angle=0):
    s = radius / 78
    return (f'<g transform="translate({cx:.1f} {cy:.1f}) rotate({angle}) translate({-100 * s:.2f} {-100 * s:.2f}) scale({s:.4f})">'
            f'{body(color)}{face(expr)}</g>')


def paddle(cx, top, width, fill, edge, angle=0):
    s = width / 430
    return (f'<g transform="translate({cx:.1f} {top:.1f}) rotate({angle}) translate({-width / 2:.2f} 0) scale({s:.4f})">'
            f'<rect x="0" y="21" width="430" height="110" rx="55" fill="{edge}"/>'
            f'<rect x="0" y="0" width="430" height="110" rx="55" fill="{fill}"/>'
            '<rect x="48" y="18" width="150" height="14" rx="7" fill="#FFFFFF" fill-opacity="0.3"/></g>')


def trail(points, color):
    return "".join(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{color}" fill-opacity="{o}"/>' for x, y, r, o in points)


def feature_background():
    blobs = (f'<rect width="{WIDTH}" height="{HEIGHT}" fill="{CREAM}"/>'
             f'<circle cx="930" cy="40" r="250" fill="#FFE2C4"/>'
             f'<circle cx="80" cy="520" r="230" fill="#FFDCE4"/>'
             f'<circle cx="610" cy="560" r="150" fill="#FFE9D3"/>')
    art = (trail([(560, 150, 10, 0.25), (600, 135, 13, 0.35), (645, 128, 16, 0.45)], PINGI)
           + ball(735, 150, 74, PINGI, "happy", -8)
           + paddle(735, 268, 190, TEAL, "#1E9C90", -6)
           + ball(905, 300, 52, "#4FD1B0", "happy", 6)
           + paddle(905, 382, 130, "#FFC93C", "#E5A800", 5)
           + ball(600, 360, 44, "#9B6BFF", "idle", -4))
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{HEIGHT}" viewBox="0 0 {WIDTH} {HEIGHT}">{blobs}{art}</svg>'
    return rasterize(svg, "feature_art", WIDTH, HEIGHT)


def title(draw, text, center, size, color, angle_image):
    font = ImageFont.truetype(FONT, size)
    layer = Image.new("RGBA", angle_image.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    box = d.textbbox((0, 0), text, font=font)
    w, h = box[2] - box[0], box[3] - box[1]
    x, y = center[0] - w / 2 - box[0], center[1] - h / 2 - box[1]
    d.text((x, y + size * 0.09), text, font=font, fill=INK + (28,))
    d.text((x, y), text, font=font, fill=color)
    return layer


def feature_graphic():
    path = feature_background()
    base = Image.open(path).convert("RGBA")
    pingi = title(None, "Pingi", (255, 175), 150, PINGI, base)
    pongi = title(None, "Pongi", (275, 315), 150, TEAL, base)
    words = Image.alpha_composite(pingi, pongi).rotate(4, resample=Image.BICUBIC, center=(265, 245))
    final = Image.alpha_composite(base, words).convert("RGB")
    out = os.path.join(OUT, "feature_graphic.png")
    final.save(out)
    os.remove(path)
    return out


if __name__ == "__main__":
    print(feature_graphic())
