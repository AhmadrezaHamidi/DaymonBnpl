namespace BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileInfo;

public sealed class UpsertProfileInfoCommandHandler(IBnplDbContext db, ICustomerProfileProvider profiles, IBnplCurrentCustomer customer,
    TimeProvider time)
    : BnplHandlerBase(db, time), IRequestHandler<UpsertProfileInfoCommand, BnplMessageResponse<CustomerProfileStatusDto>>
{
    public async Task<BnplMessageResponse<CustomerProfileStatusDto>> Handle(UpsertProfileInfoCommand r, CancellationToken ct)
    {
        if (!NationalCodeRule.IsValid(r.NationalCode)) throw new BnplDomainException("کد ملی معتبر نیست.");
        if (!ShebaRule.IsValid(r.Sheba)) throw new BnplDomainException("شماره شبا معتبر نیست.");
        var me = customer.Value;
        var input = new CustomerInfoInput(me.MobileNumber, r.NationalCode, r.FirstName.Trim(), r.LastName.Trim(), r.FatherName.Trim(),
            r.PersianBirthDate, r.Gender, r.Sheba);
        await profiles.UpsertInfoAsync(input, ct);

        // رونوشت برای «مشاهده و ویرایش اطلاعات ثبت‌شده»؛ فقط بعد از پذیرش دایادایموند.
        var copy = await ProfileCopyAsync(Db, me, ct);
        copy.SetInfo(me.MobileNumber, input.FirstName, input.LastName, input.FatherName, input.NationalCode, input.PersianBirthDate,
            input.Gender, input.Sheba, Now);
        await Db.SaveChangesAsync(ct);
        return MessageResponse.Generate(200, await profiles.GetOrAddAsync(me.MobileNumber, ct), "اطلاعات فردی ذخیره شد.");
    }

    /// <summary>رونوشت پروفایل مشتری؛ اگر نیست ساخته و به Context اضافه می‌شود.</summary>
    internal static async Task<CustomerProfile> ProfileCopyAsync(IBnplDbContext db, BnplCustomerRef me, CancellationToken ct)
    {
        var copy = await db.CustomerProfiles.FirstOrDefaultAsync(p => p.CustomerId == me.UserId, ct);
        if (copy is null)
        {
            copy = new CustomerProfile(me.UserId, me.MobileNumber);
            db.CustomerProfiles.Add(copy);
        }
        return copy;
    }
}
