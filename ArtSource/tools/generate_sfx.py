import math
import os
import random
import struct
import wave

RATE = 44100
PEAK = 10 ** (-3 / 20)
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Audio", "SFX")


def silence(seconds):
    return [0.0] * int(RATE * seconds)


def mix(base, layer, at=0.0, gain=1.0):
    start = int(RATE * at)
    need = start + len(layer)
    if need > len(base):
        base.extend([0.0] * (need - len(base)))
    for i, v in enumerate(layer):
        base[start + i] += v * gain
    return base


def tone(f0, f1, seconds, decay, partials=((1.0, 1.0),), glide=0.02, attack=0.002):
    n = int(RATE * seconds)
    out = []
    phases = [0.0] * len(partials)
    for i in range(n):
        t = i / RATE
        k = min(1.0, t / glide) if glide > 0 else 1.0
        f = f0 + (f1 - f0) * (1 - (1 - k) ** 3)
        env = min(1.0, t / attack) * math.exp(-t / decay)
        v = 0.0
        for p, (mult, amp) in enumerate(partials):
            phases[p] += 2 * math.pi * f * mult / RATE
            part_env = math.exp(-t / (decay / (1 + (mult - 1) * 0.6)))
            v += math.sin(phases[p]) * amp * part_env
        out.append(v * env)
    return out


def click(seconds=0.003, gain=0.35, seed=7):
    rng = random.Random(seed)
    n = int(RATE * seconds)
    return [(rng.random() * 2 - 1) * gain * (1 - i / n) for i in range(n)]


def marimba(freq, seconds=0.35, decay=0.09, bright=0.28):
    body = tone(freq * 1.03, freq, seconds, decay, ((1.0, 1.0), (4.0, bright), (10.0, bright * 0.25)), glide=0.012)
    return mix(body, click(0.002, 0.18), 0.0)


def fade_tail(samples, seconds=0.02):
    n = min(len(samples), int(RATE * seconds))
    for i in range(n):
        samples[len(samples) - n + i] *= 1 - i / n
    return samples


def normalize(samples, peak=PEAK):
    top = max(abs(v) for v in samples) or 1.0
    return [math.tanh(v / top * 1.1) / math.tanh(1.1) * peak for v in samples]


def note(name):
    names = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}
    semis = names[name[0]] + (1 if "#" in name else 0)
    octave = int(name[-1])
    return 440.0 * 2 ** ((semis - 9) / 12 + (octave - 4))


def paddle_hit():
    return marimba(note("C5"), 0.2, 0.06, 0.3)


def wall_bounce():
    s = tone(430, 320, 0.12, 0.035, ((1.0, 1.0), (2.0, 0.15)), glide=0.02)
    return mix(s, click(0.0015, 0.1, 3))


def perfect():
    s = tone(note("E6"), note("E6"), 0.5, 0.16, ((1.0, 1.0), (3.0, 0.12)))
    return mix(s, tone(note("B6"), note("B6"), 0.45, 0.12), 0.03, 0.55)


def ui_tap():
    return tone(620, 1100, 0.085, 0.03, ((1.0, 1.0), (2.0, 0.1)), glide=0.045)


def ui_back():
    return tone(900, 520, 0.095, 0.034, ((1.0, 1.0), (2.0, 0.1)), glide=0.06)


def new_best():
    s = silence(0.0)
    for i, n in enumerate(("C5", "E5", "G5")):
        mix(s, marimba(note(n), 0.3, 0.08), i * 0.09)
    mix(s, marimba(note("C6"), 0.7, 0.2, 0.22), 0.27)
    mix(s, tone(note("E7"), note("E7"), 0.5, 0.12), 0.3, 0.12)
    return s


def game_over():
    s = marimba(note("E4"), 0.35, 0.11, 0.2)
    mix(s, marimba(note("C4"), 0.6, 0.2, 0.18), 0.17)
    mix(s, tone(note("C3"), note("C3"), 0.6, 0.18), 0.17, 0.35)
    return s


def unlock():
    s = silence(0.0)
    for i, n in enumerate(("G5", "C6", "E6", "G6")):
        mix(s, marimba(note(n), 0.35, 0.09, 0.24), i * 0.06)
    for i, n in enumerate(("C7", "E7", "G7")):
        mix(s, tone(note(n), note(n), 0.35, 0.09), 0.26 + i * 0.05, 0.1)
    return s


def countdown_tick():
    return marimba(note("G5"), 0.18, 0.05, 0.22)


def countdown_go():
    s = marimba(note("C6"), 0.4, 0.12, 0.3)
    mix(s, marimba(note("E6"), 0.4, 0.12, 0.25), 0.0, 0.8)
    mix(s, tone(note("G7"), note("G7"), 0.3, 0.07), 0.02, 0.1)
    return s


def point():
    s = marimba(note("E5"), 0.22, 0.06, 0.25)
    mix(s, marimba(note("A5"), 0.4, 0.11, 0.25), 0.08)
    return s


def match_win():
    s = silence(0.0)
    for i, n in enumerate(("C5", "E5", "G5", "C6")):
        mix(s, marimba(note(n), 0.32, 0.09, 0.24), i * 0.085)
    mix(s, marimba(note("E6"), 0.9, 0.26, 0.2), 0.34)
    for i, n in enumerate(("G6", "C7", "E7")):
        mix(s, tone(note(n), note(n), 0.45, 0.1), 0.38 + i * 0.06, 0.1)
    return s


SOUNDS = {
    "sfx_paddle_hit": paddle_hit,
    "sfx_wall_bounce": wall_bounce,
    "sfx_perfect": perfect,
    "sfx_ui_tap": ui_tap,
    "sfx_ui_back": ui_back,
    "sfx_new_best": new_best,
    "sfx_game_over": game_over,
    "sfx_unlock": unlock,
    "sfx_countdown_tick": countdown_tick,
    "sfx_countdown_go": countdown_go,
    "sfx_point": point,
    "sfx_match_win": match_win,
}


def write(name, samples):
    samples = normalize(fade_tail(samples))
    path = os.path.normpath(os.path.join(OUT, name + ".wav"))
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, v)) * 32767)) for v in samples))
    return path, len(samples) / RATE


if __name__ == "__main__":
    import sys
    os.makedirs(OUT, exist_ok=True)
    wanted = sys.argv[1:] or list(SOUNDS)
    for name in wanted:
        make = SOUNDS[name]
        path, seconds = write(name, make())
        print(f"{name}: {seconds:.2f}s -> {path}")
