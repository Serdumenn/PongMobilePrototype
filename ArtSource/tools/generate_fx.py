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


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, text in (("spr_fx_dot", dot()), ("spr_fx_ring", ring()), ("spr_fx_spark", spark()), ("spr_fx_glow", glow()), ("spr_fx_confetti", confetti())):
        path = os.path.join(OUT, name + ".svg")
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        print(path)


if __name__ == "__main__":
    main()
