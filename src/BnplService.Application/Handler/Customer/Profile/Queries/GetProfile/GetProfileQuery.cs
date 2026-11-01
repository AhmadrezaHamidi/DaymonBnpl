namespace BnplService.Application.Handler.Customer.Profile.Queries.GetProfile;

/// <summary>«اطلاعات ثبت‌شده» برای مشاهده و ویرایش (US-3-01، US-3-02).</summary>
public sealed record GetProfileQuery : BnplBaseQuery<BnplMessageResponse<CustomerProfileDto>>;

public sealed class GetProfileQueryHandler(IBnplDbContext db, ICustomerProfileProvider profiles, IBnplCurrentCustomer customer,
    TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<GetProfileQuery, BnplMessageResponse<CustomerProfileDto>>
{
    public async Task<BnplMessageResponse<CustomerProfileDto>> Handle(GetProfileQuery query, CancellationToken ct)
    {
        var me = customer.Value;
        var status = await profiles.GetOrAddAsync(me.MobileNumber, ct);
        var p = await Db.CustomerProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == me.UserId, ct);
        return Ok(new CustomerProfileDto(me.MobileNumber, status.ProfileCompleted, status.AddressCompleted,
            p?.FirstName, p?.LastName, p?.FatherName, p?.NationalCode, p?.PersianBirthDate, p?.Gender, p?.Sheba,
            p?.Province, p?.City, p?.Street, p?.PostalCode, p?.TelephoneNumber, p?.InfoUpdatedAt, p?.AddressUpdatedAt));
    }
}
