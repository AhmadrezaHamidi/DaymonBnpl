namespace BnplService.Application.Handler.Admin.Settings.Queries.GetSettings;

public sealed record BnplAdminSettingsDto(BnplSettingsDto Settings, IReadOnlyList<PlanDto> Plans);

/// <summary>تنظیمات سراسری و همهٔ طرح‌ها (فعال و غیرفعال).</summary>
public sealed record GetSettingsQuery : BnplBaseQuery<BnplMessageResponse<BnplAdminSettingsDto>>;

public sealed class GetSettingsQueryHandler(IBnplDbContext db, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<GetSettingsQuery, BnplMessageResponse<BnplAdminSettingsDto>>
{
    public async Task<BnplMessageResponse<BnplAdminSettingsDto>> Handle(GetSettingsQuery query, CancellationToken ct)
    {
        var settings = await SettingsAsync(ct);
        var plans = await Db.Plans.OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync(ct);
        return Ok(new BnplAdminSettingsDto(BnplSettingsDto.Of(settings), plans.Select(PlanDto.Of).ToList()));
    }
}
