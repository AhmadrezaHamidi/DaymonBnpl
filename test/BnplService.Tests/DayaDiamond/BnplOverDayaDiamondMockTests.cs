using System.Net;
using BnplService.Application.Handler.Customer.Payments.Commands.CompleteGatewayPayment;
using BnplService.Application.Handler.Customer.Payments.Commands.StartPayment;
using BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileAddress;
using BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileInfo;
using BnplService.Application.Handler.Customer.Profile.Queries.GetProfileStatus;
using BnplService.Application.Handler.Customer.Requests.Commands.AcceptTerms;
using BnplService.Application.Handler.Customer.Requests.Commands.CreateRequest;
using BnplService.Application.Handler.Customer.Requests.Commands.SignContract;
using BnplService.Application.Handler.Customer.Requests.Queries.GetCollateralStatus;
using BnplService.Application.Handler.Customer.Requests.Queries.GetMyRequest;
using BnplService.Application.Handler.Customer.Config.Queries.GetConfig;
using BnplService.Tests.Handlers;
using DayaDiamond.Client;
using DayaDiamond.Client.Models;
using DayaDiamond.Mock;

namespace BnplService.Tests.DayaDiamond;

/// <summary>
/// سرتاسری: Handlerهای BNPL + دیتابیس (SQLite یا SQL Server) + adapterهای واقعی دایادایموند + کلاینت HTTP + سرور mock.
/// هیچ ارائه‌دهندهٔ جعلی‌ای در این تست‌ها نیست؛ موجودی و قفل و پرداخت‌ها در mock بررسی می‌شوند.
/// </summary>
public class BnplOverDayaDiamondMockTests
{
    private const string Mobile = "09121111111";   // همان مشتری TestCurrentCustomer

    private static async Task<(DayaDiamondMock Mock, BnplTestHost Host)> StartAsync(bool completeProfile = true)
    {
        var mock = new DayaDiamondMock();
        if (completeProfile)
            lock (mock.State.Sync)
            {
                var c = mock.State.Customer(Mobile);
                c.Registered = c.ProfileCompleted = c.AddressCompleted = true;
            }
        var host = await BnplTestHost.CreateAsync(services => mock.AddProviders(services));
        return (mock, host);
    }

    /// <summary>شروع پرداخت قسط، پرداخت در صفحهٔ درگاه mock و کال‌بک به سرویس.</summary>
    private static async Task<PaymentCallbackResultDto> PayAsync(DayaDiamondMock mock, BnplTestHost host, long requestId, int number, bool success = true)
    {
        var start = await host.Ok(new StartPaymentCommand
            { RequestId = requestId, InstallmentNumber = number, GatewayId = 5, ReturnUrl = "https://app.test/bnpl/result" });
        var extId = start.PayUrl!.Split("paymentExtId=")[1];
        var back = await mock.Browser.GetAsync($"mock/pay/{extId}?success={success}");
        Assert.Equal(HttpStatusCode.Redirect, back.StatusCode);
        // درگاه به کال‌بک همین سرویس برمی‌گردد
        Assert.StartsWith($"https://bnpl.test/v2/bnpl/Payments/{start.PaymentId}/Callback?paymentExtId=", back.Headers.Location!.ToString());
        return await host.Ok(new CompleteGatewayPaymentCommand(start.PaymentId));
    }

