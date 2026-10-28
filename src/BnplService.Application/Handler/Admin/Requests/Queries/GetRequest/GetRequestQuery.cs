using BnplService.Application.Handler.Admin.Shared;

namespace BnplService.Application.Handler.Admin.Requests.Queries.GetRequest;

/// <summary>جزئیات کامل قرارداد در پنل: اقساط، تاریخچه و همهٔ تلاش‌های پرداخت.</summary>
public sealed record GetRequestQuery(long Id) : BnplBaseQuery<BnplMessageResponse<AdminRequestDetailsDto>>;

public sealed class GetRequestQueryHandler(IBnplDbContext db, IBnplCurrentUser user, TimeProvider time)
    : AdminHandlerBase(db, user, time), IRequestHandler<GetRequestQuery, BnplMessageResponse<AdminRequestDetailsDto>>
{
    public async Task<BnplMessageResponse<AdminRequestDetailsDto>> Handle(GetRequestQuery query, CancellationToken ct) =>
        Ok(await AdminDetailsAsync(await RequestAsync(query.Id, ct), ct));
}
