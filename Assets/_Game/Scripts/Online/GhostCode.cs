using System.Text;

public static class GhostCode
{
    public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public const int Length = 6;
    public const int ValidDays = 7;

    public static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var code = new StringBuilder(Length);
        foreach (char c in input)
        {
            if (char.IsWhiteSpace(c) || c == '-') continue;
            code.Append(char.ToUpperInvariant(c));
        }
        return code.ToString();
    }

    public static bool IsValid(string code)
    {
        if (code == null || code.Length != Length) return false;

        foreach (char c in code)
            if (Alphabet.IndexOf(c) < 0) return false;
        return true;
    }
}