    [Fact]
    public async Task Whole_contract_runs_against_the_mock_from_profile_to_collateral_release()
    {
        var (mock, host) = await StartAsync(completeProfile: false);
        await using var _ = mock;
        await using var __ = host;

        // پروفایل: getOrAdd با موبایل رمز‌شده، سپس دو فرم
        Assert.False((await host.Ok(new GetProfileStatusQuery())).IsComplete);
        Assert.Equal(412, (await host.Send(new CreateRequestCommand { Amount = 50_000_000 })).StatusCode);
        await host.Ok(new UpsertProfileInfoCommand
        {
            FirstName = "علی", LastName = "محمدی", FatherName = "رضا", NationalCode = "0013542419",
            PersianBirthDate = "1360/11/12", Gender = "Male", Sheba = "820540102680020817909002",
        });
        await host.Ok(new UpsertProfileAddressCommand
            { Province = "تهران", City = "تهران", Street = "خیابان ولیعصر، پلاک ۱", PostalCode = "1234567890", TelephoneNumber = "02188888888" });
        Assert.True(mock.Customer(Mobile).ProfileCompleted && mock.Customer(Mobile).AddressCompleted);

        // قیمت زنده از mock: ۱٬۵۰۱٬۰۰۰ تومان = ۱۵٬۰۱۰٬۰۰۰ ریال
        Assert.Equal(15_010_000, (await host.Ok(new GetConfigQuery())).TokenPrice);

        var created = await host.Ok(new CreateRequestCommand { Amount = 50_000_000 });
        var id = created.Summary.Id;
        await host.Ok(new AcceptTermsCommand { Id = id });
        var collateral = await host.Ok(new GetCollateralStatusQuery(id));
        Assert.True(collateral.IsEnough);
        Assert.Equal((3.332m, 100m), (collateral.RequiredTokens, collateral.FreeBalance));

        // امضا ← وثیقه در حساب دایادایموند قفل می‌شود
        var signed = await host.Ok(new SignContractCommand { Id = id });
        var contract = signed.Summary.ContractNumber;
        Assert.True(signed.CollateralLocked);
        Assert.Equal((96.668m, 3.332m), (mock.Customer(Mobile).FreeAmount, mock.Customer(Mobile).LockedAmount));
        Assert.Equal(3.332m, mock.State.Locks[contract]);

        // قسط اول از درگاه mock ← فعال‌سازی و واریز توکن خریده‌شده با اعتبار
        var first = await PayAsync(mock, host, id, 1);
        Assert.Equal(PaymentStatus.Succeeded, first.Status);
        Assert.StartsWith("MOCK", first.Receipt.TrackingNumber);
        var active = await host.Ok(new GetMyRequestQuery(id));
        Assert.Equal(BnplRequestStatus.Active, active.Summary.Status);
        Assert.True(active.TokensDelivered);
        Assert.Equal(3.331m, active.Summary.TokenAmount);
        Assert.Contains(contract, mock.State.Credits);
        Assert.Equal(96.668m + 3.331m, mock.Customer(Mobile).FreeAmount);

        // قسط دوم توکنی ← کسر از موجودی آزاد با reference یکتا
        var tokenPay = await host.Ok(new StartPaymentCommand { RequestId = id, InstallmentNumber = 2, Method = PaymentMethod.Token });
        Assert.Equal(PaymentStatus.Succeeded, tokenPay.Status);
        var debitTokens = tokenPay.Receipt!.TokenAmount!.Value;
        Assert.Equal(Money.TokensToPay(4_916_660, 15_010_000), debitTokens);
        Assert.Equal($"{contract}/{tokenPay.PaymentId}", Assert.Single(mock.State.Debits.Keys));

        // بقیهٔ اقساط از درگاه ← تسویه ← آزادسازی وثیقه
        for (var n = 3; n <= 12; n++) Assert.Equal(PaymentStatus.Succeeded, (await PayAsync(mock, host, id, n)).Status);
        var settled = await host.Ok(new GetMyRequestQuery(id));
        Assert.Equal(BnplRequestStatus.Settled, settled.Summary.Status);
        Assert.True(settled.CollateralReleased);
        Assert.Equal(0, mock.State.Locks[contract]);
        Assert.Equal(0, mock.Customer(Mobile).LockedAmount);
        Assert.Equal(100 + 3.331m - debitTokens, mock.Customer(Mobile).FreeAmount);
        Assert.Equal(11, mock.State.Payments.Values.Count(p => p.Kind == MockPaymentKind.Installment && p.Status == "Paid"));
    }

