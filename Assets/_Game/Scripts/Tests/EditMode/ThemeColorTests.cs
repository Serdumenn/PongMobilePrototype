using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public sealed class ThemeColorTests
{
    private const float Strong = 7f;
    private const float Readable = 4f;
    private const float Accent = 3f;

    private static readonly Regex Rule = new Regex(@"([^{}]+)\{([^{}]*)\}");
    private static readonly Regex Token = new Regex(@"(--[a-z0-9-]+)\s*:\s*([^;]+);");
    private static readonly Regex Literal = new Regex(@"#[0-9A-Fa-f]{3,8}\b|rgba?\(");

    private static readonly string[] Themes = { "", "theme-mint", "theme-sunset", "theme-night" };

    private static readonly (string text, string surface, float min)[] Pairs =
    {
        ("ink", "bg", Strong),
        ("ink", "surface", Strong),
        ("ink", "select", Strong),
        ("ink", "field", Strong),
        ("ink", "inset", Strong),
        ("on-chip", "chip", Strong),
        ("sun-bright", "chip-deep", Strong),
        ("on-sun", "sun", Strong),
        ("ink-muted", "bg", Readable),
        ("ink-muted", "surface", Readable),
        ("ink-muted", "select", Readable),
        ("ink-muted", "field", Readable),
        ("ink-muted", "inset", Readable),
        ("on-sun-muted", "sun", Readable),
        ("coral-ink", "coral-tint", Readable),
        ("teal-ink", "teal-tint", Readable),
        ("teal-ink", "surface", Readable),
        ("sun-ink", "sun-tint", Readable),
        ("on-accent", "coral", Accent),
        ("on-accent", "teal-fill", Accent),
        ("coral-text", "surface", Accent),
        ("coral-text", "bg", Accent),
        ("coral-text", "coral-tint", Accent),
        ("teal-text", "surface", Accent),
        ("teal-text", "bg", Accent),
        ("teal-text", "select", Accent),
        ("teal-text", "field", Accent),
        ("player-1-text", "bg", Accent),
        ("player-2-text", "bg", Accent),
        ("player-3-text", "bg", Accent)
    };

    private static string ThemeFolder => Path.Combine(Application.dataPath, "_Game", "UI", "Theme");

    private static string Read(string file)
    {
        return File.ReadAllText(Path.Combine(ThemeFolder, file));
    }

    private static string Normalize(string selector)
    {
        return string.Join(" ", selector.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static void Collect(string sheet, string selector, Dictionary<string, string> into)
    {
        foreach (Match rule in Rule.Matches(sheet))
        {
            if (Normalize(rule.Groups[1].Value) != selector) continue;
            foreach (Match token in Token.Matches(rule.Groups[2].Value)) into[token.Groups[1].Value] = token.Groups[2].Value.Trim();
        }
    }

    private static Dictionary<string, string> TokensFor(string theme)
    {
        var tokens = new Dictionary<string, string>();
        Collect(Read("Tokens.uss"), ":root", tokens);
        Collect(Read("Match.uss"), ":root", tokens);
        if (theme.Length > 0) Collect(Read("Themes.uss"), ".root." + theme, tokens);
        return tokens;
    }

    private static float Channel(string hex, int index)
    {
        float value = int.Parse(hex.Substring(1 + index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255f;
        return value <= 0.03928f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
    }

    private static float Luminance(string hex)
    {
        return 0.2126f * Channel(hex, 0) + 0.7152f * Channel(hex, 1) + 0.0722f * Channel(hex, 2);
    }

    private static float Contrast(string a, string b)
    {
        float la = Luminance(a);
        float lb = Luminance(b);
        return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
    }

    [Test]
    public void StyleSheets_TakeColorsFromTokens()
    {
        var stray = new List<string>();
        foreach (string path in Directory.GetFiles(ThemeFolder, "*.uss"))
        {
            string file = Path.GetFileName(path);
            if (file == "Tokens.uss" || file == "Themes.uss") continue;

            foreach (Match rule in Rule.Matches(File.ReadAllText(path)))
            {
                string selector = Normalize(rule.Groups[1].Value);
                if (selector == ":root") continue;

                foreach (string raw in rule.Groups[2].Value.Split(';'))
                {
                    string declaration = raw.Trim();
                    if (declaration.Length == 0 || declaration.StartsWith("--") || declaration.StartsWith("text-shadow")) continue;
                    if (!Literal.IsMatch(declaration) || declaration.EndsWith("rgba(0, 0, 0, 0)")) continue;
                    stray.Add(file + ": " + selector + " { " + declaration + " }");
                }
            }
        }

        Assert.IsEmpty(stray, "Colors outside Tokens.uss do not follow the theme:\n" + string.Join("\n", stray));
    }

    [Test]
    public void Themes_OverrideOnlyKnownTokens()
    {
        var known = new HashSet<string>(TokensFor(string.Empty).Keys);
        foreach (string theme in Themes.Where(t => t.Length > 0))
        {
            var own = new Dictionary<string, string>();
            Collect(Read("Themes.uss"), ".root." + theme, own);
            Assert.IsNotEmpty(own, theme + " has no overrides");
            foreach (string token in own.Keys) Assert.IsTrue(known.Contains(token), theme + " overrides unknown token " + token);
        }
    }

    [Test]
    public void EveryTheme_KeepsTextReadable()
    {
        var weak = new List<string>();
        foreach (string theme in Themes)
        {
            var tokens = TokensFor(theme);
            string name = theme.Length > 0 ? theme : "default";
            foreach (var pair in Pairs)
            {
                Assert.IsTrue(tokens.TryGetValue("--color-" + pair.text, out string text), "Missing token " + pair.text);
                Assert.IsTrue(tokens.TryGetValue("--color-" + pair.surface, out string surface), "Missing token " + pair.surface);
                Assert.IsTrue(text.StartsWith("#") && surface.StartsWith("#"), pair.text + " and " + pair.surface + " must be solid colors");

                float ratio = Contrast(text, surface);
                if (ratio < pair.min) weak.Add(name + ": " + pair.text + " on " + pair.surface + " = " + ratio.ToString("F2", CultureInfo.InvariantCulture) + ", needs " + pair.min.ToString("F1", CultureInfo.InvariantCulture));
            }
        }

        Assert.IsEmpty(weak, "Low contrast:\n" + string.Join("\n", weak));
    }
}
