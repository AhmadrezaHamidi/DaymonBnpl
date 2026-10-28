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
/// تا API پرداخت قسط مشخص شود، Infrastructure یک درگاه آزمایشی (Sandbox) دارد که فقط در توسعه فعال است.
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
/// نگهداری توکن‌های قرارداد نزد دایادایموند: «پس از تأیید و امضای قرارداد توکن‌ها تا پایان اقساط در حساب شما مسدود می‌شود»
/// و «پرداخت توکنی» قسط. ⚠ این سرویس‌ها در مستند فعلی دایادایموند نیستند؛ پیاده‌سازی واقعی منتظر مستند است.
/// </summary>
public interface ITokenCustody
{
    /// <summary>موجودی آزاد توکن مشتری (برای پرداخت توکنی).</summary>
    Task<decimal> GetFreeBalanceAsync(string mobileNumber, CancellationToken ct);

    /// <summary>واریز و مسدودسازی توکن‌های قرارداد هنگام فعال‌سازی.</summary>
    Task LockAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct);

    /// <summary>آزادسازی توکن‌های قرارداد پس از تسویه.</summary>
    Task ReleaseAsync(string mobileNumber, string contractNumber, decimal tokenAmount, CancellationToken ct);

    /// <summary>کسر توکن از موجودی آزاد مشتری بابت قسط؛ خروجی شماره پیگیری.</summary>
    Task<string> DebitAsync(string mobileNumber, string contractNumber, decimal tokenAmount, string description, CancellationToken ct);
}
