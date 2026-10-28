using System.ComponentModel.DataAnnotations;

namespace BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileInfo;

/// <summary>فرم «اطلاعات فردی» (دایادایموند: profile/upsertInfo). شماره موبایل از نشست مشتری می‌آید، نه از بدنه.</summary>
public sealed record UpsertProfileInfoCommand : BnplBaseCommand<BnplMessageResponse<CustomerProfileStatusDto>>
{
    [Required, StringLength(50, MinimumLength = 2)] public string FirstName { get; init; } = null!;
    [Required, StringLength(50, MinimumLength = 2)] public string LastName { get; init; } = null!;
    [Required, StringLength(50, MinimumLength = 2)] public string FatherName { get; init; } = null!;

    /// <summary>کد ملی ۱۰ رقمی.</summary>
    [Required, RegularExpression(@"^\d{10}$", ErrorMessage = "کد ملی ۱۰ رقمی را وارد کنید.")] public string NationalCode { get; init; } = null!;

    /// <summary>تاریخ تولد شمسی، مثل 1360/11/12.</summary>
    [Required, RegularExpression(@"^1[34]\d{2}/\d{2}/\d{2}$", ErrorMessage = "تاریخ تولد را به شکل ۱۳۶۰/۱۱/۱۲ وارد کنید.")]
    public string PersianBirthDate { get; init; } = null!;

    /// <summary>Male یا Female.</summary>
    [Required, RegularExpression("^(Male|Female)$", ErrorMessage = "جنسیت را انتخاب کنید.")] public string Gender { get; init; } = null!;

    /// <summary>۲۴ رقم پس از IR.</summary>
    [Required, RegularExpression(@"^\d{24}$", ErrorMessage = "۲۴ رقم پس از IR را وارد کنید.")] public string Sheba { get; init; } = null!;
}
