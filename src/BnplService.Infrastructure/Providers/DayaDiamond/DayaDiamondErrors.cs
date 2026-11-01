using DayaDiamond.Client;

namespace BnplService.Infrastructure.Providers.DayaDiamond;

/// <summary>خطاهای کلاینت دایادایموند را به BnplProviderException با پیام فارسی قابل نمایش تبدیل می‌کند.</summary>
internal static class DayaDiamondErrors
{
    public static async Task<T> Call<T>(Func<Task<T>> call, string operation)
    {
        try
        {
            return await call();
        }
        catch (DayaDiamondException ex) when ((int?)ex.StatusCode >= 500)
        {
            throw new BnplProviderException($"{operation}: سرویس دایادایموند در دسترس نیست.", ex);
        }
        catch (DayaDiamondException ex)
        {
            // خطای کسب‌وکاری سرور (مثل «موجودی آزاد کافی نیست») همان‌طور که هست نشان داده می‌شود.
            throw new BnplProviderException($"{operation}: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new BnplProviderException($"{operation}: سرویس دایادایموند در دسترس نیست.", ex);
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            throw new BnplProviderException($"{operation}: پاسخ سرویس دایادایموند به‌موقع نرسید.", ex);
        }
    }

    public static Task Call(Func<Task> call, string operation) => Call(async () =>
    {
        await call();
        return true;
    }, operation);
}
