using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BnplService.Application.Handler.Admin.Shared;

namespace BnplService.Application.Handler.Admin.Settings.Commands.SavePlan;

/// <summary>
/// ساخت (Id = null) یا ویرایش طرح اقساط. طرح حذف نمی‌شود، غیرفعال می‌شود؛ قراردادهای قبلی درصدهای خودشان را دارند.
/// </summary>
public sealed record SavePlanCommand : BnplBaseCommand<BnplMessageResponse<PlanDto>>
{
    [JsonIgnore, BindNever] public int? Id { get; init; }
    [Required, StringLength(100, MinimumLength = 2)] public string Title { get; init; } = null!;
    [Range(1, 60)] public int InstallmentCount { get; init; }
    [Range(0, 100)] public decimal FeePercent { get; init; }
    [Range(0, 100)] public decimal UpfrontFeePercent { get; init; }
    [Range(0, 10)] public decimal LatePenaltyDailyPercent { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;

    /// <summary>برای ویرایش الزامی.</summary>
    [StringLength(20)] public string? RowVersion { get; init; }
}

public sealed class SavePlanCommandHandler(IBnplDbContext db, IBnplCurrentUser user, TimeProvider time)
    : AdminHandlerBase(db, user, time), IRequestHandler<SavePlanCommand, BnplMessageResponse<PlanDto>>
{
    public async Task<BnplMessageResponse<PlanDto>> Handle(SavePlanCommand c, CancellationToken ct)
    {
        BnplPlan plan;
        if (c.Id is { } id)
        {
            plan = await Db.Plans.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw BnplDomainException.NotFound("طرح یافت نشد.");
            plan.EnsureRowVersion(c.RowVersion);
            plan.Update(c.Title, c.InstallmentCount, c.FeePercent, c.UpfrontFeePercent, c.LatePenaltyDailyPercent, c.SortOrder, c.IsActive);
        }
        else
        {
            plan = new BnplPlan(c.Title, c.InstallmentCount, c.FeePercent, c.UpfrontFeePercent, c.LatePenaltyDailyPercent, c.SortOrder);
            if (!c.IsActive) plan.Update(c.Title, c.InstallmentCount, c.FeePercent, c.UpfrontFeePercent, c.LatePenaltyDailyPercent, c.SortOrder, false);
            Db.Plans.Add(plan);
        }
        await Db.SaveChangesAsync(ct);
        return Ok(PlanDto.Of(plan), "طرح ذخیره شد.");
    }
}
