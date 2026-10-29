using BnplService.Application.Handler.Admin.Payments.Commands.MarkRefunded;
using BnplService.Application.Handler.Admin.Payments.Queries.SearchPayments;
using K = BnplService.Application.Authorization.BnplPermissionKeys;

namespace BnplService.Api.Controllers.V2.Admin;

/// <summary>پنل ادمین: پرداخت‌ها و استرداد.</summary>
[Route(RoutePrefix + "/Admin/Payments")]
[SwaggerTag("پنل: پرداخت‌ها")]
public sealed class AdminPaymentsController : BnplControllerBase
{
    [HttpGet]
    [RequireBnplPermission(K.PaymentsView)]
    [SwaggerOperation(OperationId = "Admin_SearchPayments", Summary = "جستجوی پرداخت‌ها", Description = "status = RefundRequired فهرست پرداخت‌هایی است که باید برگشت داده شوند.")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplPagedResponse<AdminPaymentListItemDto>>))]
    public Task<IActionResult> Search([FromQuery] SearchPaymentsQuery query, CancellationToken ct) => Send(query, ct);

    [HttpPost("{id:long}/Refunded")]
    [RequireBnplPermission(K.PaymentsRefund)]
    [SwaggerOperation(OperationId = "Admin_MarkRefunded", Summary = "ثبت برگشت وجه یک پرداخت «نیازمند برگشت وجه» با شماره پیگیری")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<AdminPaymentListItemDto>))]
    public Task<IActionResult> MarkRefunded(long id, [FromBody] MarkRefundedCommand command, CancellationToken ct) =>
        Send(command with { PaymentId = id }, ct);
}
