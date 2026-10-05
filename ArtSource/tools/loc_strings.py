import argparse
import glob
import html
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
GAME = os.path.join(ROOT, "Assets", "_Game")
TABLE = os.path.join(GAME, "Localization", "Resources", "loc_strings.txt")
IGNORE = os.path.join(GAME, "Localization", "loc_ignore.txt")
LANGUAGES = ["tr", "ja", "ko", "de", "es", "pt", "fr"]
HEADER = ["key", "en", "note"] + LANGUAGES

UXML_TEXT = re.compile(r'\btext="([^"]*)"')
CS_LITERAL = r'"((?:[^"\\]|\\.)*)"'
CS_T = re.compile(r"\bLoc\.T\(\s*" + CS_LITERAL)
CS_PLURAL = re.compile(r"\bLoc\.Plural\(\s*" + CS_LITERAL + r"\s*,\s*" + CS_LITERAL)
ASSET_TEXT = re.compile(r"<(DisplayName|Tagline)>k__BackingField: (.*)$", re.M)


SUFFIX = re.compile(r"#[A-Za-z][\w-]*$")


def source(key):
    return SUFFIX.sub("", key)


def cs_unescape(value):
    return value.replace('\\"', '"').replace("\\n", "\n").replace("\\\\", "\\")


def yaml_unquote(value):
    value = value.strip()
    if len(value) >= 2 and value[0] == value[-1] and value[0] in "'\"":
        inner = value[1:-1]
        return inner.replace("''", "'") if value[0] == "'" else inner.replace('\\"', '"')
    return value


def escape(cell):
    return cell.replace("\t", "\\t").replace("\n", "\\n")


def unescape(cell):
    return cell.replace("\\n", "\n").replace("\\t", "\t")


def translatable(text, ignore):
    return bool(re.search(r"[A-Za-z]{2,}", text)) and text not in ignore


def collect(ignore):
    found = {}

    def add(text, where, explicit=False):
        if not explicit and not translatable(text, ignore):
            return
        if text in ignore:
            return
        found.setdefault(text, [])
        if where not in found[text]:
            found[text].append(where)

    for path in sorted(glob.glob(os.path.join(GAME, "UI", "Screens", "*.uxml"))):
        name = os.path.basename(path)
        for match in UXML_TEXT.finditer(open(path, encoding="utf-8").read()):
            add(html.unescape(match.group(1)), name)

    for path in sorted(glob.glob(os.path.join(GAME, "Scripts", "**", "*.cs"), recursive=True)):
        if os.sep + "Tests" + os.sep in path:
            continue
        name = os.path.basename(path)
        source = open(path, encoding="utf-8").read()
        for match in CS_T.finditer(source):
            add(cs_unescape(match.group(1)), name, True)
        for match in CS_PLURAL.finditer(source):
            add(cs_unescape(match.group(1)), name, True)
            add(cs_unescape(match.group(2)), name, True)

    for folder in ("Modes", "Cosmetics"):
        for path in sorted(glob.glob(os.path.join(GAME, "Data", folder, "*.asset"))):
            name = os.path.basename(path)
            for match in ASSET_TEXT.finditer(open(path, encoding="utf-8").read()):
                add(yaml_unquote(match.group(2)), name)

    return found


def read_table():
    rows = {}
    order = []
    if not os.path.exists(TABLE):
        return rows, order

    lines = open(TABLE, encoding="utf-8").read().replace("\r\n", "\n").split("\n")
    header = lines[0].split("\t")
    for line in lines[1:]:
        if not line:
            continue
        cells = line.split("\t")
        row = {header[i]: unescape(cells[i]) if i < len(cells) else "" for i in range(len(header))}
        rows[row["key"]] = row
        order.append(row["key"])
    return rows, order


def write_table(rows, order):
    os.makedirs(os.path.dirname(TABLE), exist_ok=True)
    out = ["\t".join(HEADER)]
    for key in order:
        row = rows[key]
        out.append("\t".join(escape(row.get(column, "")) for column in HEADER))
    open(TABLE, "w", encoding="utf-8", newline="\n").write("\n".join(out) + "\n")


def main():
    parser = argparse.ArgumentParser(description="Sync the localization table with the game's texts.")
    parser.add_argument("--check", action="store_true", help="fail if the table is missing texts or has unused ones")
    args = parser.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")

    ignore = set()
    if os.path.exists(IGNORE):
        ignore = {line.rstrip("\n") for line in open(IGNORE, encoding="utf-8") if line.strip() and not line.startswith("#")}

    found = collect(ignore)
    rows, order = read_table()

    added = [text for text in found if text not in rows]
    unused = [key for key in order if key not in found]

    if args.check:
        for text in added:
            print("missing:", text)
        for key in unused:
            print("unused:", key)
        sys.exit(1 if added or unused else 0)

    for text in added:
        rows[text] = {"key": text, "en": source(text)}
        order.append(text)
    for key in unused:
        order.remove(key)
        del rows[key]
    for key in order:
        rows[key]["note"] = ", ".join(found[key])
        if not rows[key].get("en"):
            rows[key]["en"] = source(key)

    write_table(rows, order)
    missing = {code: sum(1 for key in order if not rows[key].get(code)) for code in LANGUAGES}
    print(f"{len(order)} texts, {len(added)} added, {len(unused)} removed")
    print("untranslated:", ", ".join(f"{code} {count}" for code, count in missing.items()))
    for key in unused:
        print("removed:", key)


if __name__ == "__main__":
    main()
