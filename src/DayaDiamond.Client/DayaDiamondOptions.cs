namespace DayaDiamond.Client;

/// <summary>
/// تنظیمات اتصال به سرویس دایادایموند. مقادیر واقعی را از appsettings / Secret Manager / متغیر محیطی بخوانید.
/// </summary>
public sealed class DayaDiamondOptions
{
    public const string SectionName = "DayaDiamond";

    /// <summary>آدرس پایه، مثلا https://api.example.com</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>کلید AES به صورت Base64 برای Encrypt شماره موبایل (همان _base64Key مستند).</summary>
    public string MobileKeyBase64 { get; set; } = string.Empty;

    /// <summary>IV به صورت Base64 (همان _base64IV مستند). در حالت ECB عملا استفاده نمی‌شود ولی طبق مستند باید ارسال شود.</summary>
    public string MobileIvBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Scheme هدر Authorization. مستند فقط «JwtToken» گفته؛ پیش‌فرض Bearer است.
    /// اگر سرور توکن خام می‌خواهد، رشته خالی بگذارید.
    /// </summary>
    public string AuthorizationScheme { get; set; } = "Bearer";

    /// <summary>چند ثانیه قبل از انقضای JWT، توکن جدید گرفته شود.</summary>
    public int TokenRefreshSkewSeconds { get; set; } = 60;
}
