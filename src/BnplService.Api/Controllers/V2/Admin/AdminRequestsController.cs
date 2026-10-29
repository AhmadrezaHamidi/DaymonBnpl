using BnplService.Application.Handler.Admin.Payments.Commands.RegisterManualPayment;
using BnplService.Application.Handler.Admin.Requests.Commands.TerminateRequest;
using BnplService.Application.Handler.Admin.Requests.Commands.WaivePenalty;
using BnplService.Application.Handler.Admin.Requests.Queries.GetRequest;
using BnplService.Application.Handler.Admin.Requests.Queries.SearchRequests;
using K = BnplService.Application.Authorization.BnplPermissionKeys;

namespace BnplService.Api.Controllers.V2.Admin;

/// <summary>پنل ادمین: قراردادها.</summary>
[Route(RoutePrefix + "/Admin/Requests")]
[SwaggerTag("پنل: قراردادها")]
public sealed class AdminRequestsController : BnplControllerBase
{
    [HttpGet]
    [RequireBnplPermission(K.RequestsView)]
    [SwaggerOperation(OperationId = "Admin_SearchRequests", Summary = "جستجوی قراردادها (وضعیت، موبایل/شماره قرارداد/نام، معوق، بازهٔ تاریخ)")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplPagedResponse<AdminRequestListItemDto>>))]
    public Task<IActionResult> Search([FromQuery] SearchRequestsQuery query, CancellationToken ct) => Send(query, ct);

    [HttpGet("{id:long}")]
    [RequireBnplPermission(K.RequestsView)]
    [SwaggerOperation(OperationId = "Admin_GetRequest", Summary = "جزئیات قرارداد، اقساط، پرداخت‌ها و تاریخچه")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<AdminRequestDetailsDto>))]
    public Task<IActionResult> Get(long id, CancellationToken ct) => Send(new GetRequestQuery(id), ct);

    [HttpPost("{id:long}/Terminate")]
    [RequireBnplPermission(K.RequestsTerminate)]
    [SwaggerOperation(OperationId = "Admin_Terminate", Summary = "فسخ قرارداد با دلیل", Description = "rowVersion آخرین جزئیات الزامی است.")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<AdminRequestDetailsDto>))]
    public Task<IActionResult> Terminate(long id, [FromBody] TerminateRequestCommand command, CancellationToken ct) =>
        Send(command with { Id = id }, ct);

    [HttpPost("{id:long}/Installments/{number:int}/WaivePenalty")]
    [RequireBnplPermission(K.PenaltiesWaive)]
    [SwaggerOperation(OperationId = "Admin_WaivePenalty", Summary = "بخشودگی جریمهٔ دیرکرد یک قسط")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<AdminRequestDetailsDto>))]
    public Task<IActionResult> WaivePenalty(long id, int number, [FromBody] WaivePenaltyCommand command, CancellationToken ct) =>
        Send(command with { Id = id, InstallmentNumber = number }, ct);

    [HttpPost("{id:long}/ManualPayments")]
    [RequireBnplPermission(K.PaymentsManual)]
    [SwaggerOperation(OperationId = "Admin_ManualPayment", Summary = "ثبت پرداخت دستی یک قسط (واریز خارج از سامانه) با شماره پیگیری")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<AdminRequestDetailsDto>))]
    public Task<IActionResult> ManualPayment(long id, [FromBody] RegisterManualPaymentCommand command, CancellationToken ct) =>
        Send(command with { RequestId = id }, ct);
}
