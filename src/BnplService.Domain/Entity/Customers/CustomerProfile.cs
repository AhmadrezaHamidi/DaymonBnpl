namespace BnplService.Domain.Entity.Customers;

/// <summary>
/// رونوشت اطلاعات هویتی و نشانی مشتری که از همین سرویس به دایادایموند فرستاده شده است.
/// دایادایموند API خواندن پروفایل ندارد؛ این رونوشت برای «مشاهده و ویرایش اطلاعات ثبت‌شده» (US-3-01، US-3-02) است.
/// منبع اصلی همچنان دایادایموند است؛ این ردیف فقط بعد از پذیرش موفق آن‌جا به‌روز می‌شود.
/// </summary>
public class CustomerProfile : VersionedEntity
{
    public string CustomerId { get; private set; } = null!;
    public string MobileNumber { get; private set; } = null!;

    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? FatherName { get; private set; }
    public string? NationalCode { get; private set; }
    public string? PersianBirthDate { get; private set; }
    public string? Gender { get; private set; }

    /// <summary>۲۴ رقم بدون IR.</summary>
    public string? Sheba { get; private set; }

    public DateTime? InfoUpdatedAt { get; private set; }

    public string? Province { get; private set; }
    public string? City { get; private set; }
    public string? Street { get; private set; }
    public string? PostalCode { get; private set; }
    public string? TelephoneNumber { get; private set; }
    public DateTime? AddressUpdatedAt { get; private set; }

    private CustomerProfile() { }

    public CustomerProfile(string customerId, string mobileNumber)
    {
        CustomerId = customerId;
        MobileNumber = mobileNumber;
    }

    public void SetInfo(string mobileNumber, string firstName, string lastName, string fatherName, string nationalCode,
        string persianBirthDate, string gender, string sheba, DateTime now)
    {
        MobileNumber = mobileNumber;
        FirstName = firstName;
        LastName = lastName;
        FatherName = fatherName;
        NationalCode = nationalCode;
        PersianBirthDate = persianBirthDate;
        Gender = gender;
        Sheba = sheba;
        InfoUpdatedAt = now;
        Bump();
    }

    public void SetAddress(string mobileNumber, string province, string city, string street, string postalCode, string telephoneNumber,
        DateTime now)
    {
        MobileNumber = mobileNumber;
        Province = province;
        City = city;
        Street = street;
        PostalCode = postalCode;
        TelephoneNumber = telephoneNumber;
        AddressUpdatedAt = now;
        Bump();
    }
}
