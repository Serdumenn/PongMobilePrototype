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


def champ(e):
    defs = f'<defs><clipPath id="hb"><circle cx="{C}" cy="{C}" r="{R}"/></clipPath></defs>'
    band = (f'<g clip-path="url(#hb)"><rect x="0" y="{C-52}" width="200" height="19" fill="#FF5A6E"/>'
            f'<rect x="0" y="{C-45}" width="200" height="4" fill="#FFFFFF" fill-opacity="0.9"/></g>')
    tails = (f'<path d="M{C+70} {C-46}q16 2 24 14q-14-2-22-6z" fill="#FF5A6E"/>'
             f'<path d="M{C+70} {C-40}q10 10 10 26q-8-10-14-18z" fill="#E8475B"/>'
             f'<circle cx="{C+70}" cy="{C-43}" r="7" fill="#E8475B"/>')
    shine = f'<circle cx="{C-46}" cy="{C-22}" r="8" fill="#FFFFFF" fill-opacity="0.55"/>'
    return wrap(f'<circle cx="{C}" cy="{C}" r="{R}" fill="#4FA8FF"/>' + band + shine + face(e, cheek="#FF8FB1") + tails, defs)


def bunny(e):
    fur, inner = "#FFE3EE", "#FFB3CF"
    ears = (f'<ellipse cx="{C-30}" cy="{C-78}" rx="17" ry="40" fill="{fur}" transform="rotate(-12 {C-30} {C-78})"/>'
            f'<ellipse cx="{C+30}" cy="{C-78}" rx="17" ry="40" fill="{fur}" transform="rotate(12 {C+30} {C-78})"/>'
            f'<ellipse cx="{C-30}" cy="{C-76}" rx="8" ry="27" fill="{inner}" transform="rotate(-12 {C-30} {C-76})"/>'
            f'<ellipse cx="{C+30}" cy="{C-76}" rx="8" ry="27" fill="{inner}" transform="rotate(12 {C+30} {C-76})"/>')
    tooth = f'<rect x="{C-6}" y="{C+24}" width="12" height="11" rx="3" fill="#FFFFFF" stroke="{INK}" stroke-width="2.5"/>' if e == "idle" else ""
    return wrap(ears + body(fur, 0.9) + face(e, cheek="#FF9ACB") + tooth)


def bear(e):
    fur, light = "#C98B5E", "#EBC29A"
    ears = (f'<circle cx="{C-54}" cy="{C-54}" r="23" fill="{fur}"/><circle cx="{C+54}" cy="{C-54}" r="23" fill="{fur}"/>'
            f'<circle cx="{C-54}" cy="{C-54}" r="12" fill="{light}"/><circle cx="{C+54}" cy="{C-54}" r="12" fill="{light}"/>')
    muzzle = f'<ellipse cx="{C}" cy="{C+24}" rx="34" ry="26" fill="{light}"/><ellipse cx="{C}" cy="{C+8}" rx="9" ry="6.5" fill="#5A3A2A"/>'
    return wrap(ears + body(fur) + muzzle + face(e, cheek="#F28F7A"))


def penguin(e):
    dark, white = "#33415F", "#F5F7FF"
    defs = f'<defs><clipPath id="pg"><circle cx="{C}" cy="{C}" r="{R}"/></clipPath></defs>'
    mask = f'<g clip-path="url(#pg)"><ellipse cx="{C}" cy="{C+18}" rx="62" ry="64" fill="{white}"/></g>'
    beak = f'<path d="M{C-9} {C+5}h18l-9 10z" fill="#FFB443" stroke="#FFB443" stroke-width="3" stroke-linejoin="round"/>'
    return wrap(f'<circle cx="{C}" cy="{C}" r="{R}" fill="{dark}"/>' + mask + f'<circle cx="{C-31}" cy="{C-52}" r="10" fill="#FFFFFF" fill-opacity="0.45"/>' + face(e, cheek="#FF9ACB") + beak, defs)


def octo(e):
    skin = "#B98CFF"
    legs = "".join(f'<path d="M{x} {C+48}q{dx} 34 {dx*2} 40" fill="none" stroke="{skin}" stroke-width="20" stroke-linecap="round"/>'
                   for x, dx in ((C-52, -8), (C-20, -4), (C+20, 4), (C+52, 8)))
    dots = "".join(f'<circle cx="{x}" cy="{y}" r="4" fill="#FFFFFF" fill-opacity="0.6"/>' for x, y in ((C-62, C+78), (C-26, C+84), (C+26, C+84), (C+62, C+78)))
    spots = f'<circle cx="{C+44}" cy="{C-40}" r="8" fill="#A372F5"/><circle cx="{C+58}" cy="{C-18}" r="5" fill="#A372F5"/>'
    return wrap(legs + dots + body(skin) + spots + face(e, cheek="#FF9ACB"))


