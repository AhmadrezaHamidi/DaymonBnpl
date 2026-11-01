namespace BnplService.Application.Handler.Customer.Requests.Queries.GetCollateralStatus;

/// <summary>
/// دکمهٔ «ادامه» در درخواست ناتمام: توکن وثیقهٔ لازم با قیمت لحظه‌ای، موجودی آزاد مشتری و کمبود.
/// اگر کم باشد، اپ هشدار «تعداد n توکن کم دارید» و مسیر خرید توکن را نشان می‌دهد.
/// </summary>
public sealed record GetCollateralStatusQuery(long Id) : BnplBaseQuery<BnplMessageResponse<CollateralStatusDto>>;
