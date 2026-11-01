namespace BnplService.Domain.Helpers;

/// <summary>
/// استان‌های ایران با پیش‌شمارهٔ تلفن ثابت (US-3-02: انتخاب استان از فهرست، شماره ثابت همراه با پیش‌شماره).
/// فهرست شهرهای هر استان منبع داده لازم دارد و فعلاً شهر متن آزاد است.
/// </summary>
public static class IranProvinces
{
    public sealed record Province(string Name, string AreaCode);

    public static IReadOnlyList<Province> All { get; } =
    [
        new("آذربایجان شرقی", "041"), new("آذربایجان غربی", "044"), new("اردبیل", "045"), new("اصفهان", "031"),
        new("البرز", "026"), new("ایلام", "084"), new("بوشهر", "077"), new("تهران", "021"),
        new("چهارمحال و بختیاری", "038"), new("خراسان جنوبی", "056"), new("خراسان رضوی", "051"), new("خراسان شمالی", "058"),
        new("خوزستان", "061"), new("زنجان", "024"), new("سمنان", "023"), new("سیستان و بلوچستان", "054"),
        new("فارس", "071"), new("قزوین", "028"), new("قم", "025"), new("کردستان", "087"),
        new("کرمان", "034"), new("کرمانشاه", "083"), new("کهگیلویه و بویراحمد", "074"), new("گلستان", "017"),
        new("گیلان", "013"), new("لرستان", "066"), new("مازندران", "011"), new("مرکزی", "086"),
        new("هرمزگان", "076"), new("همدان", "081"), new("یزد", "035"),
    ];

    /// <summary>استان با نام (ی/ک عربی و فاصلهٔ اضافه نادیده گرفته می‌شود)؛ null یعنی ناشناخته.</summary>
    public static Province? Find(string? name)
    {
        var n = Normalize(name);
        return All.FirstOrDefault(p => Normalize(p.Name) == n);
    }

    private static string Normalize(string? s) =>
        string.Join(' ', (s ?? "").Replace('ي', 'ی').Replace('ك', 'ک').Replace('‌', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
