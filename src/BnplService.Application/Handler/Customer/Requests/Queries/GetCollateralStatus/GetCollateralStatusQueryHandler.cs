namespace BnplService.Application.Handler.Customer.Requests.Queries.GetCollateralStatus;

public sealed class GetCollateralStatusQueryHandler(IBnplDbContext db, IBnplCurrentCustomer customer, ITokenPriceProvider prices,
    ITokenCustody custody, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<GetCollateralStatusQuery, BnplMessageResponse<CollateralStatusDto>>
{
    public async Task<BnplMessageResponse<CollateralStatusDto>> Handle(GetCollateralStatusQuery query, CancellationToken ct)
    {
        var request = await RequestAsync(query.Id, ct, customer.Value.UserId);
        var free = await custody.GetFreeBalanceAsync(customer.Value.MobileNumber, ct);
        if (request.CollateralLocked)
            return Ok(new CollateralStatusDto(request.CollateralTokenAmount, free, 0, true, true, request.TokenPrice, null));
        if (!request.IsIncomplete) throw new BnplDomainException("این درخواست دیگر قابل ادامه نیست.");

        var price = (await prices.GetAsync(ct)).PriceRial;
        var required = BnplRequest.RequiredCollateral(request.Amount, request.CollateralPercent, price);
        var shortfall = Math.Max(0, required - free);
        var message = shortfall > 0
            ? $"تعداد {PersianNumber.Of(shortfall)} توکن کم دارید، برای خرید به درگاه پرداخت منتقل می‌شوید."
            : null;
        return Ok(new CollateralStatusDto(required, free, shortfall, shortfall == 0, false, price, message));
    }
}
