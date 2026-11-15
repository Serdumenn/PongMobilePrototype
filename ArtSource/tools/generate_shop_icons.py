import base64
import importlib.util
import json
import math
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import generate_skins as skins

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Game", "Art", "Sprites", "Shop")
SPEC = os.path.join(ROOT, "ArtSource", "shop_items.json")
SHEET = os.path.join(ROOT, "docs", "design", "shop", "art.html")
INK = "#2B2D42"
RB = ["#FF6B57", "#FFC93C", "#7ED957", "#4FA8FF", "#9B6BFF"]


def svg(body, defs=""):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128">{defs}{body}</svg>'


def star(x, y, r, c, rot=0):
    pts = []
    for i in range(10):
        rr = r if i % 2 == 0 else r * 0.45
        a = math.radians(-90 + i * 36 + rot)
        pts.append(f"{x + rr * math.cos(a):.1f} {y + rr * math.sin(a):.1f}")
    return f'<path d="M{"L".join(pts)}Z" fill="{c}" stroke="{c}" stroke-width="{r*0.18:.1f}" stroke-linejoin="round"/>'


def spark(x, y, r, c):
    s = r * 0.25
    return f'<path d="M{x} {y-r}L{x+s} {y-s}L{x+r} {y}L{x+s} {y+s}L{x} {y+r}L{x-s} {y+s}L{x-r} {y}L{x-s} {y-s}Z" fill="{c}"/>'


def heart(x, y, r, c):
    k = r / 10
    return (f'<path transform="translate({x} {y}) scale({k:.2f})" d="M0 9C-6 4-10 1-10-4c0-3 2.5-5.5 5.5-5.5 2 0 3.6 1 4.5 2.6.9-1.6 2.5-2.6 4.5-2.6 3 0 5.5 2.5 5.5 5.5 0 5-4 8-10 13z" fill="{c}"/>')


def bubble(x, y, r, c):
    return f'<circle cx="{x}" cy="{y}" r="{r}" fill="{c}" fill-opacity="0.35" stroke="{c}" stroke-width="{r*0.25:.1f}"/><circle cx="{x-r*0.35:.1f}" cy="{y-r*0.35:.1f}" r="{r*0.25:.1f}" fill="#FFFFFF"/>'


def leaf(x, y, r, c, rot=30):
    return f'<path transform="translate({x} {y}) rotate({rot})" d="M0 {-r}C{r*0.7} {-r*0.4} {r*0.7} {r*0.4} 0 {r}C{-r*0.7} {r*0.4} {-r*0.7} {-r*0.4} 0 {-r}Z" fill="{c}"/>'


def flake(x, y, r, c):
    arms = "".join(f'<path transform="rotate({a} {x} {y})" d="M{x} {y-r}V{y+r}"/>' for a in (0, 60, 120))
    return f'<g stroke="{c}" stroke-width="{max(2, r*0.28):.1f}" stroke-linecap="round">{arms}</g>'


def flame(x, y, r, c):
    k = r / 12
    return f'<path transform="translate({x} {y}) scale({k:.2f})" d="M0 12c-6 0-10-4-10-9 0-6 5-9 6-15 3 2 5 5 5 8 1-1 2-2 3-4 3 3 6 7 6 11 0 5-4 9-10 9z" fill="{c}"/>'


def drop(x, y, r, c):
    return f'<path d="M{x} {y-r}C{x} {y-r} {x-r*0.7:.1f} {y} {x-r*0.7:.1f} {y+r*0.3:.1f}a{r*0.7:.1f} {r*0.7:.1f} 0 0 0 {r*1.4:.1f} 0C{x+r*0.7:.1f} {y} {x} {y-r} {x} {y-r}z" fill="{c}"/>'


