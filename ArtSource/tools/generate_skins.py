import os
INK = "#2B2D42"
C = 100
R = 78

def face(expr, eye_fill=INK, cheek="#FF9A8A", eyes=True, pupil=None):
    s = []
    if cheek:
        s.append(f'<ellipse cx="{C-40}" cy="{C+18}" rx="10" ry="6" fill="{cheek}"/><ellipse cx="{C+40}" cy="{C+18}" rx="10" ry="6" fill="{cheek}"/>')
    if expr == "idle":
        if eyes: s.append(f'<ellipse cx="{C-21.5}" cy="{C-8}" rx="10" ry="13.3" fill="{eye_fill}"/><ellipse cx="{C+21.5}" cy="{C-8}" rx="10" ry="13.3" fill="{eye_fill}"/>')
        s.append(f'<path d="M{C-17.5} {C+21.5}q17.5 14 35 0" fill="none" stroke="{INK}" stroke-width="8.6" stroke-linecap="round"/>')
    elif expr == "happy":
        if eyes: s.append(f'<path d="M{C-33} {C-4}q11.5-15 23 0M{C+10} {C-4}q11.5-15 23 0" fill="none" stroke="{eye_fill}" stroke-width="8.6" stroke-linecap="round"/>')
        s.append(f'<path d="M{C-23} {C+14}H{C+23}Q{C+23} {C+42} {C} {C+42}Q{C-23} {C+42} {C-23} {C+14}Z" fill="{INK}"/><ellipse cx="{C}" cy="{C+34}" rx="11" ry="6" fill="#FF9A8A"/>')
    elif expr == "wow":
        if eyes:
            s.append(f'<ellipse cx="{C-22}" cy="{C-10}" rx="12.5" ry="15.5" fill="{eye_fill}"/><ellipse cx="{C+22}" cy="{C-10}" rx="12.5" ry="15.5" fill="{eye_fill}"/>')
            if pupil:
                s.append(f'<circle cx="{C-22}" cy="{C-9}" r="9" fill="{pupil}"/><circle cx="{C+22}" cy="{C-9}" r="9" fill="{pupil}"/>')
                s.append(f'<circle cx="{C-25}" cy="{C-12.5}" r="3.4" fill="#FFFFFF"/><circle cx="{C+19}" cy="{C-12.5}" r="3.4" fill="#FFFFFF"/>')
            else:
                s.append(f'<circle cx="{C-26}" cy="{C-16}" r="4.6" fill="#FFFFFF"/><circle cx="{C+18}" cy="{C-16}" r="4.6" fill="#FFFFFF"/><circle cx="{C-18.5}" cy="{C-4}" r="2.3" fill="#FFFFFF"/><circle cx="{C+25.5}" cy="{C-4}" r="2.3" fill="#FFFFFF"/>')
        s.append(f'<ellipse cx="{C}" cy="{C+31}" rx="12.5" ry="14" fill="{INK}"/><ellipse cx="{C}" cy="{C+38}" rx="7.5" ry="5" fill="#FF9A8A"/>')
    else:
        if eyes: s.append(f'<path d="M{C-34} {C-8}q12-9 24 0M{C+10} {C-8}q12-9 24 0" fill="none" stroke="{eye_fill}" stroke-width="8.6" stroke-linecap="round"/>')
        s.append(f'<path d="M{C-17.5} {C+34}q17.5-14 35 0" fill="none" stroke="{INK}" stroke-width="8.6" stroke-linecap="round"/><path d="M{C+36} {C+2}c-5 8-7 11.5-7 14.5a7 7 0 0 0 14 0c0-3-2-6.5-7-14.5z" fill="#7FD3F7"/>')
    return "".join(s)

def body(fill, hl=0.55):
    return f'<circle cx="{C}" cy="{C}" r="{R}" fill="{fill}"/><circle cx="{C-31}" cy="{C-38}" r="12.5" fill="#FFFFFF" fill-opacity="{hl}"/>'

def wrap(inner, defs=""):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="200" height="200" viewBox="0 0 200 200">{defs}{inner}</svg>'

