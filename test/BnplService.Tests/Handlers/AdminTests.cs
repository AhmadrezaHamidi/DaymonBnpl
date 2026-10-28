using BnplService.Application.Handler.Admin.Dashboard.Queries.GetDashboard;
using BnplService.Application.Handler.Admin.Payments.Commands.MarkRefunded;
using BnplService.Application.Handler.Admin.Payments.Commands.RegisterManualPayment;
using BnplService.Application.Handler.Admin.Payments.Queries.SearchPayments;
using BnplService.Application.Handler.Admin.Requests.Commands.TerminateRequest;
using BnplService.Application.Handler.Admin.Requests.Commands.WaivePenalty;
using BnplService.Application.Handler.Admin.Requests.Queries.GetRequest;
using BnplService.Application.Handler.Admin.Requests.Queries.SearchRequests;
using BnplService.Application.Handler.Admin.Settings.Commands.SavePlan;
using BnplService.Application.Handler.Admin.Settings.Commands.UpdateSettings;
using BnplService.Application.Handler.Admin.Settings.Queries.GetSettings;
using BnplService.Application.Handler.Customer.Config.Queries.GetConfig;
using BnplService.Application.Handler.Customer.Payments.Commands.CompleteGatewayPayment;
using BnplService.Application.Handler.Customer.Payments.Commands.StartPayment;
using BnplService.Application.Handler.Customer.Requests.Commands.CreateRequest;
using BnplService.Application.Handler.Customer.Requests.Queries.GetMyRequest;
using BnplService.Application.Handler.Maintenance.Commands.RunMaintenance;

namespace BnplService.Tests.Handlers;

/// <summary>سناریوهای پنل ادمین و کار دوره‌ای.</summary>
public class AdminTests
{
    [Fact]
    public async Task Search_and_details_show_customer_payments_and_history()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var active = await CustomerFlowTests.ActiveAsync(host);

        var page = await host.Ok(new SearchRequestsQuery { Term = "۰۹۱۲۱۱۱۱۱۱۱" });
        var item = Assert.Single(page.Items);
        Assert.Equal("09121111111", item.MobileNumber);
        Assert.Equal(11 * 4_916_660, item.UnpaidDebt);

        Assert.Single((await host.Ok(new SearchRequestsQuery { Term = active.Summary.ContractNumber })).Items);
        Assert.Empty((await host.Ok(new SearchRequestsQuery { Status = BnplRequestStatus.Settled })).Items);

