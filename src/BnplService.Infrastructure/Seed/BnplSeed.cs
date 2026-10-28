namespace BnplService.Infrastructure.Seed;

/// <summary>
/// دادهٔ اولیه (همان Migration «InitialBnpl»): ردیف تنظیمات و طرح «۱۲ قسط» فیگما — کارمزد ۲۲٪ (۴٪ با قسط اول)، جریمهٔ روزانه ۱٪.
/// برای SQLite محلی و تست؛ روی SQL Server خود Migration این را می‌سازد.
/// </summary>
public static class BnplSeed
{
    public static BnplPlan DefaultPlan() => new("۱۲ قسط", 12, 22, 4, 1, 1);

    public static async Task EnsureSeedAsync(BnplDbContext db, CancellationToken ct = default)
    {
        if (!await db.Settings.AnyAsync(ct)) db.Settings.Add(new BnplSettings());
        if (!await db.Plans.AnyAsync(ct)) db.Plans.Add(DefaultPlan());
        await db.SaveChangesAsync(ct);
    }
}