def simple(fill, cheek):
    return lambda e: wrap(body(fill) + face(e, cheek=cheek))

def panda(e):
    ears = f'<circle cx="{C-56}" cy="{C-56}" r="22" fill="{INK}"/><circle cx="{C+56}" cy="{C-56}" r="22" fill="{INK}"/>'
    ring = f'<circle cx="{C}" cy="{C}" r="{R+3}" fill="#E7D9C7"/>'
    patches = f'<ellipse cx="{C-22}" cy="{C-8}" rx="18" ry="21" fill="{INK}" transform="rotate(-18 {C-22} {C-8})"/><ellipse cx="{C+22}" cy="{C-8}" rx="18" ry="21" fill="{INK}" transform="rotate(18 {C+22} {C-8})"/>'
    return wrap(ears + ring + body("#FBFAF7", 0.9) + patches + face(e, eye_fill="#FFFFFF", cheek="#FFB3C1", pupil=INK))

def kitty(e):
    ears = (f'<path d="M{C-66} {C-30}L{C-58} {C-92}L{C-18} {C-68}Z" fill="#FF9F43"/><path d="M{C+66} {C-30}L{C+58} {C-92}L{C+18} {C-68}Z" fill="#FF9F43"/>'
            f'<path d="M{C-56} {C-44}L{C-53} {C-78}L{C-32} {C-64}Z" fill="#FFD1A6"/><path d="M{C+56} {C-44}L{C+53} {C-78}L{C+32} {C-64}Z" fill="#FFD1A6"/>')
    wh = f'<path d="M{C-58} {C+8}H{C-80}M{C-58} {C+18}L{C-78} {C+26}M{C+58} {C+8}H{C+80}M{C+58} {C+18}L{C+78} {C+26}" stroke="{INK}" stroke-opacity="0.55" stroke-width="3.5" stroke-linecap="round"/>'
    return wrap(ears + body("#FF9F43") + face(e, cheek="#FFB37A") + wh)

def froggy(e):
    g = "#7ED957"
    bumps = f'<circle cx="{C-32}" cy="{C-66}" r="24" fill="{g}"/><circle cx="{C+32}" cy="{C-66}" r="24" fill="{g}"/>'
    if e == "idle":
        eyes = f'<circle cx="{C-32}" cy="{C-68}" r="14" fill="#FFFFFF"/><circle cx="{C+32}" cy="{C-68}" r="14" fill="#FFFFFF"/><circle cx="{C-30}" cy="{C-66}" r="7.5" fill="{INK}"/><circle cx="{C+34}" cy="{C-66}" r="7.5" fill="{INK}"/>'
    elif e == "happy":
        eyes = f'<path d="M{C-44} {C-64}q12-14 24 0M{C+20} {C-64}q12-14 24 0" fill="none" stroke="{INK}" stroke-width="8" stroke-linecap="round"/>'
    elif e == "wow":
        eyes = (f'<circle cx="{C-32}" cy="{C-68}" r="16.5" fill="#FFFFFF"/><circle cx="{C+32}" cy="{C-68}" r="16.5" fill="#FFFFFF"/><circle cx="{C-32}" cy="{C-67}" r="10" fill="{INK}"/><circle cx="{C+32}" cy="{C-67}" r="10" fill="{INK}"/>'
                f'<circle cx="{C-35.5}" cy="{C-71}" r="3.6" fill="#FFFFFF"/><circle cx="{C+28.5}" cy="{C-71}" r="3.6" fill="#FFFFFF"/>')
    else:
        eyes = f'<path d="M{C-44} {C-68}q12-8 24 0M{C+20} {C-68}q12-8 24 0" fill="none" stroke="{INK}" stroke-width="8" stroke-linecap="round"/>'
    return wrap(bumps + body(g) + eyes + face(e, cheek="#FF9A8A", eyes=False))