def note(x, y, r, c):
    return (f'<ellipse cx="{x-r*0.35:.1f}" cy="{y+r*0.55:.1f}" rx="{r*0.42:.1f}" ry="{r*0.32:.1f}" fill="{c}"/>'
            f'<rect x="{x:.1f}" y="{y-r:.1f}" width="{r*0.2:.1f}" height="{r*1.55:.1f}" rx="{r*0.1:.1f}" fill="{c}"/>'
            f'<path d="M{x+r*0.1:.1f} {y-r:.1f}q{r*0.6:.1f} {r*0.2:.1f} {r*0.6:.1f} {r*0.7:.1f}" fill="none" stroke="{c}" stroke-width="{r*0.2:.1f}" stroke-linecap="round"/>')


def bolt(x, y, r, c):
    k = r / 12
    return f'<path transform="translate({x} {y}) scale({k:.2f})" d="M2-12L-7 2h6l-2 10 10-14H1z" fill="{c}" stroke="{c}" stroke-width="1.5" stroke-linejoin="round"/>'


def confetti(x, y, r, c, rot=20):
    return f'<rect transform="rotate({rot} {x} {y})" x="{x-r*0.35:.1f}" y="{y-r*0.6:.1f}" width="{r*0.7:.1f}" height="{r*1.2:.1f}" rx="{r*0.15:.1f}" fill="{c}"/>'


def dot(x, y, r, c):
    return f'<circle cx="{x}" cy="{y}" r="{r}" fill="{c}"/>'


SHAPE = {
    "spr_fx_star": lambda x, y, r, c, i: star(x, y, r, c, i * 17),
    "spr_fx_spark": lambda x, y, r, c, i: spark(x, y, r, c),
    "spr_fx_heart": lambda x, y, r, c, i: heart(x, y, r, c),
    "spr_bg_bubble": lambda x, y, r, c, i: bubble(x, y, r * 0.8, c),
    "spr_fx_leaf": lambda x, y, r, c, i: leaf(x, y, r, c, 20 + i * 40),
    "spr_fx_flake": lambda x, y, r, c, i: flake(x, y, r, c),
    "spr_fx_flame": lambda x, y, r, c, i: flame(x, y, r, c),
    "spr_fx_drop": lambda x, y, r, c, i: drop(x, y, r, c),
    "spr_fx_note": lambda x, y, r, c, i: note(x, y, r, c),
    "spr_fx_bolt": lambda x, y, r, c, i: bolt(x, y, r, c),
    "spr_fx_confetti": lambda x, y, r, c, i: confetti(x, y, r, c, i * 47),
    "spr_fx_dot": lambda x, y, r, c, i: dot(x, y, r * 0.5, c),
}


def mini_ball(x, y, r, fill="#FF6B57"):
    return (f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}"/><circle cx="{x-r*0.38:.1f}" cy="{y-r*0.45:.1f}" r="{r*0.18:.1f}" fill="#FFFFFF" fill-opacity="0.6"/>'
            f'<circle cx="{x-r*0.3:.1f}" cy="{y-r*0.05:.1f}" r="{r*0.13:.1f}" fill="{INK}"/><circle cx="{x+r*0.3:.1f}" cy="{y-r*0.05:.1f}" r="{r*0.13:.1f}" fill="{INK}"/>'
            f'<path d="M{x-r*0.22:.1f} {y+r*0.3:.1f}q{r*0.22:.1f} {r*0.18:.1f} {r*0.44:.1f} 0" fill="none" stroke="{INK}" stroke-width="{r*0.12:.1f}" stroke-linecap="round"/>')


