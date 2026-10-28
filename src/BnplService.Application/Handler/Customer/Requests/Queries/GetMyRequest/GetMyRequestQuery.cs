namespace BnplService.Application.Handler.Customer.Requests.Queries.GetMyRequest;

/// <summary>صفحهٔ «جزئیات» قرارداد با «لیست اقساط».</summary>
public sealed record GetMyRequestQuery(long Id) : BnplBaseQuery<BnplMessageResponse<BnplRequestDetailsDto>>;