def golden(e):
    defs = '<defs><linearGradient id="gd" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFE37A"/><stop offset="1" stop-color="#F2B400"/></linearGradient></defs>'
    crown = f'<path d="M{C-30} {C-66}L{C-34} {C-96}L{C-15} {C-80}L{C} {C-100}L{C+15} {C-80}L{C+34} {C-96}L{C+30} {C-66}Z" fill="#FFB300"/><circle cx="{C}" cy="{C-80}" r="5" fill="#FF6B57"/>'
    spark = f'<path d="M{C+66} {C-60}l4 10 10 4-10 4-4 10-4-10-10-4 10-4z" fill="#FFFFFF"/>'
    return wrap(body("url(#gd)", 0.7) + crown + face(e, cheek="#FFB067") + spark, defs)

def astro(e):
    glass = (f'<circle cx="{C}" cy="{C}" r="94" fill="#DDF4FF" fill-opacity="0.22" stroke="#FFFFFF" stroke-width="6" stroke-opacity="0.95"/>'
             f'<path d="M{C-62} {C-50}q24-34 66-40" fill="none" stroke="#FFFFFF" stroke-width="7" stroke-linecap="round" stroke-opacity="0.8"/>')
    star = f'<path d="M{C+60} {C+56}l3.5 8 8 3.5-8 3.5-3.5 8-3.5-8-8-3.5 8-3.5z" fill="#FFD65A"/>'
    return wrap(body("#6C8CFF", 0.5) + face(e, cheek="#FF9ACB") + glass + star)

def stella(e):
    glow = f'<circle cx="{C}" cy="{C}" r="{R+4}" fill="#B9BEFF" fill-opacity="0.55"/>'
    star_mark = f'<path d="M{C} {C-62}l6 12 13 2-9.5 9 2.3 13-11.8-6.2-11.8 6.2 2.3-13-9.5-9 13-2z" fill="#FFE27A"/>'
    twinkles = (f'<path d="M{C+60} {C-48}l3 7 7 3-7 3-3 7-3-7-7-3 7-3z" fill="#FFFFFF"/>'
                f'<path d="M{C-64} {C+40}l2.4 5.6 5.6 2.4-5.6 2.4-2.4 5.6-2.4-5.6-5.6-2.4 5.6-2.4z" fill="#FFFFFF" fill-opacity="0.85"/>')
    return wrap(glow + body("#7C83FD", 0.5) + star_mark + face(e, cheek="#FF9ACB") + twinkles)


EXPRESSIONS = ("idle", "happy", "sad", "wow")

BALLS = {
    "pingi":  simple("#FF6B57", "#FF9A8A"),
    "minty":  simple("#4FD1B0", "#FFA7B7"),
    "grape":  simple("#9B6BFF", "#FF9ACB"),
    "sunny":  simple("#FFC93C", "#FF9A5A"),
    "panda":  panda,
    "kitty":  kitty,
    "froggy": froggy,
    "golden": golden,
    "astro":  astro,
    "stella": stella,
}

def paddle(face_fill, edge, extra="", defs=""):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="430" height="131" viewBox="0 0 430 131">{defs}'
            f'<rect x="0" y="21" width="430" height="110" rx="55" fill="{edge}"/><rect x="0" y="0" width="430" height="110" rx="55" fill="{face_fill}"/>{extra}'
            f'<rect x="48" y="18" width="150" height="14" rx="7" fill="#FFFFFF" fill-opacity="0.3"/></svg>')

