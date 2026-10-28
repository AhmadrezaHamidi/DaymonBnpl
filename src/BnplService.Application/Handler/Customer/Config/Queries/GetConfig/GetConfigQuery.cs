namespace BnplService.Application.Handler.Customer.Config.Queries.GetConfig;

/// <summary>صفحهٔ «درخواست جدید»: بازهٔ اسلایدر، طرح‌های فعال و قیمت لحظه‌ای توکن.</summary>
public sealed record GetConfigQuery : BnplBaseQuery<BnplMessageResponse<BnplConfigDto>>;
