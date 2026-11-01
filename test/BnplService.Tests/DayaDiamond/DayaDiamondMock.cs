using DayaDiamond.Client;
using DayaDiamond.Mock;
using BnplService.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BnplService.Tests.DayaDiamond;

/// <summary>
/// سرور mock دایادایموند درون همین پروسه (WebApplicationFactory). کلاینت واقعی و adapterهای واقعی Infrastructure
/// با HttpClientی به آن وصل می‌شوند که به‌جای شبکه مستقیم به TestServer می‌رود. هر تست mock تازهٔ خودش را دارد.
/// </summary>
public sealed class DayaDiamondMock : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();

    public MockState State => _factory.Services.GetRequiredService<MockState>();
    public MockOptions Options => _factory.Services.GetRequiredService<MockOptions>();

    /// <summary>کلاینت بی‌احراز برای مسیرهای کنترل تست (mock/*) و صفحهٔ درگاه؛ ریدایرکت را دنبال نمی‌کند.</summary>
    public HttpClient Browser => _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public IConfiguration Configuration(string? password = null) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["DayaDiamond:BaseUrl"] = "http://localhost/",
        ["DayaDiamond:Username"] = Options.Username,
        ["DayaDiamond:Password"] = password ?? Options.Password,
        ["DayaDiamond:MobileKeyBase64"] = Options.MobileKeyBase64,
        ["DayaDiamond:MobileIvBase64"] = Options.MobileIvBase64,
    }).Build();

    /// <summary>ثبت adapterهای دایادایموند (همان AddBnplDayaDiamondProviders برنامه) روی این mock.</summary>
    public void AddProviders(IServiceCollection services, string? password = null)
    {
        services.AddBnplDayaDiamondProviders(Configuration(password));
        services.AddHttpClient<IDayaDiamondClient, DayaDiamondClient>()
            .ConfigurePrimaryHttpMessageHandler(() => _factory.Server.CreateHandler());
    }

    /// <summary>کلاینت مستقل برای تست مستقیم کلاینت.</summary>
    public ServiceProvider ClientServices(string? password = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        AddProviders(services, password);
        return services.BuildServiceProvider();
    }

    public MockCustomer Customer(string phone)
    {
        lock (State.Sync) return State.Customer(phone);
    }

    public MockPayment Payment(string paymentExtId)
    {
        lock (State.Sync) return State.Payments[paymentExtId];
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
