using System.Globalization;

namespace BnplService.Domain.Helpers;

/// <summary>عدد با جداکنندهٔ هزارگان و ارقام فارسی برای پیام‌های خطا.</summary>
public static class PersianNumber
{
    private static readonly string Digits = "۰۱۲۳۴۵۶۷۸۹";

    public static string Of(long value) =>
        new(value.ToString("#,0", CultureInfo.InvariantCulture).Select(c => c switch
        {
            >= '0' and <= '9' => Digits[c - '0'],
            ',' => '٬',
            _ => c,
        }).ToArray());

    public static string Rial(long value) => $"{Of(value)} ریال";
}
