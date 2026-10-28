namespace BnplService.Application.Handler.Customer.Payments.Queries.SearchMyPayments;

/// <summary>تراکنش‌های مشتری (همه یا یک قرارداد)، جدیدترین اول.</summary>
public sealed record SearchMyPaymentsQuery : BnplPagedQuery<BnplMessageResponse<BnplPagedResponse<PaymentReceiptDto>>>
{
    public long? RequestId { get; init; }
}

public sealed class SearchMyPaymentsQueryHandler(IBnplDbContext db, IBnplCurrentCustomer customer, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<SearchMyPaymentsQuery, BnplMessageResponse<BnplPagedResponse<PaymentReceiptDto>>>
{
    public async Task<BnplMessageResponse<BnplPagedResponse<PaymentReceiptDto>>> Handle(SearchMyPaymentsQuery query, CancellationToken ct)
    {
        var (page, size) = Paging(query.Page, query.PageSize);
        var me = customer.Value.UserId;
        var q = Db.Payments.Where(p => p.CustomerId == me && (query.RequestId == null || p.RequestId == query.RequestId));
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Ok(Page(items.Select(PaymentReceiptDto.Of).ToList(), total, page, size));
    }
}
