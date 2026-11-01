using BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileInfo;

namespace BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileAddress;

public sealed class UpsertProfileAddressCommandHandler(IBnplDbContext db, ICustomerProfileProvider profiles, IBnplCurrentCustomer customer,
    TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<UpsertProfileAddressCommand, BnplMessageResponse<CustomerProfileStatusDto>>
{
    public async Task<BnplMessageResponse<CustomerProfileStatusDto>> Handle(UpsertProfileAddressCommand r, CancellationToken ct)
    {
        var province = IranProvinces.Find(r.Province) ?? throw new BnplDomainException("استان را از فهرست انتخاب کنید.");
        if (!r.TelephoneNumber.StartsWith(province.AreaCode, StringComparison.Ordinal))
            throw new BnplDomainException($"پیش‌شمارهٔ تلفن ثابت استان {province.Name} {string.Concat(province.AreaCode.Select(d => (char)('۰' + d - '0')))} است.");

        var me = customer.Value;
        var input = new CustomerAddressInput(me.MobileNumber, province.Name, r.City.Trim(), r.Street.Trim(), r.PostalCode, r.TelephoneNumber);
        await profiles.UpsertAddressAsync(input, ct);

        var copy = await UpsertProfileInfoCommandHandler.ProfileCopyAsync(Db, me, ct);
        copy.SetAddress(me.MobileNumber, input.Province, input.City, input.Street, input.PostalCode, input.TelephoneNumber, Now);
        await Db.SaveChangesAsync(ct);
        return MessageResponse.Generate(200, await profiles.GetOrAddAsync(me.MobileNumber, ct), "نشانی ذخیره شد.");
    }
}
