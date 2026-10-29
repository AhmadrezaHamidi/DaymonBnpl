using BnplService.Application.Handler.Maintenance.Commands.RunMaintenance;

namespace BnplService.Api.Workers;

/// <summary>
/// اجرای دوره‌ای RunMaintenanceCommand (پیش‌فرض هر ۶۰ ثانیه):
///  ۱) پرداخت‌های درگاهیِ مهلت‌گذشته که کال‌بکشان نرسید ← استعلام از درگاه
///  ۲) درخواست‌های تکمیل‌نشدهٔ مهلت‌گذشته ← منقضی
///  ۳) قفل/آزادسازی توکن‌هایی که نزد دایادایموند انجام نشد ← تلاش دوباره
/// تنظیمات: Bnpl:Maintenance = { Enabled: true, IntervalSeconds: 60, BatchSize: 100 }.
/// چند نمونهٔ هم‌زمان مشکلی ندارند: هر ردیف نسخه دارد و تداخل فقط به دور بعد می‌افتد.
/// </summary>
public sealed class BnplMaintenanceWorker(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<BnplMaintenanceWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, configuration.GetValue("Bnpl:Maintenance:IntervalSeconds", 60)));
        var batch = Math.Clamp(configuration.GetValue("Bnpl:Maintenance:BatchSize", 100), 1, 1000);
        using var timer = new PeriodicTimer(interval);
        do
        {
            await RunOnceAsync(batch, stoppingToken);
        } while (await WaitAsync(timer, stoppingToken));
    }

    public async Task<MaintenanceResultDto?> RunOnceAsync(int batchSize, CancellationToken ct)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var result = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new RunMaintenanceCommand(batchSize), ct);
            if (result.ExpiredRequests + result.VerifiedPayments + result.CustodySynced > 0)
                logger.LogInformation("BNPL maintenance: {Expired} expired, {Verified} payments settled, {Synced} custody synced",
                    result.ExpiredRequests, result.VerifiedPayments, result.CustodySynced);
            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BNPL maintenance run failed; retrying on the next tick");
            return null;
        }
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
