using BnplService.Application.Handler.Admin.Settings.Commands.SavePlan;
using BnplService.Application.Handler.Admin.Settings.Commands.UpdateSettings;
using BnplService.Application.Handler.Admin.Settings.Queries.GetSettings;
using K = BnplService.Application.Authorization.BnplPermissionKeys;

namespace BnplService.Api.Controllers.V2.Admin;

/// <summary>پنل ادمین: تنظیمات پیش‌خرید و طرح‌های اقساط.</summary>
[Route(RoutePrefix + "/Admin")]
[SwaggerTag("پنل: تنظیمات و طرح‌ها")]
public sealed class AdminSettingsController : BnplControllerBase
{
    [HttpGet("Settings")]
    [RequireBnplPermission(K.SettingsView)]
    [SwaggerOperation(OperationId = "Admin_GetSettings", Summary = "تنظیمات و همهٔ طرح‌ها")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplAdminSettingsDto>))]
    public Task<IActionResult> Get(CancellationToken ct) => Send(new GetSettingsQuery(), ct);

    [HttpPut("Settings")]
    [RequireBnplPermission(K.SettingsEdit)]
    [SwaggerOperation(OperationId = "Admin_UpdateSettings", Summary = "ویرایش تنظیمات (فعال/غیرفعال، بازهٔ مبلغ، گام، مهلت‌ها)")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplSettingsDto>))]
    public Task<IActionResult> Update([FromBody] UpdateSettingsCommand command, CancellationToken ct) => Send(command, ct);

    [HttpPost("Plans")]
    [RequireBnplPermission(K.SettingsEdit)]
    [SwaggerOperation(OperationId = "Admin_CreatePlan", Summary = "تعریف طرح اقساط")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<PlanDto>))]
    public Task<IActionResult> CreatePlan([FromBody] SavePlanCommand command, CancellationToken ct) => Send(command with { Id = null }, ct);

    [HttpPut("Plans/{id:int}")]
    [RequireBnplPermission(K.SettingsEdit)]
    [SwaggerOperation(OperationId = "Admin_UpdatePlan", Summary = "ویرایش طرح اقساط", Description = "قراردادهای قبلی با شرایط خودشان می‌مانند.")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<PlanDto>))]
    public Task<IActionResult> UpdatePlan(int id, [FromBody] SavePlanCommand command, CancellationToken ct) => Send(command with { Id = id }, ct);
}
