using Microsoft.Extensions.Options;

namespace BnplService.Application.Handler.Customer.Payments.Commands.StartPayment;

public sealed class StartPaymentCommandHandler(
    IBnplDbContext db, IBnplCurrentCustomer customer, IBnplPaymentGateway gateway, ITokenCustody custody, ITokenPriceProvider prices,
    BnplPaymentProcessor processor, IOptions<BnplOptions> options, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<StartPaymentCommand, BnplMessageResponse<PaymentStartDto>>
{
    public async Task<BnplMessageResponse<PaymentStartDto>> Handle(StartPaymentCommand command, CancellationToken ct)
    {
        var me = customer.Value;
        var request = await RequestAsync(command.RequestId, ct, me.UserId);
        if (await ExpireIfDueAsync(request, ct))
        {
            await Db.SaveChangesAsync(ct);
            throw new BnplDomainException("مهلت پرداخت قسط اول تمام شده است؛ درخواست جدید ثبت کنید.");
        }
        var installment = request.PayableInstallment(command.InstallmentNumber, Now);
        var penalty = request.PenaltyOf(installment, Today);

        return command.Method switch
        {
            PaymentMethod.Gateway => await GatewayAsync(command, request, installment, penalty, me, ct),
            PaymentMethod.Token => await TokenAsync(request, installment, penalty, me, ct),
            _ => throw new BnplDomainException("روش پرداخت نامعتبر است."),
        };
    }

    private async Task<BnplMessageResponse<PaymentStartDto>> GatewayAsync(StartPaymentCommand command, BnplRequest request,
        Installment installment, long penalty, BnplCustomerRef me, CancellationToken ct)
    {
        if (command.GatewayId is not { } gatewayId) throw new BnplDomainException("درگاه پرداخت را انتخاب کنید.");
        var returnUrl = options.Value.ResolveReturnUrl(command.ReturnUrl);
        var gatewayInfo = (await gateway.ListAsync(ct)).FirstOrDefault(g => g.Id == gatewayId)
                          ?? throw new BnplDomainException("درگاه پرداخت انتخاب‌شده در دسترس نیست.");
        var settings = await SettingsAsync(ct);

        var payment = BnplPayment.Start(request, installment, penalty, PaymentMethod.Gateway, Actor.Customer(me), Now,
            TimeSpan.FromMinutes(settings.GatewayPaymentMinutes));
        Db.Payments.Add(payment);
        await Db.SaveChangesAsync(ct);   // شناسهٔ پرداخت برای کال‌بک لازم است

        try
        {
            var started = await gateway.StartAsync(new GatewayPaymentStartInput(payment.Id, gatewayId, payment.Total, me.MobileNumber,
                payment.Description, options.Value.CallbackUrl(payment.Id)), ct);
            payment.AttachGateway(gatewayId, gatewayInfo.Title, started.ProviderReference, started.PayUrl, returnUrl);
        }
        catch (BnplProviderException ex)
        {
            payment.Fail(ex.Message, Now);
            await Db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        await Db.SaveChangesAsync(ct);
        return Ok(new PaymentStartDto(payment.Id, payment.Method, payment.Status, payment.PayUrl, payment.Amount, payment.Penalty,
            payment.Total, null), "در حال انتقال به درگاه پرداخت.");
    }

    private async Task<BnplMessageResponse<PaymentStartDto>> TokenAsync(BnplRequest request, Installment installment, long penalty,
        BnplCustomerRef me, CancellationToken ct)
    {
        if (request.Status != BnplRequestStatus.Active)
            throw new BnplDomainException("فعال‌سازی قرارداد فقط با پرداخت قسط اول از درگاه بانکی انجام می‌شود.");
        var price = (await prices.GetAsync(ct)).PriceRial;
        var payment = BnplPayment.Start(request, installment, penalty, PaymentMethod.Token, Actor.Customer(me), Now);
        var tokens = Money.TokensToPay(payment.Total, price);
        if (await custody.GetFreeBalanceAsync(me.MobileNumber, ct) < tokens)
            throw new BnplDomainException("موجودی توکن آزاد شما برای پرداخت این قسط کافی نیست.");
        payment.AttachTokens(tokens, price);
        Db.Payments.Add(payment);
        await Db.SaveChangesAsync(ct);

        string tracking;
        try
        {
            tracking = await custody.DebitAsync(me.MobileNumber, request.ContractNumber, tokens, payment.Description, ct);
        }
        catch (BnplProviderException ex)
        {
            payment.Fail(ex.Message, Now);
            await Db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        payment.Succeed(tracking, Now);
        await processor.ApplyAsync(request, payment, Actor.Customer(me), ct);
        await Db.SaveChangesAsync(CancellationToken.None);   // توکن کسر شده است؛ ثبتش نباید با لغو درخواست HTTP قطع شود
        await processor.SyncCustodyAsync(request, CancellationToken.None);

        var receipt = PaymentReceiptDto.Of(payment);
        return Ok(new PaymentStartDto(payment.Id, payment.Method, payment.Status, null, payment.Amount, payment.Penalty, payment.Total, receipt),
            receipt.Title);
    }
}
