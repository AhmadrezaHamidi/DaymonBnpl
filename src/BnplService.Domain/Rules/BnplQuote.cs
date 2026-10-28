namespace BnplService.Domain.Rules;

/// <summary>
/// محاسبهٔ «شرایط وام» فیگما. نمونهٔ فیگما (۵۰ میلیون ریال، ۱۲ قسط، کارمزد ۲۲٪ که ۴٪ آن با قسط اول):
///   کارمزد قسط اول      = ۵۰٬۰۰۰٬۰۰۰ × ۴٪                       = ۲٬۰۰۰٬۰۰۰
///   مبلغ هر قسط         = ⌊(۵۰٬۰۰۰٬۰۰۰ + ۵۰٬۰۰۰٬۰۰۰ × ۱۸٪) ÷ ۱۲⌋₁₀ = ۴٬۹۱۶٬۶۶۰
///   قسط اول + کارمزد    = ۴٬۹۱۶٬۶۶۰ + ۲٬۰۰۰٬۰۰۰                 = ۶٬۹۱۶٬۶۶۰
///   جمع کل بازپرداخت    = ۴٬۹۱۶٬۶۶۰ × ۱۲ + ۲٬۰۰۰٬۰۰۰             = ۶۰٬۹۹۹٬۹۲۰
/// (⌊⌋₁₀ یعنی رند به پایین تا ده ریال؛ کسری که می‌ماند به نفع مشتری است، همان‌طور که فیگما نشان می‌دهد.)
/// تخفیف فقط از قسط آخر کم می‌شود (ماده پنج قرارداد).
/// </summary>
public sealed record BnplQuote(
    long Amount,
    int InstallmentCount,
    decimal FeePercent,
    decimal UpfrontFeePercent,
    decimal LatePenaltyDailyPercent,
    long UpfrontFee,
    long InstallmentAmount,
    long FirstInstallmentAmount,
    long LastInstallmentAmount,
    long DiscountAmount,
    long TotalFee,
    long TotalRepayment,
    long TokenPrice,
    decimal TokenAmount)
{
    public static BnplQuote Calculate(long amount, BnplPlan plan, long tokenPrice, DiscountCode? discount = null) =>
        Calculate(amount, plan.InstallmentCount, plan.FeePercent, plan.UpfrontFeePercent, plan.LatePenaltyDailyPercent, tokenPrice,
            discount is null ? null : discount.AmountFor);

    public static BnplQuote Calculate(long amount, int installmentCount, decimal feePercent, decimal upfrontFeePercent,
        decimal latePenaltyDailyPercent, long tokenPrice, Func<long, long>? discountForFee = null)
    {
        if (amount <= 0) throw new BnplDomainException("مبلغ پیش‌خرید باید مثبت باشد.");
        if (installmentCount < 1) throw new BnplDomainException("تعداد اقساط نامعتبر است.");

        var upfrontFee = Money.Percent(amount, upfrontFeePercent);
        var spreadFee = amount * (feePercent - upfrontFeePercent) / 100m;
        var installment = Money.FloorTo10((amount + spreadFee) / installmentCount);
        var total = installment * installmentCount + upfrontFee;
        var totalFee = total - amount;

        var first = installment + upfrontFee;
        var lastBase = installmentCount == 1 ? first : installment;
        // تخفیف هرگز قسط آخر را منفی نمی‌کند؛ در قرارداد تک‌قسطی قسط اول همان قسط آخر است.
        var discount = Math.Min(Math.Max(0, discountForFee?.Invoke(Math.Max(0, totalFee)) ?? 0), lastBase);
        var last = lastBase - discount;
        if (installmentCount == 1) first = last;

        return new BnplQuote(amount, installmentCount, feePercent, upfrontFeePercent, latePenaltyDailyPercent,
            upfrontFee, installment, first, last, discount, totalFee, total - discount, tokenPrice, Money.Tokens(amount, tokenPrice));
    }

    /// <summary>مبلغ قسط n (از ۱).</summary>
    public long AmountOf(int number) =>
        number == 1 ? FirstInstallmentAmount : number == InstallmentCount ? LastInstallmentAmount : InstallmentAmount;
}
