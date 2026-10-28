using Microsoft.Extensions.Logging;

namespace BnplService.Application.Handler.Maintenance.Commands.RunMaintenance;

public sealed record MaintenanceResultDto(int ExpiredRequests, int VerifiedPayments, int CustodySynced);

/// <summary>
/// کار دوره‌ای (BnplMaintenanceWorker، هر دقیقه):
///  ۱) پرداخت‌های درگاهیِ مهلت‌گذشته که کال‌بکشان نرسید ← استعلام از درگاه و اعمال نتیجه
///  ۲) درخواست‌های تکمیل‌نشدهٔ مهلت‌گذشته ← منقضی
///  ۳) قراردادهایی که توکنشان مسدود/آزاد نشده ← تلاش دوباره نزد دایادایموند
/// </summary>
public sealed record RunMaintenanceCommand(int BatchSize = 100) : BnplBaseCommand<MaintenanceResultDto>;

public sealed class RunMaintenanceCommandHandler(IBnplDbContext db, BnplGatewayVerifier verifier, BnplPaymentProcessor processor,
    TimeProvider time, ILogger<RunMaintenanceCommandHandler> logger)
    : BnplHandlerBase(db, time), IRequestHandler<RunMaintenanceCommand, MaintenanceResultDto>
{
    public async Task<MaintenanceResultDto> Handle(RunMaintenanceCommand command, CancellationToken ct)
    {
        var now = Now;
        var verified = 0;
        foreach (var payment in await Db.Payments
                     .Where(p => p.Status == PaymentStatus.Pending && p.Method == PaymentMethod.Gateway && p.ExpiresAt < now)
                     .OrderBy(p => p.Id).Take(command.BatchSize).ToListAsync(ct))
        {
            try
            {
                await verifier.VerifyAndApplyAsync(payment, ct);
                if (!payment.IsPending) verified++;
            }
            catch (Exception ex) when (ex is BnplProviderException or DbUpdateConcurrencyException)
            {
                logger.LogWarning(ex, "Maintenance could not settle payment {PaymentId}", payment.Id);
            }
        }

        var expired = 0;
        var open = await RequestsWithDetails().Where(r => BnplRequest.IncompleteStatuses.Contains(r.Status)
                && ((r.Status != BnplRequestStatus.Signed && r.ExpiresAt < now) || (r.Status == BnplRequestStatus.Signed && r.ActivationDeadline < now)))
            .OrderBy(r => r.Id).Take(command.BatchSize).ToListAsync(ct);
        foreach (var r in open)
            if (await ExpireIfDueAsync(r, ct)) expired++;
        if (expired > 0) await Db.SaveChangesAsync(ct);

        var synced = 0;
        foreach (var r in await Db.Requests.Where(r => r.ActivatedAt != null
                         && ((!r.TokensLocked && (r.Status == BnplRequestStatus.Active || r.Status == BnplRequestStatus.Settled))
                             || (r.Status == BnplRequestStatus.Settled && r.TokensLocked && !r.TokensReleased)))
                     .OrderBy(r => r.Id).Take(command.BatchSize).ToListAsync(ct))
        {
            await processor.SyncCustodyAsync(r, ct);
            if (r.TokensLocked && (r.Status != BnplRequestStatus.Settled || r.TokensReleased)) synced++;
        }

        return new MaintenanceResultDto(expired, verified, synced);
    }
}
