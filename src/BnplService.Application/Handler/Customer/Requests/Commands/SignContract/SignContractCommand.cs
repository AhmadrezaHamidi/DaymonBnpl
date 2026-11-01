using BnplService.Application.Handler.Customer.Requests.Shared;

namespace BnplService.Application.Handler.Customer.Requests.Commands.SignContract;

/// <summary>
/// «تأیید و امضای قرارداد»: توکن وثیقه با قیمت همین لحظه قطعی و از موجودی آزاد مشتری مسدود می‌شود
/// (کمبود ← 412 با تعداد توکن کم). از این لحظه ActivationMinutes (پیش‌فرض ۳۰ دقیقه) برای پرداخت قسط اول فرصت هست.
/// </summary>
public sealed record SignContractCommand : CustomerRequestCommand;
