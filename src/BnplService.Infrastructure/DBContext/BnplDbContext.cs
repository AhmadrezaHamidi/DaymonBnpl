using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BnplService.Infrastructure.DBContext;

/// <summary>جداول BNPL در اسکیمای «bnpl». Migrationها: Migrations/؛ اسکریپت DBA: Database/bnpl_migrations.sql</summary>
public class BnplDbContext(DbContextOptions<BnplDbContext> options) : DbContext(options), IBnplDbContext
{
    public DbSet<BnplSettings> Settings => Set<BnplSettings>();
    public DbSet<BnplPlan> Plans => Set<BnplPlan>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();
    public DbSet<BnplRequest> Requests => Set<BnplRequest>();
    public DbSet<BnplPayment> Payments => Set<BnplPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        BnplModel.Configure(modelBuilder);
        if (Database.IsSqlite())
        {
            // SQLite (فقط محلی/تست) decimal را مرتب و جمع نمی‌زند؛ به double ذخیره می‌شود.
            foreach (var p in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties())
                         .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
                p.SetValueConverter(p.ClrType == typeof(decimal)
                    ? new ValueConverter<decimal, double>(v => (double)v, v => (decimal)v)
                    : new ValueConverter<decimal?, double?>(v => (double?)v, v => (decimal?)v));
        }
    }
}
