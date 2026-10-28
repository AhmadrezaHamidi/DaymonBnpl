using System.ComponentModel.DataAnnotations;
using BnplService.Application.Handler.Admin.Shared;

namespace BnplService.Application.Handler.Admin.Payments.Queries.SearchPayments;

/// <summary>همهٔ تراکنش‌ها در پنل؛ مثلاً Status = RefundRequired برای صف برگشت وجه.</summary>
public sealed record SearchPaymentsQuery : BnplPagedQuery<BnplMessageResponse<BnplPagedResponse<AdminPaymentListItemDto>>>
{
    public PaymentStatus? Status { get; init; }
    public PaymentMethod? Method { get; init; }
    public long? RequestId { get; init; }

    /// <summary>شماره قرارداد یا شماره پیگیری.</summary>
    [StringLength(100)] public string? Term { get; init; }

    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

public sealed class SearchPaymentsQueryHandler(IBnplDbContext db, IBnplCurrentUser user, TimeProvider time)
    : AdminHandlerBase(db, user, time), IRequestHandler<SearchPaymentsQuery, BnplMessageResponse<BnplPagedResponse<AdminPaymentListItemDto>>>
{
    public async Task<BnplMessageResponse<BnplPagedResponse<AdminPaymentListItemDto>>> Handle(SearchPaymentsQuery query, CancellationToken ct)
    {
        var (page, size) = Paging(query.Page, query.PageSize);
        var q = Db.Payments.AsQueryable();
        if (query.Status is { } status) q = q.Where(p => p.Status == status);
        if (query.Method is { } method) q = q.Where(p => p.Method == method);
        if (query.RequestId is { } requestId) q = q.Where(p => p.RequestId == requestId);
        if (!string.IsNullOrWhiteSpace(query.Term))
        {
            var term = query.Term.Trim();
            q = q.Where(p => p.ContractNumber.Contains(term) || p.TrackingNumber == term);
        }
        if (query.From is { } from) q = q.Where(p => p.CreatedAt >= from);
        if (query.To is { } to) q = q.Where(p => p.CreatedAt <= to);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Ok(Page(items.Select(p => new AdminPaymentListItemDto(PaymentReceiptDto.Of(p), p.RequestId, p.CustomerId, ActorDto.Of(p.By)))
            .ToList(), total, page, size));
    }
}
