namespace BnplService.Contracts.Profile;

/// <summary>
/// «اطلاعات ثبت‌شده» برای مشاهده و ویرایش (US-3-01، US-3-02): وضعیت تکمیل از دایادایموند،
/// مقادیر از رونوشتی که همین سرویس ذخیره کرده (اگر از جای دیگری ثبت شده باشد، مقدارها خالی‌اند).
/// موبایل فقط‌خواندنی است و از نشست کاربر می‌آید.
/// </summary>
public sealed record CustomerProfileDto(
    string MobileNumber, bool ProfileCompleted, bool AddressCompleted,
    string? FirstName, string? LastName, string? FatherName, string? NationalCode, string? PersianBirthDate, string? Gender,
    string? Sheba, string? Province, string? City, string? Street, string? PostalCode, string? TelephoneNumber,
    DateTime? InfoUpdatedAt, DateTime? AddressUpdatedAt);

/// <summary>استان با پیش‌شمارهٔ تلفن ثابت.</summary>
public sealed record ProvinceDto(string Name, string AreaCode);
