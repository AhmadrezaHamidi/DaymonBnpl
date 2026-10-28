namespace BnplService.Application.Handler.Customer.Config.Queries.GetConfig;

public sealed class GetConfigQueryHandler(IBnplDbContext db, ITokenPriceProvider prices, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<GetConfigQuery, BnplMessageResponse<BnplConfigDto>>
{
    public async Task<BnplMessageResponse<BnplConfigDto>> Handle(GetConfigQuery request, CancellationToken ct)
    {
        var settings = await SettingsAsync(ct);
        var plans = await Db.Plans.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync(ct);
        var price = await prices.GetAsync(ct);
        return Ok(new BnplConfigDto(settings.IsEnabled, settings.MinAmount, settings.MaxAmount, settings.AmountStep,
            settings.ActivationMinutes, plans.Select(PlanDto.Of).ToList(), price.AssetCode, price.PriceRial));
    }
}
