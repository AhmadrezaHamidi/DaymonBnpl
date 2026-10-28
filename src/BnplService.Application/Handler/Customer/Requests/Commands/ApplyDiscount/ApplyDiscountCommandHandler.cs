using BnplService.Application.Handler.Customer.Requests.Shared;

namespace BnplService.Application.Handler.Customer.Requests.Commands.ApplyDiscount;

public sealed class ApplyDiscountCommandHandler(IBnplDbContext db, ITokenPriceProvider prices, IBnplCurrentCustomer customer, TimeProvider time)
    : CustomerRequestHandlerBase(db, customer, time), IRequestHandler<ApplyDiscountCommand, BnplMessageResponse<BnplRequestDetailsDto>>
{
    public async Task<BnplMessageResponse<BnplRequestDetailsDto>> Handle(ApplyDiscountCommand command, CancellationToken ct)
    {
        var request = await MyOpenRequestAsync(command, ct);
        var discount = await DiscountAsync(command.Code, ct);
        var price = await prices.GetAsync(ct);
        var quote = BnplQuote.Calculate(request.Amount, request.InstallmentCount, request.FeePercent, request.UpfrontFeePercent,
            request.LatePenaltyDailyPercent, price.PriceRial, discount is null ? null : discount.AmountFor);
        request.Requote(quote, discount);
        await Db.SaveChangesAsync(ct);
        return Ok(request.ToDetails(Today), discount is null ? "کد تخفیف حذف شد." : "کد تخفیف اعمال شد؛ از قسط آخر کسر می‌شود.");
    }
}
