import os
import sys

from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
SOURCE = os.path.join(ROOT, "ArtSource", "app_icon", "app_icon_source.png")
UNITY = os.path.join(ROOT, "Assets", "_Game", "Art", "AppIcon")
STORE = os.path.join(ROOT, "docs", "release", "store")
SITE = os.path.normpath(os.path.join(ROOT, "..", "serdumenn.github.io", "assets"))

LAYER = 432
VISIBLE = LAYER * 72 // 108
SIZE = 512
CORNER_THRESH = 60
SUPERSAMPLE = 4


def corner_mask(img):
    probe = img.copy()
    w, h = probe.size
    marker = (255, 0, 255)
    for xy in [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]:
        if probe.getpixel(xy) != marker:
            ImageDraw.floodfill(probe, xy, marker, thresh=CORNER_THRESH)
    mask = Image.new("L", probe.size, 0)
    src, dst = probe.load(), mask.load()
    for y in range(h):
        for x in range(w):
            if src[x, y] == marker:
                dst[x, y] = 255
    return mask.filter(ImageFilter.MaxFilter(7))


def fill_corners(img, mask):
    out = img.copy()
    px, m = out.load(), mask.load()
    w, h = out.size
    pending = {(x, y) for y in range(h) for x in range(w) if m[x, y]}
    while pending:
        done = {}
        for x, y in pending:
            total, count = [0, 0, 0], 0
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    nx, ny = x + dx, y + dy
                    if (dx or dy) and 0 <= nx < w and 0 <= ny < h and not m[nx, ny]:
                        c = px[nx, ny]
                        total[0] += c[0]
                        total[1] += c[1]
                        total[2] += c[2]
                        count += 1
            if count:
                done[(x, y)] = tuple(v // count for v in total)
        if not done:
            break
        for (x, y), c in done.items():
            px[x, y] = c
            m[x, y] = 0
        pending -= done.keys()
    soft = out.filter(ImageFilter.GaussianBlur(4))
    return Image.composite(soft, out, mask.filter(ImageFilter.GaussianBlur(3)))


def clean_source():
    img = Image.open(SOURCE).convert("RGB")
    if img.size != (SIZE, SIZE):
        img = img.resize((SIZE, SIZE), Image.LANCZOS)
    return fill_corners(img, corner_mask(img))


def adaptive_background(art):
    inner = art.resize((VISIBLE, VISIBLE), Image.LANCZOS)
    pad = (LAYER - VISIBLE) // 2
    ring = Image.new("RGB", (LAYER, LAYER))
    src, dst = inner.load(), ring.load()
    for y in range(LAYER):
        sy = min(max(y - pad, 0), VISIBLE - 1)
        for x in range(LAYER):
            sx = min(max(x - pad, 0), VISIBLE - 1)
            dst[x, y] = src[sx, sy]
    ring = ring.filter(ImageFilter.GaussianBlur(10))
    ring.paste(inner, (pad, pad))
    return ring.convert("RGBA")


def shaped(art, draw_shape):
    big = SIZE * SUPERSAMPLE
    mask = Image.new("L", (big, big), 0)
    draw_shape(ImageDraw.Draw(mask), big)
    mask = mask.resize((SIZE, SIZE), Image.LANCZOS)
    out = art.convert("RGBA")
    out.putalpha(mask)
    return out


def save(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, optimize=True)
    print(path)


def main():
    art = clean_source()
    save(adaptive_background(art), os.path.join(UNITY, "app_icon_bg.png"))
    save(Image.new("RGBA", (LAYER, LAYER), (0, 0, 0, 0)), os.path.join(UNITY, "app_icon_fg.png"))
    save(shaped(art, lambda d, s: d.ellipse((0, 0, s - 1, s - 1), fill=255)), os.path.join(UNITY, "app_icon_round.png"))
    save(shaped(art, lambda d, s: d.rounded_rectangle((0, 0, s - 1, s - 1), radius=s * 0.2, fill=255)), os.path.join(UNITY, "app_icon_legacy.png"))
    save(art.convert("RGBA"), os.path.join(STORE, "store_icon_512.png"))
    if os.path.isdir(SITE) or "--site" in sys.argv:
        save(art.resize((256, 256), Image.LANCZOS), os.path.join(SITE, "pingi-pongi", "icon.png"))
        save(art.resize((180, 180), Image.LANCZOS), os.path.join(SITE, "apple-touch-icon.png"))
        save(art.resize((64, 64), Image.LANCZOS), os.path.join(SITE, "favicon.png"))


if __name__ == "__main__":
    main()
