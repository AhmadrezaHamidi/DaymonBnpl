using BnplService.Infrastructure;
using BnplService.Infrastructure.Services.DayaDiamond;
using BnplService.Infrastructure.Services.Sandbox;
using DayaDiamond.Client;
using DayaDiamond.Client.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace BnplService.Tests.Infrastructure;

/// <summary>پیاده‌سازی پورت‌ها روی دایادایموند، درگاه و نگهداری Sandbox، و انتخاب آن‌ها از تنظیمات.</summary>
public class ProviderTests
{
    [Fact]
    public async Task Token_price_is_converted_from_toman_to_rial()
    {
        var price = await new DayaDiamondTokenPriceProvider(new FakeDaya { Price = 1_605_046 }).GetAsync(default);
        Assert.Equal(16_050_460, price.PriceRial);
        Assert.Equal("DAYADIAMOND", price.AssetCode);
    }

    [Fact]
    public async Task DayaDiamond_errors_and_outages_become_provider_errors()
    {
        var failing = new DayaDiamondTokenPriceProvider(new FakeDaya { Error = new DayaDiamondException("کاربر یافت نشد.") });
        Assert.Equal("کاربر یافت نشد.", (await Assert.ThrowsAsync<BnplProviderException>(() => failing.GetAsync(default))).Message);

        var down = new DayaDiamondProfileProvider(new FakeDaya { Error = new HttpRequestException("refused") });
        Assert.Equal(DayaDiamondCall.Unavailable,
            (await Assert.ThrowsAsync<BnplProviderException>(() => down.GetOrAddAsync("09121111111", default))).Message);
    }

    [Fact]
    public async Task Profile_status_and_gateways_come_from_DayaDiamond()
    {
        var daya = new FakeDaya();
        var status = await new DayaDiamondProfileProvider(daya).GetOrAddAsync("09121111111", default);
        Assert.True(status.ProfileCompleted);
        Assert.False(status.AddressCompleted);

        var gateway = new DayaDiamondPaymentGateway(daya);
        var list = await gateway.ListAsync(default);
        Assert.Equal("OmidPay", Assert.Single(list).Title);
        await Assert.ThrowsAsync<BnplProviderException>(() =>
            gateway.StartAsync(new GatewayPaymentStartInput(1, 5, 1000, "09121111111", "قسط", "https://cb"), default));
    }

    [Fact]
    public async Task Sandbox_gateway_verifies_only_the_paid_amount()
    {
        var store = new SandboxStore(new SandboxOptions());
        var gateway = new SandboxPaymentGateway(store, "https://bnpl.test/v2/bnpl/Sandbox/Pay");
        var started = await gateway.StartAsync(new GatewayPaymentStartInput(1, 5, 1000, "09121111111", "قسط", "https://cb"), default);
        Assert.Equal($"https://bnpl.test/v2/bnpl/Sandbox/Pay/{started.ProviderReference}", started.PayUrl);

        Assert.False((await gateway.VerifyAsync(started.ProviderReference, 1000, default)).IsPaid);
        store.FindPayment(started.ProviderReference)!.State = SandboxStore.PaymentState.Paid;
        Assert.False((await gateway.VerifyAsync(started.ProviderReference, 999, default)).IsPaid);
        Assert.True((await gateway.VerifyAsync(started.ProviderReference, 1000, default)).IsPaid);
    }

    [Fact]
    public async Task Sandbox_custody_locks_releases_and_debits()
    {
        var custody = new SandboxTokenCustody(new SandboxStore(new SandboxOptions { FreeTokenBalance = 10 }));
        const string mobile = "09121111111";
        await custody.LockAsync(mobile, "P5_U1234567", 3.331m, default);
        await custody.DebitAsync(mobile, "P5_U1234567", 2, "قسط دوم", default);
        Assert.Equal(8, await custody.GetFreeBalanceAsync(mobile, default));
        await custody.ReleaseAsync(mobile, "P5_U1234567", 3.331m, default);
        Assert.Equal(11.331m, await custody.GetFreeBalanceAsync(mobile, default));
        await Assert.ThrowsAsync<BnplProviderException>(() => custody.DebitAsync(mobile, "P5_U1234567", 100, "قسط", default));
    }

    [Fact]
    public void Providers_are_chosen_from_configuration_and_sandbox_is_refused_in_production()
    {
        var sandbox = Services("Development", "Sandbox");
        Assert.IsType<SandboxPaymentGateway>(sandbox.GetRequiredService<IBnplPaymentGateway>());
        Assert.IsType<SandboxTokenPriceProvider>(sandbox.GetRequiredService<ITokenPriceProvider>());

        var real = Services("Production", null);
        Assert.IsType<DayaDiamondPaymentGateway>(real.GetRequiredService<IBnplPaymentGateway>());
        Assert.IsType<DayaDiamondTokenCustody>(real.GetRequiredService<ITokenCustody>());

        var ex = Assert.Throws<InvalidOperationException>(() => Services("Production", "Sandbox"));
        Assert.Contains("Production", ex.Message);
    }

    private static ServiceProvider Services(string environment, string? provider)
    {
        var settings = new Dictionary<string, string?>
        {
            ["DayaDiamond:BaseUrl"] = "https://daya.invalid", ["DayaDiamond:MobileKeyBase64"] = Convert.ToBase64String(new byte[16]),
        };
        foreach (var port in new[] { "Price", "Profile", "Gateway", "Custody" }) settings[$"Bnpl:Providers:{port}"] = provider;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection().AddLogging().AddBnplProviders(configuration, new Env(environment)).BuildServiceProvider();
    }

    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "BnplService.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeDaya : IDayaDiamondClient
    {
        public decimal Price { get; init; } = 1_605_046;
        public Exception? Error { get; init; }

        private Task<T> Result<T>(T value) => Error is null ? Task.FromResult(value) : Task.FromException<T>(Error);

        public Task<string> LoginAsync(CancellationToken ct = default) => Result("jwt");
        public Task<TokenPriceResult> GetTokenPriceAsync(CancellationToken ct = default) =>
            Result(new TokenPriceResult { AssetCode = "DAYADIAMOND", TokenPrice = Price });
        public Task<IReadOnlyList<Gateway>> GetGatewaysAsync(CancellationToken ct = default) =>
            Result<IReadOnlyList<Gateway>>([new Gateway { Id = 5, Title = "OmidPay", Description = "امید پی" }]);
        public Task<UserStatus> GetOrAddUserAsync(string mobile, CancellationToken ct = default) =>
            Result(new UserStatus { PhoneNumber = mobile, ProfileCompleted = true, AddressCompleted = false });
        public Task UpsertProfileInfoAsync(UpsertProfileInfoRequest request, CancellationToken ct = default) => Result(true);
        public Task UpsertAddressAsync(UpsertAddressRequest request, CancellationToken ct = default) => Result(true);
        public Task<BuyPolicy> GetBuyPolicyAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<BuyPriceResult> GetBuyPriceAsync(decimal amount, string assetCode = "DAYADIAMOND", CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<BuyRequestResult> CreateBuyRequestAsync(BuyRequest request, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ValidateWithdrawRequestAsync(decimal tokenAmount, string phoneNumber, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
