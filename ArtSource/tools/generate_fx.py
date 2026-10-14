import math
import os

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Game", "Art", "Sprites", "Fx")
WHITE = "#FFFFFF"


def svg(size, body):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{size}" height="{size}" viewBox="0 0 {size} {size}">{body}</svg>\n'


def dot():
    return svg(64, f'<circle cx="32" cy="32" r="32" fill="{WHITE}"/>')


def ring():
    return svg(256, f'<circle cx="128" cy="128" r="112" fill="none" stroke="{WHITE}" stroke-width="26"/>')


def spark():
    c, long, short = 64, 62, 15
    points = [(c, c - long), (c + short, c - short), (c + long, c), (c + short, c + short),
              (c, c + long), (c - short, c + short), (c - long, c), (c - short, c - short)]
    path = "M" + "L".join(f"{x} {y}" for x, y in points) + "Z"
    return svg(128, f'<path d="{path}" fill="{WHITE}" stroke="{WHITE}" stroke-width="6" stroke-linejoin="round"/>')


def glow():
    stops = ((0, 0.95), (0.3, 0.55), (0.62, 0.18), (1, 0))
    ramp = "".join(f'<stop offset="{at}" stop-color="{WHITE}" stop-opacity="{alpha}"/>' for at, alpha in stops)
    return svg(256, f'<defs><radialGradient id="g" cx="0.5" cy="0.5" r="0.5">{ramp}</radialGradient></defs><circle cx="128" cy="128" r="128" fill="url(#g)"/>')


def confetti():
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="64" height="112" viewBox="0 0 64 112"><rect x="0" y="0" width="64" height="112" rx="14" fill="{WHITE}"/></svg>\n'


def star():
    c, outer, inner = 64, 60, 26
    points = []
    for i in range(10):
        radius = outer if i % 2 == 0 else inner
        angle = math.radians(-90 + i * 36)
        points.append((c + radius * math.cos(angle), c + 4 + radius * math.sin(angle)))
    path = "M" + "L".join(f"{x:.1f} {y:.1f}" for x, y in points) + "Z"
    return svg(128, f'<path d="{path}" fill="{WHITE}" stroke="{WHITE}" stroke-width="10" stroke-linejoin="round"/>')


def heart():
    body = "M64 116C30 92 10 72 10 45 10 26 25 12 43 12c9 0 17 4 21 12 4-8 12-12 21-12 18 0 33 14 33 33 0 27-20 47-54 71z"
    return svg(128, f'<path d="{body}" fill="{WHITE}" stroke="{WHITE}" stroke-width="6" stroke-linejoin="round"/>')


def flame():
    body = "M64 122c-25 0-42-17-42-39 0-24 21-38 25-63a4.5 4.5 0 0 1 6.5-3.2c14 9 20 21 21 32 6-4 10-10 11-16a4.5 4.5 0 0 1 7-2.6c14 12 23 28 23 48 0 23-19 44-51 44z"
    return svg(128, f'<path d="{body}" fill="{WHITE}"/>')


def drop():
    return svg(128, f'<path d="M64 8C64 8 24 58 24 84a40 40 0 0 0 80 0C104 58 64 8 64 8z" fill="{WHITE}"/>')


def note():
    head = '<ellipse cx="44" cy="98" rx="24" ry="19" transform="rotate(-20 44 98)" fill="#FFFFFF"/>'
    stem = '<rect x="58" y="14" width="12" height="86" rx="6" fill="#FFFFFF"/>'
    flag = '<path d="M64 14c14 4 34 12 38 30 2 9-1 18-6 24 0-12-10-22-32-26z" fill="#FFFFFF"/>'
    return svg(128, head + stem + flag)


def leaf():
    return svg(128, f'<path d="M64 6C100 30 112 74 64 122 16 74 28 30 64 6z" fill="{WHITE}" transform="translate(64 64) rotate(32) scale(0.84) translate(-64 -64)"/>')


