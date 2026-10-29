namespace BnplService.Infrastructure.Services.DayaDiamond;

/// <summary>
/// نگهداری توکن‌های قرارداد نزد دایادایموند (مسدودسازی، آزادسازی، کسر توکن قسط).
/// ⚠ این سرویس‌ها در مستند فعلی دایادایموند نیستند؛ تا مستند برسد همهٔ متدها خطای روشن می‌دهند.
/// پرداخت با درگاه کار می‌کند و قفل نشدن توکن فعال‌سازی را نگه نمی‌دارد: Worker بعداً دوباره تلاش می‌کند.
/// </summary>
public sealed class DayaDiamondTokenCustody : ITokenCustody
{
    public const string NotDocumented = "نگهداری و پرداخت توکنی هنوز در سرویس دایادایموند فعال نشده است.";

    public Task<decimal> GetFreeBalanceAsync(string mobileNumber, CancellationToken ct) => throw new BnplProviderException(NotDocumented);

    public Task LockAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct) =>
        throw new BnplProviderException(NotDocumented);

    public Task ReleaseAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct) =>
        throw new BnplProviderException(NotDocumented);

    public Task<string> DebitAsync(string mobileNumber, string contractNumber, decimal tokenAmount, string description, CancellationToken ct) =>
        throw new BnplProviderException(NotDocumented);
}
