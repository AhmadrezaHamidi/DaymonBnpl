using BnplService.Application.Handler.Shared;
using BnplService.Infrastructure.DBContext;
using BnplService.Infrastructure.Seed;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace BnplService.Tests.Handlers;

/// <summary>
/// میزبان تست Handlerها: SQLite درون‌حافظه + ارائه‌دهنده‌های جعلی (قیمت، پروفایل، درگاه، نگهداری توکن) + ساعت قابل جلو بردن.
/// هر Send در scope تازه اجرا می‌شود، مثل یک درخواست HTTP.
/// </summary>
public sealed class BnplTestHost : IAsyncDisposable
{
    public static readonly DateTimeOffset Start = new(2026, 10, 28, 8, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ServiceProvider _services;

    public FakeTimeProvider Time { get; } = new(Start);
    public FakeTokenPrice Price { get; } = new();
    public FakeProfiles Profiles { get; } = new();
    public FakeGateway Gateway { get; } = new();
    public FakeCustody Custody { get; } = new();
    public TestCurrentCustomer Customer { get; } = new();
    public TestCurrentUser Admin { get; } = new();

    private BnplTestHost()
    {
        _connection.Open();
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<TimeProvider>(Time);
        services.AddBnplApplication();
        services.AddDbContext<BnplDbContext>(o => o.UseSqlite(_connection));
        services.AddScoped<IBnplDbContext>(sp => sp.GetRequiredService<BnplDbContext>());
        services.AddSingleton<ITokenPriceProvider>(Price);
        services.AddSingleton<ICustomerProfileProvider>(Profiles);
        services.AddSingleton<IBnplPaymentGateway>(Gateway);
        services.AddSingleton<ITokenCustody>(Custody);
        services.AddSingleton<IBnplCurrentCustomer>(Customer);
        services.AddSingleton<IBnplCurrentUser>(Admin);
        services.Configure<BnplOptions>(o =>
        {
            o.PublicBaseUrl = "https://bnpl.test";
            o.AllowedReturnUrlPrefixes = ["https://app.test/"];
        });
        _services = services.BuildServiceProvider();
    }

    public static async Task<BnplTestHost> CreateAsync()
    {
        var host = new BnplTestHost();
        await using var scope = host._services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BnplDbContext>();
        await db.Database.EnsureCreatedAsync();
        await BnplSeed.EnsureSeedAsync(db);
        return host;
    }

    public async Task<T> Send<T>(IRequest<T> request)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }

    /// <summary>پاسخ موفق؛ وگرنه تست با پیام فارسی سرویس شکست می‌خورد.</summary>
    public async Task<T> Ok<T>(IRequest<BnplMessageResponse<T>> request)
    {
        var response = await Send(request);
        Assert.True(response.IsSuccess, $"{response.StatusCode}: {response.Message}");
        return response.Data!;
    }

    public async Task<TResult> Db<TResult>(Func<BnplDbContext, Task<TResult>> query)
    {
        await using var scope = _services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<BnplDbContext>());
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

public sealed class FakeTokenPrice : ITokenPriceProvider
{
    public long PriceRial { get; set; } = 15_010_000;
    public bool Fail { get; set; }

    public Task<TokenPriceDto> GetAsync(CancellationToken ct) =>
        Fail ? throw new BnplProviderException("سرویس قیمت در دسترس نیست.") : Task.FromResult(new TokenPriceDto("DAYADIAMOND", PriceRial));
}

public sealed class FakeProfiles : ICustomerProfileProvider
{
    public bool ProfileCompleted { get; set; } = true;
    public bool AddressCompleted { get; set; } = true;
    public CustomerInfoInput? LastInfo { get; private set; }
    public CustomerAddressInput? LastAddress { get; private set; }

    public Task<CustomerProfileStatusDto> GetOrAddAsync(string mobileNumber, CancellationToken ct) =>
        Task.FromResult(new CustomerProfileStatusDto(mobileNumber, ProfileCompleted, AddressCompleted));

    public Task UpsertInfoAsync(CustomerInfoInput input, CancellationToken ct)
    {
        LastInfo = input;
        ProfileCompleted = true;
        return Task.CompletedTask;
    }

    public Task UpsertAddressAsync(CustomerAddressInput input, CancellationToken ct)
    {
        LastAddress = input;
        AddressCompleted = true;
        return Task.CompletedTask;
    }
}

/// <summary>درگاه جعلی: هر پرداخت پیش‌فرض «پرداخت‌نشده» است تا تست Pay(ref) را صدا بزند.</summary>
public sealed class FakeGateway : IBnplPaymentGateway
{
    private readonly Dictionary<string, (long Amount, bool Paid)> _payments = new();
    public GatewayPaymentStartInput? LastStart { get; private set; }

    public Task<IReadOnlyList<PaymentGatewayDto>> ListAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PaymentGatewayDto>>([new PaymentGatewayDto(5, "OmidPay", "امید پی")]);

    public Task<GatewayPaymentStartDto> StartAsync(GatewayPaymentStartInput input, CancellationToken ct)
    {
        LastStart = input;
        var reference = Guid.NewGuid().ToString("N");
        _payments[reference] = (input.Amount, false);
        return Task.FromResult(new GatewayPaymentStartDto(reference, $"https://ipg.test/{reference}"));
    }

    public void Pay(string reference) => _payments[reference] = (_payments[reference].Amount, true);

    public Task<GatewayPaymentResultDto> VerifyAsync(string providerReference, long expectedAmount, CancellationToken ct)
    {
        var p = _payments[providerReference];
        return Task.FromResult(p.Paid && p.Amount == expectedAmount
            ? new GatewayPaymentResultDto(true, "TRK" + providerReference[..8], null)
            : new GatewayPaymentResultDto(false, null, "پرداخت انجام نشد."));
    }
}

public sealed class FakeCustody : ITokenCustody
{
    public decimal FreeBalance { get; set; } = 100;
    public bool FailLock { get; set; }
    public List<(string Contract, decimal Tokens)> Locked { get; } = [];
    public List<(string Contract, decimal Tokens)> Released { get; } = [];
    public List<(string Contract, decimal Tokens)> Debited { get; } = [];

    public Task<decimal> GetFreeBalanceAsync(string mobileNumber, CancellationToken ct) => Task.FromResult(FreeBalance);

    public Task LockAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct)
    {
        if (FailLock) throw new BnplProviderException("دایادایموند در دسترس نیست.");
        Locked.Add((contractNumber, tokenAmount));
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct)
    {
        Released.Add((contractNumber, tokenAmount));
        return Task.CompletedTask;
    }

    public Task<string> DebitAsync(string mobileNumber, string contractNumber, decimal tokenAmount, string description, CancellationToken ct)
    {
        FreeBalance -= tokenAmount;
        Debited.Add((contractNumber, tokenAmount));
        return Task.FromResult("TOK" + Debited.Count);
    }
}

public sealed class TestCurrentCustomer : IBnplCurrentCustomer
{
    public BnplCustomerRef Value { get; set; } = new("1001", "09121111111", "علی محمدی");
}

public sealed class TestCurrentUser : IBnplCurrentUser
{
    public BnplPanelUser Value { get; set; } = new(7, "admin", "ادمین تست", 1,
        new PanelPermissions(BnplPermissionKeys.All.Select(k => new PanelGrant(k, PanelPermissionEffect.Allow, PanelScopeType.Global, null)).ToList()));
}
