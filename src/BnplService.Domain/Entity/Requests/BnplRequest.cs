using System.Security.Cryptography;

namespace BnplService.Domain.Entity.Requests;

/// <summary>
/// درخواست/قرارداد پیش‌خرید اقساطی (aggregate). مبلغ‌ها و درصدهای طرح لحظهٔ ثبت روی همین ردیف کپی می‌شوند.
/// تعداد توکن تا پرداخت قسط اول تخمینی است و «بر اساس نرخ توکن در زمان پرداخت قسط اول» قطعی می‌شود.
/// </summary>
public class BnplRequest : VersionedEntity
{
    public const string ContractPrefix = "P5_U";

    public long Id { get; private set; }

    /// <summary>شمارهٔ قرارداد، مثل P5_U7656895.</summary>
    public string ContractNumber { get; private set; } = null!;

    public string CustomerId { get; private set; } = null!;
    public string MobileNumber { get; private set; } = null!;
    public string? CustomerName { get; private set; }

    public int PlanId { get; private set; }
    public string PlanTitle { get; private set; } = null!;

    public long Amount { get; private set; }
    public int InstallmentCount { get; private set; }
    public decimal FeePercent { get; private set; }
    public decimal UpfrontFeePercent { get; private set; }
    public decimal LatePenaltyDailyPercent { get; private set; }
    public long UpfrontFee { get; private set; }
    public long InstallmentAmount { get; private set; }
    public long TotalFee { get; private set; }
    public long TotalRepayment { get; private set; }

    public int? DiscountCodeId { get; private set; }
    public string? DiscountCode { get; private set; }
    public long DiscountAmount { get; private set; }

    /// <summary>قیمت هر توکن (ریال): تا فعال‌سازی آخرین قیمت دیده‌شده، بعد از آن قیمت قطعی قرارداد.</summary>
    public long TokenPrice { get; private set; }
    public decimal TokenAmount { get; private set; }

    public BnplRequestStatus Status { get; private set; } = BnplRequestStatus.Draft;
    public DateTime CreatedAt { get; private set; }

    /// <summary>مهلت تکمیل درخواست (قبل از امضا).</summary>
    public DateTime ExpiresAt { get; private set; }

    public DateTime? TermsAcceptedAt { get; private set; }
    public DateTime? SignedAt { get; private set; }

    /// <summary>مهلت پرداخت قسط اول پس از امضا.</summary>
    public DateTime? ActivationDeadline { get; private set; }

    public DateTime? ActivatedAt { get; private set; }
    public DateTime? SettledAt { get; private set; }

    /// <summary>زمان لغو، انقضا یا خاتمه.</summary>
    public DateTime? ClosedAt { get; private set; }
    public string? CloseReason { get; private set; }

    /// <summary>توکن‌های قرارداد نزد دایادایموند مسدود شد؟ (بعد از فعال‌سازی؛ اگر نشد، Worker دوباره تلاش می‌کند)</summary>
    public bool TokensLocked { get; private set; }

    /// <summary>توکن‌ها پس از تسویه آزاد شد؟</summary>
    public bool TokensReleased { get; private set; }

    public List<Installment> Installments { get; private set; } = [];
    public List<BnplRequestHistory> History { get; private set; } = [];

    private BnplRequest() { }

    public static readonly BnplRequestStatus[] IncompleteStatuses =
        [BnplRequestStatus.Draft, BnplRequestStatus.TermsAccepted, BnplRequestStatus.Signed];

    /// <summary>«تسویه نشده» در تاریخچهٔ فیگما.</summary>
    public static readonly BnplRequestStatus[] UnsettledStatuses = [BnplRequestStatus.Active];

    /// <summary>«تسویه شده» در تاریخچهٔ فیگما.</summary>
    public static readonly BnplRequestStatus[] SettledStatuses = [BnplRequestStatus.Settled];

    public bool IsIncomplete => IncompleteStatuses.Contains(Status);

    public static string NewContractNumber() => ContractPrefix + RandomNumberGenerator.GetInt32(1_000_000, 10_000_000);

