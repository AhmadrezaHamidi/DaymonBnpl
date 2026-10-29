using DayaDiamond.Client;
using DayaDiamond.Client.Models;

namespace BnplService.Infrastructure.Services.DayaDiamond;

/// <summary>پروفایل مشتری نزد دایادایموند: user/getOrAdd (موبایل رمز می‌شود)، profile/upsertInfo، profile/upsertAddress.</summary>
public sealed class DayaDiamondProfileProvider(IDayaDiamondClient client) : ICustomerProfileProvider
{
    public async Task<CustomerProfileStatusDto> GetOrAddAsync(string mobileNumber, CancellationToken ct)
    {
        var user = await DayaDiamondCall.RunAsync(() => client.GetOrAddUserAsync(mobileNumber, ct), ct);
        return new CustomerProfileStatusDto(string.IsNullOrWhiteSpace(user.PhoneNumber) ? mobileNumber : user.PhoneNumber,
            user.ProfileCompleted, user.AddressCompleted);
    }

    public Task UpsertInfoAsync(CustomerInfoInput input, CancellationToken ct) =>
        DayaDiamondCall.RunAsync(() => client.UpsertProfileInfoAsync(new UpsertProfileInfoRequest
        {
            PhoneNumber = input.PhoneNumber,
            NationalCode = input.NationalCode,
            FirstName = input.FirstName,
            LastName = input.LastName,
            FatherName = input.FatherName,
            PersianBirthDate = input.PersianBirthDate,
            Gender = input.Gender,
            Sheba = input.Sheba,
        }, ct), ct);

    public Task UpsertAddressAsync(CustomerAddressInput input, CancellationToken ct) =>
        DayaDiamondCall.RunAsync(() => client.UpsertAddressAsync(new UpsertAddressRequest
        {
            PhoneNumber = input.PhoneNumber,
            Province = input.Province,
            City = input.City,
            Street = input.Street,
            PostalCode = input.PostalCode,
            TelephoneNumber = input.TelephoneNumber,
        }, ct), ct);
}
