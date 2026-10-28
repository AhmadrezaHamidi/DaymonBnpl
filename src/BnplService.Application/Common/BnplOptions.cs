namespace BnplService.Application.Common;

/// <summary>
/// تنظیمات «Bnpl» در appsettings:
///   PublicBaseUrl              آدرس عمومی همین سرویس؛ کال‌بک درگاه = {PublicBaseUrl}/v2/bnpl/Payments/{id}/Callback
///   AllowedReturnUrlPrefixes   آدرس‌هایی که مشتری پس از پرداخت به آن‌ها برمی‌گردد (جلوگیری از open redirect)
///   DefaultReturnUrl           اگر اپ returnUrl نفرستاد
/// </summary>
public sealed class BnplOptions
{
    public const string SectionName = "Bnpl";

    public string PublicBaseUrl { get; set; } = "http://localhost:55460";
    public string[] AllowedReturnUrlPrefixes { get; set; } = [];
    public string? DefaultReturnUrl { get; set; }

    public string CallbackUrl(long paymentId) => $"{PublicBaseUrl.TrimEnd('/')}/v2/bnpl/Payments/{paymentId}/Callback";

    /// <summary>returnUrl مجاز یا پیش‌فرض؛ آدرس ناشناخته خطا می‌دهد.</summary>
    public string? ResolveReturnUrl(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested)) return DefaultReturnUrl;
        if (AllowedReturnUrlPrefixes.Any(p => requested.StartsWith(p, StringComparison.OrdinalIgnoreCase))) return requested;
        throw new BnplDomainException("آدرس بازگشت مجاز نیست.");
    }
}
