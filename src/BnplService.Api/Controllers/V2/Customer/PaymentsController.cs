using BnplService.Application.Handler.Customer.Payments.Commands.CompleteGatewayPayment;
using BnplService.Application.Handler.Customer.Payments.Queries.GetGateways;
using BnplService.Application.Handler.Customer.Payments.Queries.GetMyPayment;
using BnplService.Application.Handler.Customer.Payments.Queries.SearchMyPayments;

namespace BnplService.Api.Controllers.V2.Customer;

/// <summary>پرداخت‌ها و «گزارش تراکنش»؛ به‌علاوهٔ کال‌بک درگاه (بدون احراز هویت؛ نتیجه از خود درگاه استعلام می‌شود).</summary>
[Route(RoutePrefix + "/Payments")]
[SwaggerTag("مشتری: پرداخت‌ها")]
public sealed class PaymentsController : BnplControllerBase
{
    [HttpGet("Gateways")]
    [RequireBnplCustomer]
    [SwaggerOperation(OperationId = "Customer_Gateways", Summary = "فهرست درگاه‌های پرداخت")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<IReadOnlyList<PaymentGatewayDto>>))]
    public Task<IActionResult> Gateways(CancellationToken ct) => Send(new GetGatewaysQuery(), ct);

    [HttpGet]
    [RequireBnplCustomer]
    [SwaggerOperation(OperationId = "Customer_SearchPayments", Summary = "تاریخچهٔ پرداخت‌ها (اختیاری: یک قرارداد)")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplPagedResponse<PaymentReceiptDto>>))]
    public Task<IActionResult> Search([FromQuery] SearchMyPaymentsQuery query, CancellationToken ct) => Send(query, ct);

    [HttpGet("{id:long}")]
    [RequireBnplCustomer]
    [SwaggerOperation(OperationId = "Customer_GetPayment", Summary = "رسید یک پرداخت (گزارش تراکنش)")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<PaymentReceiptDto>))]
    public Task<IActionResult> Get(long id, CancellationToken ct) => Send(new GetMyPaymentQuery(id), ct);

    /// <summary>
    /// درگاه بعد از پرداخت مرورگر مشتری را به این آدرس برمی‌گرداند (Bnpl:PublicBaseUrl + این مسیر).
    /// به پارامترهای مرورگر اعتماد نمی‌شود: نتیجه با Verify از خود درگاه گرفته می‌شود و تکرار کال‌بک اثری ندارد.
    /// اگر اپ returnUrl داده بود ← 302 به آن با paymentId و status؛ وگرنه JSON فقط با شناسه و وضعیت
    /// (این مسیر بی‌احراز است، پس رسید کامل را نمی‌دهد؛ اپ آن را از GET Payments/{id} می‌گیرد).
    /// </summary>
    [HttpGet("{id:long}/Callback")]
    [HttpPost("{id:long}/Callback")]
    [SwaggerOperation(OperationId = "Gateway_Callback", Summary = "کال‌بک درگاه (بدون احراز هویت)")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<GatewayCallbackDto>))]
    [SwaggerResponse(302, "هدایت به returnUrl اپ")]
    public async Task<IActionResult> Callback(long id, CancellationToken ct)
    {
        var response = await Mediator.Send(new CompleteGatewayPaymentCommand(id), ct);
        if (response is { IsSuccess: true, Data.RedirectUrl: { } redirect }) return Redirect(redirect);
        return HandleResult(MessageResponse.Generate(response.StatusCode, response.Data is { } d ? new GatewayCallbackDto(d.PaymentId, d.Status) : null,
            response.Message));
    }
}

/// <summary>پاسخ کال‌بک وقتی returnUrl نیست.</summary>
public sealed record GatewayCallbackDto(long PaymentId, PaymentStatus Status);
