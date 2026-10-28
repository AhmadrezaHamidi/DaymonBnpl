namespace BnplService.Application.Handler.Customer.Requests.Queries.SearchMyRequests;

public sealed class SearchMyRequestsQueryHandler(IBnplDbContext db, IBnplCurrentCustomer customer, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<SearchMyRequestsQuery, BnplMessageResponse<BnplPagedResponse<BnplRequestListItemDto>>>
{
    /// <summary>«همه» یعنی قراردادهای امضاشده؛ درخواست تکمیل‌نشده/لغوشده/منقضی در تاریخچه نیست (فیگما).</summary>
    private static readonly BnplRequestStatus[] Contracts =
        [BnplRequestStatus.Active, BnplRequestStatus.Settled, BnplRequestStatus.Terminated];

    public async Task<BnplMessageResponse<BnplPagedResponse<BnplRequestListItemDto>>> Handle(SearchMyRequestsQuery query, CancellationToken ct)
    {
        var (page, size) = Paging(query.Page, query.PageSize);
        var statuses = query.Filter switch
        {
            MyRequestsFilter.Unsettled => BnplRequest.UnsettledStatuses,
            MyRequestsFilter.Settled => BnplRequest.SettledStatuses,
            _ => Contracts,
        };
        var me = customer.Value.UserId;
        var q = Db.Requests.Where(r => r.CustomerId == me && statuses.Contains(r.Status));
        var total = await q.CountAsync(ct);
        var items = await q.Include(r => r.Installments).OrderByDescending(r => r.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return Ok(Page(items.Select(r => r.ToListItem(Today)).ToList(), total, page, size));
    }
}
