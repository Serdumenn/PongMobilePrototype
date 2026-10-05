import importlib.util
import os
import re
import sys

sys.path.insert(0, os.path.dirname(__file__))
import loc_strings as table

LANGUAGES = table.LANGUAGES


def load(path):
    spec = importlib.util.spec_from_file_location(os.path.basename(path)[:-3], path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.T


def outside_braces(text, convert):
    parts = re.split(r"(\{[^}]*\})", text)
    return "".join(part if part.startswith("{") else convert(part) for part in parts)


def polish(code, text):
    if code == "ja":
        return outside_braces(text, lambda s: s.replace("!", "！").replace("?", "？").replace(":", "："))
    if code == "fr":
        return outside_braces(text, lambda s: re.sub(r" ([!?:;])", " \\1", s))
    return text


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    sources = sys.argv[1:]
    merged = {}
    for path in sources:
        for key, values in load(path).items():
            if len(values) != len(LANGUAGES):
                sys.exit(f"{key}: expected {len(LANGUAGES)} translations, got {len(values)}")
            merged[key] = values

    rows, order = table.read_table()
    unknown = [key for key in merged if key not in rows]
    for key, values in merged.items():
        if key not in rows:
            continue
        for code, value in zip(LANGUAGES, values):
            rows[key][code] = polish(code, value)
    table.write_table(rows, order)

    missing = {code: [key for key in order if not rows[key].get(code)] for code in LANGUAGES}
    print(f"merged {len(merged) - len(unknown)} texts")
    for key in unknown:
        print("not in table:", key)
    for code, keys in missing.items():
        for key in keys:
            print(f"untranslated {code}:", key)


if __name__ == "__main__":
    main()