        var details = await host.Ok(new GetRequestQuery(active.Summary.Id));
        Assert.Single(details.Payments);
        Assert.Equal(
            [BnplRequestStatus.Draft, BnplRequestStatus.TermsAccepted, BnplRequestStatus.Signed, BnplRequestStatus.Active],
            details.Details.History.Select(h => h.ToStatus));
    }

    [Fact]
    public async Task Overdue_filter_waive_penalty_and_manual_payment()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var active = await CustomerFlowTests.ActiveAsync(host);
        host.Time.Advance(TimeSpan.FromDays(40));

        var overdue = await host.Ok(new SearchRequestsQuery { OverdueOnly = true });
        Assert.Single(overdue.Items);
        Assert.True(overdue.Items[0].Request.NextInstallment!.Penalty > 0);

        var waived = await host.Ok(new WaivePenaltyCommand { Id = active.Summary.Id, InstallmentNumber = 2, Reason = "مشکل درگاه بانک" });
        var second = waived.Details.Installments[1];
        Assert.True(second.PenaltyWaived);
        Assert.Equal(0, second.Penalty);

        var paid = await host.Ok(new RegisterManualPaymentCommand { RequestId = active.Summary.Id, InstallmentNumber = 2, TrackingNumber = "FISH-123" });
        Assert.True(paid.Details.Installments[1].Status == InstallmentStatus.Paid);
        var manual = paid.Payments.First();
        Assert.Equal(PaymentMethod.Manual, manual.Method);
        Assert.Equal(4_916_660, manual.Total);
        Assert.Equal("FISH-123", manual.TrackingNumber);
    }

    [Fact]
    public async Task Terminate_requires_reason_and_current_row_version()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var active = await CustomerFlowTests.ActiveAsync(host);
        var details = await host.Ok(new GetRequestQuery(active.Summary.Id));

        Assert.Equal(409, (await host.Send(new TerminateRequestCommand { Id = active.Summary.Id, Reason = "عدم پرداخت اقساط", RowVersion = "1" })).StatusCode);
        var terminated = await host.Ok(new TerminateRequestCommand
            { Id = active.Summary.Id, Reason = "عدم پرداخت اقساط", RowVersion = details.Details.Summary.RowVersion });
        Assert.Equal(BnplRequestStatus.Terminated, terminated.Details.Summary.Status);
        Assert.Equal("ادمین تست", terminated.Details.History.Last().By.Name);

        var pay = await host.Send(new StartPaymentCommand { RequestId = active.Summary.Id, InstallmentNumber = 2, GatewayId = 5 });
        Assert.Equal(400, pay.StatusCode);
    }

    [Fact]
    public async Task Payment_that_arrives_after_cancellation_needs_refund()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var signed = await CustomerFlowTests.SignedAsync(host);
        var start = await host.Ok(new StartPaymentCommand { RequestId = signed.Summary.Id, InstallmentNumber = 1, GatewayId = 5 });

        // مشتری درگاه را رها کرد؛ مهلت پرداخت و فعال‌سازی گذشت و Worker درخواست را منقضی کرد
        host.Time.Advance(TimeSpan.FromMinutes(45));
        await host.Send(new RunMaintenanceCommand());
        Assert.Equal(PaymentStatus.Failed, (await host.Ok(new CompleteGatewayPaymentCommand(start.PaymentId))).Status);
        Assert.Equal(BnplRequestStatus.Expired, (await host.Ok(new GetMyRequestQuery(signed.Summary.Id))).Summary.Status);

        // درخواست جدید، پرداخت موفق دیرهنگام روی قسطی که قبلاً پرداخت شده
        var active = await CustomerFlowTests.ActiveAsync(host);
        var late = await host.Ok(new StartPaymentCommand { RequestId = active.Summary.Id, InstallmentNumber = 2, GatewayId = 5 });
        await CustomerFlowTests.PayByGatewayAsync(host, active.Summary.Id, 2);
        host.Gateway.Pay(late.PayUrl!.Split('/').Last());
        var duplicate = await host.Ok(new CompleteGatewayPaymentCommand(late.PaymentId));
        Assert.Equal(PaymentStatus.RefundRequired, duplicate.Status);

        var queue = await host.Ok(new SearchPaymentsQuery { Status = PaymentStatus.RefundRequired });
        Assert.Single(queue.Items);
        var dashboard = await host.Ok(new GetDashboardQuery());
        Assert.Equal(1, dashboard.RefundRequiredPayments);

        var refunded = await host.Ok(new MarkRefundedCommand { PaymentId = late.PaymentId, TrackingNumber = "RF-9" });
        Assert.Equal(PaymentStatus.Refunded, refunded.Payment.Status);
    }

    [Fact]
    public async Task Maintenance_settles_a_paid_gateway_payment_whose_callback_never_came()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var signed = await CustomerFlowTests.SignedAsync(host);
        var start = await host.Ok(new StartPaymentCommand { RequestId = signed.Summary.Id, InstallmentNumber = 1, GatewayId = 5 });
        host.Gateway.Pay(start.PayUrl!.Split('/').Last());

        host.Time.Advance(TimeSpan.FromMinutes(21));
        var result = await host.Send(new RunMaintenanceCommand());
        Assert.Equal(1, result.VerifiedPayments);
        Assert.Equal(BnplRequestStatus.Active, (await host.Ok(new GetMyRequestQuery(signed.Summary.Id))).Summary.Status);
    }

    [Fact]
    public async Task Dashboard_counts_contracts_and_money()
    {
        await using var host = await BnplTestHost.CreateAsync();
        await CustomerFlowTests.ActiveAsync(host);
        host.Customer.Value = new BnplCustomerRef("2002", "09350000000", "مشتری دوم");
        await host.Ok(new CreateRequestCommand { Amount = 100_000_000 });

        var d = await host.Ok(new GetDashboardQuery());
        Assert.Equal(1, d.ActiveContracts);
        Assert.Equal(50_000_000, d.ActivePrincipal);
        Assert.Equal(6_916_660, d.CollectedTotal);
        Assert.Equal(11 * 4_916_660, d.OutstandingTotal);
        Assert.Equal(2, d.NewRequestsToday);
        Assert.Equal(1, d.ActivatedToday);
        Assert.Equal(1, d.ByStatus.Single(s => s.Status == BnplRequestStatus.Draft).Count);
    }

    [Fact]
    public async Task Settings_and_plans_are_editable_and_disable_new_requests()
    {
        await using var host = await BnplTestHost.CreateAsync();
        var current = await host.Ok(new GetSettingsQuery());
        var plan = Assert.Single(current.Plans);

        await host.Ok(new SavePlanCommand { Title = "۶ قسط", InstallmentCount = 6, FeePercent = 12, UpfrontFeePercent = 2, LatePenaltyDailyPercent = 1, SortOrder = 0 });
        Assert.Equal(2, (await host.Ok(new GetConfigQuery())).Plans.Count);

        var bad = await host.Send(new SavePlanCommand
            { Id = plan.Id, Title = plan.Title, InstallmentCount = 12, FeePercent = 3, UpfrontFeePercent = 4, LatePenaltyDailyPercent = 1, RowVersion = plan.RowVersion });
        Assert.Equal(400, bad.StatusCode);

        var s = current.Settings;
        await host.Ok(new UpdateSettingsCommand
        {
            IsEnabled = false, MinAmount = s.MinAmount, MaxAmount = s.MaxAmount, AmountStep = s.AmountStep, CompletionHours = s.CompletionHours,
            ActivationMinutes = s.ActivationMinutes, GatewayPaymentMinutes = s.GatewayPaymentMinutes,
            MaxActiveContractsPerCustomer = s.MaxActiveContractsPerCustomer, RowVersion = s.RowVersion,
        });
        var blocked = await host.Send(new CreateRequestCommand { Amount = 50_000_000 });
        Assert.Equal(400, blocked.StatusCode);
        Assert.Contains("امکان‌پذیر نیست", blocked.Message);
    }
}
