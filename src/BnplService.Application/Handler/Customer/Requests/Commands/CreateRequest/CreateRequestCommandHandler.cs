using BnplService.Application.Handler.Customer.Config.Queries.GetQuote;

namespace BnplService.Application.Handler.Customer.Requests.Commands.CreateRequest;

public sealed class CreateRequestCommandHandler(IBnplDbContext db, ITokenPriceProvider prices, ICustomerProfileProvider profiles,
    IBnplCurrentCustomer customer, TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<CreateRequestCommand, BnplMessageResponse<BnplRequestDetailsDto>>
{
    public async Task<BnplMessageResponse<BnplRequestDetailsDto>> Handle(CreateRequestCommand request, CancellationToken ct)
    {
        var me = customer.Value;
        var settings = await SettingsAsync(ct);
        if (!settings.IsEnabled) throw new BnplDomainException("ثبت درخواست پیش‌خرید اقساطی در حال حاضر امکان‌پذیر نیست.");
        settings.EnsureAmountAllowed(request.Amount);

        foreach (var open in await RequestsWithDetails()
                     .Where(r => r.CustomerId == me.UserId && BnplRequest.IncompleteStatuses.Contains(r.Status)).ToListAsync(ct))
        {
            if (!await ExpireIfDueAsync(open, ct))
                throw new BnplDomainException("یک درخواست تکمیل نشده دارید؛ آن را ادامه دهید یا لغو کنید.", 409);
        }
        var active = await Db.Requests.CountAsync(r => r.CustomerId == me.UserId && r.Status == BnplRequestStatus.Active, ct);
        if (active >= settings.MaxActiveContractsPerCustomer)
            throw new BnplDomainException($"حداکثر {PersianNumber.Of(settings.MaxActiveContractsPerCustomer)} قرارداد فعال هم‌زمان مجاز است.");

        var profile = await profiles.GetOrAddAsync(me.MobileNumber, ct);
        if (!profile.IsComplete)
            throw new BnplDomainException("ابتدا اطلاعات حساب کاربری (اطلاعات فردی و نشانی) را تکمیل کنید.", 412);

        var plan = await GetQuoteQueryHandler.ActivePlanAsync(Db, request.PlanId, ct);
        var price = await prices.GetAsync(ct);
        var quote = BnplQuote.Calculate(request.Amount, plan, price.PriceRial);

        var contract = await NewContractNumberAsync(ct);
        var entity = BnplRequest.Create(me, plan, quote, contract, settings, Now, Today);
        Db.Requests.Add(entity);
        await Db.SaveChangesAsync(ct);
        return Ok(entity.ToDetails(Today), "درخواست پیش‌خرید ثبت شد.");
    }

    private async Task<string> NewContractNumberAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var number = BnplRequest.NewContractNumber();
            if (!await Db.Requests.AnyAsync(r => r.ContractNumber == number, ct)) return number;
        }
        throw new InvalidOperationException("Could not allocate a unique contract number.");
    }
}
