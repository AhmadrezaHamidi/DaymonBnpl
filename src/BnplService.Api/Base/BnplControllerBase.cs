namespace BnplService.Api.Base;

/// <summary>
/// پایهٔ کنترلرها (مثل FacilityPanelControllerBase): MediatR و تبدیل BnplMessageResponse به کد HTTP.
/// پاسخ همیشه همان قالب است:  { "statusCode": 200, "message": "...", "data": {...} }
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class BnplControllerBase : ControllerBase
{
    /// <summary>پیشوند همهٔ مسیرها؛ کال‌بک درگاه در BnplOptions.CallbackUrl هم همین را می‌سازد.</summary>
    public const string RoutePrefix = "v2/bnpl";

    protected IMediator Mediator => HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected IActionResult HandleResult<T>(BnplMessageResponse<T> response) =>
        StatusCode(response.StatusCode == 0 ? StatusCodes.Status200OK : response.StatusCode, response);

    protected async Task<IActionResult> Send<T>(IRequest<BnplMessageResponse<T>> request, CancellationToken ct) =>
        HandleResult(await Mediator.Send(request, ct));
}
