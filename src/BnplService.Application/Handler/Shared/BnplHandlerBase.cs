namespace BnplService.Application.Handler.Shared;

/// <summary>پایهٔ Handlerها: دیتابیس، زمان، پاسخ موفق، تنظیمات، بارگذاری درخواست و صفحه‌بندی (مثل FacilityPanelHandlerBase).</summary>
public abstract class BnplHandlerBase(IBnplDbContext db, TimeProvider time)
{
    protected IBnplDbContext Db { get; } = db;
    protected DateTime Now => time.GetUtcNow().UtcDateTime;
    protected DateOnly Today => BnplClock.Today(time);

    protected static BnplMessageResponse<T> Ok<T>(T data, string message = "عملیات با موفقیت انجام شد.") =>
        MessageResponse.Generate(200, data, message);

    protected async Task<BnplSettings> SettingsAsync(CancellationToken ct) =>
        await Db.Settings.FirstOrDefaultAsync(s => s.Id == BnplSettings.SingletonId, ct)
        ?? throw BnplDomainException.NotFound("تنظیمات پیش‌خرید اقساطی تعریف نشده است.");

    protected IQueryable<BnplRequest> RequestsWithDetails() =>
        Db.Requests.Include(r => r.Installments).Include(r => r.History);

    protected async Task<BnplRequest> RequestAsync(long id, CancellationToken ct, string? customerId = null)
    {
        var request = await RequestsWithDetails().FirstOrDefaultAsync(r => r.Id == id && (customerId == null || r.CustomerId == customerId), ct);
        return request ?? throw BnplDomainException.NotFound("درخواست یافت نشد.");
    }

    /// <summary>
    /// درخواست تکمیل‌نشده‌ای که مهلتش گذشته را همین‌جا منقضی می‌کند (Worker هم هر دقیقه همین کار را می‌کند)،
    /// مگر پرداخت درگاهیِ در جریان داشته باشد. true یعنی منقضی شد و باید ذخیره شود.
    /// </summary>
    protected async Task<bool> ExpireIfDueAsync(BnplRequest request, CancellationToken ct)
    {
        if (!request.IsDue(Now)) return false;
        var now = Now;
        var pending = await Db.Payments.AnyAsync(p => p.RequestId == request.Id && p.Status == PaymentStatus.Pending
                                                       && (p.ExpiresAt == null || p.ExpiresAt >= now), ct);
        return !pending && request.ExpireIfDue(now);
    }

    protected static BnplPagedResponse<T> Page<T>(IReadOnlyList<T> items, int total, int page, int pageSize) => new(items, total, page, pageSize);

    protected static (int Page, int Size) Paging(int page, int size, int defaultSize = 20, int max = 100) =>
        (Math.Max(1, page), size is < 1 ? defaultSize : Math.Min(size, max));

    protected async Task<DiscountCode?> DiscountAsync(string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var normalized = DiscountCode.NormalizeCode(code) ?? throw new BnplDomainException("کد تخفیف معتبر نیست.");
        var discount = await Db.DiscountCodes.FirstOrDefaultAsync(d => d.Code == normalized, ct)
                       ?? throw new BnplDomainException("کد تخفیف معتبر نیست.");
        discount.EnsureUsable(Now);
        return discount;
    }
}
