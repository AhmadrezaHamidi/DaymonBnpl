using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BnplService.Api.Authorization;
using BnplService.Infrastructure.DBContext;
using BnplService.Infrastructure.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BnplService.Tests.Api;

/// <summary>
/// از HTTP تا دیتابیس: Program واقعی، کنترلرها، فیلترهای احراز، MediatR، ارائه‌دهنده‌های Sandbox و EF روی SQLite درون‌حافظه.
/// Identity با کلاینت‌های جعلی جایگزین شده: توکن پنل «admin» همهٔ مجوزها را دارد و «viewer» فقط داشبورد؛ توکن مشتری «good».
/// </summary>
public sealed class ApiTestHost : IAsyncDisposable
{
    public const string PublicBaseUrl = "http://localhost";
    public const string ReturnUrl = "https://app.test/bnpl/result";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public WebApplicationFactory<Program> Factory { get; }
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private ApiTestHost(Dictionary<string, string?> settings)
    {
        _connection.Open();
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.UseSetting("ConnectionStrings:Bnpl", "Data Source=unused");
            b.UseSetting("Bnpl:DatabaseProvider", "Sqlite");
            b.UseSetting("Bnpl:PublicBaseUrl", PublicBaseUrl);
            b.UseSetting("Bnpl:AllowedReturnUrlPrefixes:0", "https://app.test/");
            b.UseSetting("Bnpl:Maintenance:Enabled", "false");
            foreach (var port in new[] { "Price", "Profile", "Gateway", "Custody" }) b.UseSetting($"Bnpl:Providers:{port}", "Sandbox");
            foreach (var (key, value) in settings) b.UseSetting(key, value);
            b.ConfigureTestServices(s =>
            {
                s.RemoveAll<DbContextOptions<BnplDbContext>>();
                s.AddDbContext<BnplDbContext>(o => o.UseSqlite(_connection));
                s.RemoveAll<IIdentityAuthorizationClient>();
                s.AddSingleton<IIdentityAuthorizationClient, FakePanelIdentity>();
                s.RemoveAll<IIdentityCustomerClient>();
                s.AddSingleton<IIdentityCustomerClient, FakeCustomerIdentity>();
            });
        });
    }

    public static async Task<ApiTestHost> CreateAsync(Dictionary<string, string?>? settings = null)
    {
        var host = new ApiTestHost(settings ?? []);
        await using var scope = host.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BnplDbContext>();
        await db.Database.EnsureCreatedAsync();
        await BnplSeed.EnsureSeedAsync(db);
        return host;
    }

    /// <summary>کلاینت بدون دنبال کردن Redirect تا مسیر درگاه ← کال‌بک ← اپ قدم‌به‌قدم بررسی شود.</summary>
    public HttpClient Client(string? customerToken = null, string? panelToken = null)
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri(PublicBaseUrl) });
        if (customerToken is not null)
        {
            client.DefaultRequestHeaders.Add("Token", customerToken);
            client.DefaultRequestHeaders.Add("MobileNumber", FakeCustomerIdentity.Mobile);
        }
        if (panelToken is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", panelToken);
        return client;
    }

    public static async Task<JsonElement> Data(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");
        return body.GetProperty("data");
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

public sealed class FakePanelIdentity : IIdentityAuthorizationClient
{
    public Task<BnplPanelUser?> ResolveAsync(string token, CancellationToken cancellationToken)
    {
        var keys = token switch
        {
            "admin" => BnplPermissionKeys.All,
            "viewer" => [BnplPermissionKeys.DashboardView],
            _ => null,
        };
        return Task.FromResult(keys is null ? null : new BnplPanelUser(token == "admin" ? 1 : 2, token, token, 1,
            new PanelPermissions(keys.Select(k => new PanelGrant(k, PanelPermissionEffect.Allow, PanelScopeType.Global, null)).ToList())));
    }
}

public sealed class FakeCustomerIdentity : IIdentityCustomerClient
{
    public const string Mobile = "09121111111";

    public Task<BnplCustomerRef?> AuthenticateAsync(string mobileNumber, string activeToken, CancellationToken cancellationToken) =>
        Task.FromResult(activeToken == "good" && mobileNumber == Mobile ? new BnplCustomerRef("1001", Mobile, "علی محمدی") : null);
}
