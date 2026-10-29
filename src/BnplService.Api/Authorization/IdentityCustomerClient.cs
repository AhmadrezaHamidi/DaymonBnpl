using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BnplService.Domain.Entity.Common;
using BnplService.Domain.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace BnplService.Api.Authorization;

/// <summary>
/// مشتری اپ ← Identity: توکن فعال و موبایل مشتری را با POST v2/InternalIdentity/Authenticate بررسی می‌کند
/// (همان سرویسی که BaseApplicationApi.AuthenticateAsync در بقیهٔ سرویس‌ها صدا می‌زند) و کاربر را برمی‌گرداند.
/// نتیجه چند ثانیه با هش (موبایل + توکن) کش می‌شود.
/// </summary>
public interface IIdentityCustomerClient
{
    /// <summary>null یعنی توکن نامعتبر یا کاربر غیرفعال ← 401.</summary>
    Task<BnplCustomerRef?> AuthenticateAsync(string mobileNumber, string activeToken, CancellationToken cancellationToken);
}

public sealed class IdentityCustomerClient(HttpClient http, IMemoryCache cache, IConfiguration configuration, ILogger<IdentityCustomerClient> logger)
    : IIdentityCustomerClient
{
    public const string DefaultPath = "v2/InternalIdentity/Authenticate";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<BnplCustomerRef?> AuthenticateAsync(string mobileNumber, string activeToken, CancellationToken cancellationToken)
    {
        var key = "bnpl:customer-session:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(mobileNumber + "|" + activeToken)));
        if (cache.TryGetValue(key, out BnplCustomerRef? cached))
            return cached;

        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync(configuration["Identity:CustomerAuthenticatePath"] ?? DefaultPath,
                new { mobileNumber, activeToken }, Json, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Identity customer authentication is unreachable");
            throw;
        }

        BnplCustomerRef? customer = null;
        if (response.IsSuccessStatusCode)
        {
            // ⚠ قالب پاسخ: BaseMessageResponse<UserModel> Identity؛ فقط data.id، data.mobileNumber، نام و isActive خوانده می‌شود.
            var body = await response.Content.ReadFromJsonAsync<AuthenticateResponse>(Json, cancellationToken);
            if (body?.Data is { Id: > 0, IsActive: not false } user)
            {
                var name = $"{user.FirstName} {user.LastName}".Trim();
                customer = new BnplCustomerRef(user.Id.ToString(), MobileNumber.Normalize(user.MobileNumber) ?? mobileNumber,
                    string.IsNullOrWhiteSpace(name) ? null : name);
            }
        }
        cache.Set(key, customer, IdentityAuthorizationClient.CacheDuration);
        return customer;
    }

    private sealed record AuthenticateResponse(UserData? Data);

    private sealed record UserData(int Id, string? MobileNumber, string? FirstName, string? LastName, bool? IsActive);
}

public sealed class BnplCurrentCustomerAccessor(IHttpContextAccessor accessor) : IBnplCurrentCustomer
{
    public const string HttpContextKey = "__BnplCustomer";

    public BnplCustomerRef Value =>
        accessor.HttpContext?.Items[HttpContextKey] as BnplCustomerRef
        ?? throw new InvalidOperationException("Endpoint باید [RequireBnplCustomer] داشته باشد.");
}
