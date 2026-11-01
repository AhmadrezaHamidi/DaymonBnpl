using System.Net;
using System.Net.Http.Json;
using DayaDiamond.Client;
using DayaDiamond.Client.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BnplService.Tests.DayaDiamond;

/// <summary>کلاینت و adapterهای دایادایموند در برابر mock: سرویس‌های مستند و سرویس‌های پیشنهادی پیش‌خرید.</summary>
public class DayaDiamondClientMockTests
{
    private const string Mobile = "09121111111";

    [Fact]
    public async Task Login_and_price_go_through_the_jwt_and_toman_is_converted_to_rial()
    {
        await using var mock = new DayaDiamondMock();
        await using var sp = mock.ClientServices();
        var client = sp.GetRequiredService<IDayaDiamondClient>();

        var price = await client.GetTokenPriceAsync();
        Assert.Equal("DAYADIAMOND", price.AssetCode);
        Assert.Equal(1_501_000, price.TokenPrice);

        using var scope = sp.CreateScope();
        var rial = await scope.ServiceProvider.GetRequiredService<ITokenPriceProvider>().GetAsync(default);
        Assert.Equal(15_010_000, rial.PriceRial);

        var gateway = Assert.Single(await client.GetGatewaysAsync());
        Assert.Equal((5, "OmidPay"), (gateway.Id, gateway.Title));
    }

    [Fact]
    public async Task Wrong_password_is_reported_and_expired_session_is_refreshed()
    {
        await using var mock = new DayaDiamondMock();
        await using (var bad = mock.ClientServices(password: "wrong"))
        {
            var ex = await Assert.ThrowsAsync<DayaDiamondException>(() => bad.GetRequiredService<IDayaDiamondClient>().GetTokenPriceAsync());
            Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        }

        await using var sp = mock.ClientServices();
        var client = sp.GetRequiredService<IDayaDiamondClient>();
        await client.GetTokenPriceAsync();
        lock (mock.State.Sync) mock.State.IssuedTokens.Clear();   // سرور توکن را باطل کرد ← 401 ← لاگین دوباره
        Assert.Equal(1_501_000, (await client.GetTokenPriceAsync()).TokenPrice);
        Assert.Single(mock.State.IssuedTokens);
    }

    [Fact]
    public async Task Encrypted_mobile_registers_the_user_and_profile_forms_complete_it()
    {
        await using var mock = new DayaDiamondMock();
        await using var sp = mock.ClientServices();
        using var scope = sp.CreateScope();
        var profiles = scope.ServiceProvider.GetRequiredService<ICustomerProfileProvider>();

        var status = await profiles.GetOrAddAsync(Mobile, default);
        Assert.Equal(Mobile, status.PhoneNumber);     // mock موبایل را با همان کلید رمزگشایی کرد
        Assert.False(status.IsComplete);

        await profiles.UpsertInfoAsync(new CustomerInfoInput(Mobile, "0011111111", "علی", "محمدی", "رضا", "1360/11/12", "Male",
            "360560611828006835245102"), default);
        await profiles.UpsertAddressAsync(new CustomerAddressInput(Mobile, "تهران", "تهران", "خیابان ولیعصر", "1234567890", "02188888888"), default);

        Assert.True((await profiles.GetOrAddAsync(Mobile, default)).IsComplete);
        Assert.Equal("علی", mock.Customer(Mobile).Info["firstName"]);
    }

    [Fact]
    public async Task Profile_of_an_unregistered_user_is_rejected_with_the_server_message()
    {
        await using var mock = new DayaDiamondMock();
        await using var sp = mock.ClientServices();
        using var scope = sp.CreateScope();
        var ex = await Assert.ThrowsAsync<BnplProviderException>(() => scope.ServiceProvider.GetRequiredService<ICustomerProfileProvider>()
            .UpsertAddressAsync(new CustomerAddressInput("09129999999", "تهران", "تهران", "خیابان ولیعصر", "1234567890", "02188888888"), default));
        Assert.Contains("کاربر یافت نشد", ex.Message);
    }

