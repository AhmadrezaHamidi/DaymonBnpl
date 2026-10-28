namespace BnplService.Domain.Entity.Settings;

/// <summary>
/// تنظیمات سراسری پیش‌خرید اقساطی (یک ردیف، Id = 1). ادمین از پنل ویرایش می‌کند.
/// مقادیر پیش‌فرض از فیگما: بازهٔ اسلایدر ۵۰ میلیون تا ۵ میلیارد ریال، ۳۰ دقیقه مهلت فعال‌سازی پس از امضا.
/// </summary>
public class BnplSettings : VersionedEntity
{
    public const int SingletonId = 1;

    public int Id { get; private set; } = SingletonId;

    /// <summary>خاموش ← ثبت درخواست جدید بسته است (درخواست‌ها و اقساط موجود ادامه دارند).</summary>
    public bool IsEnabled { get; private set; } = true;

    public long MinAmount { get; private set; } = 50_000_000;
    public long MaxAmount { get; private set; } = 5_000_000_000;

    /// <summary>گام اسلایدر مبلغ (ریال).</summary>
    public long AmountStep { get; private set; } = 10_000_000;

    /// <summary>درخواست تکمیل‌نشده (قبل از امضا) پس از این مدت منقضی می‌شود («مهلت تکمیل»).</summary>
    public int CompletionHours { get; private set; } = 24;

    /// <summary>«پس از مشاهده و امضای قرارداد ۳۰ دقیقه فرصت دارید تا قرارداد را فعال کنید.»</summary>
    public int ActivationMinutes { get; private set; } = 30;

    /// <summary>پرداخت درگاهی که در این مدت نتیجه‌اش نیامده، ناموفق حساب می‌شود.</summary>
    public int GatewayPaymentMinutes { get; private set; } = 20;

    /// <summary>حداکثر قرارداد فعال هم‌زمان برای یک مشتری.</summary>
    public int MaxActiveContractsPerCustomer { get; private set; } = 3;

    public DateTime? UpdatedAt { get; private set; }
    public Actor? UpdatedBy { get; private set; }

    public void Update(bool isEnabled, long minAmount, long maxAmount, long amountStep, int completionHours, int activationMinutes,
        int gatewayPaymentMinutes, int maxActiveContracts, Actor by, DateTime at)
    {
        if (minAmount <= 0 || maxAmount < minAmount) throw new BnplDomainException("بازهٔ مبلغ نامعتبر است (حداقل باید مثبت و کمتر از حداکثر باشد).");
        if (amountStep <= 0 || (maxAmount - minAmount) % amountStep != 0)
            throw new BnplDomainException("گام مبلغ باید مثبت باشد و فاصلهٔ حداقل تا حداکثر بر آن بخش‌پذیر باشد.");
        if (completionHours is < 1 or > 720) throw new BnplDomainException("مهلت تکمیل باید بین ۱ تا ۷۲۰ ساعت باشد.");
        if (activationMinutes is < 5 or > 1440) throw new BnplDomainException("مهلت فعال‌سازی باید بین ۵ تا ۱۴۴۰ دقیقه باشد.");
        if (gatewayPaymentMinutes is < 5 or > 120) throw new BnplDomainException("مهلت پرداخت درگاهی باید بین ۵ تا ۱۲۰ دقیقه باشد.");
        if (maxActiveContracts is < 1 or > 100) throw new BnplDomainException("حداکثر قرارداد فعال باید بین ۱ تا ۱۰۰ باشد.");
        IsEnabled = isEnabled;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
        AmountStep = amountStep;
        CompletionHours = completionHours;
        ActivationMinutes = activationMinutes;
        GatewayPaymentMinutes = gatewayPaymentMinutes;
        MaxActiveContractsPerCustomer = maxActiveContracts;
        UpdatedAt = at;
        UpdatedBy = by;
        Bump();
    }

    public void EnsureAmountAllowed(long amount)
    {
        if (amount < MinAmount || amount > MaxAmount)
            throw new BnplDomainException($"مبلغ پیش‌خرید باید بین {PersianNumber.Rial(MinAmount)} و {PersianNumber.Rial(MaxAmount)} باشد.");
        if ((amount - MinAmount) % AmountStep != 0)
            throw new BnplDomainException($"مبلغ پیش‌خرید باید مضربی از {PersianNumber.Rial(AmountStep)} باشد.");
    }
}
