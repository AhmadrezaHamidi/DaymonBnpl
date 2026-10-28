namespace BnplService.Domain.Helpers;

/// <summary>مبالغ به ریال و long. رند به پایین تا ده ریال (همان اعداد فیگما: ۴٬۹۱۶٬۶۶۰).</summary>
public static class Money
{
    public static long FloorTo10(decimal rial) => (long)(Math.Floor(rial / 10m) * 10m);

    public static long Percent(long amount, decimal percent) => FloorTo10(amount * percent / 100m);

    /// <summary>تعداد توکن با سه رقم اعشار، رند به پایین (مشتری هرگز بیش از پولش توکن نمی‌گیرد).</summary>
    public static decimal Tokens(long rial, long tokenPriceRial)
    {
        if (tokenPriceRial <= 0) throw new BnplDomainException("قیمت توکن در دسترس نیست؛ چند لحظه بعد دوباره تلاش کنید.");
        return Math.Floor(rial * 1000m / tokenPriceRial) / 1000m;
    }

    /// <summary>توکن لازم برای پرداخت مبلغ، رند به بالا (پرداخت توکنی هرگز کمتر از مبلغ قسط نمی‌پردازد).</summary>
    public static decimal TokensToPay(long rial, long tokenPriceRial)
    {
        if (tokenPriceRial <= 0) throw new BnplDomainException("قیمت توکن در دسترس نیست؛ چند لحظه بعد دوباره تلاش کنید.");
        return Math.Ceiling(rial * 1000m / tokenPriceRial) / 1000m;
    }
}
