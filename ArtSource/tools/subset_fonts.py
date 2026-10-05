import os
import sys

from fontTools import subset
from fontTools.ttLib import TTFont

sys.path.insert(0, os.path.dirname(__file__))
import loc_strings as table

ROOT = table.ROOT
SOURCE = os.path.join(ROOT, "ArtSource", "fonts")
OUT = os.path.join(table.GAME, "Fonts")

BASE = "".join(chr(c) for c in range(0x20, 0x7F)) + "…·–—×•’“”〜~！？：、。「」『』（）・ー"
NATIVE = {"ja": "日本語", "ko": "한국어"}

FONTS = [
    ("ja", "MPLUSRounded1c-Medium.ttf", "fnt_mplus_rounded_medium_ja.ttf"),
    ("ja", "MPLUSRounded1c-Bold.ttf", "fnt_mplus_rounded_bold_ja.ttf"),
    ("ja", "MPLUSRounded1c-ExtraBold.ttf", "fnt_mplus_rounded_extrabold_ja.ttf"),
    ("ko", "Jua-Regular.ttf", "fnt_jua_ko.ttf"),
]


def characters(code):
    rows, order = table.read_table()
    text = "".join(rows[key].get(code, "") for key in order)
    chars = set(text) | set(BASE) | set(NATIVE[code])
    return "".join(sorted(c for c in chars if c >= " "))


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    primary = TTFont(os.path.join(OUT, "fnt_fredoka_bold.ttf")).getBestCmap()
    chains = {code: set() for code in NATIVE}
    for code, source, target in FONTS:
        chains[code] |= set(TTFont(os.path.join(SOURCE, source)).getBestCmap())
    for code, source, target in FONTS:
        chars = characters(code)
        font = TTFont(os.path.join(SOURCE, source))
        cmap = font.getBestCmap()
        missing = [c for c in chars if ord(c) not in cmap and ord(c) not in primary and ord(c) not in chains["ja"] and c != " "]

        options = subset.Options()
        options.layout_features = ["*"]
        options.name_IDs = ["*"]
        options.name_languages = ["*"]
        options.notdef_outline = True
        options.hinting = False
        subsetter = subset.Subsetter(options)
        subsetter.populate(text=chars)
        subsetter.subset(font)

        path = os.path.join(OUT, target)
        font.save(path)
        print(f"{target}: {len(chars)} characters, {os.path.getsize(path) // 1024} KB" + (f", missing {''.join(missing)}" if missing else ""))


if __name__ == "__main__":
    main()
