using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BnplService.Application.Handler.Admin.Shared;

namespace BnplService.Application.Handler.Admin.Payments.Commands.RegisterManualPayment;

/// <summary>
/// ثبت دستی پرداخت قسط (واریز خارج از سامانه، مثل فیش بانکی) با شماره پیگیری. مبلغ همان «قسط + جریمهٔ تا امروز» است.
/// قسط اول قرارداد امضاشده را هم می‌پذیرد و قرارداد را فعال می‌کند.
/// </summary>
public sealed record RegisterManualPaymentCommand : BnplBaseCommand<BnplMessageResponse<AdminRequestDetailsDto>>
{
    [JsonIgnore, BindNever] public long RequestId { get; init; }
    [Range(1, 60)] public int InstallmentNumber { get; init; }
    [Required, StringLength(100, MinimumLength = 3)] public string TrackingNumber { get; init; } = null!;
}

public sealed class RegisterManualPaymentCommandHandler(IBnplDbContext db, IBnplCurrentUser user, BnplPaymentProcessor processor, TimeProvider time)
    : AdminHandlerBase(db, user, time), IRequestHandler<RegisterManualPaymentCommand, BnplMessageResponse<AdminRequestDetailsDto>>
{
    public async Task<BnplMessageResponse<AdminRequestDetailsDto>> Handle(RegisterManualPaymentCommand command, CancellationToken ct)
    {
        var request = await RequestAsync(command.RequestId, ct);
        var installment = request.PayableInstallment(command.InstallmentNumber, Now);
        var payment = BnplPayment.Start(request, installment, request.PenaltyOf(installment, Today), PaymentMethod.Manual, Me, Now);
        Db.Payments.Add(payment);
        await Db.SaveChangesAsync(ct);
        payment.Succeed(command.TrackingNumber.Trim(), Now);
        await processor.ApplyAsync(request, payment, Me, ct);
        if (payment.Status != PaymentStatus.Succeeded) throw new BnplDomainException(payment.FailureReason ?? "ثبت پرداخت ممکن نیست.");
        await Db.SaveChangesAsync(ct);
        await processor.SyncCustodyAsync(request, CancellationToken.None);
        return Ok(await AdminDetailsAsync(request, ct), "پرداخت قسط ثبت شد.");
    }
}
