namespace BnplService.Application.Handler.Customer.Payments.Queries.GetGateways;

/// <summary>درگاه‌های قابل انتخاب در «انتخاب روش پرداخت».</summary>
public sealed record GetGatewaysQuery : BnplBaseQuery<BnplMessageResponse<IReadOnlyList<PaymentGatewayDto>>>;

public sealed class GetGatewaysQueryHandler(IBnplPaymentGateway gateway)
    : IRequestHandler<GetGatewaysQuery, BnplMessageResponse<IReadOnlyList<PaymentGatewayDto>>>
{
    public async Task<BnplMessageResponse<IReadOnlyList<PaymentGatewayDto>>> Handle(GetGatewaysQuery request, CancellationToken ct) =>
        MessageResponse.Generate(200, await gateway.ListAsync(ct), "عملیات با موفقیت انجام شد.");
}
