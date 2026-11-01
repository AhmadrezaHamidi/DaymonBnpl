using BnplService.Application.Handler.Customer.Requests.Shared;
using Microsoft.Extensions.Logging;

namespace BnplService.Application.Handler.Customer.Requests.Commands.SignContract;

public sealed class SignContractCommandHandler(IBnplDbContext db, IBnplCurrentCustomer customer, ITokenPriceProvider prices,
    ITokenCustody custody, TimeProvider time, ILogger<SignContractCommandHandler> logger)
    : CustomerRequestHandlerBase(db, customer, time), IRequestHandler<SignContractCommand, BnplMessageResponse<BnplRequestDetailsDto>>
{
    public async Task<BnplMessageResponse<BnplRequestDetailsDto>> Handle(SignContractCommand command, CancellationToken ct)
    {
        var request = await MyOpenRequestAsync(command, ct);
        if (request.DiscountCodeId is { } id)
            (await Db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == id, ct) ?? throw new BnplDomainException("کد تخفیف معتبر نیست."))
                .EnsureUsable(Now);
        var settings = await SettingsAsync(ct);
        var price = await prices.GetAsync(ct);
        var freeBalance = await custody.GetFreeBalanceAsync(Me.MobileNumber, ct);
        request.Sign(Actor.Customer(Me), Now, settings.ActivationMinutes, price.PriceRial, freeBalance);

        // مسدودسازی وثیقه قبل از ذخیره: اگر دایادایموند نپذیرفت (502)، امضا ثبت نمی‌شود.
        await custody.LockAsync(Me.MobileNumber, request.ContractNumber, request.CollateralTokenAmount, ct);
        request.MarkCollateralLocked();
        try
        {
            await Db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // امضا ذخیره نشد ← وثیقهٔ مسدودشده را برگردان تا توکن مشتری بی‌دلیل قفل نماند.
            try
            {
                await custody.ReleaseAsync(Me.MobileNumber, request.ContractNumber, request.CollateralTokenAmount, CancellationToken.None);
            }
            catch (Exception releaseEx)
            {
                logger.LogError(releaseEx, "Collateral of {Contract} stayed locked after a failed sign; release it manually", request.ContractNumber);
            }
            throw;
        }
        return Ok(request.ToDetails(Today),
            $"قرارداد امضا شد و {PersianNumber.Of(request.CollateralTokenAmount)} توکن وثیقه مسدود گردید؛ " +
            $"{PersianNumber.Of(settings.ActivationMinutes)} دقیقه برای پرداخت قسط اول و فعال‌سازی فرصت دارید.");
    }
}
