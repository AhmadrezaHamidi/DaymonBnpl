using BnplService.Api.Authorization;
using BnplService.Domain.Helpers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BnplService.Api.Attributes;

/// <summary>
/// Endpoint مشتری اپ: هدرهای توکن فعال و موبایل (مثل بقیهٔ سرویس‌های اپ) با Identity بررسی می‌شوند.
///   توکن: هدر «Token» یا Authorization: Bearer ...      (نام هدر: Bnpl:CustomerAuth:TokenHeader)
///   موبایل: هدر «MobileNumber»                          (نام هدر: Bnpl:CustomerAuth:MobileHeader)
/// ⚠ نام هدرها را با BaseApplicationApi پکیج OmidBank.Common یکی کنید.
/// </summary>
public sealed class RequireBnplCustomerAttribute : TypeFilterAttribute
{
    public RequireBnplCustomerAttribute() : base(typeof(BnplCustomerFilter)) { }
}

public sealed class BnplCustomerFilter(IIdentityCustomerClient identity, IConfiguration configuration, ILogger<BnplCustomerFilter> logger)
    : IAsyncAuthorizationFilter
{
    public const string DefaultTokenHeader = "Token";
    public const string DefaultMobileHeader = "MobileNumber";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var token = http.Request.Headers[configuration["Bnpl:CustomerAuth:TokenHeader"] ?? DefaultTokenHeader].ToString();
        if (string.IsNullOrWhiteSpace(token)) token = BnplAuthResults.BearerToken(http);
        var mobile = MobileNumber.Normalize(http.Request.Headers[configuration["Bnpl:CustomerAuth:MobileHeader"] ?? DefaultMobileHeader].ToString());
        if (string.IsNullOrWhiteSpace(token) || mobile is null)
        {
            context.Result = BnplAuthResults.Error(StatusCodes.Status401Unauthorized, BnplAuthResults.InvalidSession);
            return;
        }

        BnplService.Domain.Entity.Common.BnplCustomerRef? customer;
        try
        {
            customer = await identity.AuthenticateAsync(mobile, token, http.RequestAborted);
        }
        catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !http.RequestAborted.IsCancellationRequested)
        {
            logger.LogError(ex, "Identity customer authentication is unreachable for {Path}", http.Request.Path);
            context.Result = BnplAuthResults.Error(StatusCodes.Status503ServiceUnavailable, BnplAuthResults.IdentityDown);
            return;
        }
        if (customer is null)
        {
            context.Result = BnplAuthResults.Error(StatusCodes.Status401Unauthorized, BnplAuthResults.InvalidSession);
            return;
        }

        http.Items[BnplCurrentCustomerAccessor.HttpContextKey] = customer;
    }
}

internal static class BnplAuthResults
{
    public const string InvalidSession = "نشست نامعتبر است؛ دوباره وارد شوید.";
    public const string IdentityDown = "سرویس احراز هویت در دسترس نیست؛ چند لحظه بعد دوباره تلاش کنید.";

    public static string? BearerToken(HttpContext http)
    {
        var header = http.Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : null;
    }

    public static ObjectResult Error(int status, string message) => new(new { statusCode = status, message }) { StatusCode = status };
}
