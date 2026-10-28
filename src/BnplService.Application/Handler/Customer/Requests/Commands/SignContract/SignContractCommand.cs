using BnplService.Application.Handler.Customer.Requests.Shared;

namespace BnplService.Application.Handler.Customer.Requests.Commands.SignContract;

/// <summary>«تأیید و امضای قرارداد» ← از این لحظه ActivationMinutes (پیش‌فرض ۳۰ دقیقه) برای پرداخت قسط اول فرصت هست.</summary>
public sealed record SignContractCommand : CustomerRequestCommand;
