using System.Globalization;

namespace BnplService.Domain.Helpers;

/// <summary>تاریخ شمسی برای سررسید ماهانهٔ اقساط. ذخیره همیشه میلادی است.</summary>
public static class Jalali
{
    private static readonly PersianCalendar Calendar = new();

    public static (int Year, int Month, int Day) Of(DateOnly date)
    {
        var dt = date.ToDateTime(TimeOnly.MinValue);
        return (Calendar.GetYear(dt), Calendar.GetMonth(dt), Calendar.GetDayOfMonth(dt));
    }

    public static DateOnly ToDate(int year, int month, int day) =>
        DateOnly.FromDateTime(Calendar.ToDateTime(year, month, Math.Min(day, Calendar.GetDaysInMonth(year, month)), 0, 0, 0, 0));

    /// <summary>n ماه شمسی جلو با حفظ روز (اگر ماه مقصد کوتاه‌تر است، آخرین روزش).</summary>
    public static DateOnly AddMonths(DateOnly date, int months)
    {
        var (y, m, d) = Of(date);
        var index = y * 12 + (m - 1) + months;
        return ToDate(index / 12, index % 12 + 1, d);
    }

    /// <summary>«1405/07/15» با ارقام لاتین (همان قالب فیگما).</summary>
    public static string Format(DateOnly date)
    {
        var (y, m, d) = Of(date);
        return $"{y:D4}/{m:D2}/{d:D2}";
    }
}
