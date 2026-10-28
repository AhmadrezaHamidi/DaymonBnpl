namespace BnplService.Domain.Enums;

/// <summary>
/// چرخهٔ عمر درخواست پیش‌خرید اقساطی (همان صفحه‌های فیگما):
/// Draft (انتخاب مبلغ) → TermsAccepted (تأیید شرایط) → Signed (امضای قرارداد، ۳۰ دقیقه تا پرداخت قسط اول)
/// → Active (قسط اول پرداخت شد) → Settled (همهٔ اقساط پرداخت شد).
/// Draft/TermsAccepted/Signed همان «درخواست تکمیل‌نشده» است که مشتری می‌تواند ادامه یا لغو کند.
/// </summary>
public enum BnplRequestStatus : byte
{
    Draft = 1,
    TermsAccepted = 2,
    Signed = 3,
    Active = 4,
    Settled = 5,
    Cancelled = 6,
    Expired = 7,
    Terminated = 8,
}

public enum InstallmentStatus : byte { Unpaid = 1, Paid = 2 }

public enum PaymentMethod : byte
{
    /// <summary>درگاه بانکی</summary>
    Gateway = 1,
    /// <summary>پرداخت توکنی (کسر از موجودی توکن مشتری)</summary>
    Token = 2,
    /// <summary>ثبت دستی پرداخت توسط ادمین (واریز خارج از سامانه)</summary>
    Manual = 3,
}

public enum PaymentStatus : byte
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    /// <summary>پول دریافت شد ولی قسط دیگر قابل پرداخت نبود (درخواست لغو/منقضی شده یا قسط قبلاً پرداخت شده) ← باید برگشت داده شود.</summary>
    RefundRequired = 4,
    /// <summary>وجهِ RefundRequired به مشتری برگشت داده شد (ثبت ادمین).</summary>
    Refunded = 5,
}

public enum ActorType : byte { Customer = 1, Admin = 2, System = 3 }
