using DayaDiamond.Client;

namespace BnplService.Infrastructure.Services.DayaDiamond;

/// <summary>
/// درگاه پرداخت قسط روی دایادایموند. فهرست درگاه‌ها (gateway/list) واقعی است.
/// ⚠ مستند دایادایموند فقط «خرید توکن» (buy/request) را دارد، نه پرداخت مبلغ دلخواه و verify آن؛
/// تا آن API برسد StartAsync و VerifyAsync خطای روشن می‌دهند و در توسعه از درگاه Sandbox استفاده می‌شود (Bnpl:Providers:Gateway).
/// </summary>
public sealed class DayaDiamondPaymentGateway(IDayaDiamondClient client) : IBnplPaymentGateway
{
    public const string NotDocumented = "پرداخت قسط از درگاه هنوز در سرویس دایادایموند فعال نشده است.";

    public async Task<IReadOnlyList<PaymentGatewayDto>> ListAsync(CancellationToken ct)
    {
        var gateways = await DayaDiamondCall.RunAsync(() => client.GetGatewaysAsync(ct), ct);
        return gateways.Select(g => new PaymentGatewayDto(g.Id, g.Title, g.Description)).ToList();
    }

    public Task<GatewayPaymentStartDto> StartAsync(GatewayPaymentStartInput input, CancellationToken ct) =>
        throw new BnplProviderException(NotDocumented);

    public Task<GatewayPaymentResultDto> VerifyAsync(string providerReference, long expectedAmount, CancellationToken ct) =>
        throw new BnplProviderException(NotDocumented);
}