    [Fact]
    public async Task Documented_buy_flow_adds_tokens_after_the_gateway_payment()
    {
        await using var mock = new DayaDiamondMock();
        await using var sp = mock.ClientServices();
        var client = sp.GetRequiredService<IDayaDiamondClient>();

        var policy = await client.GetBuyPolicyAsync();
        Assert.Equal((0.5m, 120m), (policy.MinAmount, policy.MaxAmount));
        Assert.Equal(4_503_000, (await client.GetBuyPriceAsync(3)).Price);

        var buy = await client.CreateBuyRequestAsync(new BuyRequest { PhoneNumber = Mobile, Amount = 3, GatewayId = 5, ReturnUrl = "https://app.test/buy" });
        Assert.Contains("/gateway/redirectToIpg/?paymentExtId=" + buy.PaymentExtId, buy.PayUrl);

        var browser = mock.Browser;
        var page = await browser.GetStringAsync(new Uri(buy.PayUrl).PathAndQuery);
        Assert.Contains("درگاه پرداخت آزمایشی", page);
        var back = await browser.GetAsync($"mock/pay/{buy.PaymentExtId}?success=true");
        Assert.Equal(HttpStatusCode.Redirect, back.StatusCode);
        Assert.StartsWith("https://app.test/buy?paymentExtId=", back.Headers.Location!.ToString());

        Assert.Equal(103, mock.Customer(Mobile).FreeAmount);
        await client.ValidateWithdrawRequestAsync(103, Mobile);
        var ex = await Assert.ThrowsAsync<DayaDiamondException>(() => client.ValidateWithdrawRequestAsync(104, Mobile));
        Assert.Contains("موجودی آزاد", ex.Message);
    }

    [Fact]
    public async Task Custody_is_idempotent_by_reference()
    {
        await using var mock = new DayaDiamondMock();
        await using var sp = mock.ClientServices();
        using var scope = sp.CreateScope();
        var custody = scope.ServiceProvider.GetRequiredService<ITokenCustody>();

        await custody.LockAsync(Mobile, "C1", 10, default);
        await custody.LockAsync(Mobile, "C1", 10, default);          // تکرار
        Assert.Equal(90, await custody.GetFreeBalanceAsync(Mobile, default));
        Assert.Equal(10, mock.Customer(Mobile).LockedAmount);

        await custody.CreditAsync(Mobile, "C1", 5, default);
        await custody.CreditAsync(Mobile, "C1", 5, default);
        Assert.Equal(95, await custody.GetFreeBalanceAsync(Mobile, default));

        var t1 = await custody.DebitAsync(Mobile, "C1/7", 2, "قسط دوم", default);
        var t2 = await custody.DebitAsync(Mobile, "C1/7", 2, "قسط دوم", default);
        Assert.Equal(t1, t2);
        Assert.Equal(93, await custody.GetFreeBalanceAsync(Mobile, default));

        await custody.ReleaseAsync(Mobile, "C1", 10, default);
        await custody.ReleaseAsync(Mobile, "C1", 10, default);
        Assert.Equal((103m, 0m), (mock.Customer(Mobile).FreeAmount, mock.Customer(Mobile).LockedAmount));

        var tooMuch = await Assert.ThrowsAsync<BnplProviderException>(() => custody.LockAsync(Mobile, "C2", 1000, default));
        Assert.Contains("موجودی آزاد کافی نیست", tooMuch.Message);
        await Assert.ThrowsAsync<BnplProviderException>(() => custody.ReleaseAsync(Mobile, "C-unknown", 1, default));
    }

    [Fact]
    public async Task Outage_becomes_a_persian_provider_error()
    {
        await using var mock = new DayaDiamondMock();
        await using var sp = mock.ClientServices();
        (await mock.Browser.PostAsJsonAsync("mock/outage", new { enabled = true })).EnsureSuccessStatusCode();

        using var scope = sp.CreateScope();
        var ex = await Assert.ThrowsAsync<BnplProviderException>(() => scope.ServiceProvider.GetRequiredService<ITokenPriceProvider>().GetAsync(default));
        Assert.StartsWith("دریافت قیمت توکن:", ex.Message);
        Assert.Contains("در دسترس نیست", ex.Message);
    }
}
