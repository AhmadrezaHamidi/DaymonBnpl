namespace BnplService.Contracts.Providers;

/// <summary>قیمت لحظه‌ای توکن (دایادایموند: GET price/token، به تومان ← این‌جا ریال).</summary>
public interface ITokenPriceProvider
{
    Task<TokenPriceDto> GetAsync(CancellationToken ct);
}

public sealed record TokenPriceDto(string AssetCode, long PriceRial);

/// <summary>
/// پروفایل مشتری نزد دایادایموند (user/getOrAdd، profile/upsertInfo، profile/upsertAddress).
/// پیش‌خرید فقط با پروفایل و نشانی کامل ممکن است («پس از تکمیل، می‌توانید فرایند پیش‌خرید را ادامه دهید»).
/// </summary>
public interface ICustomerProfileProvider
{
    Task<CustomerProfileStatusDto> GetOrAddAsync(string mobileNumber, CancellationToken ct);
    Task UpsertInfoAsync(CustomerInfoInput input, CancellationToken ct);
    Task UpsertAddressAsync(CustomerAddressInput input, CancellationToken ct);
}

public sealed record CustomerProfileStatusDto(string PhoneNumber, bool ProfileCompleted, bool AddressCompleted)
{
    public bool IsComplete => ProfileCompleted && AddressCompleted;
}

public sealed record CustomerInfoInput(string PhoneNumber, string NationalCode, string FirstName, string LastName, string FatherName,
    string PersianBirthDate, string Gender, string Sheba);

public sealed record CustomerAddressInput(string PhoneNumber, string Province, string City, string Street, string PostalCode,
    string TelephoneNumber);

/// <summary>
/// درگاه پرداخت قسط. ⚠ مستند دایادایموند فقط «خرید توکن» (buy/request) را دارد، نه پرداخت مبلغ دلخواه؛
/// پیاده‌سازی فعلی روی سرویس پیشنهادی bnpl/payment/* است که فعلاً فقط mock (tools/DayaDiamond.Mock) آن را دارد.
/// </summary>
public interface IBnplPaymentGateway
{
    Task<IReadOnlyList<PaymentGatewayDto>> ListAsync(CancellationToken ct);

    /// <summary>ایجاد پرداخت؛ مشتری به PayUrl هدایت می‌شود و درگاه به returnUrl (کال‌بک این سرویس) برمی‌گردد.</summary>
    Task<GatewayPaymentStartDto> StartAsync(GatewayPaymentStartInput input, CancellationToken ct);

    /// <summary>استعلام نتیجه از خود درگاه (به پارامترهای برگشتی مرورگر اعتماد نمی‌کنیم).</summary>
    Task<GatewayPaymentResultDto> VerifyAsync(string providerReference, long expectedAmount, CancellationToken ct);
}

public sealed record PaymentGatewayDto(int Id, string Title, string? Description);

public sealed record GatewayPaymentStartInput(long PaymentId, int GatewayId, long Amount, string MobileNumber, string Description, string CallbackUrl);

public sealed record GatewayPaymentStartDto(string ProviderReference, string PayUrl);

public sealed record GatewayPaymentResultDto(bool IsPaid, string? TrackingNumber, string? FailureReason);

/// <summary>
/// حساب توکن مشتری نزد دایادایموند:
///  - وثیقه: «پس از تأیید و امضای قرارداد، توکن‌ها تا پایان اقساط مسدود می‌شوند و قابل برداشت یا وثیقه‌گذاری مجدد نیستند»
///  - واریز توکن‌هایی که با اعتبار پیش‌خرید خریده شده‌اند
///  - «پرداخت توکنی» قسط
/// ⚠ این سرویس‌ها در مستند فعلی دایادایموند نیستند؛ پیاده‌سازی واقعی منتظر مستند است.
/// </summary>
public interface ITokenCustody
{
    /// <summary>موجودی آزاد (غیرمسدود) توکن مشتری: برای وثیقه و پرداخت توکنی.</summary>
    Task<decimal> GetFreeBalanceAsync(string mobileNumber, CancellationToken ct);

    /// <summary>مسدودسازی توکن وثیقه از موجودی آزاد مشتری هنگام امضا. با contractNumber تکراری نباید دوباره مسدود کند.</summary>
    Task LockAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct);

    /// <summary>آزادسازی وثیقه پس از تسویه یا لغو/انقضای قرارداد امضاشده.</summary>
    Task ReleaseAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct);

    /// <summary>
    /// واریز توکن‌های خریده‌شده با اعتبار پیش‌خرید به حساب مشتری (پس از پرداخت قسط اول).
    /// با contractNumber تکراری نباید دوباره واریز کند.
    /// </summary>
    Task CreditAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct);

    /// <summary>
    /// کسر توکن از موجودی آزاد مشتری بابت قسط؛ خروجی شماره پیگیری.
    /// reference برای هر پرداخت یکتاست («شمارهٔ قرارداد/شناسهٔ پرداخت») تا تکرار همان پرداخت دوباره کسر نکند.
    /// </summary>
    Task<string> DebitAsync(string mobileNumber, string reference, decimal tokenAmount, string description, CancellationToken ct);
}
