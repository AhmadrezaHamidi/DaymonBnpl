using DayaDiamond.Client.Models;

namespace DayaDiamond.Client;

/// <summary>
/// ⚠ سرویس‌های «پیشنهادی» دایادایموند برای پیش‌خرید اقساطی. این‌ها در مستند فعلی دایادایموند نیستند؛
/// شکلشان پیشنهاد ماست و فعلاً فقط mock (tools/DayaDiamond.Mock) آن‌ها را پیاده کرده است.
/// پیش از اتصال به سرور واقعی، مسیرها و فیلدها باید با تیم دایادایموند نهایی شوند.
/// </summary>
public interface IDayaDiamondBnplApi
{
    /// <summary>GET bnpl/wallet/balance — موجودی آزاد و مسدود توکن مشتری.</summary>
    Task<WalletBalance> GetWalletBalanceAsync(string phoneNumber, CancellationToken ct = default);

    /// <summary>POST bnpl/wallet/lock — مسدودسازی توکن وثیقه.</summary>
    Task LockTokensAsync(WalletOperationRequest request, CancellationToken ct = default);

    /// <summary>POST bnpl/wallet/release — آزادسازی وثیقه.</summary>
    Task ReleaseTokensAsync(WalletOperationRequest request, CancellationToken ct = default);

    /// <summary>POST bnpl/wallet/credit — واریز توکن خریده‌شده با اعتبار.</summary>
    Task CreditTokensAsync(WalletOperationRequest request, CancellationToken ct = default);

    /// <summary>POST bnpl/wallet/debit — کسر توکن بابت پرداخت توکنی قسط.</summary>
    Task<WalletOperationResult> DebitTokensAsync(WalletOperationRequest request, CancellationToken ct = default);

    /// <summary>POST bnpl/payment/request — شروع پرداخت مبلغ قسط از درگاه.</summary>
    Task<BuyRequestResult> CreateInstallmentPaymentAsync(InstallmentPaymentRequest request, CancellationToken ct = default);

    /// <summary>GET bnpl/payment/status — نتیجهٔ پرداخت از خود دایادایموند.</summary>
    Task<InstallmentPaymentStatus> GetInstallmentPaymentStatusAsync(string paymentExtId, CancellationToken ct = default);
}
