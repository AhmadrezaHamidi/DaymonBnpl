using System.ComponentModel.DataAnnotations;

namespace BnplService.Application.Handler.Admin.Discounts.Queries.SearchDiscounts;

public sealed record SearchDiscountsQuery : BnplPagedQuery<BnplMessageResponse<BnplPagedResponse<DiscountCodeDto>>>
{
    [StringLength(30)] public string? Term { get; init; }
    public bool? IsActive { get; init; }
}

public sealed class SearchDiscountsQueryHandler(IBnplDbContext db, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<SearchDiscountsQuery, BnplMessageResponse<BnplPagedResponse<DiscountCodeDto>>>
{
    public async Task<BnplMessageResponse<BnplPagedResponse<DiscountCodeDto>>> Handle(SearchDiscountsQuery query, CancellationToken ct)
    {
        var (page, size) = Paging(query.Page, query.PageSize);
        var q = Db.DiscountCodes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Term))
        {
            var term = query.Term.Trim();
            var upper = term.ToUpperInvariant();
            q = q.Where(d => d.Code.Contains(upper) || d.Title.Contains(term));
        }
        if (query.IsActive is { } active) q = q.Where(d => d.IsActive == active);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(d => d.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Ok(Page(items.Select(DiscountCodeDto.Of).ToList(), total, page, size));
    }
}
