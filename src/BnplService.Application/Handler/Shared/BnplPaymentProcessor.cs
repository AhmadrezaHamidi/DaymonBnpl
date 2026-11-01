using Microsoft.Extensions.Logging;

namespace BnplService.Application.Handler.Shared;

/// <summary>
/// اعمال پرداخت موفق روی قرارداد، مشترک بین کال‌بک درگاه، پرداخت توکنی، ثبت دستی ادمین و Worker:
///  - قسط اول ← فعال‌سازی با قیمت لحظه‌ای توکن، شمارش کد تخفیف
///  - قسط دیگر قابل پرداخت نیست (لغو/انقضا/قبلاً پرداخت‌شده) ← پرداخت «نیازمند برگشت وجه»
///  - بعد از ذخیره: واریز توکن خریده‌شده و آزادسازی وثیقه نزد دایادایموند (اگر نشد، Worker دوباره تلاش می‌کند)
/// </summary>
public sealed class BnplPaymentProcessor(IBnplDbContext db, ITokenPriceProvider prices, ITokenCustody custody, TimeProvider time,
    ILogger<BnplPaymentProcessor> logger)
{
    /// <summary>payment باید Succeeded باشد و request با اقساط بارگذاری شده باشد. SaveChanges با فراخوان است.</summary>
    public async Task ApplyAsync(BnplRequest request, BnplPayment payment, Actor by, CancellationToken ct)
    {
        var activating = request.Status == BnplRequestStatus.Signed;
        long? price = null;
        if (activating)
        {
            try
            {
                price = (await prices.GetAsync(ct)).PriceRial;
            }
            catch (BnplProviderException ex)
            {
                // پول گرفته شده است؛ فعال‌سازی معطل قیمت نمی‌ماند ← آخرین قیمت دیده‌شده (لحظهٔ امضا) مبنا می‌شود.
                logger.LogWarning(ex, "Token price unavailable while activating {Contract}; using last seen price", request.ContractNumber);
            }
        }

        try
        {
            request.ApplyPayment(payment, price, by, time.GetUtcNow().UtcDateTime, BnplClock.Today(time));
        }
        catch (BnplDomainException ex)
        {
            logger.LogWarning("Payment {PaymentId} for {Contract} could not be applied: {Reason}", payment.Id, request.ContractNumber, ex.Message);
            payment.RequireRefund(ex.Message);
            return;
        }

        if (activating && request.DiscountCodeId is { } discountId)
            (await db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == discountId, ct))?.MarkUsed();
    }

    /// <summary>بعد از SaveChanges صدا زده شود. خطای دایادایموند فقط لاگ می‌شود؛ Worker بعداً دوباره تلاش می‌کند.</summary>
    public async Task SyncCustodyAsync(BnplRequest request, CancellationToken ct)
    {
        try
        {
            var changed = false;
            if (request.NeedsTokenDelivery)
            {
                await custody.CreditAsync(request.MobileNumber, request.ContractNumber, request.TokenAmount, ct);
                request.MarkTokensDelivered();
                changed = true;
            }
            if (request.NeedsCollateralRelease)
            {
                await custody.ReleaseAsync(request.MobileNumber, request.ContractNumber, request.CollateralTokenAmount, ct);
                request.MarkCollateralReleased();
                changed = true;
            }
            if (changed) await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Token custody sync failed for {Contract}; the maintenance worker will retry", request.ContractNumber);
        }
    }
}
