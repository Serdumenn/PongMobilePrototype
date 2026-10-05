using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

public static class Loc
{
    public const string ResourceName = "loc_strings";
    public const string PrefKey = "Language";
    public const string English = "en";
    public const string Pseudo = "qps";

    public static readonly string[] Codes = { "en", "tr", "ja", "ko", "de", "es", "pt", "fr" };

    private static readonly Dictionary<string, string> NativeNames = new Dictionary<string, string>
    {
        { "en", "English" },
        { "tr", "Türkçe" },
        { "ja", "日本語" },
        { "ko", "한국어" },
        { "de", "Deutsch" },
        { "es", "Español" },
        { "pt", "Português" },
        { "fr", "Français" },
        { Pseudo, "Pseudo" }
    };

    private static readonly Dictionary<string, string> CultureNames = new Dictionary<string, string>
    {
        { "en", "en-US" },
        { "tr", "tr-TR" },
        { "ja", "ja-JP" },
        { "ko", "ko-KR" },
        { "de", "de-DE" },
        { "es", "es-MX" },
        { "pt", "pt-BR" },
        { "fr", "fr-FR" },
        { Pseudo, "en-US" }
    };

    private static readonly Dictionary<string, Dictionary<string, string>> tables = new Dictionary<string, Dictionary<string, string>>();
    private static readonly List<string> keys = new List<string>();
    private static bool loaded;
    private static string language = English;
    private static CultureInfo culture = CultureInfo.InvariantCulture;

    public static event Action Changed;

    public static string Language
    {
        get
        {
            EnsureLoaded();
            return language;
        }
    }

    public static CultureInfo Culture
    {
        get
        {
            EnsureLoaded();
            return culture;
        }
    }

    public static IReadOnlyList<string> Keys
    {
        get
        {
            EnsureLoaded();
            return keys;
        }
    }

    public static string NativeName(string code)
    {
        return code != null && NativeNames.TryGetValue(code, out var name) ? name : code;
    }

    public static bool Has(string key)
    {
        EnsureLoaded();
        return key != null && tables.TryGetValue(English, out var table) && table.ContainsKey(key);
    }

    public static string T(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        EnsureLoaded();

        if (language == Pseudo) return PseudoText(Lookup(English, key) ?? Source(key));
        return Lookup(language, key) ?? Lookup(English, key) ?? Source(key);
    }

    public static string T(string key, params object[] args)
    {
        string pattern = T(key);
        try
        {
            return string.Format(culture, pattern, args);
        }
        catch (FormatException)
        {
            return string.Format(CultureInfo.InvariantCulture, Lookup(English, key) ?? Source(key), args);
        }
    }

    public static string Plural(string one, string other, int count)
    {
        return T(IsOne(Language, count) ? one : other, count);
    }

    public static bool IsOne(string code, int count)
    {
        switch (code)
        {
            case "ja":
            case "ko":
                return false;
            case "pt":
            case "fr":
                return count == 0 || count == 1;
            default:
                return count == 1;
        }
    }

    public static void SetLanguage(string code, bool save = true)
    {
        EnsureLoaded();
        if (code != Pseudo && Array.IndexOf(Codes, code) < 0) code = English;
        Apply(code);

        if (save && code != Pseudo)
        {
            PlayerPrefs.SetString(PrefKey, code);
            PlayerPrefs.Save();
        }

        Changed?.Invoke();
    }

    public static string FromSystemLanguage(SystemLanguage system)
    {
        return system switch
        {
            SystemLanguage.Turkish => "tr",
            SystemLanguage.Japanese => "ja",
            SystemLanguage.Korean => "ko",
            SystemLanguage.German => "de",
            SystemLanguage.Spanish => "es",
            SystemLanguage.Portuguese => "pt",
            SystemLanguage.French => "fr",
            _ => English
        };
    }

    public static void Load(string text)
    {
        tables.Clear();
        keys.Clear();
        loaded = true;
        if (string.IsNullOrEmpty(text)) return;

        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 0) return;

        string[] header = lines[0].Split('\t');
        int keyColumn = Array.IndexOf(header, "key");
        if (keyColumn < 0) return;

        for (int c = 0; c < header.Length; c++)
            if (c != keyColumn && header[c] != "note") tables[header[c]] = new Dictionary<string, string>();

        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].Length == 0) continue;
            string[] cells = lines[i].Split('\t');
            if (cells.Length <= keyColumn) continue;

            string key = Unescape(cells[keyColumn]);
            if (key.Length == 0 || tables[English].ContainsKey(key)) continue;
            keys.Add(key);

            for (int c = 0; c < header.Length && c < cells.Length; c++)
            {
                if (c == keyColumn || !tables.TryGetValue(header[c], out var table)) continue;
                string value = Unescape(cells[c]);
                if (value.Length > 0) table[key] = value;
            }

            if (!tables[English].ContainsKey(key)) tables[English][key] = Source(key);
        }
    }

    public static string Lookup(string code, string key)
    {
        return tables.TryGetValue(code, out var table) && table.TryGetValue(key, out var value) ? value : null;
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;

        var asset = Resources.Load<TextAsset>(ResourceName);
        Load(asset != null ? asset.text : string.Empty);
        if (!tables.ContainsKey(English)) tables[English] = new Dictionary<string, string>();

        string saved = PlayerPrefs.GetString(PrefKey, string.Empty);
        Apply(Array.IndexOf(Codes, saved) >= 0 ? saved : FromSystemLanguage(Application.systemLanguage));
    }

    private static void Apply(string code)
    {
        language = code;
        try
        {
            culture = CultureInfo.GetCultureInfo(CultureNames.TryGetValue(code, out var name) ? name : "en-US");
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.InvariantCulture;
        }
    }

    public static string Source(string key)
    {
        int mark = key.LastIndexOf('#');
        if (mark <= 0 || mark == key.Length - 1 || !char.IsLetter(key[mark + 1])) return key;
        for (int i = mark + 1; i < key.Length; i++)
            if (!char.IsLetterOrDigit(key[i]) && key[i] != '_' && key[i] != '-') return key;
        return key.Substring(0, mark);
    }

    private static string Unescape(string cell)
    {
        if (cell.IndexOf('\\') < 0) return cell;
        return cell.Replace("\\n", "\n").Replace("\\t", "\t");
    }

    private static string PseudoText(string text)
    {
        var sb = new StringBuilder(text.Length * 2);
        sb.Append('[');
        int depth = 0;
        int letters = 0;
        foreach (char c in text)
        {
            if (c == '{') depth++;
            if (depth == 0 && char.IsLetter(c))
            {
                letters++;
                sb.Append(Accent(c));
            }
            else sb.Append(c);
            if (c == '}' && depth > 0) depth--;
        }
        sb.Append(' ');
        sb.Append('~', Math.Max(1, letters * 2 / 5));
        sb.Append(']');
        return sb.ToString();
    }

    private static char Accent(char c)
    {
        const string plain = "aceinosuyACEINOSUY";
        const string fancy = "åçéîñöšüýÅÇÉÎÑÖŠÜÝ";
        int i = plain.IndexOf(c);
        return i >= 0 ? fancy[i] : c;
    }
}
