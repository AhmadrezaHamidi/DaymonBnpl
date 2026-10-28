using System.ComponentModel.DataAnnotations;

namespace BnplService.Application.Handler.Customer.Profile.Commands.UpsertProfileAddress;

/// <summary>فرم «نشانی و اطلاعات تماس» (دایادایموند: profile/upsertAddress)؛ در قرارداد استفاده می‌شود.</summary>
public sealed record UpsertProfileAddressCommand : BnplBaseCommand<BnplMessageResponse<CustomerProfileStatusDto>>
{
    [Required, StringLength(50, MinimumLength = 2)] public string Province { get; init; } = null!;
    [Required, StringLength(50, MinimumLength = 2)] public string City { get; init; } = null!;

    /// <summary>آدرس (حداکثر ۲۶۰ حرف، همان شمارندهٔ فیگما).</summary>
    [Required, StringLength(260, MinimumLength = 5)] public string Street { get; init; } = null!;

    [Required, RegularExpression(@"^\d{10}$", ErrorMessage = "کد پستی را وارد کنید.")] public string PostalCode { get; init; } = null!;

    /// <summary>شماره تلفن ثابت به همراه کد استان.</summary>
    [Required, RegularExpression(@"^0\d{10}$", ErrorMessage = "شماره تلفن ثابت به همراه کد استان وارد شود.")]
    public string TelephoneNumber { get; init; } = null!;
}