def trail_icon(t):
    head, tail = t.get("head", "#FF6B57"), t.get("tail", "#FF6B57")
    width = 16 * t.get("width", 1)
    if t.get("rainbow"):
        stops = "".join(f'<stop offset="{i/4}" stop-color="{c}"/>' for i, c in enumerate(RB))
    else:
        stops = f'<stop offset="0" stop-color="{tail}" stop-opacity="0.15"/><stop offset="1" stop-color="{head}" stop-opacity="0.85"/>'
    defs = f'<defs><linearGradient id="tg" x1="0" y1="1" x2="1" y2="0">{stops}</linearGradient></defs>'
    body = f'<path d="M18 112Q30 70 86 44" fill="none" stroke="url(#tg)" stroke-width="{width:.1f}" stroke-linecap="round"/>'
    shape = t.get("shape")
    if shape:
        for i, (px, py, pr) in enumerate(((30, 96, 7), (46, 76, 9), (64, 62, 8), (24, 76, 6))):
            body += SHAPE[shape](px, py, pr, head if i % 2 == 0 else tail, i)
    return svg(body + mini_ball(92, 38, 20), defs)


def hit_icon(h):
    colors = h.get("colors", ["#FF6B57"])
    shape = h.get("shape", "spr_fx_dot")
    body = f'<rect x="24" y="100" width="80" height="16" rx="8" fill="#1E9C90"/><rect x="24" y="96" width="80" height="15" rx="7.5" fill="#2EC4B6"/>'
    count = min(8, max(4, h.get("count", 6)))
    for i in range(count):
        a = math.radians(-160 + i * (140 / (count - 1)))
        d = 34 + (i % 2) * 12
        body += SHAPE[shape](64 + math.cos(a) * d, 84 + math.sin(a) * d, 8, colors[i % len(colors)], i)
    return svg(body + mini_ball(64, 78, 14))


def party_icon(p):
    style, shape = p.get("style"), p.get("shape", "spr_fx_confetti")
    body = ""
    if style == "Fireworks":
        for cx, cy, c in ((44, 46, "#FF6B57"), (86, 36, "#FFC93C"), (78, 84, "#9B6BFF")):
            for i in range(10):
                a = math.radians(i * 36)
                body += dot(cx + math.cos(a) * 18, cy + math.sin(a) * 18, 3.6, c)
            body += dot(cx, cy, 4, c)
    elif style == "PingiParty":
        for x, y, f in ((34, 80, "#FF6B57"), (64, 54, "#4FD1B0"), (94, 80, "#9B6BFF")):
            body += mini_ball(x, y, 17, f)
        body += star(64, 22, 7, "#FFC93C") + star(22, 46, 5, "#FFC93C") + star(106, 46, 5, "#FFC93C")
    elif style == "Rain":
        palette = RB if shape == "spr_fx_confetti" else (["#FF8FB8", "#FF4D7E"] if shape == "spr_fx_heart" else ["#8FD16A", "#3E9C6A", "#FFC93C"])
        for i, (x, y) in enumerate(((22, 20), (52, 34), (84, 16), (108, 40), (34, 62), (70, 70), (100, 84), (20, 100), (58, 108), (92, 114))):
            body += SHAPE[shape](x, y, 8, palette[i % len(palette)], i)
    elif style == "Stars":
        for i, (x, y, r) in enumerate(((64, 30, 14), (34, 54, 9), (94, 54, 9), (22, 86, 7), (106, 86, 7), (50, 92, 6), (78, 96, 6))):
            body += star(x, y, r, ["#FFC93C", "#FFE27A", "#FF9A5A"][i % 3], i * 12)
    else:
        for i, (x, y) in enumerate(((26, 34), (60, 22), (98, 34), (40, 66), (84, 62), (22, 98), (64, 96), (104, 96))):
            body += confetti(x, y, 9, RB[i % 5], i * 41)
    return svg(body)