def dragon(e):
    skin, deep = "#6FD49B", "#3E9C6A"
    horns = (f'<path d="M{C-46} {C-58}q-10-30 6-44q4 22 16 34z" fill="#FFC93C"/>'
             f'<path d="M{C+46} {C-58}q10-30-6-44q-4 22-16 34z" fill="#FFC93C"/>')
    spikes = (f'<path d="M{C-14} {C-74}l14-22 14 22z" fill="{deep}"/>'
              f'<path d="M{C+18} {C-70}l11-17 9 20z" fill="{deep}"/><path d="M{C-38} {C-64}l7-18 12 13z" fill="{deep}"/>')
    nose = f'<circle cx="{C-7}" cy="{C+8}" r="3.4" fill="{deep}"/><circle cx="{C+7}" cy="{C+8}" r="3.4" fill="{deep}"/>'
    belly = f'<path d="M{C-40} {C+52}q40 26 80 0" fill="none" stroke="#B6F0CC" stroke-width="10" stroke-linecap="round"/>'
    return wrap(horns + spikes + body(skin) + belly + face(e, cheek="#FFB3A0") + nose)


def fox(e):
    fur, cream = "#FF8A3D", "#FFF3E6"
    ears = (f'<path d="M{C-64} {C-28}L{C-60} {C-94}L{C-16} {C-66}Z" fill="{fur}"/><path d="M{C+64} {C-28}L{C+60} {C-94}L{C+16} {C-66}Z" fill="{fur}"/>'
            f'<path d="M{C-61} {C-74}L{C-60} {C-94}L{C-44} {C-82}Z" fill="{INK}"/><path d="M{C+61} {C-74}L{C+60} {C-94}L{C+44} {C-82}Z" fill="{INK}"/>')
    defs = f'<defs><clipPath id="fx"><circle cx="{C}" cy="{C}" r="{R}"/></clipPath></defs>'
    cheeks = f'<g clip-path="url(#fx)"><path d="M{C-90} {C+4}Q{C-40} {C+2} {C} {C+40}Q{C+40} {C+2} {C+90} {C+4}V{C+90}H{C-90}Z" fill="{cream}"/></g>'
    nose = f'<ellipse cx="{C}" cy="{C+10}" rx="7" ry="5" fill="{INK}"/>'
    return wrap(ears + body(fur) + cheeks + face(e, cheek="#FFB08A") + nose, defs)


def snow(e):
    white, line = "#F4F9FF", "#C9DDF2"
    hat = (f'<rect x="{C-44}" y="{C-80}" width="88" height="12" rx="6" fill="#3A3F6B"/>'
           f'<rect x="{C-28}" y="{C-118}" width="56" height="44" rx="6" fill="#3A3F6B"/>'
           f'<rect x="{C-28}" y="{C-90}" width="56" height="9" fill="#FF6B57"/>')
    rim = f'<circle cx="{C}" cy="{C}" r="{R}" fill="{white}" stroke="{line}" stroke-width="5"/>'
    carrot = f'<path d="M{C-2} {C+3}L{C+30} {C+10}L{C-2} {C+15}Z" fill="#FF8A3D" stroke="#FF8A3D" stroke-width="3" stroke-linejoin="round"/>'
    flakes = f'<circle cx="{C+62}" cy="{C+52}" r="4" fill="#BFE3FF"/><circle cx="{C-66}" cy="{C+40}" r="3" fill="#BFE3FF"/>'
    return wrap(rim + f'<circle cx="{C-31}" cy="{C-38}" r="12.5" fill="#FFFFFF"/>' + face(e, cheek="#FFB3CF") + carrot + hat + flakes)


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
    "champ":  champ,
    "bunny":  bunny,
    "bear":   bear,
    "penguin": penguin,
    "octo":   octo,
    "dragon": dragon,
    "fox":    fox,
    "snow":   snow,
}

PW, PH, CAP, BEND, LIP = 430, 110, 55, 12, 21


