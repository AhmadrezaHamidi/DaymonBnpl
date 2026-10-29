using System.Collections.Concurrent;

namespace BnplService.Infrastructure.Services.Sandbox;

/// <summary>
/// حافظهٔ ارائه‌دهنده‌های آزمایشی (فقط توسعه و QA؛ با ری‌استارت پاک می‌شود).
/// درگاه: هر پرداخت با شناسهٔ مرجع و مبلغ نگه داشته می‌شود تا صفحهٔ Sandbox آن را موفق یا ناموفق کند.
/// توکن: موجودی آزاد و قفل‌شدهٔ هر موبایل.
/// </summary>
public sealed class SandboxStore
{
    public enum PaymentState { Pending, Paid, Failed }

    public sealed record Payment(string Reference, long PaymentId, long Amount, string Description, string CallbackUrl)
    {
        public PaymentState State { get; set; } = PaymentState.Pending;
    }

    public sealed class Wallet
    {
        public decimal Free { get; set; }
        public Dictionary<string, decimal> Locked { get; } = new();
    }

    private readonly ConcurrentDictionary<string, Payment> _payments = new();
    private readonly ConcurrentDictionary<string, Wallet> _wallets = new();
    private readonly ConcurrentDictionary<string, CustomerProfileStatusDto> _profiles = new();

    public SandboxStore(SandboxOptions options) => Options = options;

    public SandboxOptions Options { get; }

    public Payment AddPayment(GatewayPaymentStartInput input)
    {
        var payment = new Payment(Guid.NewGuid().ToString("N"), input.PaymentId, input.Amount, input.Description, input.CallbackUrl);
        _payments[payment.Reference] = payment;
        return payment;
    }

    public Payment? FindPayment(string reference) => _payments.GetValueOrDefault(reference);

    public Wallet WalletOf(string mobile) => _wallets.GetOrAdd(mobile, _ => new Wallet { Free = Options.FreeTokenBalance });

    public CustomerProfileStatusDto ProfileOf(string mobile) =>
        _profiles.GetOrAdd(mobile, m => new CustomerProfileStatusDto(m, Options.ProfileCompleted, Options.ProfileCompleted));

    public void SetProfile(CustomerProfileStatusDto profile) => _profiles[profile.PhoneNumber] = profile;
}

/// <summary>تنظیمات «Bnpl:Sandbox».</summary>
public sealed class SandboxOptions
{
    public const string SectionName = "Bnpl:Sandbox";

    /// <summary>قیمت ثابت هر توکن به ریال (وقتی Providers:Price = Sandbox).</summary>
    public long TokenPriceRial { get; set; } = 15_010_000;

    /// <summary>موجودی آزاد اولیهٔ توکن هر مشتری برای پرداخت توکنی.</summary>
    public decimal FreeTokenBalance { get; set; } = 100;

    /// <summary>پروفایل و نشانی مشتری تازه از اول کامل باشد.</summary>
    public bool ProfileCompleted { get; set; } = true;

    /// <summary>آدرس صفحهٔ پرداخت آزمایشی؛ PayUrl = {PayPageBaseUrl}/{reference}. خالی ← {Bnpl:PublicBaseUrl}/v2/bnpl/Sandbox/Pay</summary>
    public string? PayPageBaseUrl { get; set; }
}
