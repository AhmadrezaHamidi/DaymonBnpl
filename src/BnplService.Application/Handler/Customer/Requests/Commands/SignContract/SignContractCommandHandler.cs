using BnplService.Application.Handler.Customer.Requests.Shared;

namespace BnplService.Application.Handler.Customer.Requests.Commands.SignContract;

public sealed class SignContractCommandHandler(IBnplDbContext db, IBnplCurrentCustomer customer, TimeProvider time)
    : CustomerRequestHandlerBase(db, customer, time), IRequestHandler<SignContractCommand, BnplMessageResponse<BnplRequestDetailsDto>>
{
    public async Task<BnplMessageResponse<BnplRequestDetailsDto>> Handle(SignContractCommand command, CancellationToken ct)
    {
        var request = await MyOpenRequestAsync(command, ct);
        if (request.DiscountCodeId is { } id)
            (await Db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == id, ct) ?? throw new BnplDomainException("کد تخفیف معتبر نیست."))
                .EnsureUsable(Now);
        var settings = await SettingsAsync(ct);
        request.Sign(Actor.Customer(Me), Now, settings.ActivationMinutes);
        await Db.SaveChangesAsync(ct);
        return Ok(request.ToDetails(Today),
            $"قرارداد امضا شد؛ {PersianNumber.Of(settings.ActivationMinutes)} دقیقه برای پرداخت قسط اول و فعال‌سازی فرصت دارید.");
    }
}
