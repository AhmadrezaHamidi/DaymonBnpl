namespace BnplService.Application.Handler.Customer.Profile.Queries.GetProfileStatus;

/// <summary>
/// ثبت‌نام/دریافت مشتری نزد دایادایموند (user/getOrAdd) و وضعیت تکمیل پروفایل و نشانی.
/// اگر کامل نیست، فرانت صفحهٔ «تکمیل اطلاعات حساب کاربری» را نشان می‌دهد.
/// </summary>
public sealed record GetProfileStatusQuery : BnplBaseQuery<BnplMessageResponse<CustomerProfileStatusDto>>;
