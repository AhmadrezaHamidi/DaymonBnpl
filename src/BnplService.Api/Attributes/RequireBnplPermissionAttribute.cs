using BnplService.Api.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BnplService.Api.Attributes;

/// <summary>
/// Endpoint پنل ادمین: توکن پنل (کوکی یا Bearer) با Identity بررسی می‌شود و کاربر باید یکی از کلیدها را داشته باشد
/// (مثل RequireFacilityPermission). کلیدها: BnplPermissionKeys؛ اسکریپت ساخت آن‌ها: Database/identity/bnpl_panel_permissions.sql
/// </summary>
public sealed class RequireBnplPermissionAttribute : TypeFilterAttribute
{
    public RequireBnplPermissionAttribute(params string[] anyOfKeys) : base(typeof(BnplPermissionFilter))
    {
        Arguments = [anyOfKeys];
    }
}

public sealed class BnplPermissionFilter(
    string[] anyOfKeys,
    IIdentityAuthorizationClient identity,
    IConfiguration configuration,
    ILogger<BnplPermissionFilter> logger) : IAsyncAuthorizationFilter
{
    /// <summary>نام پیش‌فرض کوکی توکن پنل؛ اگر فرق دارد «Bnpl:PanelTokenCookieName».</summary>
    public const string TokenCookie = "token";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var token = http.Request.Cookies[configuration["Bnpl:PanelTokenCookieName"] ?? TokenCookie];
        if (string.IsNullOrEmpty(token)) token = BnplAuthResults.BearerToken(http);
        if (string.IsNullOrEmpty(token))
        {
            context.Result = BnplAuthResults.Error(StatusCodes.Status401Unauthorized, BnplAuthResults.InvalidSession);
            return;
        }

        BnplPanelUser? user;
        try
        {
            user = await identity.ResolveAsync(token, http.RequestAborted);
        }
        catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !http.RequestAborted.IsCancellationRequested)
        {
            logger.LogError(ex, "Identity authorization service is unreachable for {Path}", http.Request.Path);
            context.Result = BnplAuthResults.Error(StatusCodes.Status503ServiceUnavailable, BnplAuthResults.IdentityDown);
            return;
        }
        if (user is null)
        {
            context.Result = BnplAuthResults.Error(StatusCodes.Status401Unauthorized, BnplAuthResults.InvalidSession);
            return;
        }

        if (anyOfKeys.Length > 0 && !anyOfKeys.Any(user.Permissions.Has))
        {
            logger.LogWarning("BNPL permission denied. User {UserId} lacks any of [{Keys}] for {Path}",
                user.PanelUserId, string.Join(", ", anyOfKeys), http.Request.Path);
            context.Result = BnplAuthResults.Error(StatusCodes.Status403Forbidden, "به این بخش دسترسی ندارید.");
            return;
        }

        http.Items[BnplCurrentUserAccessor.HttpContextKey] = user;
    }
}
