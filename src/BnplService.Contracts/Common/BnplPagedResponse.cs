namespace BnplService.Contracts.Common;

public sealed record BnplPagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public sealed record ActorDto(ActorType Type, string Id, string Name)
{
    public static ActorDto Of(Actor a) => new(a.Type, a.Id, a.Name);
}

/// <summary>عنوان فارسی وضعیت‌ها، همان برچسب‌های فیگما.</summary>
public static class BnplTitles
{
    public static string Of(BnplRequestStatus s) => s switch
    {
        BnplRequestStatus.Draft => "تکمیل‌نشده",
        BnplRequestStatus.TermsAccepted => "تکمیل‌نشده (شرایط تأیید شد)",
        BnplRequestStatus.Signed => "در انتظار پرداخت قسط اول",
        BnplRequestStatus.Active => "تسویه نشده",
        BnplRequestStatus.Settled => "تسویه شده",
        BnplRequestStatus.Cancelled => "لغو شده",
        BnplRequestStatus.Expired => "منقضی شده",
        BnplRequestStatus.Terminated => "خاتمه یافته",
        _ => s.ToString(),
    };

    public static string Of(InstallmentStatus s) => s == InstallmentStatus.Paid ? "پرداخت شده" : "پرداخت نشده";

    public static string Of(PaymentMethod m) => m switch
    {
        PaymentMethod.Gateway => "درگاه پرداخت",
        PaymentMethod.Token => "پرداخت توکنی",
        PaymentMethod.Manual => "ثبت دستی",
        _ => m.ToString(),
    };

    public static string Of(PaymentStatus s) => s switch
    {
        PaymentStatus.Pending => "در انتظار پرداخت",
        PaymentStatus.Succeeded => "موفق",
        PaymentStatus.Failed => "ناموفق",
        PaymentStatus.RefundRequired => "نیازمند برگشت وجه",
        PaymentStatus.Refunded => "وجه برگشت داده شد",
        _ => s.ToString(),
    };

    private static readonly string[] Ordinals =
        ["اول", "دوم", "سوم", "چهارم", "پنجم", "ششم", "هفتم", "هشتم", "نهم", "دهم", "یازدهم", "دوازدهم"];

    /// <summary>«قسط دوم»؛ بعد از دوازدهم با عدد.</summary>
    public static string Installment(int number) =>
        number is >= 1 and <= 12 ? $"قسط {Ordinals[number - 1]}" : $"قسط {PersianNumber.Of(number)}";

    public const string AssetName = "توکن الماس امید";
}
