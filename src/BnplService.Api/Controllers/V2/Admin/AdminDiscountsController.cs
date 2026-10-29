using BnplService.Application.Handler.Admin.Discounts.Commands.SaveDiscount;
using BnplService.Application.Handler.Admin.Discounts.Queries.SearchDiscounts;
using K = BnplService.Application.Authorization.BnplPermissionKeys;

namespace BnplService.Api.Controllers.V2.Admin;

/// <summary>پنل ادمین: کدهای تخفیف.</summary>
[Route(RoutePrefix + "/Admin/Discounts")]
[SwaggerTag("پنل: کدهای تخفیف")]
public sealed class AdminDiscountsController : BnplControllerBase
{
    [HttpGet]
    [RequireBnplPermission(K.DiscountsView)]
    [SwaggerOperation(OperationId = "Admin_SearchDiscounts", Summary = "فهرست کدهای تخفیف")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplPagedResponse<DiscountCodeDto>>))]
    public Task<IActionResult> Search([FromQuery] SearchDiscountsQuery query, CancellationToken ct) => Send(query, ct);

    [HttpPost]
    [RequireBnplPermission(K.DiscountsEdit)]
    [SwaggerOperation(OperationId = "Admin_CreateDiscount", Summary = "تعریف کد تخفیف (درصد از کارمزد یا مبلغ ثابت)")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<DiscountCodeDto>))]
    public Task<IActionResult> Create([FromBody] SaveDiscountCommand command, CancellationToken ct) => Send(command with { Id = null }, ct);

    [HttpPut("{id:int}")]
    [RequireBnplPermission(K.DiscountsEdit)]
    [SwaggerOperation(OperationId = "Admin_UpdateDiscount", Summary = "ویرایش کد تخفیف")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<DiscountCodeDto>))]
    public Task<IActionResult> Update(int id, [FromBody] SaveDiscountCommand command, CancellationToken ct) => Send(command with { Id = id }, ct);
}