clip = '<clipPath id="pc"><rect x="0" y="0" width="430" height="110" rx="55"/></clipPath>'
stripes = '<g clip-path="url(#pc)">' + "".join(f'<path d="M{x} 0h28l-50 110h-28z" fill="#FF8FA3"/>' for x in range(0, 520, 64)) + '</g>'
grain = '<g clip-path="url(#pc)" fill="none" stroke="#B97A45" stroke-width="5" stroke-linecap="round"><path d="M40 40q80-14 160 0t190 0"/><path d="M20 72q100 16 200 0t190-4"/><path d="M230 26q40 10 80 0"/></g>'
rainbow_d = '<defs><linearGradient id="rb" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#FF6B57"/><stop offset="0.25" stop-color="#FFC93C"/><stop offset="0.5" stop-color="#7ED957"/><stop offset="0.75" stop-color="#4FA8FF"/><stop offset="1" stop-color="#9B6BFF"/></linearGradient></defs>'
gold_d = '<defs><linearGradient id="gp" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFE37A"/><stop offset="1" stop-color="#F2B400"/></linearGradient></defs>'
sky_clip = '<clipPath id="sc"><rect x="0" y="0" width="430" height="110" rx="55"/></clipPath>'
night_stars = ('<g clip-path="url(#sc)" fill="#FFE27A">'
               + "".join(f'<path d="M{x} {y}l4 9 9 4-9 4-4 9-4-9-9-4 9-4z"/>' for x, y in ((92, 30), (168, 66), (240, 26), (300, 70), (372, 38)))
               + '</g><g fill="#FFFFFF" fill-opacity="0.75">'
               + "".join(f'<circle cx="{x}" cy="{y}" r="4"/>' for x, y in ((130, 80), (206, 40), (270, 88), (340, 22), (60, 70)))
               + '</g>')
aurora_d = '<defs><linearGradient id="au" x1="0" y1="0" x2="1" y2="0.4"><stop offset="0" stop-color="#3DDBB4"/><stop offset="0.5" stop-color="#7C83FD"/><stop offset="1" stop-color="#FF8FC7"/></linearGradient></defs>'
PADDLES = {
    "teal":    paddle("#2EC4B6", "#1E9C90"),
    "coral":   paddle("#FF6B57", "#D94C39"),
    "candy":   paddle("#FFFFFF", "#E9C7C2", stripes, "<defs>" + clip + "</defs>"),
    "wood":    paddle("#D9A066", "#9C6232", grain, "<defs>" + clip + "</defs>"),
    "rainbow": paddle("url(#rb)", "#6B5BD6", "", rainbow_d),
    "gold":    paddle("url(#gp)", "#C98F00", '<path d="M360 30l4 10 10 4-10 4-4 10-4-10-10-4 10-4z" fill="#FFFFFF"/>', gold_d),
    "starry":  paddle("#2E3270", "#1C1F4D", night_stars, "<defs>" + sky_clip + "</defs>"),
    "aurora":  paddle("url(#au)", "#5B4BC4", '<path d="M372 28l4 10 10 4-10 4-4 10-4-10-10-4 10-4z" fill="#FFFFFF"/>', aurora_d),
}

PLAYER_PADDLES = {
    "coral": paddle("#FF6B57", "#D94C39"),
    "teal":  paddle("#2EC4B6", "#1E9C90"),
    "sun":   paddle("#FFC93C", "#E5A800"),
    "grape": paddle("#8C6CF2", "#6A4FD0"),
}

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BALL_DIR = os.path.join(ROOT, "Assets", "_Game", "Art", "Sprites", "Skins", "Balls")
PADDLE_DIR = os.path.join(ROOT, "Assets", "_Game", "Art", "Sprites", "Skins", "Paddles")
PLAYER_DIR = os.path.join(ROOT, "Assets", "_Game", "Art", "Sprites", "Players")

if __name__ == "__main__":
    os.makedirs(BALL_DIR, exist_ok=True)
    os.makedirs(PADDLE_DIR, exist_ok=True)
    for name, fn in BALLS.items():
        for e in EXPRESSIONS:
            with open(os.path.join(BALL_DIR, f"spr_ball_{name}_{e}.svg"), "w", encoding="utf-8") as f:
                f.write(fn(e))
    for name, svg in PADDLES.items():
        with open(os.path.join(PADDLE_DIR, f"spr_paddle_{name}.svg"), "w", encoding="utf-8") as f:
            f.write(svg)
    os.makedirs(PLAYER_DIR, exist_ok=True)
    for name, svg in PLAYER_PADDLES.items():
        with open(os.path.join(PLAYER_DIR, f"spr_paddle_player_{name}.svg"), "w", encoding="utf-8") as f:
            f.write(svg)
    print(len(BALLS) * len(EXPRESSIONS), "ball sprites,", len(PADDLES), "paddles,", len(PLAYER_PADDLES), "player paddles")
