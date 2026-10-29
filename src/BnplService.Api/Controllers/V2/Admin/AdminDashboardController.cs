using BnplService.Application.Handler.Admin.Dashboard.Queries.GetDashboard;
using K = BnplService.Application.Authorization.BnplPermissionKeys;

namespace BnplService.Api.Controllers.V2.Admin;

/// <summary>پنل ادمین: داشبورد.</summary>
[Route(RoutePrefix + "/Admin/Dashboard")]
[SwaggerTag("پنل: داشبورد")]
public sealed class AdminDashboardController : BnplControllerBase
{
    [HttpGet]
    [RequireBnplPermission(K.DashboardView)]
    [SwaggerOperation(OperationId = "Admin_Dashboard", Summary = "شمار قراردادها به تفکیک وضعیت، مبالغ و معوقات")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplDashboardDto>))]
    public Task<IActionResult> Get(CancellationToken ct) => Send(new GetDashboardQuery(), ct);
}
