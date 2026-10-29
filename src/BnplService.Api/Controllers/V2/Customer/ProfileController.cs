using BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileAddress;
using BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileInfo;
using BnplService.Application.Handler.Customer.Profile.Queries.GetProfileStatus;

namespace BnplService.Api.Controllers.V2.Customer;

/// <summary>پروفایل و نشانی مشتری نزد دایادایموند؛ پیش‌خرید فقط با پروفایل و نشانی کامل ممکن است.</summary>
[Route(RoutePrefix + "/Profile")]
[RequireBnplCustomer]
[SwaggerTag("مشتری: پروفایل")]
public sealed class ProfileController : BnplControllerBase
{
    [HttpGet]
    [SwaggerOperation(OperationId = "Customer_ProfileStatus", Summary = "وضعیت تکمیل پروفایل و نشانی")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<CustomerProfileStatusDto>))]
    public Task<IActionResult> Status(CancellationToken ct) => Send(new GetProfileStatusQuery(), ct);

    [HttpPost("Info")]
    [SwaggerOperation(OperationId = "Customer_ProfileInfo", Summary = "ثبت یا ویرایش اطلاعات هویتی (کد ملی، شبا، ...)")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<CustomerProfileStatusDto>))]
    public Task<IActionResult> Info([FromBody] UpsertProfileInfoCommand command, CancellationToken ct) => Send(command, ct);

    [HttpPost("Address")]
    [SwaggerOperation(OperationId = "Customer_ProfileAddress", Summary = "ثبت یا ویرایش نشانی محل سکونت (برای قرارداد)")]
    [SwaggerResponse(200, null, typeof(BnplMessageResponse<CustomerProfileStatusDto>))]
    public Task<IActionResult> Address([FromBody] UpsertProfileAddressCommand command, CancellationToken ct) => Send(command, ct);
}