    public static BnplRequest Create(BnplCustomerRef customer, BnplPlan plan, BnplQuote quote, string contractNumber,
        BnplSettings settings, DateTime now, DateOnly today)
    {
        if (!settings.IsEnabled) throw new BnplDomainException("ثبت درخواست پیش‌خرید اقساطی در حال حاضر امکان‌پذیر نیست.");
        if (!plan.IsActive) throw new BnplDomainException("طرح انتخاب‌شده فعال نیست.");
        settings.EnsureAmountAllowed(quote.Amount);
        var r = new BnplRequest
        {
            ContractNumber = contractNumber,
            CustomerId = customer.UserId,
            MobileNumber = customer.MobileNumber,
            CustomerName = customer.FullName,
            PlanId = plan.Id,
            PlanTitle = plan.Title,
            CreatedAt = now,
            ExpiresAt = now.AddHours(settings.CompletionHours),
        };
        r.ApplyQuote(quote);
        for (var n = 1; n <= quote.InstallmentCount; n++)
            r.Installments.Add(new Installment(n, Jalali.AddMonths(today, n - 1), quote.AmountOf(n)));
        r.History.Add(new BnplRequestHistory(null, BnplRequestStatus.Draft, Actor.Customer(customer), now, "ثبت درخواست"));
        return r;
    }

    // ── مراحل تکمیل ──

    /// <summary>صفحهٔ «شرایط و موجودی توکن» ← تأیید شرایط.</summary>
    public void AcceptTerms(Actor by, DateTime now)
    {
        EnsureNotExpired(now);
        if (Status != BnplRequestStatus.Draft) throw new BnplDomainException("شرایط این درخواست قبلاً تأیید شده است.");
        TermsAcceptedAt = now;
        Transition(BnplRequestStatus.TermsAccepted, by, now, "تأیید شرایط");
    }

    /// <summary>
    /// کد تخفیف (یا حذف آن با null) و/یا قیمت تازهٔ توکن؛ فقط تا قبل از امضا.
    /// quote باید با همان مبلغ و درصدهای این درخواست ساخته شده باشد.
    /// </summary>
    public void Requote(BnplQuote quote, DiscountCode? code)
    {
        if (Status is not (BnplRequestStatus.Draft or BnplRequestStatus.TermsAccepted))
            throw new BnplDomainException("پس از امضای قرارداد، تغییر کد تخفیف ممکن نیست.");
        if (quote.Amount != Amount || quote.InstallmentCount != InstallmentCount || quote.FeePercent != FeePercent
            || quote.UpfrontFeePercent != UpfrontFeePercent)
            throw new InvalidOperationException("quote با شرایط این درخواست نمی‌خواند.");
        ApplyQuote(quote);
        DiscountCodeId = code?.Id;
        DiscountCode = code?.Code;
        foreach (var i in Installments) i.SetAmount(quote.AmountOf(i.Number));
        Bump();
    }

    /// <summary>«تأیید و امضای قرارداد» ← مهلت پرداخت قسط اول شروع می‌شود.</summary>
    public void Sign(Actor by, DateTime now, int activationMinutes)
    {
        EnsureNotExpired(now);
        if (Status == BnplRequestStatus.Draft) throw new BnplDomainException("اول شرایط پیش‌خرید را تأیید کنید.");
        if (Status != BnplRequestStatus.TermsAccepted) throw new BnplDomainException("این قرارداد قبلاً امضا شده است.");
        SignedAt = now;
        ActivationDeadline = now.AddMinutes(activationMinutes);
        Transition(BnplRequestStatus.Signed, by, now, "امضای قرارداد");
    }

    /// <summary>«لغو درخواست» در صفحهٔ درخواست تکمیل‌نشده.</summary>
    public void Cancel(Actor by, string? reason, DateTime now)
    {
        if (!IsIncomplete) throw new BnplDomainException("فقط درخواست تکمیل‌نشده قابل لغو است.");
        Close(BnplRequestStatus.Cancelled, by, now, string.IsNullOrWhiteSpace(reason) ? "لغو درخواست" : reason.Trim());
    }

    /// <summary>درخواست تکمیل‌نشده‌ای که مهلتش گذشته را منقضی می‌کند. true یعنی همین الان منقضی شد.</summary>
    public bool ExpireIfDue(DateTime now)
    {
        if (!IsDue(now)) return false;
        var reason = Status == BnplRequestStatus.Signed ? "پایان مهلت پرداخت قسط اول" : "پایان مهلت تکمیل درخواست";
        Close(BnplRequestStatus.Expired, Actor.System, now, reason);
        return true;
    }

    /// <summary>درخواستی که پرداخت درگاهیِ در جریان دارد منقضی نمی‌شود تا نتیجهٔ درگاه برسد (فراخوان این را چک می‌کند).</summary>
    public bool IsDue(DateTime now) => Status switch
    {
        BnplRequestStatus.Draft or BnplRequestStatus.TermsAccepted => now > ExpiresAt,
        BnplRequestStatus.Signed => now > ActivationDeadline,
        _ => false,
    };

    // ── پرداخت ──

