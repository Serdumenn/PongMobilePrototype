using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public sealed class LocTests
{
    private static readonly Regex Placeholder = new Regex(@"\{(\d+)(:[^}]*)?\}");
    private static readonly Regex UxmlText = new Regex("\\btext=\"([^\"]*)\"");
    private static readonly Regex CodeText = new Regex("\\bLoc\\.(?:T|Plural)\\(\\s*\"((?:[^\"\\\\]|\\\\.)*)\"(?:\\s*,\\s*\"((?:[^\"\\\\]|\\\\.)*)\")?");

    private static string GameFolder => Path.Combine(Application.dataPath, "_Game");

    private static HashSet<string> Ignored()
    {
        var path = Path.Combine(GameFolder, "Localization", "loc_ignore.txt");
        return new HashSet<string>(File.ReadAllLines(path).Where(l => l.Trim().Length > 0 && !l.StartsWith("#")));
    }

    private static string Unescape(string literal)
    {
        return literal.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\\\", "\\");
    }

    private static IEnumerable<string> Shown(HashSet<string> ignored)
    {
        foreach (var file in Directory.GetFiles(Path.Combine(GameFolder, "UI", "Screens"), "*.uxml"))
            foreach (Match match in UxmlText.Matches(File.ReadAllText(file)))
            {
                string text = System.Net.WebUtility.HtmlDecode(match.Groups[1].Value);
                if (Regex.IsMatch(text, "[A-Za-z]{2,}") && !ignored.Contains(text)) yield return text;
            }

        foreach (var file in Directory.GetFiles(Path.Combine(GameFolder, "Scripts"), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains(Path.DirectorySeparatorChar + "Tests" + Path.DirectorySeparatorChar)) continue;
            foreach (Match match in CodeText.Matches(File.ReadAllText(file)))
            {
                yield return Unescape(match.Groups[1].Value);
                if (match.Groups[2].Success) yield return Unescape(match.Groups[2].Value);
            }
        }
    }

    [Test]
    public void Table_CoversEveryTextOnScreen()
    {
        var ignored = Ignored();
        var missing = Shown(ignored).Where(t => !Loc.Has(t)).Distinct().ToList();
        Assert.IsEmpty(missing, "Run ArtSource/tools/loc_strings.py to add these:\n" + string.Join("\n", missing));
    }

    [Test]
    public void Translations_KeepTheSamePlaceholders()
    {
        var problems = new List<string>();
        foreach (string key in Loc.Keys)
        {
            string english = Loc.Lookup(Loc.English, key);
            var expected = Placeholder.Matches(english).Cast<Match>().Select(m => m.Groups[1].Value).OrderBy(v => v).ToList();
            foreach (string code in Loc.Codes)
            {
                string text = Loc.Lookup(code, key);
                if (text == null || code == Loc.English) continue;
                var found = Placeholder.Matches(text).Cast<Match>().Select(m => m.Groups[1].Value).OrderBy(v => v).ToList();
                if (!expected.SequenceEqual(found)) problems.Add($"{code}: {key}");
            }
        }
        Assert.IsEmpty(problems, string.Join("\n", problems));
    }

    [Test]
    public void Plurals_FollowEachLanguage()
    {
        Assert.IsTrue(Loc.IsOne("en", 1));
        Assert.IsFalse(Loc.IsOne("en", 0));
        Assert.IsTrue(Loc.IsOne("fr", 0), "French counts zero as singular");
        Assert.IsTrue(Loc.IsOne("pt", 1));
        Assert.IsFalse(Loc.IsOne("de", 2));
        Assert.IsFalse(Loc.IsOne("ja", 1), "Japanese has one form");
        Assert.IsFalse(Loc.IsOne("ko", 1));
        Assert.AreEqual("1 shared life", Loc.Plural("{0} shared life", "{0} shared lives", 1));
        Assert.AreEqual("3 shared lives", Loc.Plural("{0} shared life", "{0} shared lives", 3));
        Assert.AreEqual("4-day streak", Loc.Plural("{0}-day streak", "{0}-day streak#other", 4), "A #note never shows in English");
    }

    [Test]
    public void English_IsTheSourceAndFallback()
    {
        Assert.AreEqual("Play again", Loc.T("Play again"));
        Assert.AreEqual("Best 7", Loc.T("Best {0}", 7));
        Assert.AreEqual("Not in the table", Loc.T("Not in the table"));
        Assert.AreEqual("Oct 5", DailyChallenge.Label(new System.DateTime(2026, 10, 5)));
        Assert.AreEqual("English", Loc.NativeName("en"));
        Assert.AreEqual("Türkçe", Loc.NativeName("tr"));
        Assert.AreEqual("tr", Loc.FromSystemLanguage(SystemLanguage.Turkish));
        Assert.AreEqual("en", Loc.FromSystemLanguage(SystemLanguage.Russian), "Unsupported languages fall back to English");
        Assert.AreEqual("Today's best 23 · Rank #128", Loc.T("Today's best {0} · Rank #{1}", 23, 128), "A # inside the text is not a note");
        Assert.AreEqual("{0}-day streak", Loc.Source("{0}-day streak#other"));

        var differs = Loc.Keys.Where(k => Loc.Lookup(Loc.English, k) != Loc.Source(k)).ToList();
        Assert.IsEmpty(differs, "The English column must equal the key:\n" + string.Join("\n", differs));
    }

    [Test]
    public void EveryLetter_HasAFont()
    {
        var chains = new[]
        {
            new[] { "fnt_fredoka_medium", "fnt_mplus_rounded_medium_ja", "fnt_jua_ko" },
            new[] { "fnt_fredoka_semibold", "fnt_mplus_rounded_bold_ja", "fnt_jua_ko" },
            new[] { "fnt_fredoka_bold", "fnt_mplus_rounded_extrabold_ja", "fnt_jua_ko" }
        };

        var letters = new SortedSet<int>();
        foreach (string code in Loc.Codes)
        {
            Collect(Loc.NativeName(code), letters);
            foreach (string key in Loc.Keys) Collect(Loc.Lookup(code, key), letters);
        }

        var covered = new Dictionary<string, HashSet<int>>();
        foreach (var chain in chains)
            foreach (string name in chain)
                if (!covered.ContainsKey(name)) covered[name] = Coverage(name, letters);

        var problems = new List<string>();
        foreach (var chain in chains)
        {
            var missing = letters.Where(c => !chain.Any(name => covered[name].Contains(c))).ToList();
            if (missing.Count > 0) problems.Add(chain[0] + ": " + string.Join(" ", missing.Select(c => char.ConvertFromUtf32(c) + " U+" + c.ToString("X4"))));
        }

        Assert.IsEmpty(problems, "Run ArtSource/tools/subset_fonts.py, then Pingi → Rebuild Language Fonts:\n" + string.Join("\n", problems));
    }

    [Test]
    public void LanguageFonts_FollowFredokaLines()
    {
        foreach (string weight in new[] { "medium", "semibold", "bold" })
        {
            string path = "Assets/_Game/Fonts/fnt_fredoka_" + weight + "_sdf.asset";
            var primary = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>(path);
            Assert.IsNotNull(primary, "Missing " + path);

            var names = primary.fallbackFontAssetTable.Where(f => f != null).Select(f => f.name).ToList();
            Assert.IsTrue(names.Any(n => n.Contains("_ja_")), weight + " has no Japanese fallback");
            Assert.IsTrue(names.Any(n => n.Contains("_ko_")), weight + " has no Korean fallback");

            foreach (var fallback in primary.fallbackFontAssetTable)
            {
                float ratio = fallback.faceInfo.pointSize / primary.faceInfo.pointSize;
                string because = fallback.name + " lines differ from Fredoka; run Pingi → Rebuild Language Fonts";
                Assert.AreEqual(primary.faceInfo.ascentLine * ratio, fallback.faceInfo.ascentLine, 0.01f, because);
                Assert.AreEqual(primary.faceInfo.descentLine * ratio, fallback.faceInfo.descentLine, 0.01f, because);
                Assert.AreEqual(primary.faceInfo.lineHeight * ratio, fallback.faceInfo.lineHeight, 0.01f, because);
            }
        }
    }

    private static void Collect(string text, ISet<int> letters)
    {
        if (string.IsNullOrEmpty(text)) return;
        for (int i = 0; i < text.Length; i += char.IsSurrogatePair(text, i) ? 2 : 1)
        {
            int c = char.ConvertToUtf32(text, i);
            if (c > ' ') letters.Add(c);
        }
    }

    private static HashSet<int> Coverage(string name, IEnumerable<int> letters)
    {
        string path = "Assets/_Game/Fonts/" + name + ".ttf";
        var font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(path);
        Assert.IsNotNull(font, "Missing font " + path);
        Assert.AreEqual(UnityEngine.TextCore.LowLevel.FontEngineError.Success, UnityEngine.TextCore.LowLevel.FontEngine.LoadFontFace(font), "Cannot read " + path);

        var found = new HashSet<int>();
        foreach (int c in letters)
            if (UnityEngine.TextCore.LowLevel.FontEngine.TryGetGlyphIndex((uint)c, out uint glyph) && glyph != 0)
                found.Add(c);
        return found;
    }

    [Test]
    public void Pseudo_StretchesTextButKeepsPlaceholders()
    {
        string before = Loc.Language;
        try
        {
            Loc.SetLanguage(Loc.Pseudo, false);
            string text = Loc.T("Best {0}", 12);
            StringAssert.StartsWith("[", text);
            StringAssert.Contains("12", text);
            StringAssert.Contains("~", text);
            Assert.Greater(text.Length, "Best 12".Length + 3);
        }
        finally
        {
            Loc.SetLanguage(before, false);
        }
    }
}
