namespace BnplService.Domain.Entity.Settings;

/// <summary>
/// «باکس پیام پویا» (US-5): متن و رنگ راهنما/هشدار/اطلاع‌رسانی هر صفحه که ادمین از پنل تعیین می‌کند.
/// اگر برای صفحه‌ای پیام فعالی نباشد، باکس اصلاً نمایش داده نمی‌شود.
/// </summary>
public class PageMessage : VersionedEntity
{
    public int Id { get; private set; }

    /// <summary>کلید صفحه، مثل bnpl.new-request (حروف کوچک لاتین، عدد، نقطه و خط تیره).</summary>
    public string PageKey { get; private set; } = null!;

    public string Text { get; private set; } = null!;
    public PageMessageKind Kind { get; private set; }

    /// <summary>رنگ اختیاری (#RRGGBB)؛ خالی یعنی رنگ پیش‌فرض نوع پیام در اپ.</summary>
    public string? Color { get; private set; }

    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private PageMessage() { }

    public PageMessage(string pageKey, string text, PageMessageKind kind, string? color, DateTime? validFrom, DateTime? validTo,
        DateTime now)
    {
        PageKey = NormalizeKey(pageKey) ?? throw new BnplDomainException("کلید صفحه باید ۳ تا ۶۰ حرف کوچک لاتین، عدد، نقطه یا خط تیره باشد.");
        Set(text, kind, color, validFrom, validTo);
        CreatedAt = now;
    }

    public void Update(string text, PageMessageKind kind, string? color, DateTime? validFrom, DateTime? validTo, bool isActive)
    {
        Set(text, kind, color, validFrom, validTo);
        IsActive = isActive;
        Bump();
    }

    public bool IsShownAt(DateTime now) => IsActive && (ValidFrom is null || ValidFrom <= now) && (ValidTo is null || now <= ValidTo);

    public static string? NormalizeKey(string? key)
    {
        var k = (key ?? "").Trim().ToLowerInvariant();
        return k.Length is >= 3 and <= 60 && k.All(ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '-') ? k : null;
    }

    private void Set(string text, PageMessageKind kind, string? color, DateTime? validFrom, DateTime? validTo)
    {
        text = (text ?? "").Trim();
        if (text.Length is < 3 or > 500) throw new BnplDomainException("متن پیام باید ۳ تا ۵۰۰ حرف باشد.");
        if (!Enum.IsDefined(kind)) throw new BnplDomainException("نوع پیام نامعتبر است.");
        color = string.IsNullOrWhiteSpace(color) ? null : color.Trim().ToUpperInvariant();
        if (color is not null && (color.Length != 7 || color[0] != '#' || !color.Skip(1).All(Uri.IsHexDigit)))
            throw new BnplDomainException("رنگ باید به شکل #RRGGBB باشد.");
        if (validFrom is { } f && validTo is { } t && t <= f) throw new BnplDomainException("پایان نمایش باید بعد از شروع آن باشد.");
        Text = text;
        Kind = kind;
        Color = color;
        ValidFrom = validFrom;
        ValidTo = validTo;
    }
}