    [Fact]
    public async Task Collateral_shortfall_is_fixed_by_buying_tokens_through_the_documented_buy_api()
    {
        var (mock, host) = await StartAsync();
        await using var _ = mock;
        await using var __ = host;
        mock.Customer(Mobile).FreeAmount = 1;

        var created = await host.Ok(new CreateRequestCommand { Amount = 50_000_000 });
        var id = created.Summary.Id;
        await host.Ok(new AcceptTermsCommand { Id = id });

        var status = await host.Ok(new GetCollateralStatusQuery(id));
        Assert.False(status.IsEnough);
        Assert.Equal(2.332m, status.Shortfall);
        Assert.Contains("کم دارید", status.Message);
        Assert.Equal(412, (await host.Send(new SignContractCommand { Id = id })).StatusCode);
        Assert.Empty(mock.State.Locks);

        // «برای خرید به درگاه پرداخت منتقل می‌شوید»: خرید ۳ توکن با buy/request مستند
        await host.Service<IDayaDiamondClient, bool>(async client =>
        {
            var buy = await client.CreateBuyRequestAsync(new BuyRequest { PhoneNumber = Mobile, Amount = 3, GatewayId = 5, ReturnUrl = "https://app.test/buy" });
            (await mock.Browser.GetAsync($"mock/pay/{buy.PaymentExtId}?success=true")).Dispose();
            return true;
        });
        Assert.True((await host.Ok(new GetCollateralStatusQuery(id))).IsEnough);

        var signed = await host.Ok(new SignContractCommand { Id = id });
        Assert.True(signed.CollateralLocked);
        Assert.Equal(4 - 3.332m, mock.Customer(Mobile).FreeAmount);
    }

    [Fact]
    public async Task Cancelled_gateway_payment_fails_and_unfinished_one_stays_pending()
    {
        var (mock, host) = await StartAsync();
        await using var _ = mock;
        await using var __ = host;
        var created = await host.Ok(new CreateRequestCommand { Amount = 50_000_000 });
        var id = created.Summary.Id;
        await host.Ok(new AcceptTermsCommand { Id = id });
        await host.Ok(new SignContractCommand { Id = id });

        var cancelled = await PayAsync(mock, host, id, 1, success: false);
        Assert.Equal(PaymentStatus.Failed, cancelled.Status);
        Assert.Equal(BnplRequestStatus.Signed, (await host.Ok(new GetMyRequestQuery(id))).Summary.Status);

        // کال‌بک پیش از پرداخت: نتیجه هنوز معلوم نیست ← خطای ۵۰۲ و پرداخت در انتظار می‌ماند
        var start = await host.Ok(new StartPaymentCommand
            { RequestId = id, InstallmentNumber = 1, GatewayId = 5, ReturnUrl = "https://app.test/bnpl/result" });
        var early = await host.Send(new CompleteGatewayPaymentCommand(start.PaymentId));
        Assert.Equal(502, early.StatusCode);
        Assert.Contains("هنوز", early.Message);
        Assert.Equal(PaymentStatus.Pending, await host.Db(db => Task.FromResult(db.Payments.Single(p => p.Id == start.PaymentId).Status)));

        (await mock.Browser.GetAsync($"mock/pay/{start.PayUrl!.Split("paymentExtId=")[1]}?success=true")).Dispose();
        Assert.Equal(PaymentStatus.Succeeded, (await host.Ok(new CompleteGatewayPaymentCommand(start.PaymentId))).Status);
        Assert.Equal(BnplRequestStatus.Active, (await host.Ok(new GetMyRequestQuery(id))).Summary.Status);
    }

    [Fact]
    public async Task DayaDiamond_outage_at_signing_leaves_nothing_locked()
    {
        var (mock, host) = await StartAsync();
        await using var _ = mock;
        await using var __ = host;
        var created = await host.Ok(new CreateRequestCommand { Amount = 50_000_000 });
        var id = created.Summary.Id;
        await host.Ok(new AcceptTermsCommand { Id = id });

        lock (mock.State.Sync) mock.State.Outage = true;
        var response = await host.Send(new SignContractCommand { Id = id });
        Assert.Equal(502, response.StatusCode);
        Assert.Contains("دایادایموند", response.Message);
        Assert.Equal(BnplRequestStatus.TermsAccepted, (await host.Ok(new GetMyRequestQuery(id))).Summary.Status);

        lock (mock.State.Sync) mock.State.Outage = false;
        Assert.Equal(0, mock.Customer(Mobile).LockedAmount);
        Assert.True((await host.Ok(new SignContractCommand { Id = id })).CollateralLocked);
    }
}
