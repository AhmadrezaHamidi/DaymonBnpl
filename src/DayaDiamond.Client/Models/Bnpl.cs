using System.Text.Json.Serialization;

namespace DayaDiamond.Client.Models;

// ⚠ مدل‌های سرویس‌های «پیشنهادی» پیش‌خرید اقساطی؛ در مستند دایادایموند نیستند (IDayaDiamondBnplApi را ببینید).

/// <summary>موجودی توکن مشتری: آزاد (قابل برداشت، وثیقه یا پرداخت) و مسدود.</summary>
public sealed class WalletBalance
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("freeAmount")]
    public decimal FreeAmount { get; set; }

    [JsonPropertyName("lockedAmount")]
    public decimal LockedAmount { get; set; }
}

/// <summary>عملیات روی حساب توکن مشتری؛ reference (شمارهٔ قرارداد) عملیات تکراری را بی‌اثر می‌کند.</summary>
public sealed class WalletOperationRequest
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public sealed class WalletOperationResult
{
    [JsonPropertyName("trackingNumber")]
    public string TrackingNumber { get; set; } = string.Empty;
}

/// <summary>درخواست پرداخت مبلغ دلخواه (قسط) از درگاه؛ مبلغ به ریال.</summary>
public sealed class InstallmentPaymentRequest
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("amountRial")]
    public long AmountRial { get; set; }

    [JsonPropertyName("gatewayId")]
    public int GatewayId { get; set; }

    [JsonPropertyName("returnUrl")]
    public string ReturnUrl { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;
}

public static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Failed = "Failed";
}

public sealed class InstallmentPaymentStatus
{
    [JsonPropertyName("paymentExtId")]
    public string PaymentExtId { get; set; } = string.Empty;

    /// <summary>Pending، Paid یا Failed.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = PaymentStatuses.Pending;

    [JsonPropertyName("amountRial")]
    public long AmountRial { get; set; }

    [JsonPropertyName("trackingNumber")]
    public string? TrackingNumber { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
