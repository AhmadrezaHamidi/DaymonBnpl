using System.Text.Json.Serialization;

namespace DayaDiamond.Client.Models;

public sealed class TokenPriceResult
{
    /// <summary>شناسه دارایی، مثلا DAYADIAMOND</summary>
    [JsonPropertyName("assetCode")]
    public string AssetCode { get; set; } = string.Empty;

    /// <summary>مبلغ توکن به تومان</summary>
    [JsonPropertyName("tokenPrice")]
    public decimal TokenPrice { get; set; }
}

public sealed class BuyPriceResult
{
    /// <summary>مبلغ قابل پرداخت (تومان)</summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}
