using BnplService.Application.Handler.Customer.Requests.Shared;

namespace BnplService.Application.Handler.Customer.Requests.Commands.CancelRequest;

public sealed class CancelRequestCommandHandler(IBnplDbContext db, IBnplCurrentCustomer customer, TimeProvider time)
    : CustomerRequestHandlerBase(db, customer, time), IRequestHandler<CancelRequestCommand, BnplMessageResponse<BnplRequestDetailsDto>>
{
    public async Task<BnplMessageResponse<BnplRequestDetailsDto>> Handle(CancelRequestCommand command, CancellationToken ct)
    {
        var request = await MyOpenRequestAsync(command, ct);
        var now = Now;
        // پرداخت درگاهیِ در جریان: اگر بعداً موفق برگردد، «نیازمند برگشت وجه» می‌شود (BnplPaymentProcessor).
        if (await Db.Payments.AnyAsync(p => p.RequestId == request.Id && p.Status == PaymentStatus.Pending
                                            && (p.ExpiresAt == null || p.ExpiresAt >= now), ct))
            throw new BnplDomainException("پرداخت قسط اول در جریان است؛ چند دقیقه بعد دوباره تلاش کنید.", 409);
        request.Cancel(Actor.Customer(Me), null, now);
        await Db.SaveChangesAsync(ct);
        return Ok(request.ToDetails(Today), "درخواست قبلی لغو گردید و می‌توانید درخواست جدید ثبت نمایید.");
    }
}
