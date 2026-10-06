using System;
using System.Text.RegularExpressions;

public static class FriendTag
{
    private static readonly Regex Pattern = new Regex(@"([A-Za-z][A-Za-z0-9]*)(?:[ \t]+([A-Za-z][A-Za-z0-9]*))?[ \t]*#[ \t]*(\d{3,8})(?!\d)");

    public static string BaseName(string playerId)
    {
        return PlayerNames.ForId(playerId).Replace(" ", string.Empty);
    }

    public static string NameOf(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return string.Empty;
        int mark = tag.IndexOf('#');
        return mark > 0 ? tag.Substring(0, mark) : tag;
    }

    public static bool HasBase(string tag, string baseName)
    {
        return !string.IsNullOrEmpty(tag) && string.Equals(NameOf(tag), baseName, StringComparison.Ordinal) && tag.Length > baseName.Length + 1;
    }

    public static string Parse(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        var match = Pattern.Match(text);
        if (!match.Success) return null;

        string first = match.Groups[1].Value;
        string second = match.Groups[2].Success ? match.Groups[2].Value : null;
        string name;
        if (second == null) name = Canonical(first);
        else
        {
            string joined = Canonical(first + second);
            name = IsGenerated(joined) ? joined : Canonical(second);
        }

        return name.Length > 1 ? name + "#" + match.Groups[3].Value : null;
    }

    public static string Canonical(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;

        foreach (var adjective in PlayerNames.AdjectiveList)
        {
            if (!name.StartsWith(adjective, StringComparison.OrdinalIgnoreCase)) continue;
            string rest = name.Substring(adjective.Length);
            foreach (var animal in PlayerNames.AnimalList)
                if (string.Equals(rest, animal, StringComparison.OrdinalIgnoreCase)) return adjective + animal;
        }
        return name;
    }

    private static bool IsGenerated(string name)
    {
        foreach (var adjective in PlayerNames.AdjectiveList)
        {
            if (!name.StartsWith(adjective, StringComparison.Ordinal)) continue;
            string rest = name.Substring(adjective.Length);
            foreach (var animal in PlayerNames.AnimalList)
                if (rest == animal) return true;
        }
        return false;
    }
}
