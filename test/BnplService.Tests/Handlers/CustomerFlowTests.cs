using BnplService.Application.Handler.Customer.Config.Queries.GetConfig;
using BnplService.Application.Handler.Customer.Config.Queries.GetQuote;
using BnplService.Application.Handler.Customer.Payments.Commands.CompleteGatewayPayment;
using BnplService.Application.Handler.Customer.Payments.Commands.StartPayment;
using BnplService.Application.Handler.Customer.Payments.Queries.SearchMyPayments;
using BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileInfo;
using BnplService.Application.Handler.Customer.Requests.Commands.AcceptTerms;
using BnplService.Application.Handler.Customer.Requests.Commands.ApplyDiscount;
using BnplService.Application.Handler.Customer.Requests.Commands.CancelRequest;
using BnplService.Application.Handler.Customer.Requests.Commands.CreateRequest;
using BnplService.Application.Handler.Customer.Requests.Commands.SignContract;
using BnplService.Application.Handler.Customer.Requests.Queries.GetIncompleteRequest;
using BnplService.Application.Handler.Customer.Requests.Queries.GetMyRequest;
using BnplService.Application.Handler.Customer.Requests.Queries.SearchMyRequests;
using BnplService.Application.Handler.Admin.Discounts.Commands.SaveDiscount;
using Microsoft.EntityFrameworkCore;

namespace BnplService.Tests.Handlers;

/// <summary>سناریوهای مشتری، به ترتیب صفحه‌های فیگما.</summary>
public class CustomerFlowTests
{
    internal static async Task<BnplRequestDetailsDto> SignedAsync(BnplTestHost host, long amount = 50_000_000)
    {
        var created = await host.Ok(new CreateRequestCommand { Amount = amount });
        await host.Ok(new AcceptTermsCommand { Id = created.Summary.Id });
        return await host.Ok(new SignContractCommand { Id = created.Summary.Id });
    }

    /// <summary>پرداخت قسط از درگاه و برگشت کال‌بک موفق.</summary>
    internal static async Task<PaymentCallbackResultDto> PayByGatewayAsync(BnplTestHost host, long requestId, int number)
    {
        var start = await host.Ok(new StartPaymentCommand
            { RequestId = requestId, InstallmentNumber = number, GatewayId = 5, ReturnUrl = "https://app.test/bnpl/result" });
        var reference = start.PayUrl!.Split('/').Last();
        host.Gateway.Pay(reference);
        return await host.Ok(new CompleteGatewayPaymentCommand(start.PaymentId));
    }

    internal static async Task<BnplRequestDetailsDto> ActiveAsync(BnplTestHost host, long amount = 50_000_000)
    {
        var signed = await SignedAsync(host, amount);
        await PayByGatewayAsync(host, signed.Summary.Id, 1);
        return await host.Ok(new GetMyRequestQuery(signed.Summary.Id));
    }

    [Fact]
    public async Task Config_and_quote_use_the_live_token_price()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var config = await host.Ok(new GetConfigQuery());
        Assert.Equal(50_000_000, config.MinAmount);
        Assert.Equal(5_000_000_000, config.MaxAmount);
        Assert.Equal(15_010_000, config.TokenPrice);
        var plan = Assert.Single(config.Plans);
        Assert.Equal(12, plan.InstallmentCount);

