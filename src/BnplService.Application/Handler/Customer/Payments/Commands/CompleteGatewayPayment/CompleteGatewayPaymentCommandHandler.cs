namespace BnplService.Application.Handler.Customer.Payments.Commands.CompleteGatewayPayment;

public sealed class CompleteGatewayPaymentCommandHandler(IBnplDbContext db, BnplGatewayVerifier verifier, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<CompleteGatewayPaymentCommand, BnplMessageResponse<PaymentCallbackResultDto>>
{
    public async Task<BnplMessageResponse<PaymentCallbackResultDto>> Handle(CompleteGatewayPaymentCommand command, CancellationToken ct)
    {
        var payment = await Db.Payments.FirstOrDefaultAsync(p => p.Id == command.PaymentId && p.Method == PaymentMethod.Gateway, ct)
                      ?? throw BnplDomainException.NotFound("پرداخت یافت نشد.");
        if (payment.IsPending) await verifier.VerifyAndApplyAsync(payment, ct);

        var receipt = PaymentReceiptDto.Of(payment);
        return Ok(new PaymentCallbackResultDto(payment.Id, payment.Status, RedirectUrl(payment), receipt), receipt.Title);
    }

    private static string? RedirectUrl(BnplPayment p)
    {
        if (string.IsNullOrWhiteSpace(p.ReturnUrl)) return null;
        var separator = p.ReturnUrl.Contains('?') ? '&' : '?';
        return $"{p.ReturnUrl}{separator}paymentId={p.Id}&status={p.Status}";
    }
}
