using DayaDiamond.Client;

namespace BnplService.Infrastructure.Services.DayaDiamond;

/// <summary>خطای کلاینت دایادایموند (پاسخ ناموفق، قطعی شبکه، تایم‌اوت) ← BnplProviderException با پیام فارسی (502).</summary>
public static class DayaDiamondCall
{
    public const string Unavailable = "سرویس دایادایموند در دسترس نیست؛ چند لحظه بعد دوباره تلاش کنید.";

    public static async Task<T> RunAsync<T>(Func<Task<T>> call, CancellationToken ct)
    {
        try
        {
            return await call();
        }
        catch (DayaDiamondException ex)
        {
            throw new BnplProviderException(string.IsNullOrWhiteSpace(ex.Message) ? Unavailable : ex.Message, ex);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException
                                   || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            throw new BnplProviderException(Unavailable, ex);
        }
    }

    public static Task RunAsync(Func<Task> call, CancellationToken ct) =>
        RunAsync(async () => { await call(); return true; }, ct);
}
