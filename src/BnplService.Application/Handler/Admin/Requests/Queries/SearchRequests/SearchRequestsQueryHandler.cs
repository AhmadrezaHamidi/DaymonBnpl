using BnplService.Application.Handler.Admin.Shared;

namespace BnplService.Application.Handler.Admin.Requests.Queries.SearchRequests;

public sealed class SearchRequestsQueryHandler(IBnplDbContext db, IBnplCurrentUser user, TimeProvider time)
    : AdminHandlerBase(db, user, time), IRequestHandler<SearchRequestsQuery, BnplMessageResponse<BnplPagedResponse<AdminRequestListItemDto>>>
{
    public async Task<BnplMessageResponse<BnplPagedResponse<AdminRequestListItemDto>>> Handle(SearchRequestsQuery query, CancellationToken ct)
    {
        var (page, size) = Paging(query.Page, query.PageSize);
        var q = Db.Requests.AsQueryable();
        if (query.Status is { } status) q = q.Where(r => r.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Term))
        {
            var term = query.Term.Trim();
            var mobile = MobileNumber.Normalize(term);
            q = q.Where(r => r.ContractNumber.Contains(term) || (r.CustomerName != null && r.CustomerName.Contains(term))
                             || r.MobileNumber == (mobile ?? term));
        }
        if (query.From is { } from) q = q.Where(r => r.CreatedAt >= from);
        if (query.To is { } to) q = q.Where(r => r.CreatedAt <= to);
        if (query.OverdueOnly)
        {
            var today = Today;
            q = q.Where(r => r.Status == BnplRequestStatus.Active
                             && r.Installments.Any(i => i.Status == InstallmentStatus.Unpaid && i.DueDate < today));
        }

        var total = await q.CountAsync(ct);
        var items = await q.Include(r => r.Installments).OrderByDescending(r => r.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Ok(Page(items.Select(ToAdminItem).ToList(), total, page, size));
    }
}
