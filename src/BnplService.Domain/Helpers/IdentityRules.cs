namespace BnplService.Domain.Helpers;

/// <summary>کد ملی ۱۰ رقمی با رقم کنترل («کد ملی معتبر نیست.» در فیگما).</summary>
public static class NationalCodeRule
{
    public static bool IsValid(string? code)
    {
        if (code is not { Length: 10 } || !code.All(char.IsAsciiDigit) || code.Distinct().Count() == 1) return false;
        var sum = 0;
        for (var i = 0; i < 9; i++) sum += (code[i] - '0') * (10 - i);
        var r = sum % 11;
        var check = code[9] - '0';
        return r < 2 ? check == r : check == 11 - r;
    }
}

/// <summary>شبا: ۲۴ رقم پس از IR با کنترل mod 97 («شماره شبا معتبر نیست.» در فیگما).</summary>
public static class ShebaRule
{
    public static bool IsValid(string? digits)
    {
        if (digits is not { Length: 24 } || !digits.All(char.IsAsciiDigit)) return false;
        // IR = 18 27؛ چهار کاراکتر اول (IRkk) به انتها می‌رود.
        var rearranged = digits[2..] + "1827" + digits[..2];
        var remainder = 0;
        foreach (var ch in rearranged) remainder = (remainder * 10 + (ch - '0')) % 97;
        return remainder == 1;
    }
}
