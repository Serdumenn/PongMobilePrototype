import math
import os
import subprocess

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "ArtSource", "brand")
OUT = os.path.join(ROOT, "Assets", "_Game", "Art", "Brand")
FONT = os.path.join(ROOT, "Assets", "_Game", "Fonts", "fnt_fredoka_bold.ttf")
INKSCAPE = r"C:\Program Files\Inkscape\bin\inkscape.exe"

NAVY = "#1E2140"
CORAL = "#FF6B57"
CORAL_DARK = "#E04E3A"
TEAL = "#2EC4B6"
SUN = "#FFC93C"
WHITE = "#FFFFFF"


def star(cx, cy, r, color, inner=0.28):
    pts = []
    for i in range(8):
        a = math.pi / 4 * i - math.pi / 2
        rr = r if i % 2 == 0 else r * inner
        pts.append(f"{cx + rr * math.cos(a):.1f},{cy + rr * math.sin(a):.1f}")
    return f'<polygon points="{" ".join(pts)}" fill="{color}" stroke="{color}" stroke-width="6" stroke-linejoin="round"/>'


def emblem():
    anvil = ("M72,212 C120,206 170,208 200,214 C228,196 256,196 282,212 C308,228 336,228 362,212 "
             "C378,203 396,201 412,204 L412,262 C380,268 352,282 340,300 L340,336 L392,372 L392,398 "
             "L132,398 L132,372 L184,336 L184,300 C160,282 120,266 92,246 C80,237 72,226 72,212 Z")
    body = f'<path d="{anvil}" fill="{CORAL}" stroke="{CORAL}" stroke-width="12" stroke-linejoin="round"/>'
    body += f'<path d="M132,376 L392,376 L392,398 L132,398 Z" fill="{CORAL_DARK}" stroke="{CORAL_DARK}" stroke-width="12" stroke-linejoin="round"/>'
    body += f'<path d="M214,238 C236,224 258,224 280,236 C302,248 322,248 344,236" fill="none" stroke="{WHITE}" stroke-opacity="0.55" stroke-width="12" stroke-linecap="round"/>'
    body += star(296, 118, 56, SUN) + star(372, 154, 19, TEAL) + star(218, 134, 15, TEAL)
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 512 512">{body}</svg>'


def rasterize(svg_text, name, size):
    os.makedirs(SRC, exist_ok=True)
    svg_path = os.path.join(SRC, name + ".svg")
    png_path = os.path.join(SRC, name + ".png")
    with open(svg_path, "w", encoding="utf-8") as f:
        f.write(svg_text)
    subprocess.run([INKSCAPE, svg_path, "--export-type=png", f"--export-filename={png_path}",
                    f"--export-width={size}", f"--export-height={size}"], check=True, capture_output=True)
    return Image.open(png_path).convert("RGBA")


def lockup(icon, size, word_size, background=None):
    canvas = Image.new("RGBA", (size, size), background or (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.truetype(FONT, word_size)
    first, second = "Ocean ", "Forge"
    wa = draw.textlength(first, font=font)
    wb = draw.textlength(second, font=font)
    ascent, descent = font.getmetrics()
    gap = word_size * 0.45
    block = icon.height + gap + ascent + descent
    top = (size - block) / 2
    canvas.alpha_composite(icon, (int((size - icon.width) / 2), int(top)))
    x = (size - wa - wb) / 2
    y = top + icon.height + gap
    draw.text((x, y), first, font=font, fill=WHITE)
    draw.text((x + wa, y), second, font=font, fill=CORAL)
    return canvas


def main():
    os.makedirs(OUT, exist_ok=True)
    icon = rasterize(emblem(), "ocean_forge_emblem", 1024)
    icon = icon.crop(icon.getbbox())
    mark = icon.resize((480, round(icon.height * 480 / icon.width)), Image.LANCZOS)
    lockup(mark, 1024, 132).save(os.path.join(OUT, "ocean_forge_splash.png"), optimize=True)
    lockup(mark, 1024, 132, NAVY).save(os.path.join(SRC, "ocean_forge_logo_dark.png"), optimize=True)
    print(os.path.join(OUT, "ocean_forge_splash.png"))


if __name__ == "__main__":
    main()
