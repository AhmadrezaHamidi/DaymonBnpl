namespace BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileInfo;

public sealed class UpsertProfileInfoCommandHandler(ICustomerProfileProvider profiles, IBnplCurrentCustomer customer)
    : IRequestHandler<UpsertProfileInfoCommand, BnplMessageResponse<CustomerProfileStatusDto>>
{
    public async Task<BnplMessageResponse<CustomerProfileStatusDto>> Handle(UpsertProfileInfoCommand r, CancellationToken ct)
    {
        if (!NationalCodeRule.IsValid(r.NationalCode)) throw new BnplDomainException("کد ملی معتبر نیست.");
        if (!ShebaRule.IsValid(r.Sheba)) throw new BnplDomainException("شماره شبا معتبر نیست.");
        var mobile = customer.Value.MobileNumber;
        await profiles.UpsertInfoAsync(new CustomerInfoInput(mobile, r.NationalCode, r.FirstName.Trim(), r.LastName.Trim(),
            r.FatherName.Trim(), r.PersianBirthDate, r.Gender, r.Sheba), ct);
        return MessageResponse.Generate(200, await profiles.GetOrAddAsync(mobile, ct), "اطلاعات فردی ذخیره شد.");
    }
}
