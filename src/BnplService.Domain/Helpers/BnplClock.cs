namespace BnplService.Domain.Helpers;

/// <summary>«امروز» به وقت تهران (سررسید اقساط و جریمهٔ روزانه بر اساس روز تقویمی ایران است).</summary>
public static class BnplClock
{
    private static readonly TimeZoneInfo Tehran = FindTehran();

    public static DateOnly Today(TimeProvider time) => ToTehranDate(time.GetUtcNow().UtcDateTime);

    public static DateOnly ToTehranDate(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Tehran));

    private static TimeZoneInfo FindTehran()
    {
        foreach (var id in new[] { "Asia/Tehran", "Iran Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("Tehran", TimeSpan.FromMinutes(210), "Tehran", "Tehran");
    }
}
