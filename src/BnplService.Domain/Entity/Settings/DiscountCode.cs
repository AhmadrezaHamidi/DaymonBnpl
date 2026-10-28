namespace BnplService.Domain.Entity.Settings;

/// <summary>
/// کد تخفیف صفحهٔ قرارداد. طبق قرارداد (ماده پنج) «تخفیفات دریافت شده از قسط آخر قرارداد کسر» می‌شود،
/// پس تخفیف فقط مبلغ قسط آخر را کم می‌کند و بقیهٔ جدول اقساط دست نمی‌خورد.
/// </summary>
public class DiscountCode : VersionedEntity
{
    public int Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Title { get; private set; } = null!;

    /// <summary>درصد از کل کارمزد قرارداد؛ یا این یا FixedAmount.</summary>
    public decimal? FeePercent { get; private set; }

    /// <summary>مبلغ ثابت (ریال).</summary>
    public long? FixedAmount { get; private set; }

    /// <summary>سقف تخفیف (ریال)، اختیاری.</summary>
    public long? MaxAmount { get; private set; }

    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }

    /// <summary>حداکثر تعداد قرارداد فعال‌شده با این کد؛ null یعنی نامحدود.</summary>
    public int? UsageLimit { get; private set; }

    public int UsedCount { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private DiscountCode() { }

    public DiscountCode(string code, string title, decimal? feePercent, long? fixedAmount, long? maxAmount,
        DateTime? validFrom, DateTime? validTo, int? usageLimit, DateTime now)
    {
        Code = NormalizeCode(code) ?? throw new BnplDomainException("کد تخفیف باید ۳ تا ۳۰ حرف لاتین یا عدد باشد.");
        Set(title, feePercent, fixedAmount, maxAmount, validFrom, validTo, usageLimit);
        CreatedAt = now;
    }

    public void Update(string title, decimal? feePercent, long? fixedAmount, long? maxAmount, DateTime? validFrom, DateTime? validTo,
        int? usageLimit, bool isActive)
    {
        Set(title, feePercent, fixedAmount, maxAmount, validFrom, validTo, usageLimit);
        IsActive = isActive;
        Bump();
    }

    public static string? NormalizeCode(string? code)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        return c.Length is >= 3 and <= 30 && c.All(ch => ch is >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_') ? c : null;
    }

    /// <summary>کد الان قابل استفاده است؟ (پیام فارسی برای مشتری)</summary>
    public void EnsureUsable(DateTime now)
    {
        if (!IsActive) throw new BnplDomainException("کد تخفیف معتبر نیست.");
        if (ValidFrom is { } from && now < from) throw new BnplDomainException("زمان استفاده از این کد تخفیف هنوز نرسیده است.");
        if (ValidTo is { } to && now > to) throw new BnplDomainException("مهلت استفاده از این کد تخفیف تمام شده است.");
        if (UsageLimit is { } limit && UsedCount >= limit) throw new BnplDomainException("ظرفیت استفاده از این کد تخفیف تکمیل شده است.");
    }

    /// <summary>مبلغ تخفیف برای کارمزد کل داده‌شده (ریال، رند به پایین تا ده ریال).</summary>
    public long AmountFor(long totalFee)
    {
        var amount = FeePercent is { } p ? Money.Percent(totalFee, p) : FixedAmount ?? 0;
        if (MaxAmount is { } max) amount = Math.Min(amount, max);
        return Math.Max(0, amount);
    }

    /// <summary>با فعال شدن قرارداد شمرده می‌شود (درخواست لغوشده ظرفیت کد را مصرف نمی‌کند).</summary>
    public void MarkUsed()
    {
        UsedCount++;
        Bump();
    }

    private void Set(string title, decimal? feePercent, long? fixedAmount, long? maxAmount, DateTime? validFrom, DateTime? validTo, int? usageLimit)
    {
        title = (title ?? "").Trim();
        if (title.Length is < 2 or > 100) throw new BnplDomainException("عنوان کد تخفیف باید ۲ تا ۱۰۰ حرف باشد.");
        if (feePercent.HasValue == fixedAmount.HasValue) throw new BnplDomainException("فقط یکی از «درصد از کارمزد» یا «مبلغ ثابت» را وارد کنید.");
        if (feePercent is < 0 or > 100) throw new BnplDomainException("درصد تخفیف باید بین ۰ تا ۱۰۰ باشد.");
        if (fixedAmount is <= 0) throw new BnplDomainException("مبلغ ثابت تخفیف باید مثبت باشد.");
        if (maxAmount is <= 0) throw new BnplDomainException("سقف تخفیف باید مثبت باشد.");
        if (validFrom.HasValue && validTo.HasValue && validTo <= validFrom) throw new BnplDomainException("تاریخ پایان باید بعد از تاریخ شروع باشد.");
        if (usageLimit is <= 0) throw new BnplDomainException("ظرفیت استفاده باید مثبت باشد.");
        Title = title;
        FeePercent = feePercent;
        FixedAmount = fixedAmount;
        MaxAmount = maxAmount;
        ValidFrom = validFrom;
        ValidTo = validTo;
        UsageLimit = usageLimit;
    }
}
