using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DayaDiamond.Client.Models;
using Microsoft.Extensions.Options;

namespace DayaDiamond.Client;

public sealed class DayaDiamondClient : IDayaDiamondClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly DayaDiamondOptions _options;
    private readonly MobileEncryptor _encryptor;
    private readonly DayaDiamondTokenStore _tokens;

    public DayaDiamondClient(HttpClient http, IOptions<DayaDiamondOptions> options, DayaDiamondTokenStore? tokens = null)
    {
        _http = http;
        _options = options.Value;
        _tokens = tokens ?? new DayaDiamondTokenStore();
        _encryptor = new MobileEncryptor(_options.MobileKeyBase64, _options.MobileIvBase64);

        if (_http.BaseAddress is null)
        {
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                throw new InvalidOperationException("DayaDiamond:BaseUrl تنظیم نشده است.");
            _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        }
    }

    // ---------- Account ----------

    public async Task<string> LoginAsync(CancellationToken ct = default)
    {
        var body = new LoginRequest { Username = _options.Username, Password = _options.Password };
        using var response = await _http.PostAsJsonAsync("account/login", body, JsonOptions, ct);
        var result = await ReadEnvelopeAsync<LoginResult>(response, ct);

        var token = result?.Token;
        if (string.IsNullOrWhiteSpace(token))
            throw new DayaDiamondException("پاسخ لاگین توکن نداشت.", response.StatusCode);

        _tokens.Set(token, ReadJwtExpiry(token) ?? DateTimeOffset.UtcNow.AddMinutes(10));
        return token;
    }

    // ---------- Price ----------

    public Task<TokenPriceResult> GetTokenPriceAsync(CancellationToken ct = default)
        => GetAsync<TokenPriceResult>("price/token", ct)!;

    public Task<BuyPriceResult> GetBuyPriceAsync(decimal amount, string assetCode = "DAYADIAMOND", CancellationToken ct = default)
        => GetAsync<BuyPriceResult>(
            $"price/buy?amount={Num(amount)}&assetCode={Uri.EscapeDataString(assetCode)}", ct)!;

    // ---------- Gateway ----------

    public async Task<IReadOnlyList<Gateway>> GetGatewaysAsync(CancellationToken ct = default)
        => await GetAsync<List<Gateway>>("gateway/list", ct) ?? new List<Gateway>();

    // ---------- User / Profile ----------

    public async Task<UserStatus> GetOrAddUserAsync(string mobile, CancellationToken ct = default)
    {
        var body = new GetOrAddUserRequest { Code = _encryptor.Encrypt(mobile) };
        using var response = await SendAuthorizedAsync(() => JsonRequest(HttpMethod.Post, "user/getOrAdd", body), ct);

        // در مستند، پاسخ این سرویس بدون پوشش isSuccessful/result آمده است؛ هر دو حالت پشتیبانی می‌شود.
        var raw = await response.Content.ReadAsStringAsync(ct);
        EnsureHttpSuccess(response, raw);

        using var doc = JsonDocument.Parse(raw);
        if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("isSuccessful", out _))
            return Unwrap(JsonSerializer.Deserialize<ApiResponse<UserStatus>>(raw, JsonOptions), response, raw)!;

        return JsonSerializer.Deserialize<UserStatus>(raw, JsonOptions)
               ?? throw new DayaDiamondException("پاسخ getOrAdd خالی بود.", response.StatusCode, rawBody: raw);
    }

    public Task UpsertProfileInfoAsync(UpsertProfileInfoRequest request, CancellationToken ct = default)
        => PostAsync<JsonElement?>("profile/upsertInfo", request, ct);

    public Task UpsertAddressAsync(UpsertAddressRequest request, CancellationToken ct = default)
        => PostAsync<JsonElement?>("profile/upsertAddress", request, ct);

    // ---------- Buy ----------

    public Task<BuyPolicy> GetBuyPolicyAsync(CancellationToken ct = default)
        => GetAsync<BuyPolicy>("buy/policy", ct)!;

    public Task<BuyRequestResult> CreateBuyRequestAsync(BuyRequest request, CancellationToken ct = default)
        => PostAsync<BuyRequestResult>("buy/request", request, ct)!;

    // ---------- Withdraw ----------

    public Task ValidateWithdrawRequestAsync(decimal tokenAmount, string phoneNumber, CancellationToken ct = default)
        => GetAsync<JsonElement?>(
            $"withdraw/validateRequest?tokenAmount={Num(tokenAmount)}&phoneNumber={Uri.EscapeDataString(phoneNumber)}", ct);

    // ---------- Plumbing ----------

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await SendAuthorizedAsync(() => new HttpRequestMessage(HttpMethod.Get, path), ct);
        return await ReadEnvelopeAsync<T>(response, ct);
    }

    private async Task<T?> PostAsync<T>(string path, object body, CancellationToken ct)
    {
        using var response = await SendAuthorizedAsync(() => JsonRequest(HttpMethod.Post, path, body), ct);
        return await ReadEnvelopeAsync<T>(response, ct);
    }

    private static HttpRequestMessage JsonRequest(HttpMethod method, string path, object body) => new(method, path)
    {
        Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
    };

    /// <summary>درخواست را با JWT می‌فرستد؛ در صورت 401 یک بار توکن را تازه کرده و دوباره تلاش می‌کند.</summary>
    private async Task<HttpResponseMessage> SendAuthorizedAsync(Func<HttpRequestMessage> createRequest, CancellationToken ct)
    {
        var token = await GetTokenAsync(forceRefresh: false, ct);
        var response = await SendWithTokenAsync(createRequest(), token, ct);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        response.Dispose();
        token = await GetTokenAsync(forceRefresh: true, ct);
        return await SendWithTokenAsync(createRequest(), token, ct);
    }

    private async Task<HttpResponseMessage> SendWithTokenAsync(HttpRequestMessage request, string token, CancellationToken ct)
    {
        using (request)
        {
            if (string.IsNullOrEmpty(_options.AuthorizationScheme))
                request.Headers.TryAddWithoutValidation("Authorization", token);
            else
                request.Headers.Authorization = new AuthenticationHeaderValue(_options.AuthorizationScheme, token);

            return await _http.SendAsync(request, ct);
        }
    }

    private async Task<string> GetTokenAsync(bool forceRefresh, CancellationToken ct)
    {
        var skew = _options.TokenRefreshSkewSeconds;
        var stale = _tokens.Token;
        if (!forceRefresh && _tokens.IsValid(skew))
            return stale!;

        await _tokens.Lock.WaitAsync(ct);
        try
        {
            // اگر درخواست دیگری در این فاصله توکن را تازه کرده، از همان استفاده کن.
            if (_tokens.IsValid(skew) && (!forceRefresh || _tokens.Token != stale))
                return _tokens.Token!;
            return await LoginAsync(ct);
        }
        finally
        {
            _tokens.Lock.Release();
        }
    }

    private static async Task<T?> ReadEnvelopeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var raw = await response.Content.ReadAsStringAsync(ct);
        EnsureHttpSuccess(response, raw);

        ApiResponse<T>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<ApiResponse<T>>(raw, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new DayaDiamondException($"پاسخ نامعتبر از سرور: {ex.Message}", response.StatusCode, rawBody: raw);
        }

        return Unwrap(envelope, response, raw);
    }

    private static T? Unwrap<T>(ApiResponse<T>? envelope, HttpResponseMessage response, string raw)
    {
        if (envelope is null)
            throw new DayaDiamondException("پاسخ خالی از سرور.", response.StatusCode, rawBody: raw);

        if (!envelope.IsSuccessful)
            throw new DayaDiamondException(DescribeError(envelope.Error), response.StatusCode, envelope.Error, raw);

        return envelope.Result;
    }

    private static void EnsureHttpSuccess(HttpResponseMessage response, string raw)
    {
        if (response.IsSuccessStatusCode)
            return;

        JsonElement? error = null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("error", out var e))
                error = e.Clone();
        }
        catch (JsonException)
        {
            // بدنه JSON نیست.
        }

        throw new DayaDiamondException(
            $"خطای HTTP {(int)response.StatusCode}: {DescribeError(error)}", response.StatusCode, error, raw);
    }

    private static string DescribeError(JsonElement? error)
    {
        if (error is not { } e || e.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return "خطای نامشخص از سرویس دایادایموند.";

        if (e.ValueKind == JsonValueKind.String)
            return e.GetString()!;

        if (e.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in new[] { "message", "Message", "description", "title" })
                if (e.TryGetProperty(name, out var m) && m.ValueKind == JsonValueKind.String)
                    return m.GetString()!;
        }

        return e.GetRawText();
    }

    private static string Num(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>فیلد exp را از payload توکن JWT می‌خواند (بدون اعتبارسنجی امضا).</summary>
    private static DateTimeOffset? ReadJwtExpiry(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            return null;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            return doc.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds)
                ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return null;
        }
    }
}