def bar_path(dy=0):
    mid = PW / 2
    return (f"M{CAP} {BEND + dy}Q{mid} {-BEND + dy} {PW - CAP} {BEND + dy}"
            f"A{CAP} {CAP} 0 0 1 {PW - CAP} {PH + BEND + dy}"
            f"Q{mid} {PH - BEND + dy} {CAP} {PH + BEND + dy}"
            f"A{CAP} {CAP} 0 0 1 {CAP} {BEND + dy}Z")


def top_point(t):
    x = (1 - t) ** 2 * CAP + 2 * (1 - t) * t * PW / 2 + t ** 2 * (PW - CAP)
    y = (1 - t) ** 2 * BEND + 2 * (1 - t) * t * -BEND + t ** 2 * BEND
    return x, y


def shine(t1=0.04, t2=0.42, depth=25):
    x1, y1 = top_point(t1)
    x2, y2 = top_point(t2)
    cx = (1 - t1) * (1 - t2) * CAP + ((1 - t1) * t2 + t1 * (1 - t2)) * PW / 2 + t1 * t2 * (PW - CAP)
    cy = (1 - t1) * (1 - t2) * BEND + ((1 - t1) * t2 + t1 * (1 - t2)) * -BEND + t1 * t2 * BEND
    return (f'<path d="M{x1:.1f} {y1 + depth:.1f}Q{cx:.1f} {cy + depth:.1f} {x2:.1f} {y2 + depth:.1f}" '
            f'fill="none" stroke="#FFFFFF" stroke-opacity="0.3" stroke-width="14" stroke-linecap="round"/>')


def paddle(face_fill, edge, extra="", defs=""):
    height = PH + BEND + LIP
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{PW}" height="{height}" viewBox="0 0 {PW} {height}">{defs}'
            f'<path d="{bar_path(LIP)}" fill="{edge}"/><path d="{bar_path()}" fill="{face_fill}"/>{extra}{shine()}</svg>')

clip = f'<clipPath id="pc"><path d="{bar_path()}"/></clipPath>'
stripes = '<g clip-path="url(#pc)">' + "".join(f'<path d="M{x} 0h28l-50 110h-28z" fill="#FF8FA3"/>' for x in range(0, 520, 64)) + '</g>'
grain = '<g clip-path="url(#pc)" fill="none" stroke="#B97A45" stroke-width="5" stroke-linecap="round"><path d="M40 40q80-14 160 0t190 0"/><path d="M20 72q100 16 200 0t190-4"/><path d="M230 26q40 10 80 0"/></g>'
rainbow_d = '<defs><linearGradient id="rb" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#FF6B57"/><stop offset="0.25" stop-color="#FFC93C"/><stop offset="0.5" stop-color="#7ED957"/><stop offset="0.75" stop-color="#4FA8FF"/><stop offset="1" stop-color="#9B6BFF"/></linearGradient></defs>'
gold_d = '<defs><linearGradient id="gp" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFE37A"/><stop offset="1" stop-color="#F2B400"/></linearGradient></defs>'
sky_clip = f'<clipPath id="sc"><path d="{bar_path()}"/></clipPath>'
night_stars = ('<g clip-path="url(#sc)" fill="#FFE27A">'
               + "".join(f'<path d="M{x} {y}l4 9 9 4-9 4-4 9-4-9-9-4 9-4z"/>' for x, y in ((92, 30), (168, 66), (240, 26), (300, 70), (372, 38)))
               + '</g><g fill="#FFFFFF" fill-opacity="0.75">'
               + "".join(f'<circle cx="{x}" cy="{y}" r="4"/>' for x, y in ((130, 80), (206, 40), (270, 88), (340, 22), (60, 70)))
               + '</g>')
aurora_d = '<defs><linearGradient id="au" x1="0" y1="0" x2="1" y2="0.4"><stop offset="0" stop-color="#3DDBB4"/><stop offset="0.5" stop-color="#7C83FD"/><stop offset="1" stop-color="#FF8FC7"/></linearGradient></defs>'
comet_d = ('<defs><linearGradient id="cm" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#FF5E8A"/><stop offset="1" stop-color="#FFB547"/></linearGradient>'
           f'<clipPath id="cc"><path d="{bar_path()}"/></clipPath></defs>')