        var quote = await host.Ok(new GetQuoteQuery { Amount = 50_000_000 });
        Assert.Equal(6_916_660, quote.FirstInstallmentAmount);
        Assert.Equal(3.331m, quote.TokenAmount);
    }

    [Fact]
    public async Task Full_contract_from_request_to_settlement()
    {
        await using var host = await BnplTestHost.CreateAsync();

        var created = await host.Ok(new CreateRequestCommand { Amount = 50_000_000 });
        Assert.Equal(BnplRequestStatus.Draft, created.Summary.Status);
        Assert.StartsWith("P5_U", created.Summary.ContractNumber);
        Assert.Equal(12, created.Installments.Count);
        Assert.Equal(60_999_920, created.TotalRepayment);
        Assert.False(created.IsTokenAmountFinal);

        var id = created.Summary.Id;
        await host.Ok(new AcceptTermsCommand { Id = id });
        var signed = await host.Ok(new SignContractCommand { Id = id });
        Assert.Equal(BnplRequestStatus.Signed, signed.Summary.Status);
        Assert.Equal(BnplTestHost.Start.UtcDateTime.AddMinutes(30), signed.Summary.Deadline);

        // قیمت تا لحظهٔ پرداخت قسط اول عوض شد ← تعداد توکن با قیمت لحظهٔ فعال‌سازی قطعی می‌شود
        host.Price.PriceRial = 16_000_000;
        var first = await PayByGatewayAsync(host, id, 1);
        Assert.Equal(PaymentStatus.Succeeded, first.Status);
        Assert.Equal(6_916_660, first.Receipt.Total);
        Assert.StartsWith("https://app.test/bnpl/result?paymentId=", first.RedirectUrl);
        Assert.Equal("پرداخت موفق قسط اول", first.Receipt.Title);

        var active = await host.Ok(new GetMyRequestQuery(id));
        Assert.Equal(BnplRequestStatus.Active, active.Summary.Status);
        Assert.True(active.IsTokenAmountFinal);
        Assert.Equal(3.125m, active.Summary.TokenAmount);
        Assert.Equal(16_000_000, active.TokenPrice);
        Assert.Equal(1, active.Summary.PaidCount);
        Assert.Equal(8, active.PaidPercent);
        Assert.Equal(11 * 4_916_660, active.UnpaidDebt);
        Assert.Equal(("" + active.Summary.ContractNumber, 3.125m), Assert.Single(host.Custody.Locked));
        Assert.Equal(BnplClock.ToTehranDate(BnplTestHost.Start.UtcDateTime), active.Installments[0].DueDate);
        Assert.Equal(Jalali.AddMonths(active.Installments[0].DueDate, 11), active.Summary.EndDate);

        // بقیهٔ اقساط: یکی توکنی، بقیه درگاه
        var tokenPay = await host.Ok(new StartPaymentCommand { RequestId = id, InstallmentNumber = 2, Method = PaymentMethod.Token });
        Assert.Equal(PaymentStatus.Succeeded, tokenPay.Status);
        Assert.Equal("پرداخت موفق قسط دوم", tokenPay.Receipt!.Title);
        Assert.Equal(Money.TokensToPay(4_916_660, 16_000_000), tokenPay.Receipt.TokenAmount);

        for (var n = 3; n <= 12; n++) await PayByGatewayAsync(host, id, n);

        var settled = await host.Ok(new GetMyRequestQuery(id));
        Assert.Equal(BnplRequestStatus.Settled, settled.Summary.Status);
        Assert.Equal(0, settled.UnpaidDebt);
        Assert.Equal(settled.Summary.TokenAmount, settled.OwnedTokenAmount);
        Assert.Single(host.Custody.Released);

        var history = await host.Ok(new SearchMyRequestsQuery { Filter = MyRequestsFilter.Settled });
        Assert.Equal(id, Assert.Single(history.Items).Id);
        Assert.Empty((await host.Ok(new SearchMyRequestsQuery { Filter = MyRequestsFilter.Unsettled })).Items);
        Assert.Equal(12, (await host.Ok(new SearchMyPaymentsQuery { RequestId = id, PageSize = 50 })).TotalCount);
    }

    [Fact]
    public async Task Incomplete_profile_blocks_a_new_request()
    {
        await using var host = await BnplTestHost.CreateAsync();
        host.Profiles.AddressCompleted = false;
        var response = await host.Send(new CreateRequestCommand { Amount = 50_000_000 });
        Assert.Equal(412, response.StatusCode);
        Assert.Contains("تکمیل", response.Message);
    }

    [Fact]
    public async Task Profile_info_rejects_an_invalid_national_code_and_sheba()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var command = new UpsertProfileInfoCommand
        {
            FirstName = "علی", LastName = "محمدی", FatherName = "حسین", NationalCode = "0011111111",
            PersianBirthDate = "1360/11/12", Gender = "Male", Sheba = "820540102680020817909002",
        };
        Assert.Equal("کد ملی معتبر نیست.", (await host.Send(command)).Message);
        Assert.Equal("شماره شبا معتبر نیست.", (await host.Send(command with { NationalCode = "0013542419", Sheba = "820540102680020817909003" })).Message);

        await host.Ok(command with { NationalCode = "0013542419" });
        Assert.Equal("09121111111", host.Profiles.LastInfo!.PhoneNumber);   // موبایل از نشست، نه از بدنه
    }

    [Fact]
    public async Task Only_one_incomplete_request_at_a_time()
    {
        await using var host = await BnplTestHost.CreateAsync();
        await host.Ok(new CreateRequestCommand { Amount = 50_000_000 });
        var second = await host.Send(new CreateRequestCommand { Amount = 60_000_000 });
        Assert.Equal(409, second.StatusCode);
        Assert.Contains("تکمیل نشده", second.Message);
    }

    [Fact]
    public async Task Amount_outside_the_slider_is_rejected()
    {
        await using var host = await BnplTestHost.CreateAsync();
        Assert.Equal(400, (await host.Send(new CreateRequestCommand { Amount = 10_000_000 })).StatusCode);
        Assert.Equal(400, (await host.Send(new CreateRequestCommand { Amount = 55_000_000 })).StatusCode);
    }

    [Fact]
    public async Task Cancelled_request_frees_the_slot_for_a_new_one()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var created = await host.Ok(new CreateRequestCommand { Amount = 50_000_000 });
        var cancelled = await host.Ok(new CancelRequestCommand { Id = created.Summary.Id });
        Assert.Equal(BnplRequestStatus.Cancelled, cancelled.Summary.Status);
        Assert.Null(await host.Ok(new GetIncompleteRequestQuery()));
        await host.Ok(new CreateRequestCommand { Amount = 50_000_000 });
    }

    [Fact]
    public async Task Unfinished_request_expires_after_the_completion_window()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var created = await host.Ok(new CreateRequestCommand { Amount = 50_000_000 });
        Assert.NotNull(await host.Ok(new GetIncompleteRequestQuery()));

        host.Time.Advance(TimeSpan.FromHours(25));
        Assert.Null(await host.Ok(new GetIncompleteRequestQuery()));
        var expired = await host.Ok(new GetMyRequestQuery(created.Summary.Id));
        Assert.Equal(BnplRequestStatus.Expired, expired.Summary.Status);
        Assert.Equal("پایان مهلت تکمیل درخواست", expired.CloseReason);
    }

    [Fact]
    public async Task First_installment_must_be_paid_within_the_activation_window()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var signed = await SignedAsync(host);
        host.Time.Advance(TimeSpan.FromMinutes(31));
        var start = await host.Send(new StartPaymentCommand { RequestId = signed.Summary.Id, InstallmentNumber = 1, GatewayId = 5 });
        Assert.Equal(400, start.StatusCode);
        Assert.Equal(BnplRequestStatus.Expired, (await host.Ok(new GetMyRequestQuery(signed.Summary.Id))).Summary.Status);
    }

    [Fact]
    public async Task Gateway_result_counts_from_payment_start_not_callback_time()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var signed = await SignedAsync(host);
        host.Time.Advance(TimeSpan.FromMinutes(29));
        var start = await host.Ok(new StartPaymentCommand { RequestId = signed.Summary.Id, InstallmentNumber = 1, GatewayId = 5 });

        // کال‌بک بعد از پایان مهلت ۳۰ دقیقه می‌رسد؛ درخواست به خاطر پرداخت در جریان منقضی نشده است
        host.Time.Advance(TimeSpan.FromMinutes(5));
        Assert.NotNull(await host.Ok(new GetIncompleteRequestQuery()));
        host.Gateway.Pay(start.PayUrl!.Split('/').Last());
        var result = await host.Ok(new CompleteGatewayPaymentCommand(start.PaymentId));
        Assert.Equal(PaymentStatus.Succeeded, result.Status);
        Assert.Equal(BnplRequestStatus.Active, (await host.Ok(new GetMyRequestQuery(signed.Summary.Id))).Summary.Status);
    }

    [Fact]
    public async Task Failed_gateway_payment_leaves_the_installment_unpaid_and_callback_is_idempotent()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var signed = await SignedAsync(host);
        var start = await host.Ok(new StartPaymentCommand { RequestId = signed.Summary.Id, InstallmentNumber = 1, GatewayId = 5 });
        var failed = await host.Ok(new CompleteGatewayPaymentCommand(start.PaymentId));
        Assert.Equal(PaymentStatus.Failed, failed.Status);
        Assert.Equal("پرداخت قسط اول ناموفق", failed.Receipt.Title);
        Assert.Equal(PaymentStatus.Failed, (await host.Ok(new CompleteGatewayPaymentCommand(start.PaymentId))).Status);
        Assert.Equal(BnplRequestStatus.Signed, (await host.Ok(new GetMyRequestQuery(signed.Summary.Id))).Summary.Status);
    }

    [Fact]
    public async Task Return_url_must_be_whitelisted()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var signed = await SignedAsync(host);
        var response = await host.Send(new StartPaymentCommand
            { RequestId = signed.Summary.Id, InstallmentNumber = 1, GatewayId = 5, ReturnUrl = "https://evil.test/" });
        Assert.Equal("آدرس بازگشت مجاز نیست.", response.Message);
    }

    [Fact]
    public async Task Activation_cannot_be_paid_with_tokens_and_installments_go_in_order()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var signed = await SignedAsync(host);
        var token = await host.Send(new StartPaymentCommand { RequestId = signed.Summary.Id, InstallmentNumber = 1, Method = PaymentMethod.Token });
        Assert.Contains("درگاه", token.Message);

        await PayByGatewayAsync(host, signed.Summary.Id, 1);
        var outOfOrder = await host.Send(new StartPaymentCommand { RequestId = signed.Summary.Id, InstallmentNumber = 3, GatewayId = 5 });
        Assert.Contains("به ترتیب", outOfOrder.Message);
    }

    [Fact]
    public async Task Token_payment_needs_enough_free_balance()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var active = await ActiveAsync(host);
        host.Custody.FreeBalance = 0.1m;
        var response = await host.Send(new StartPaymentCommand { RequestId = active.Summary.Id, InstallmentNumber = 2, Method = PaymentMethod.Token });
        Assert.Contains("کافی نیست", response.Message);
        Assert.Empty(host.Custody.Debited);
    }

    [Fact]
    public async Task Late_installment_is_charged_one_percent_a_day()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var active = await ActiveAsync(host);
        var due2 = active.Installments[1].DueDate;
        var now = BnplTestHost.Start.UtcDateTime;
        host.Time.Advance(due2.ToDateTime(TimeOnly.MinValue) - BnplClock.ToTehranDate(now).ToDateTime(TimeOnly.MinValue) + TimeSpan.FromDays(3));

        var late = await host.Ok(new GetMyRequestQuery(active.Summary.Id));
        Assert.True(late.Summary.IsOverdue);
        Assert.Equal(3, late.Summary.NextInstallment!.DaysLate);
        Assert.Equal(147_490, late.Summary.NextInstallment.Penalty);

        var start = await host.Ok(new StartPaymentCommand { RequestId = active.Summary.Id, InstallmentNumber = 2, GatewayId = 5 });
        Assert.Equal(4_916_660 + 147_490, start.Total);
    }

    [Fact]
    public async Task Discount_code_reduces_the_last_installment_and_is_counted_on_activation()
    {
        await using var host = await BnplTestHost.CreateAsync();
        await host.Ok(new SaveDiscountCommand { Code = "yalda", Title = "یلدا", FixedAmount = 1_000_000, UsageLimit = 1 });

        var created = await host.Ok(new CreateRequestCommand { Amount = 50_000_000 });
        var withCode = await host.Ok(new ApplyDiscountCommand { Id = created.Summary.Id, Code = "YALDA" });
        Assert.Equal(1_000_000, withCode.DiscountAmount);
        Assert.Equal(3_916_660, withCode.Installments[^1].Amount);
        Assert.Equal(59_999_920, withCode.TotalRepayment);

        await host.Ok(new AcceptTermsCommand { Id = created.Summary.Id });
        await host.Ok(new SignContractCommand { Id = created.Summary.Id });
        await PayByGatewayAsync(host, created.Summary.Id, 1);
        Assert.Equal(1, await host.Db(db => db.DiscountCodes.Where(d => d.Code == "YALDA").Select(d => d.UsedCount).SingleAsync()));

        var quote = await host.Send(new GetQuoteQuery { Amount = 50_000_000, DiscountCode = "YALDA" });
        Assert.Equal("ظرفیت استفاده از این کد تخفیف تکمیل شده است.", quote.Message);
    }

    [Fact]
    public async Task Stale_row_version_is_a_conflict()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var created = await host.Ok(new CreateRequestCommand { Amount = 50_000_000 });
        await host.Ok(new AcceptTermsCommand { Id = created.Summary.Id, RowVersion = created.Summary.RowVersion });
        var stale = await host.Send(new SignContractCommand { Id = created.Summary.Id, RowVersion = created.Summary.RowVersion });
        Assert.Equal(409, stale.StatusCode);
    }

    [Fact]
    public async Task Customers_cannot_see_each_others_requests()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var created = await host.Ok(new CreateRequestCommand { Amount = 50_000_000 });
        host.Customer.Value = new BnplCustomerRef("2002", "09350000000", "مشتری دیگر");
        Assert.Equal(404, (await host.Send(new GetMyRequestQuery(created.Summary.Id))).StatusCode);
        Assert.Equal(404, (await host.Send(new CancelRequestCommand { Id = created.Summary.Id })).StatusCode);
    }

    [Fact]
    public async Task Token_lock_failure_does_not_block_activation_and_is_retried_later()
    {
        await using var host = await BnplTestHost.CreateAsync();
        host.Custody.FailLock = true;
        var active = await ActiveAsync(host);
        Assert.Equal(BnplRequestStatus.Active, active.Summary.Status);
        Assert.Empty(host.Custody.Locked);
        Assert.False(await host.Db(db => db.Requests.Where(r => r.Id == active.Summary.Id).Select(r => r.TokensLocked).SingleAsync()));

        host.Custody.FailLock = false;
        var result = await host.Send(new BnplService.Application.Handler.Maintenance.Commands.RunMaintenance.RunMaintenanceCommand());
        Assert.Equal(1, result.CustodySynced);
        Assert.Single(host.Custody.Locked);
    }
}
