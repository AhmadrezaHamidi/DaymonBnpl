using BnplService.Application.Handler.Customer.Payments.Commands.StartPayment;
using BnplService.Application.Handler.Customer.Requests.Commands.AcceptTerms;
using BnplService.Application.Handler.Customer.Requests.Commands.ApplyDiscount;
using BnplService.Application.Handler.Customer.Requests.Commands.CancelRequest;
using BnplService.Application.Handler.Customer.Requests.Commands.CreateRequest;
using BnplService.Application.Handler.Customer.Requests.Commands.SignContract;
using BnplService.Application.Handler.Customer.Requests.Queries.GetIncompleteRequest;
using BnplService.Application.Handler.Customer.Requests.Queries.GetMyRequest;
using BnplService.Application.Handler.Customer.Requests.Queries.SearchMyRequests;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BnplService.Api.Controllers.V2.Customer;

/// <summary>
/// درخواست پیش‌خرید: ثبت ← (کد تخفیف) ← تأیید شرایط ← امضا ← پرداخت قسط اول (فعال‌سازی) ← اقساط بعدی ← تسویه.
/// همهٔ پاسخ‌های جزئیات rowVersion دارند؛ اگر در بدنه فرستاده شود و نخواند ← 409.
/// </summary>
[Route(RoutePrefix + "/Requests")]
[RequireBnplCustomer]
[SwaggerTag("مشتری: درخواست‌ها")]
public sealed class RequestsController : BnplControllerBase
{
    [HttpPost]
    [SwaggerOperation(OperationId = "Customer_CreateRequest", Summary = "ثبت درخواست پیش‌خرید با مبلغ و طرح",
        Description = "هر مشتری فقط یک درخواست تکمیل‌نشده می‌تواند داشته باشد؛ پروفایل و نشانی باید کامل باشد.")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplRequestDetailsDto>))]
    public Task<IActionResult> Create([FromBody] CreateRequestCommand command, CancellationToken ct) => Send(command, ct);

    [HttpGet("Incomplete")]
    [SwaggerOperation(OperationId = "Customer_IncompleteRequest", Summary = "درخواست تکمیل‌نشدهٔ فعلی (برای «ادامه یا لغو»)؛ data = null یعنی ندارد")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplRequestDetailsDto>))]
    public Task<IActionResult> Incomplete(CancellationToken ct) => Send(new GetIncompleteRequestQuery(), ct);

    [HttpGet]
    [SwaggerOperation(OperationId = "Customer_SearchRequests", Summary = "تاریخچهٔ درخواست‌ها (همه / تسویه‌نشده / تسویه‌شده)")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplPagedResponse<BnplRequestListItemDto>>))]
    public Task<IActionResult> Search([FromQuery] SearchMyRequestsQuery query, CancellationToken ct) => Send(query, ct);

    [HttpGet("{id:long}")]
    [SwaggerOperation(OperationId = "Customer_GetRequest", Summary = "جزئیات قرارداد، اقساط و تاریخچه")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplRequestDetailsDto>))]
    public Task<IActionResult> Get(long id, CancellationToken ct) => Send(new GetMyRequestQuery(id), ct);

    [HttpPost("{id:long}/Discount")]
    [SwaggerOperation(OperationId = "Customer_ApplyDiscount", Summary = "اعمال یا حذف کد تخفیف (code خالی = حذف)", Description = "فقط پیش از امضا.")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplRequestDetailsDto>))]
    public Task<IActionResult> Discount(long id, [FromBody] ApplyDiscountCommand command, CancellationToken ct) =>
        Send(command with { Id = id }, ct);

    [HttpPost("{id:long}/AcceptTerms")]
    [SwaggerOperation(OperationId = "Customer_AcceptTerms", Summary = "تأیید شرایط و قوانین")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplRequestDetailsDto>))]
    public Task<IActionResult> AcceptTerms(long id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AcceptTermsCommand? command,
        CancellationToken ct) => Send((command ?? new AcceptTermsCommand()) with { Id = id }, ct);

    [HttpPost("{id:long}/Sign")]
    [SwaggerOperation(OperationId = "Customer_Sign", Summary = "امضای قرارداد", Description = "از این لحظه مهلت پرداخت قسط اول شروع می‌شود (پیش‌فرض ۳۰ دقیقه).")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplRequestDetailsDto>))]
    public Task<IActionResult> Sign(long id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SignContractCommand? command,
        CancellationToken ct) => Send((command ?? new SignContractCommand()) with { Id = id }, ct);

    [HttpPost("{id:long}/Cancel")]
    [SwaggerOperation(OperationId = "Customer_Cancel", Summary = "لغو درخواست تکمیل‌نشده")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplRequestDetailsDto>))]
    public Task<IActionResult> Cancel(long id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CancelRequestCommand? command,
        CancellationToken ct) => Send((command ?? new CancelRequestCommand()) with { Id = id }, ct);

    [HttpPost("{id:long}/Payments")]
    [SwaggerOperation(OperationId = "Customer_StartPayment", Summary = "«تایید و پرداخت» یک قسط",
        Description = "Gateway ← payUrl برمی‌گردد و مشتری به آن هدایت می‌شود؛ درگاه بعد از پرداخت به کال‌بک این سرویس و سپس به returnUrl اپ برمی‌گردد. "
                    + "Token ← از موجودی آزاد توکن کسر و نتیجه همان‌جا در receipt می‌آید. قسط اول (فعال‌سازی) فقط با درگاه.")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<PaymentStartDto>))]
    public Task<IActionResult> Pay(long id, [FromBody] StartPaymentCommand command, CancellationToken ct) =>
        Send(command with { RequestId = id }, ct);
}
