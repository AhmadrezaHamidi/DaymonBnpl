namespace BnplService.Application.Handler.Customer.Requests.Queries.GetIncompleteRequest;

/// <summary>«یک درخواست تکمیل نشده دارید»: درخواست باز مشتری یا data = null. درخواستِ مهلت‌گذشته همین‌جا منقضی می‌شود.</summary>
public sealed record GetIncompleteRequestQuery : BnplBaseQuery<BnplMessageResponse<BnplRequestDetailsDto?>>;
