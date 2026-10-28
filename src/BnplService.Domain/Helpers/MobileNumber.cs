using System.Text;

namespace BnplService.Domain.Helpers;

public static class MobileNumber
{
    /// <summary>«۰۹۱۲…»، «+98912…» یا «98912…» ← «0912…». null یعنی نامعتبر.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new StringBuilder();
        foreach (var ch in value)
        {
            if (ch is >= '0' and <= '9') digits.Append(ch);
            else if (ch is >= '۰' and <= '۹') digits.Append((char)('0' + (ch - '۰')));
            else if (ch is >= '٠' and <= '٩') digits.Append((char)('0' + (ch - '٠')));
        }
        var s = digits.ToString();
        if (s.StartsWith("98") && s.Length == 12) s = "0" + s[2..];
        else if (s.Length == 10 && s[0] == '9') s = "0" + s;
        return s.Length == 11 && s.StartsWith("09") ? s : null;
    }
}
