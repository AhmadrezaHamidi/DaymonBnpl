using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BnplService.Application.Handler.Customer.Payments.Commands.StartPayment;

/// <summary>
/// «تایید و پرداخت» یک قسط. قسط اول قرارداد امضاشده فقط از درگاه پرداخت می‌شود و قرارداد را فعال می‌کند.
/// درگاه ← PayUrl برمی‌گردد و نتیجه با کال‌بک می‌آید. توکنی ← همین‌جا از موجودی توکن کسر و نتیجه برگردانده می‌شود.
/// </summary>
public sealed record StartPaymentCommand : BnplBaseCommand<BnplMessageResponse<PaymentStartDto>>
{
    [JsonIgnore, BindNever] public long RequestId { get; init; }

    [Range(1, 60)] public int InstallmentNumber { get; init; }

    /// <summary>Gateway یا Token.</summary>
    public PaymentMethod Method { get; init; } = PaymentMethod.Gateway;

    /// <summary>برای درگاه الزامی (از GET Payments/Gateways).</summary>
    public int? GatewayId { get; init; }

    /// <summary>آدرس بازگشت اپ پس از درگاه؛ باید در Bnpl:AllowedReturnUrlPrefixes باشد.</summary>
    [StringLength(500)] public string? ReturnUrl { get; init; }
}
