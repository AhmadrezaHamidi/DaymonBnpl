namespace BnplService.Domain.Entity.Settings;

/// <summary>
/// طرح اقساط (فیگما: «شرایط وام»): تعداد اقساط، کارمزد کل، سهم کارمزدی که با قسط اول گرفته می‌شود و جریمهٔ دیرکرد روزانه.
/// درصدها روی درخواست کپی می‌شوند؛ تغییر طرح روی قراردادهای قبلی اثر ندارد.
/// </summary>
public class BnplPlan : VersionedEntity
{
    public int Id { get; private set; }
    public string Title { get; private set; } = null!;
    public int InstallmentCount { get; private set; }

    /// <summary>کارمزد کل، درصد از مبلغ پیش‌خرید (فیگما: ۲۲٪).</summary>
    public decimal FeePercent { get; private set; }

    /// <summary>بخشی از کارمزد که با قسط اول دریافت می‌شود و قابل استرداد نیست (فیگما: ۴٪). بقیه بین اقساط پخش می‌شود.</summary>
    public decimal UpfrontFeePercent { get; private set; }

    /// <summary>جریمهٔ دیرکرد: روزانه این درصد از مبلغ قسط (قرارداد: «روزانه ۱ درصد مبلغ آن قسط»).</summary>
    public decimal LatePenaltyDailyPercent { get; private set; }

    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    private BnplPlan() { }

    public BnplPlan(string title, int installmentCount, decimal feePercent, decimal upfrontFeePercent, decimal latePenaltyDailyPercent, int sortOrder)
    {
        Set(title, installmentCount, feePercent, upfrontFeePercent, latePenaltyDailyPercent, sortOrder);
    }

    public void Update(string title, int installmentCount, decimal feePercent, decimal upfrontFeePercent, decimal latePenaltyDailyPercent,
        int sortOrder, bool isActive)
    {
        Set(title, installmentCount, feePercent, upfrontFeePercent, latePenaltyDailyPercent, sortOrder);
        IsActive = isActive;
        Bump();
    }

    private void Set(string title, int installmentCount, decimal feePercent, decimal upfrontFeePercent, decimal latePenaltyDailyPercent, int sortOrder)
    {
        title = (title ?? "").Trim();
        if (title.Length is < 2 or > 100) throw new BnplDomainException("عنوان طرح باید ۲ تا ۱۰۰ حرف باشد.");
        if (installmentCount is < 1 or > 60) throw new BnplDomainException("تعداد اقساط باید بین ۱ تا ۶۰ باشد.");
        if (feePercent is < 0 or > 100) throw new BnplDomainException("درصد کارمزد باید بین ۰ تا ۱۰۰ باشد.");
        if (upfrontFeePercent < 0 || upfrontFeePercent > feePercent)
            throw new BnplDomainException("کارمزد قسط اول نمی‌تواند منفی یا بیشتر از کارمزد کل باشد.");
        if (latePenaltyDailyPercent is < 0 or > 10) throw new BnplDomainException("جریمهٔ دیرکرد روزانه باید بین ۰ تا ۱۰ درصد باشد.");
        Title = title;
        InstallmentCount = installmentCount;
        FeePercent = feePercent;
        UpfrontFeePercent = upfrontFeePercent;
        LatePenaltyDailyPercent = latePenaltyDailyPercent;
        SortOrder = sortOrder;
    }
}
