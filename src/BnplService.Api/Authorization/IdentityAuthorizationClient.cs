using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace BnplService.Api.Authorization;

/// <summary>
/// پنل ← Identity: توکن کاربر پنل را با POST Internal/Authorization/Resolve به کاربر و مجوزها تبدیل می‌کند (همان کلاینت Facility).
/// نتیجه چند ثانیه با هش توکن کش می‌شود؛ پس تغییر دسترسی حداکثر با همین تأخیر در BNPL اثر می‌کند.
/// </summary>
public interface IIdentityAuthorizationClient
{
    /// <summary>null یعنی نشست نامعتبر (توکن، کاربر غیرفعال، یا دسترسی تغییر کرده) ← 401.</summary>
    Task<BnplPanelUser?> ResolveAsync(string token, CancellationToken cancellationToken);
}

public sealed class IdentityAuthorizationClient(HttpClient http, IMemoryCache cache, ILogger<IdentityAuthorizationClient> logger)
    : IIdentityAuthorizationClient
{
    public static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<BnplPanelUser?> ResolveAsync(string token, CancellationToken cancellationToken)
    {
        var key = "bnpl:panel-session:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        if (cache.TryGetValue(key, out BnplPanelUser? cached))
            return cached;

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync("Internal/Authorization/Resolve", new { token }, Json, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            // Identity در دسترس نیست ← بسته (fail-closed)؛ کش نمی‌شود.
            logger.LogError(ex, "Identity authorization service is unreachable");
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            cache.Set(key, (BnplPanelUser?)null, CacheDuration);
            return null;
        }

        // ⚠ قالب پاسخ همان فرض Facility است: { isSuccess, statusCode, message, data }.
        var body = await response.Content.ReadFromJsonAsync<ResolveResponse>(Json, cancellationToken);
        var user = body is { IsSuccess: true, Data: not null } ? ToUser(body.Data) : null;
        cache.Set(key, user, CacheDuration);
        return user;
    }

    private static BnplPanelUser ToUser(SessionData d) => new(
        d.PanelUserId, d.UserName, d.FullName, d.PermissionVersion,
        new PanelPermissions(d.Grants.Select(g => new PanelGrant(g.Key, g.Effect, g.ScopeType, g.ScopeId)).ToList()));

    private sealed record ResolveResponse(bool IsSuccess, string? Message, SessionData? Data);

    private sealed record SessionData(int PanelUserId, string UserName, string? FullName, int PermissionVersion, IReadOnlyList<GrantData> Grants);

    private sealed record GrantData(string Key, PanelPermissionEffect Effect, PanelScopeType ScopeType, string? ScopeId);
}

public sealed class BnplCurrentUserAccessor(IHttpContextAccessor accessor) : IBnplCurrentUser
{
    public const string HttpContextKey = "__BnplPanelUser";

    public BnplPanelUser Value =>
        accessor.HttpContext?.Items[HttpContextKey] as BnplPanelUser
        ?? throw new InvalidOperationException("Endpoint باید [RequireBnplPermission] داشته باشد.");
}
