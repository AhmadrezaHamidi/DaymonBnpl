using DayaDiamond.Client.Models;

namespace DayaDiamond.Client;

public interface IDayaDiamondClient
{
    /// <summary>POST account/login — معمولا لازم نیست مستقیم صدا زده شود؛ کلاینت خودش توکن را مدیریت می‌کند.</summary>
    Task<string> LoginAsync(CancellationToken ct = default);

    /// <summary>GET price/token — قیمت هر توکن به تومان</summary>
    Task<TokenPriceResult> GetTokenPriceAsync(CancellationToken ct = default);

    /// <summary>GET gateway/list — لیست درگاه‌ها</summary>
    Task<IReadOnlyList<Gateway>> GetGatewaysAsync(CancellationToken ct = default);

    /// <summary>POST user/getOrAdd — ثبت نام/دریافت مشتری با موبایل خام (Encrypt داخل کلاینت انجام می‌شود)</summary>
    Task<UserStatus> GetOrAddUserAsync(string mobile, CancellationToken ct = default);

    /// <summary>POST profile/upsertInfo — افزودن/ویرایش پروفایل</summary>
    Task UpsertProfileInfoAsync(UpsertProfileInfoRequest request, CancellationToken ct = default);

    /// <summary>POST profile/upsertAddress — افزودن/ویرایش آدرس</summary>
    Task UpsertAddressAsync(UpsertAddressRequest request, CancellationToken ct = default);

    /// <summary>GET buy/policy — قوانین خرید</summary>
    Task<BuyPolicy> GetBuyPolicyAsync(CancellationToken ct = default);

    /// <summary>GET price/buy — مبلغ قابل پرداخت برای تعداد توکن</summary>
    Task<BuyPriceResult> GetBuyPriceAsync(decimal amount, string assetCode = "DAYADIAMOND", CancellationToken ct = default);

    /// <summary>POST buy/request — ثبت درخواست خرید؛ کاربر را به PayUrl هدایت کنید.</summary>
    Task<BuyRequestResult> CreateBuyRequestAsync(BuyRequest request, CancellationToken ct = default);

    /// <summary>GET withdraw/validateRequest — آیا کاربر می‌تواند درخواست بازخرید ثبت کند؟ در صورت عدم امکان DayaDiamondException می‌دهد.</summary>
    Task ValidateWithdrawRequestAsync(decimal tokenAmount, string phoneNumber, CancellationToken ct = default);
}
