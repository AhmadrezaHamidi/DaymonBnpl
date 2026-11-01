namespace DayaDiamond.Mock;

/// <summary>تنظیمات mock (بخش «Mock»). مقادیر appsettings فقط آزمایشی‌اند.</summary>
public sealed class MockOptions
{
    public string Username { get; set; } = "mock-user";
    public string Password { get; set; } = "mock-password";
    public string MobileKeyBase64 { get; set; } = string.Empty;
    public string MobileIvBase64 { get; set; } = string.Empty;
    public int TokenLifetimeSeconds { get; set; } = 3600;
    public long TokenPriceToman { get; set; } = 1_501_000;
    public decimal DefaultFreeTokens { get; set; } = 100;
    public decimal BuyFeePercentage { get; set; }
    public decimal MinBuyTokens { get; set; } = 0.5m;
    public decimal MaxBuyTokens { get; set; } = 120;

    /// <summary>پرداختی که تا این مدت در درگاه تکمیل نشود، در استعلام «Failed» برمی‌گردد (رفتار پیشنهادی برای سرور واقعی).</summary>
    public int PaymentTimeoutMinutes { get; set; } = 15;
}