FRAME_RING = {
    "": ("#D9D3CC", ""),
    "frame--crown": ("#FFC93C", '<path d="M40 30L36 8l14 12 14-18 14 18 14-12-4 22z" fill="#FFB300" stroke="#E59A00" stroke-width="2" stroke-linejoin="round"/><circle cx="64" cy="14" r="4" fill="#FF6B57"/>'),
    "frame--fire": ("#FF7A45", "".join(flame(x, y, 11, c) for x, y, c in ((30, 34, "#FF7A45"), (98, 34, "#FF7A45"), (64, 18, "#FFB443")))),
    "frame--rainbow": ("url(#rbr)", ""),
    "frame--ice": ("#9FE3FF", spark(98, 28, 9, "#FFFFFF") + spark(28, 98, 6, "#E8FAFF")),
    "frame--star": ("#FFE27A", "".join(star(x, y, 8, "#FFC93C", i * 20) for i, (x, y) in enumerate(((64, 14), (112, 64), (16, 64))))),
    "frame--galaxy": ("#7C5CFF", "".join(dot(x, y, r, "#FFFFFF") for x, y, r in ((22, 30, 2.5), (104, 26, 3), (110, 96, 2), (18, 100, 2.5)))),
    "frame--wave": ("#4FA8FF", '<path d="M14 104q12-10 24 0t24 0 24 0 24 0 24 0" fill="none" stroke="#9FDCFF" stroke-width="6" stroke-linecap="round"/>'),
}


def frame_icon(f):
    ring, extra = FRAME_RING[f.get("ui", "")]
    defs = '<defs><linearGradient id="rbr" x1="0" y1="0" x2="1" y2="1">' + "".join(f'<stop offset="{i/4}" stop-color="{c}"/>' for i, c in enumerate(RB)) + '</linearGradient></defs>'
    body = f'<circle cx="64" cy="64" r="40" fill="#FFF4EC"/>' + mini_ball(64, 64, 26) + f'<circle cx="64" cy="64" r="42" fill="none" stroke="{ring}" stroke-width="{4 if not f.get("ui") else 9}"/>' + extra
    return svg(body, defs)


def theme_swatch(t):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128"><rect width="128" height="128" rx="26" fill="{t["bg"]}"/>'
            f'<circle cx="26" cy="30" r="34" fill="{t["a"]}"/><circle cx="104" cy="100" r="40" fill="{t["b"]}"/></svg>')