def paw():
    toes = "".join(f'<ellipse cx="{x}" cy="{y}" rx="14" ry="17" fill="{WHITE}"/>' for x, y in ((28, 52), (50, 30), (78, 30), (100, 52)))
    pad = f'<path d="M64 62c22 0 40 22 40 40 0 14-12 20-22 18-7-1-11-5-18-5s-11 4-18 5c-10 2-22-4-22-18 0-18 18-40 40-40z" fill="{WHITE}"/>'
    return svg(128, toes + pad)


def bubble():
    shape = '<rect x="0" y="0" width="160" height="112" rx="44"/><path d="M50 96L34 144 96 104z"/>'
    shadow = f'<g transform="translate(4 8)" fill="#2B2D42" fill-opacity="0.16">{shape}</g>'
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="168" height="152" viewBox="0 0 168 152">{shadow}<g fill="{WHITE}">{shape}</g></svg>\n'


def token():
    shadow = '<circle cx="66" cy="72" r="58" fill="#2B2D42" fill-opacity="0.18"/>'
    rim = f'<circle cx="64" cy="64" r="58" fill="{WHITE}"/>'
    inner = '<circle cx="64" cy="64" r="46" fill="none" stroke="#2B2D42" stroke-opacity="0.12" stroke-width="5"/>'
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="132" height="134" viewBox="0 0 132 134">{shadow}{rim}{inner}</svg>\n'


def power_big():
    bar = f'<rect x="30" y="54" width="68" height="22" rx="11" fill="{WHITE}"/>'
    arrows = f'<path d="M24 65l-14-14v28zM104 65l14-14v28z" fill="{WHITE}" stroke="{WHITE}" stroke-width="6" stroke-linejoin="round"/>'
    return svg(128, bar + arrows)


def power_slow():
    face = f'<circle cx="64" cy="66" r="46" fill="none" stroke="{WHITE}" stroke-width="14"/>'
    hands = f'<path d="M64 40v28l18 12" fill="none" stroke="{WHITE}" stroke-width="13" stroke-linecap="round" stroke-linejoin="round"/>'
    return svg(128, face + hands)


def power_double():
    cross = f'<path d="M18 46l34 36M52 46L18 82" fill="none" stroke="{WHITE}" stroke-width="13" stroke-linecap="round"/>'
    two = f'<path d="M66 50c4-12 16-18 28-16 14 2 22 14 16 26-4 8-12 14-42 34h46" fill="none" stroke="{WHITE}" stroke-width="13" stroke-linecap="round" stroke-linejoin="round"/>'
    return svg(128, cross + two)


def power_shield():
    shape = "M64 10l44 16v34c0 28-18 48-44 58-26-10-44-30-44-58V26z"
    return svg(128, f'<path d="{shape}" fill="{WHITE}" stroke="{WHITE}" stroke-width="6" stroke-linejoin="round"/>')


def bar():
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="512" height="48" viewBox="0 0 512 48"><rect x="0" y="0" width="512" height="48" rx="24" fill="{WHITE}"/></svg>\n'


SHAPES = {
    "spr_fx_star": star,
    "spr_fx_heart": heart,
    "spr_fx_flame": flame,
    "spr_fx_drop": drop,
    "spr_fx_note": note,
    "spr_fx_leaf": leaf,
    "spr_fx_paw": paw,
    "spr_fx_bubble": bubble,
    "spr_fx_token": token,
    "spr_fx_power_big": power_big,
    "spr_fx_power_slow": power_slow,
    "spr_fx_power_double": power_double,
    "spr_fx_power_shield": power_shield,
    "spr_fx_bar": bar,
}


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, text in (("spr_fx_dot", dot()), ("spr_fx_ring", ring()), ("spr_fx_spark", spark()), ("spr_fx_glow", glow()), ("spr_fx_confetti", confetti())) + tuple((name, make()) for name, make in SHAPES.items()):
        path = os.path.join(OUT, name + ".svg")
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        print(path)


if __name__ == "__main__":
    main()
