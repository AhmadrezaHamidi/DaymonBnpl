using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BnplService.Application.Handler.Admin.Shared;

namespace BnplService.Application.Handler.Admin.Requests.Commands.WaivePenalty;

/// <summary>بخشودگی جریمهٔ دیرکرد یک قسط پرداخت‌نشده (با دلیل؛ در تاریخچه ثبت می‌شود).</summary>
public sealed record WaivePenaltyCommand : BnplBaseCommand<BnplMessageResponse<AdminRequestDetailsDto>>
{
    [JsonIgnore, BindNever] public long Id { get; init; }
    [JsonIgnore, BindNever] public int InstallmentNumber { get; init; }
    [Required, StringLength(1000, MinimumLength = 5)] public string Reason { get; init; } = null!;
}

public sealed class WaivePenaltyCommandHandler(IBnplDbContext db, IBnplCurrentUser user, TimeProvider time)
    : AdminHandlerBase(db, user, time), IRequestHandler<WaivePenaltyCommand, BnplMessageResponse<AdminRequestDetailsDto>>
{
    public async Task<BnplMessageResponse<AdminRequestDetailsDto>> Handle(WaivePenaltyCommand command, CancellationToken ct)
    {
        var request = await RequestAsync(command.Id, ct);
        request.WaivePenalty(command.InstallmentNumber, Me, command.Reason, Now);
        await Db.SaveChangesAsync(ct);
        return Ok(await AdminDetailsAsync(request, ct), "جریمهٔ قسط بخشیده شد.");
    }
}
