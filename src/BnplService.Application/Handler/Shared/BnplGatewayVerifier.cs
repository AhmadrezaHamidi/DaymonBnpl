using Microsoft.Extensions.Logging;

namespace BnplService.Application.Handler.Shared;

/// <summary>
/// استعلام یک پرداخت درگاهیِ در انتظار و اعمال نتیجه؛ مشترک بین کال‌بک و Worker (پرداخت‌هایی که کال‌بکشان نرسید).
/// </summary>
public sealed class BnplGatewayVerifier(IBnplDbContext db, IBnplPaymentGateway gateway, BnplPaymentProcessor processor, TimeProvider time,
    ILogger<BnplGatewayVerifier> logger)
{
    public async Task VerifyAndApplyAsync(BnplPayment payment, CancellationToken ct)
    {
        if (!payment.IsPending) return;
        var now = time.GetUtcNow().UtcDateTime;
        if (string.IsNullOrEmpty(payment.ProviderReference))
        {
            // درگاه هرگز شروع نشد (خطا بین ذخیره و StartAsync)
            if (payment.IsExpired(now)) { payment.Fail("پرداخت آغاز نشد.", now); await db.SaveChangesAsync(ct); }
            return;
        }

        GatewayPaymentResultDto result;
        try
        {
            result = await gateway.VerifyAsync(payment.ProviderReference, payment.Total, ct);
        }
        catch (BnplProviderException ex)
        {
            // نتیجه نامعلوم است: پرداخت در انتظار می‌ماند تا Worker دوباره استعلام کند.
            logger.LogWarning(ex, "Gateway verify failed for payment {PaymentId}", payment.Id);
            throw;
        }

        if (!result.IsPaid)
        {
            payment.Fail(result.FailureReason ?? "پرداخت ناموفق بود.", now);
            await db.SaveChangesAsync(ct);
            return;
        }

        payment.Succeed(result.TrackingNumber, now);
        var request = await db.Requests.Include(r => r.Installments).Include(r => r.History)
            .FirstAsync(r => r.Id == payment.RequestId, ct);
        await processor.ApplyAsync(request, payment, payment.By, ct);
        await db.SaveChangesAsync(CancellationToken.None);
        await processor.SyncCustodyAsync(request, CancellationToken.None);
    }
}