    /// <summary>
    /// قسطی که الان قابل پرداخت است. قرارداد امضاشده فقط قسط اول را (تا پایان مهلت فعال‌سازی) می‌پذیرد؛
    /// قرارداد فعال فقط اولین قسط پرداخت‌نشده را (اقساط به ترتیب پرداخت می‌شوند).
    /// </summary>
    /// <param name="paymentStartedAt">برای نتیجهٔ درگاه: مهلت فعال‌سازی با زمان شروع پرداخت سنجیده می‌شود، نه زمان رسیدن نتیجه.</param>
    public Installment PayableInstallment(int number, DateTime now, DateTime? paymentStartedAt = null)
    {
        var installment = Installments.FirstOrDefault(i => i.Number == number)
                          ?? throw BnplDomainException.NotFound("قسط یافت نشد.");
        if (installment.IsPaid) throw new BnplDomainException("این قسط قبلاً پرداخت شده است.");
        switch (Status)
        {
            case BnplRequestStatus.Signed:
                if ((paymentStartedAt ?? now) > ActivationDeadline) throw new BnplDomainException("مهلت پرداخت قسط اول تمام شده است؛ درخواست جدید ثبت کنید.");
                if (number != 1) throw new BnplDomainException("برای فعال‌سازی قرارداد ابتدا قسط اول را پرداخت کنید.");
                break;
            case BnplRequestStatus.Active:
                var next = NextUnpaid!;
                if (next.Number != number) throw new BnplDomainException($"اقساط به ترتیب پرداخت می‌شوند؛ ابتدا قسط {PersianNumber.Of(next.Number)} را پرداخت کنید.");
                break;
            case BnplRequestStatus.Draft or BnplRequestStatus.TermsAccepted:
                throw new BnplDomainException("ابتدا شرایط را تأیید و قرارداد را امضا کنید.");
            default:
                throw new BnplDomainException("این قرارداد قسط قابل پرداخت ندارد.");
        }
        return installment;
    }

    /// <summary>جریمهٔ دیرکرد قسط تا امروز (پرداخت‌شده: همان که گرفته شد؛ بخشیده‌شده یا قبل از فعال‌سازی: صفر).</summary>
    public long PenaltyOf(Installment installment, DateOnly today) =>
        installment.IsPaid ? installment.PenaltyPaid
        : installment.PenaltyWaived || Status != BnplRequestStatus.Active ? 0
        : LatePenalty.Calculate(installment.Amount, LatePenaltyDailyPercent, installment.DueDate, today);

