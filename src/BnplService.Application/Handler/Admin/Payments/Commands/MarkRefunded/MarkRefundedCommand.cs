using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BnplService.Application.Handler.Admin.Shared;

namespace BnplService.Application.Handler.Admin.Payments.Commands.MarkRefunded;

/// <summary>ثبت برگشت وجه پرداختی که «نیازمند برگشت وجه» شده است.</summary>
public sealed record MarkRefundedCommand : BnplBaseCommand<BnplMessageResponse<AdminPaymentListItemDto>>
{
    [JsonIgnore, BindNever] public long PaymentId { get; init; }
    [Required, StringLength(100, MinimumLength = 3)] public string TrackingNumber { get; init; } = null!;
}

public sealed class MarkRefundedCommandHandler(IBnplDbContext db, IBnplCurrentUser user, TimeProvider time)
    : AdminHandlerBase(db, user, time), IRequestHandler<MarkRefundedCommand, BnplMessageResponse<AdminPaymentListItemDto>>
{
    public async Task<BnplMessageResponse<AdminPaymentListItemDto>> Handle(MarkRefundedCommand command, CancellationToken ct)
    {
        var payment = await Db.Payments.FirstOrDefaultAsync(p => p.Id == command.PaymentId, ct)
                      ?? throw BnplDomainException.NotFound("پرداخت یافت نشد.");
        payment.MarkRefunded(command.TrackingNumber, Me, Now);
        await Db.SaveChangesAsync(ct);
        return Ok(new AdminPaymentListItemDto(PaymentReceiptDto.Of(payment), payment.RequestId, payment.CustomerId, ActorDto.Of(payment.By)),
            "برگشت وجه ثبت شد.");
    }
}
