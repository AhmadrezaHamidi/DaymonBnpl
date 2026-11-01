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

    /// <summary>
    /// اگر این متغیر محیطی (رشتهٔ اتصال SQL Server بدون Database) مقدار داشته باشد، هر تست روی یک دیتابیس تازهٔ SQL Server
    /// با Migrationهای واقعی اجرا می‌شود و بعد پاک می‌شود؛ وگرنه SQLite درون‌حافظه.
    /// </summary>
    public const string SqlServerVariable = "BNPL_TEST_SQLSERVER";

    private static readonly string? SqlServer = Environment.GetEnvironmentVariable(SqlServerVariable);

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ServiceProvider _services;

    public FakeTimeProvider Time { get; } = new(Start);
    public FakeTokenPrice Price { get; } = new();
    public FakeProfiles Profiles { get; } = new();
    public FakeGateway Gateway { get; } = new();
    public FakeCustody Custody { get; } = new();
    public TestCurrentCustomer Customer { get; } = new();
    public TestCurrentUser Admin { get; } = new();

    private BnplTestHost(Action<IServiceCollection>? configure)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<TimeProvider>(Time);
        services.AddBnplApplication();
        if (string.IsNullOrEmpty(SqlServer))
        {
            _connection.Open();
            services.AddDbContext<BnplDbContext>(o => o.UseSqlite(_connection));
        }
        else
        {
            var connection = $"{SqlServer.TrimEnd(';')};Database=BnplTest_{Guid.NewGuid():N}";
            services.AddDbContext<BnplDbContext>(o => o.UseSqlServer(connection, BnplModel.SqlServer));
        }
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
        configure?.Invoke(services);   // مثلاً جایگزینی ارائه‌دهنده‌های جعلی با adapterهای واقعی دایادایموند روی mock
        _services = services.BuildServiceProvider();
    }

    /// <param name="configure">ثبت‌های اضافه پس از پیش‌فرض‌ها؛ ثبت آخر برنده است.</param>
    public static async Task<BnplTestHost> CreateAsync(Action<IServiceCollection>? configure = null)
    {
        var host = new BnplTestHost(configure);
        await using var scope = host._services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BnplDbContext>();
        if (db.Database.IsSqlServer()) await db.Database.MigrateAsync();
        else await db.Database.EnsureCreatedAsync();
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

    public async Task<TResult> Service<TService, TResult>(Func<TService, Task<TResult>> use) where TService : notnull
    {
        await using var scope = _services.CreateAsyncScope();
        return await use(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async Task<TResult> Db<TResult>(Func<BnplDbContext, Task<TResult>> query)
    {
        await using var scope = _services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<BnplDbContext>());
    }

    public async ValueTask DisposeAsync()
    {
        if (!string.IsNullOrEmpty(SqlServer))
        {
            await using var scope = _services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<BnplDbContext>().Database.EnsureDeletedAsync();
        }
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
    public bool FailCredit { get; set; }
    public List<(string Contract, decimal Tokens)> Locked { get; } = [];
    public List<(string Contract, decimal Tokens)> Credited { get; } = [];
    public List<(string Contract, decimal Tokens)> Released { get; } = [];
    public List<(string Contract, decimal Tokens)> Debited { get; } = [];

    public Task<decimal> GetFreeBalanceAsync(string mobileNumber, CancellationToken ct) => Task.FromResult(FreeBalance);

    public Task LockAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct)
    {
        if (FailLock) throw new BnplProviderException("دایادایموند در دسترس نیست.");
        if (FreeBalance < tokenAmount) throw new BnplProviderException("موجودی آزاد کافی نیست.");
        FreeBalance -= tokenAmount;
        Locked.Add((contractNumber, tokenAmount));
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct)
    {
        FreeBalance += tokenAmount;
        Released.Add((contractNumber, tokenAmount));
        return Task.CompletedTask;
    }

    public Task CreditAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct)
    {
        if (FailCredit) throw new BnplProviderException("دایادایموند در دسترس نیست.");
        FreeBalance += tokenAmount;
        Credited.Add((contractNumber, tokenAmount));
        return Task.CompletedTask;
    }

    public Task<string> DebitAsync(string mobileNumber, string reference, decimal tokenAmount, string description, CancellationToken ct)
    {
        FreeBalance -= tokenAmount;
        Debited.Add((reference, tokenAmount));
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
