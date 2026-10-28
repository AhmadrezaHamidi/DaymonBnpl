namespace BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileAddress;

public sealed class UpsertProfileAddressCommandHandler(ICustomerProfileProvider profiles, IBnplCurrentCustomer customer)
    : IRequestHandler<UpsertProfileAddressCommand, BnplMessageResponse<CustomerProfileStatusDto>>
{
    public async Task<BnplMessageResponse<CustomerProfileStatusDto>> Handle(UpsertProfileAddressCommand r, CancellationToken ct)
    {
        var mobile = customer.Value.MobileNumber;
        await profiles.UpsertAddressAsync(new CustomerAddressInput(mobile, r.Province.Trim(), r.City.Trim(), r.Street.Trim(),
            r.PostalCode, r.TelephoneNumber), ct);
        return MessageResponse.Generate(200, await profiles.GetOrAddAsync(mobile, ct), "نشانی ذخیره شد.");
    }
}
