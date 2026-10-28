namespace BnplService.Application.Handler.Customer.Config.Queries.GetQuote;

public sealed class GetQuoteQueryHandler(IBnplDbContext db, ITokenPriceProvider prices, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<GetQuoteQuery, BnplMessageResponse<QuoteDto>>
{
    public async Task<BnplMessageResponse<QuoteDto>> Handle(GetQuoteQuery request, CancellationToken ct)
    {
        var settings = await SettingsAsync(ct);
        settings.EnsureAmountAllowed(request.Amount);
        var plan = await ActivePlanAsync(Db, request.PlanId, ct);
        var discount = await DiscountAsync(request.DiscountCode, ct);
        var price = await prices.GetAsync(ct);
        return Ok(QuoteDto.Of(BnplQuote.Calculate(request.Amount, plan, price.PriceRial, discount)));
    }

    /// <summary>طرح فعال با شناسه، یا اولین طرح فعال وقتی شناسه نیامده.</summary>
    internal static async Task<BnplPlan> ActivePlanAsync(IBnplDbContext db, int? planId, CancellationToken ct) =>
        await db.Plans.Where(p => p.IsActive && (planId == null || p.Id == planId))
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id).FirstOrDefaultAsync(ct)
        ?? throw new BnplDomainException(planId == null ? "هیچ طرح فعالی تعریف نشده است." : "طرح انتخاب‌شده فعال نیست.");
}
