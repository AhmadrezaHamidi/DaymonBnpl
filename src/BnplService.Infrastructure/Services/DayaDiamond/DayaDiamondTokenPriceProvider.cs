using DayaDiamond.Client;

namespace BnplService.Infrastructure.Services.DayaDiamond;

/// <summary>GET price/token. دایادایموند قیمت را به تومان می‌دهد؛ BNPL همه‌جا ریال است (× ۱۰).</summary>
public sealed class DayaDiamondTokenPriceProvider(IDayaDiamondClient client) : ITokenPriceProvider
{
    public async Task<TokenPriceDto> GetAsync(CancellationToken ct)
    {
        var price = await DayaDiamondCall.RunAsync(() => client.GetTokenPriceAsync(ct), ct);
        if (price.TokenPrice <= 0) throw new BnplProviderException("قیمت توکن از دایادایموند دریافت نشد.");
        return new TokenPriceDto(price.AssetCode, (long)Math.Round(price.TokenPrice * 10m, MidpointRounding.AwayFromZero));
    }
}
