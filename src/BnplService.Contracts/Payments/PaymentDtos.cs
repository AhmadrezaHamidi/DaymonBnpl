using BnplService.Contracts.Common;

namespace BnplService.Contracts.Payments;

/// <summary>نتیجهٔ «تایید و پرداخت»: درگاه ← PayUrl؛ توکنی ← نتیجهٔ نهایی در Receipt.</summary>
public sealed record PaymentStartDto(long PaymentId, PaymentMethod Method, PaymentStatus Status, string? PayUrl, long Amount, long Penalty,
    long Total, PaymentReceiptDto? Receipt);

/// <summary>«گزارش تراکنش» فیگما (پرداخت موفق/ناموفق قسط).</summary>
public sealed record PaymentReceiptDto(
    long Id, string Title, PaymentStatus Status, string StatusTitle, string AssetName, string ContractNumber, int InstallmentNumber,
    PaymentMethod Method, string MethodTitle, long Amount, long Penalty, long Total, decimal? TokenAmount, string? TrackingNumber,
    string? GatewayTitle, DateTime CreatedAt, DateTime? CompletedAt, string Description, string? FailureReason)
{
    public static PaymentReceiptDto Of(BnplPayment p)
    {
        var installment = BnplTitles.Installment(p.InstallmentNumber);
        var title = p.Status switch
        {
            PaymentStatus.Succeeded => $"پرداخت موفق {installment}",
            PaymentStatus.Pending => $"پرداخت {installment} در انتظار نتیجه",
            PaymentStatus.RefundRequired => $"پرداخت {installment}: نیازمند برگشت وجه",
            _ => $"پرداخت {installment} ناموفق",
        };
        return new PaymentReceiptDto(p.Id, title, p.Status, BnplTitles.Of(p.Status), BnplTitles.AssetName, p.ContractNumber,
            p.InstallmentNumber, p.Method, BnplTitles.Of(p.Method), p.Amount, p.Penalty, p.Total, p.TokenAmount, p.TrackingNumber,
            p.GatewayTitle, p.CreatedAt, p.CompletedAt, p.Description, p.FailureReason);
    }
}

public sealed record PaymentCallbackResultDto(long PaymentId, PaymentStatus Status, string? RedirectUrl, PaymentReceiptDto Receipt);