def turkish_names(path):
    names = {}
    try:
        import loc_strings as table
        rows, _ = table.read_table()
        for key, row in rows.items():
            if row.get("tr"): names[key] = row["tr"]
    except Exception:
        pass
    if path:
        spec = importlib.util.spec_from_file_location("names", path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        for key, values in module.T.items(): names[key] = values[0]
    return names


def main():
    spec = json.load(open(SPEC, encoding="utf-8"))
    names = turkish_names(sys.argv[sys.argv.index("--names") + 1] if "--names" in sys.argv else None)
    for key in ("faces", "paddles", "themes", "trails", "hits", "parties", "frames", "sets", "seasons"):
        for item in spec[key]: item["title"] = names.get(item["name"], item["name"])
    os.makedirs(OUT, exist_ok=True)
    made = {}
    for t in spec["trails"]: made[t["id"]] = trail_icon(t)
    for h in spec["hits"]: made[h["id"]] = hit_icon(h)
    for p in spec["parties"]: made[p["id"]] = party_icon(p)
    for f in spec["frames"]: made[f["id"]] = frame_icon(f)
    for name, text in made.items():
        with open(os.path.join(OUT, f"spr_icon_{name}.svg"), "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text + "\n")
    write_sheet(spec, made)
    print(len(made), "icons")


def img(text):
    data = base64.b64encode(text.encode("utf-8")).decode("ascii")
    return f'<img src="data:image/svg+xml;base64,{data}" alt="">'


def card(title, art, sub):
    if isinstance(art, (list, tuple)):
        art = "".join(img(a) for a in art)
    else:
        art = img(art)
    return f'<div class="c"><div class="a">{art}</div><b>{title}</b><span>{sub}</span></div>'


def price(item):
    if item.get("unlock") == "Free": return "Ücretsiz, varsayılan"
    if item.get("unlock") == "SetReward": return "Set ödülü"
    tag = {"Showcase": " · sadece vitrinde", "Season": " · Kış Şenliği"}.get(item.get("unlock"), "")
    tier = {"Common": "Yaygın", "Rare": "Nadir", "Epic": "Destansı", "Legendary": "Efsane"}.get(item.get("tier"), "")
    return f'{tier} · {item.get("gold", 0):,} altın{tag}'.replace(",", ".")


def write_sheet(spec, icons):
    faces = "".join(card(f["title"], (skins.BALLS[f["art"]]("idle"), skins.BALLS[f["art"]]("happy")), price(f)) for f in spec["faces"])
    paddles = "".join(card(p["title"], skins.PADDLES[p["art"]], price(p)) for p in spec["paddles"])
    themes = "".join(card(t["title"], theme_swatch(t), price(t)) for t in spec["themes"])
    def group(key):
        return "".join(card(i["title"], icons[i["id"]], price(i)) for i in spec[key])
    titles = {i["id"]: i["title"] for k in ("faces", "paddles", "themes", "trails", "hits", "parties", "frames") for i in spec[k]}
    sets = "".join(f'<div class="set"><b>{s["title"]}</b><span>{" + ".join(titles[x] for x in s["items"])}</span><span class="r">Ödül: {titles[s["reward"]]}</span></div>' for s in spec["sets"])
    html = f'''<!doctype html><html lang="tr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Pingi Pongi — Yeni eşyalar</title>
<link href="https://fonts.googleapis.com/css2?family=Fredoka:wght@500;600;700&family=Inter:wght@400;600&display=swap" rel="stylesheet">
<style>body{{margin:0;background:#131316;color:#ececf1;font-family:Inter,sans-serif}}.w{{max-width:1240px;margin:0 auto;padding:32px 16px 80px}}h1{{margin:0 0 6px}}h2{{margin:40px 0 14px}}p{{color:#b9b9c4;margin:0}}
.g{{display:grid;grid-template-columns:repeat(auto-fill,minmax(170px,1fr));gap:14px}}.c{{background:#FFF1E0;border-radius:18px;padding:12px;display:flex;flex-direction:column;align-items:center;gap:4px;color:#2B2D42;box-shadow:0 4px 0 #F0D9C0}}
.c .a{{display:flex;gap:4px;justify-content:center;align-items:center;min-height:90px}}.c .a img{{width:80px;height:80px}}.c b{{font:600 15px Fredoka,sans-serif;text-align:center}}.c span{{font-size:11.5px;color:#6B6D82;text-align:center}}
.pad .a img{{width:150px;height:auto}}.set{{background:#1c1c22;border:1px solid #2a2a33;border-radius:14px;padding:12px 14px;display:flex;flex-direction:column;gap:2px}}.set span{{color:#b9b9c4;font-size:13px}}.set .r{{color:#ffd65a}}</style></head><body><div class="w">
<h1>Yeni eşyalar: çizim sayfası</h1><p>Koleksiyon genişlemesinin tüm yeni çizimleri. Pingi'lerin normal ve mutlu ifadesi yan yana. Efektlerin oyundaki hareketli hali seçenek sayfasında (shop/index.html).</p>
<h2>Pingi (7)</h2><div class="g">{faces}</div>
<h2>Raket (5)</h2><div class="g pad">{paddles}</div>
<h2>Tema (3)</h2><div class="g">{themes}</div>
<h2>Top izi (9)</h2><div class="g">{group("trails")}</div>
<h2>Vuruş efekti (7)</h2><div class="g">{group("hits")}</div>
<h2>Kutlama (7)</h2><div class="g">{group("parties")}</div>
<h2>Profil çerçevesi (8)</h2><div class="g">{group("frames")}</div>
<h2>Setler (5)</h2><div class="g" style="grid-template-columns:repeat(auto-fill,minmax(280px,1fr))">{sets}</div>
</div></body></html>'''
    os.makedirs(os.path.dirname(SHEET), exist_ok=True)
    with open(SHEET, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(html)


if __name__ == "__main__":
    main()
