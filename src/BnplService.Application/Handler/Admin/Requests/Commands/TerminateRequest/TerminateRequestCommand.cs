using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BnplService.Application.Handler.Admin.Shared;

namespace BnplService.Application.Handler.Admin.Requests.Commands.TerminateRequest;

/// <summary>خاتمهٔ قرارداد فعال (نکول، توافق، دستور مرجع). توکن‌های مسدود دست نمی‌خورند؛ تسویه با واحد مالی است.</summary>
public sealed record TerminateRequestCommand : BnplBaseCommand<BnplMessageResponse<AdminRequestDetailsDto>>
{
    [JsonIgnore, BindNever] public long Id { get; init; }
    [Required, StringLength(1000, MinimumLength = 5)] public string Reason { get; init; } = null!;
    [Required, StringLength(20)] public string RowVersion { get; init; } = null!;
}

public sealed class TerminateRequestCommandHandler(IBnplDbContext db, IBnplCurrentUser user, TimeProvider time)
    : AdminHandlerBase(db, user, time), IRequestHandler<TerminateRequestCommand, BnplMessageResponse<AdminRequestDetailsDto>>
{
    public async Task<BnplMessageResponse<AdminRequestDetailsDto>> Handle(TerminateRequestCommand command, CancellationToken ct)
    {
        var request = await RequestAsync(command.Id, ct);
        request.EnsureRowVersion(command.RowVersion);
        request.Terminate(Me, command.Reason, Now);
        await Db.SaveChangesAsync(ct);
        return Ok(await AdminDetailsAsync(request, ct), "قرارداد خاتمه یافت.");
    }
}
