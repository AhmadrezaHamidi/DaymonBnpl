using BnplService.Application.Handler.Customer.Config.Queries.GetConfig;
using BnplService.Application.Handler.Customer.Config.Queries.GetQuote;

namespace BnplService.Api.Controllers.V2.Customer;

/// <summary>صفحهٔ «پیش‌خرید اقساطی»: اسلایدر مبلغ، طرح‌ها و محاسبهٔ شرایط.</summary>
[Route(RoutePrefix)]
[RequireBnplCustomer]
[SwaggerTag("مشتری: تنظیمات و محاسبه")]
public sealed class ConfigController : BnplControllerBase
{
    [HttpGet("Config")]
    [SwaggerOperation(OperationId = "Customer_Config", Summary = "تنظیمات اسلایدر، طرح‌های فعال و قیمت لحظه‌ای توکن")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<BnplConfigDto>))]
    public Task<IActionResult> Config(CancellationToken ct) => Send(new GetConfigQuery(), ct);

    [HttpGet("Quote")]
    [SwaggerOperation(OperationId = "Customer_Quote", Summary = "شرایط وام برای مبلغ (قسط اول، بقیهٔ اقساط، کارمزد، تعداد توکن)",
        Description = "چیزی ذخیره نمی‌شود. کد تخفیف اختیاری است و فقط از قسط آخر کم می‌شود.")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<QuoteDto>))]
    public Task<IActionResult> Quote([FromQuery] GetQuoteQuery query, CancellationToken ct) => Send(query, ct);
}
