namespace BnplService.Application.Handler.Customer.Requests.Queries.GetMyRequest;

public sealed class GetMyRequestQueryHandler(IBnplDbContext db, IBnplCurrentCustomer customer, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<GetMyRequestQuery, BnplMessageResponse<BnplRequestDetailsDto>>
{
    public async Task<BnplMessageResponse<BnplRequestDetailsDto>> Handle(GetMyRequestQuery query, CancellationToken ct)
    {
        var request = await RequestAsync(query.Id, ct, customer.Value.UserId);
        if (await ExpireIfDueAsync(request, ct)) await Db.SaveChangesAsync(ct);
        return Ok(request.ToDetails(Today));
    }
}
