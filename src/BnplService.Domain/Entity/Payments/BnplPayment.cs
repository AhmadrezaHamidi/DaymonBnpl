namespace BnplService.Domain.Entity.Payments;

/// <summary>
/// یک تلاش پرداخت قسط (درگاه، توکنی یا ثبت دستی ادمین). «گزارش تراکنش» فیگما از همین ساخته می‌شود.
/// مبلغ قسط و جریمه هنگام شروع پرداخت ثابت می‌شود تا کاربر همان مبلغی را بپردازد که دیده است.
/// </summary>
public class BnplPayment
{
    public long Id { get; private set; }
    public long RequestId { get; private set; }
    public string ContractNumber { get; private set; } = null!;
    public string CustomerId { get; private set; } = null!;
    public int InstallmentNumber { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;

    /// <summary>مبلغ قسط (ریال).</summary>
    public long Amount { get; private set; }

    /// <summary>جریمهٔ دیرکرد (ریال).</summary>
    public long Penalty { get; private set; }

    public long Total => Amount + Penalty;

    public int? GatewayId { get; private set; }
    public string? GatewayTitle { get; private set; }

    /// <summary>شناسهٔ پرداخت نزد ارائه‌دهنده (مثل paymentExtId).</summary>
    public string? ProviderReference { get; private set; }

    public string? PayUrl { get; private set; }

    /// <summary>آدرس اپ که مشتری پس از درگاه به آن برمی‌گردد (از فهرست مجاز).</summary>
    public string? ReturnUrl { get; private set; }

    /// <summary>شماره پیگیری.</summary>
    public string? TrackingNumber { get; private set; }

    /// <summary>پرداخت توکنی: تعداد و قیمت توکن کسرشده.</summary>
    public decimal? TokenAmount { get; private set; }
    public long? TokenPrice { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public string Description { get; private set; } = null!;
    public Actor By { get; private set; } = null!;

    private BnplPayment() { }

    public static BnplPayment Start(BnplRequest request, Installment installment, long penalty, PaymentMethod method, Actor by, DateTime now,
        TimeSpan? timeout = null)
    {
        var description = installment.Number == 1 && request.Status == BnplRequestStatus.Signed
            ? $"پرداخت قسط اول و فعال‌سازی قرارداد {request.ContractNumber}"
            : $"پرداخت قسط {PersianNumber.Of(installment.Number)} قرارداد {request.ContractNumber}";
        return new BnplPayment
        {
            RequestId = request.Id,
            ContractNumber = request.ContractNumber,
            CustomerId = request.CustomerId,
            InstallmentNumber = installment.Number,
            Method = method,
            Amount = installment.Amount,
            Penalty = penalty,
            CreatedAt = now,
            ExpiresAt = timeout is { } t ? now.Add(t) : null,
            Description = description,
            By = by,
        };
    }

    public bool IsPending => Status == PaymentStatus.Pending;

    public bool IsExpired(DateTime now) => IsPending && ExpiresAt is { } e && now > e;

    public void AttachGateway(int gatewayId, string? gatewayTitle, string providerReference, string payUrl, string? returnUrl)
    {
        EnsurePending();
        ReturnUrl = returnUrl;
        GatewayId = gatewayId;
        GatewayTitle = gatewayTitle;
        ProviderReference = providerReference;
        PayUrl = payUrl;
    }

    public void AttachTokens(decimal tokenAmount, long tokenPrice)
    {
        EnsurePending();
        TokenAmount = tokenAmount;
        TokenPrice = tokenPrice;
    }

    public void Succeed(string? trackingNumber, DateTime now)
    {
        EnsurePending();
        Status = PaymentStatus.Succeeded;
        TrackingNumber = trackingNumber;
        CompletedAt = now;
    }

    public void Fail(string reason, DateTime now)
    {
        EnsurePending();
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        CompletedAt = now;
    }

    /// <summary>پول آمد ولی قسط دیگر قابل پرداخت نبود؛ باید به مشتری برگردد (در پنل ادمین دیده می‌شود).</summary>
    public void RequireRefund(string reason)
    {
        if (Status != PaymentStatus.Succeeded) throw new InvalidOperationException("فقط پرداخت موفق نیاز به برگشت دارد.");
        Status = PaymentStatus.RefundRequired;
        FailureReason = reason;
    }

    /// <summary>ادمین برگشت وجه را با شماره پیگیری بانکی ثبت می‌کند.</summary>
    public void MarkRefunded(string trackingNumber, Actor by, DateTime now)
    {
        if (Status != PaymentStatus.RefundRequired) throw new BnplDomainException("این پرداخت نیازمند برگشت وجه نیست.");
        trackingNumber = (trackingNumber ?? "").Trim();
        if (trackingNumber.Length is < 3 or > 100) throw new BnplDomainException("شماره پیگیری برگشت وجه الزامی است.");
        Status = PaymentStatus.Refunded;
        RefundTrackingNumber = trackingNumber;
        RefundedAt = now;
        RefundedBy = by;
    }

    public string? RefundTrackingNumber { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public Actor? RefundedBy { get; private set; }

    private void EnsurePending()
    {
        if (!IsPending) throw new BnplDomainException("وضعیت این پرداخت قبلاً مشخص شده است.");
    }
}
