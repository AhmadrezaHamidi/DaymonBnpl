using System.Text.Json.Serialization;

namespace DayaDiamond.Client.Models;

public sealed class BuyPolicy
{
    /// <summary>حداقل تعداد توکن قابل خرید</summary>
    [JsonPropertyName("minAmount")]
    public decimal MinAmount { get; set; }

    /// <summary>حداکثر تعداد توکن قابل خرید</summary>
    [JsonPropertyName("maxAmount")]
    public decimal MaxAmount { get; set; }

    /// <summary>درصد کارمزد</summary>
    [JsonPropertyName("feePercentage")]
    public decimal FeePercentage { get; set; }

    /// <summary>روز تعطیل است یا خیر</summary>
    [JsonPropertyName("isHoliday")]
    public bool IsHoliday { get; set; }

    /// <summary>مدت بازخرید (نوع دقیق در مستند مشخص نیست، null برگردانده شده)</summary>
    [JsonPropertyName("withdrawDuration")]
    public System.Text.Json.JsonElement? WithdrawDuration { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public sealed class BuyRequest
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>تعداد توکن مورد نیاز</summary>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>شناسه درگاه (از gateway/list)</summary>
    [JsonPropertyName("gatewayId")]
    public int GatewayId { get; set; }

    /// <summary>آدرس برگشت بعد از درگاه</summary>
    [JsonPropertyName("returnUrl")]
    public string ReturnUrl { get; set; } = string.Empty;
}

public sealed class BuyRequestResult
{
    /// <summary>کاربر باید به این آدرس هدایت شود (درگاه بانکی).</summary>
    [JsonPropertyName("payUrl")]
    public string PayUrl { get; set; } = string.Empty;

    [JsonPropertyName("paymentExtId")]
    public string PaymentExtId { get; set; } = string.Empty;
}
