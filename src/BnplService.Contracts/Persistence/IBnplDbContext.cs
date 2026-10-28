using Microsoft.EntityFrameworkCore;

namespace BnplService.Contracts.Persistence;

/// <summary>
/// دسترسی Handlerها به دیتابیس BNPL. پیاده‌سازی: BnplService.Infrastructure.DBContext.BnplDbContext.
/// هر Handler یک SaveChanges دارد ← هر Command اتمیک است.
/// </summary>
public interface IBnplDbContext
{
    DbSet<BnplSettings> Settings { get; }
    DbSet<BnplPlan> Plans { get; }
    DbSet<DiscountCode> DiscountCodes { get; }
    DbSet<BnplRequest> Requests { get; }
    DbSet<BnplPayment> Payments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
