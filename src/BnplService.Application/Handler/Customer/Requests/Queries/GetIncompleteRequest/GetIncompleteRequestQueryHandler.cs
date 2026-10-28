namespace BnplService.Application.Handler.Customer.Requests.Queries.GetIncompleteRequest;

public sealed class GetIncompleteRequestQueryHandler(IBnplDbContext db, IBnplCurrentCustomer customer, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<GetIncompleteRequestQuery, BnplMessageResponse<BnplRequestDetailsDto?>>
{
    public async Task<BnplMessageResponse<BnplRequestDetailsDto?>> Handle(GetIncompleteRequestQuery request, CancellationToken ct)
    {
        var me = customer.Value.UserId;
        var open = await RequestsWithDetails()
            .Where(r => r.CustomerId == me && BnplRequest.IncompleteStatuses.Contains(r.Status))
            .OrderByDescending(r => r.Id).ToListAsync(ct);
        BnplRequest? current = null;
        var expired = false;
        foreach (var r in open)
        {
            if (await ExpireIfDueAsync(r, ct)) expired = true;
            else current ??= r;
        }
        if (expired) await Db.SaveChangesAsync(ct);
        return Ok(current?.ToDetails(Today));
    }
}