comet_tail = ('<g clip-path="url(#cc)" stroke="#FFFFFF" stroke-linecap="round" fill="none">'
              '<path d="M150 62H318" stroke-opacity="0.4" stroke-width="12"/>'
              '<path d="M205 42H322" stroke-opacity="0.28" stroke-width="6"/>'
              '<path d="M215 82H322" stroke-opacity="0.28" stroke-width="6"/></g>'
              '<circle cx="342" cy="62" r="27" fill="#FFFFFF" fill-opacity="0.28"/><circle cx="342" cy="62" r="16" fill="#FFFFFF"/>')
pc_clip = "<defs>" + f'<clipPath id="pk"><path d="{bar_path()}"/></clipPath>' + "</defs>"
melon = ('<g clip-path="url(#pk)"><rect x="0" y="92" width="430" height="40" fill="#BDF0A8"/>'
         + "".join(f'<ellipse cx="{x}" cy="{y}" rx="6" ry="9" fill="#2B2D42" transform="rotate(-12 {x} {y})"/>' for x, y in ((96, 50), (156, 70), (214, 44), (274, 72), (334, 50)))
         + '</g>')
lava_d = ('<defs><linearGradient id="lv" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#FF4D2E"/><stop offset="1" stop-color="#FF8A2B"/></linearGradient>'
          f'<clipPath id="lc"><path d="{bar_path()}"/></clipPath></defs>')
lava = ('<g clip-path="url(#lc)" fill="none" stroke="#B8361B" stroke-width="7" stroke-linecap="round"><path d="M70 30l30 26-18 30"/><path d="M250 22l-16 34 30 28"/><path d="M330 80l26-24"/></g>'
        '<g fill="#FFD23F"><circle cx="150" cy="48" r="9"/><circle cx="300" cy="44" r="7"/><circle cx="196" cy="84" r="6"/></g>')
ice = ('<g clip-path="url(#pk)" fill="#FFFFFF"><path d="M60 20l70 0-40 90-50 0z" fill-opacity="0.35"/><path d="M250 18l50 0-34 96-36 0z" fill-opacity="0.28"/></g>'
       '<path d="M360 34l4 10 10 4-10 4-4 10-4-10-10-4 10-4z" fill="#FFFFFF"/>')
bamboo = ('<g clip-path="url(#pk)" stroke="#4F8A2F" stroke-width="8"><path d="M140 0v140"/><path d="M290 0v140"/></g>'
          '<g fill="#5E9E3B"><path d="M150 40q24-22 46-12q-20 18-46 12z"/><path d="M300 74q26-18 46-4q-22 14-46 4z"/></g>')
snowflake_flakes = ('<g clip-path="url(#pk)" stroke="#FFFFFF" stroke-width="5" stroke-linecap="round">'
                    + "".join(f'<path d="M{x} {y-14}v28M{x-12} {y-7}l24 14M{x-12} {y+7}l24-14"/>' for x, y in ((100, 46), (210, 72), (320, 42)))
                    + '</g>')

PADDLES = {
    "teal":    paddle("#2EC4B6", "#1E9C90"),
    "coral":   paddle("#FF6B57", "#D94C39"),
    "candy":   paddle("#FFFFFF", "#E9C7C2", stripes, "<defs>" + clip + "</defs>"),
    "wood":    paddle("#D9A066", "#9C6232", grain, "<defs>" + clip + "</defs>"),
    "rainbow": paddle("url(#rb)", "#6B5BD6", "", rainbow_d),
    "gold":    paddle("url(#gp)", "#C98F00", '<path d="M360 30l4 10 10 4-10 4-4 10-4-10-10-4 10-4z" fill="#FFFFFF"/>', gold_d),
    "starry":  paddle("#2E3270", "#1C1F4D", night_stars, "<defs>" + sky_clip + "</defs>"),
    "aurora":  paddle("url(#au)", "#5B4BC4", '<path d="M372 28l4 10 10 4-10 4-4 10-4-10-10-4 10-4z" fill="#FFFFFF"/>', aurora_d),
    "comet":   paddle("url(#cm)", "#C9416A", comet_tail, comet_d),
    "melon":   paddle("#FF5F6D", "#3FA34D", melon, pc_clip),
    "lava":    paddle("url(#lv)", "#8E2A14", lava, lava_d),
    "ice":     paddle("#BDEBFF", "#7CC6F0", ice, pc_clip),
    "bamboo":  paddle("#9BD86B", "#5E9E3B", bamboo, pc_clip),
    "snowflake": paddle("#7FB6FF", "#4F86D9", snowflake_flakes, pc_clip),
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