    /// <summary>
    /// ثبت پرداخت موفق یک قسط. پرداخت قسط اول قرارداد امضاشده آن را فعال می‌کند: تعداد توکن با قیمت همین لحظه قطعی
    /// و سررسیدها از امروز ماه‌به‌ماه چیده می‌شوند. پرداخت آخرین قسط قرارداد را تسویه می‌کند.
    /// </summary>
    public void ApplyPayment(BnplPayment payment, long? tokenPriceNow, Actor by, DateTime now, DateOnly today)
    {
        if (payment.RequestId != Id || payment.Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException("فقط پرداخت موفقِ همین درخواست ثبت می‌شود.");
        var installment = PayableInstallment(payment.InstallmentNumber, now, payment.CreatedAt);
        // جریمه هنگام شروع پرداخت ثابت شده است؛ پرداختی که از نیمه‌شب گذشت، جریمهٔ روز بعد را نمی‌گیرد.
        if (payment.Amount != installment.Amount)
            throw new BnplDomainException("مبلغ پرداخت‌شده با مبلغ قسط برابر نیست.");

        if (Status == BnplRequestStatus.Signed)
        {
            if (tokenPriceNow is > 0) TokenPrice = tokenPriceNow.Value;
            TokenAmount = Money.Tokens(Amount, TokenPrice);
            foreach (var i in Installments) i.Reschedule(Jalali.AddMonths(today, i.Number - 1));
            ActivatedAt = now;
            Transition(BnplRequestStatus.Active, by, now, "پرداخت قسط اول و فعال‌سازی قرارداد");
        }

        installment.MarkPaid(payment.Id, payment.Penalty, now);
        if (Installments.All(i => i.IsPaid))
        {
            SettledAt = now;
            Transition(BnplRequestStatus.Settled, by, now, "پرداخت آخرین قسط");
        }
        else Bump();
    }

    // ── عملیات ادمین ──

    /// <summary>خاتمهٔ قرارداد فعال توسط ادمین (مثلاً نکول یا توافق).</summary>
    public void Terminate(Actor by, string reason, DateTime now)
    {
        if (Status != BnplRequestStatus.Active) throw new BnplDomainException("فقط قرارداد فعال قابل خاتمه است.");
        reason = (reason ?? "").Trim();
        if (reason.Length is < 5 or > 1000) throw new BnplDomainException("درج دلیل خاتمه (۵ تا ۱۰۰۰ حرف) الزامی است.");
        Close(BnplRequestStatus.Terminated, by, now, reason);
    }

    public void WaivePenalty(int number, Actor by, string reason, DateTime now)
    {
        if (Status != BnplRequestStatus.Active) throw new BnplDomainException("بخشودگی جریمه فقط برای قرارداد فعال است.");
        var installment = Installments.FirstOrDefault(i => i.Number == number) ?? throw BnplDomainException.NotFound("قسط یافت نشد.");
        if (installment.IsPaid) throw new BnplDomainException("قسط پرداخت‌شده جریمهٔ قابل بخشش ندارد.");
        if (installment.PenaltyWaived) throw new BnplDomainException("جریمهٔ این قسط قبلاً بخشیده شده است.");
        reason = (reason ?? "").Trim();
        if (reason.Length is < 5 or > 1000) throw new BnplDomainException("درج دلیل بخشودگی (۵ تا ۱۰۰۰ حرف) الزامی است.");
        installment.WaivePenalty();
        History.Add(new BnplRequestHistory(Status, Status, by, now, $"بخشودگی جریمهٔ قسط {PersianNumber.Of(number)}: {reason}"));
        Bump();
    }

    public void MarkTokensLocked()
    {
        if (ActivatedAt is null) throw new InvalidOperationException("قرارداد فعال نشده است.");
        TokensLocked = true;
        Bump();
    }

    public void MarkTokensReleased()
    {
        if (Status != BnplRequestStatus.Settled || !TokensLocked) throw new InvalidOperationException("فقط توکن مسدودشدهٔ قرارداد تسویه‌شده آزاد می‌شود.");
        TokensReleased = true;
        Bump();
    }

    // ── خلاصه‌ها (صفحهٔ جزئیات فیگما) ──

    public Installment? NextUnpaid => Installments.Where(i => !i.IsPaid).MinBy(i => i.Number);

    public int PaidCount => Installments.Count(i => i.IsPaid);

    /// <summary>«درصد پرداخت» = اقساط پرداخت‌شده ÷ کل اقساط.</summary>
    public int PaidPercent => InstallmentCount == 0 ? 0 : (int)Math.Floor(PaidCount * 100m / InstallmentCount);

    /// <summary>«جمع بدهی پرداخت نشده»: اقساط باقی‌مانده + جریمهٔ تا امروز.</summary>
    public long UnpaidDebt(DateOnly today) => Installments.Where(i => !i.IsPaid).Sum(i => i.Amount + PenaltyOf(i, today));

    public bool IsOverdue(DateOnly today) => Status == BnplRequestStatus.Active && NextUnpaid is { } n && n.DueDate < today;

    /// <summary>«خریدار تنها مالک تعداد توکن متناسب با اقساط پرداخت‌شده می‌باشد.»</summary>
    public decimal OwnedTokenAmount =>
        Status == BnplRequestStatus.Settled ? TokenAmount
        : InstallmentCount == 0 ? 0 : Math.Floor(TokenAmount * PaidCount / InstallmentCount * 1000m) / 1000m;

    // ── داخلی ──

    private void ApplyQuote(BnplQuote quote)
    {
        Amount = quote.Amount;
        InstallmentCount = quote.InstallmentCount;
        FeePercent = quote.FeePercent;
        UpfrontFeePercent = quote.UpfrontFeePercent;
        LatePenaltyDailyPercent = quote.LatePenaltyDailyPercent;
        UpfrontFee = quote.UpfrontFee;
        InstallmentAmount = quote.InstallmentAmount;
        TotalFee = quote.TotalFee;
        TotalRepayment = quote.TotalRepayment;
        DiscountAmount = quote.DiscountAmount;
        TokenPrice = quote.TokenPrice;
        TokenAmount = quote.TokenAmount;
    }

    private void EnsureNotExpired(DateTime now)
    {
        if (IsDue(now)) throw new BnplDomainException("مهلت این درخواست تمام شده است؛ درخواست جدید ثبت کنید.");
    }

    private void Close(BnplRequestStatus to, Actor by, DateTime now, string reason)
    {
        ClosedAt = now;
        CloseReason = reason;
        Transition(to, by, now, reason);
    }

    private void Transition(BnplRequestStatus to, Actor by, DateTime now, string? description)
    {
        History.Add(new BnplRequestHistory(Status, to, by, now, description));
        Status = to;
        Bump();
    }
}
