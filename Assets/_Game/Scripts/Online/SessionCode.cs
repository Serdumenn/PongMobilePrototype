using System.Text;

public static class SessionCode
{
    public const int Length = 6;

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
            if (!(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9')) return false;
        return true;
    }
}
