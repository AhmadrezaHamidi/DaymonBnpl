namespace BnplService.Application.Handler.Admin.Dashboard.Queries.GetDashboard;

public sealed class GetDashboardQueryHandler(IBnplDbContext db, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<GetDashboardQuery, BnplMessageResponse<BnplDashboardDto>>
{
    public async Task<BnplMessageResponse<BnplDashboardDto>> Handle(GetDashboardQuery query, CancellationToken ct)
    {
        var today = Today;
        var counts = await Db.Requests.GroupBy(r => r.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);
        var byStatus = Enum.GetValues<BnplRequestStatus>()
            .Select(s => new StatusCountDto(s, BnplTitles.Of(s), counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0)).ToList();

        // جریمه به «امروز» بستگی دارد و در SQL قابل محاسبه نیست؛ قراردادهای فعال در حافظه جمع زده می‌شوند.
        var active = await Db.Requests.Include(r => r.Installments).Where(r => r.Status == BnplRequestStatus.Active).ToListAsync(ct);
        var overdue = active.Where(r => r.IsOverdue(today)).ToList();

        var succeeded = Db.Payments.Where(p => p.Status == PaymentStatus.Succeeded);
        var collected = await succeeded.SumAsync(p => (long?)(p.Amount + p.Penalty), ct) ?? 0;
        var collectedPenalty = await succeeded.SumAsync(p => (long?)p.Penalty, ct) ?? 0;
        var refunds = Db.Payments.Where(p => p.Status == PaymentStatus.RefundRequired);

        var (start, end) = TodayRangeUtc();
        return Ok(new BnplDashboardDto(
            byStatus,
            active.Count,
            overdue.Count,
            active.Sum(r => r.Amount),
            collected,
            collectedPenalty,
            active.Sum(r => r.UnpaidDebt(today)),
            overdue.Sum(r => r.Installments.Where(i => !i.IsPaid && i.DueDate < today).Sum(i => i.Amount + r.PenaltyOf(i, today))),
            active.Where(r => r.TokensLocked).Sum(r => r.TokenAmount),
            await refunds.CountAsync(ct),
            await refunds.SumAsync(p => (long?)(p.Amount + p.Penalty), ct) ?? 0,
            await Db.Requests.CountAsync(r => r.CreatedAt >= start && r.CreatedAt < end, ct),
            await Db.Requests.CountAsync(r => r.ActivatedAt >= start && r.ActivatedAt < end, ct)));
    }

    /// <summary>بازهٔ UTC «امروزِ تهران».</summary>
    private (DateTime Start, DateTime End) TodayRangeUtc()
    {
        var now = Now;
        var offset = TimeSpan.FromHours(3.5);   // ایران از ۱۴۰۱ ساعت تابستانی ندارد
        var localMidnight = (now + offset).Date;
        return (localMidnight - offset, localMidnight - offset + TimeSpan.FromDays(1));
    }
}
