namespace BnplService.Application.Handler.Customer.Payments.Commands.CompleteGatewayPayment;

/// <summary>
/// کال‌بک درگاه (بدون نشست مشتری). نتیجه از خود درگاه استعلام می‌شود (VerifyAsync)، نه از پارامترهای مرورگر.
/// تکرار کال‌بک بی‌اثر است و همان نتیجهٔ قبلی را برمی‌گرداند.
/// </summary>
public sealed record CompleteGatewayPaymentCommand(long PaymentId) : BnplBaseCommand<BnplMessageResponse<PaymentCallbackResultDto>>;
