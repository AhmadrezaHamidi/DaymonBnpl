using System.Text.Json;
using System.Text.Json.Serialization;

namespace DayaDiamond.Client.Models;

/// <summary>پوشش عمومی پاسخ‌های دایادایموند: { isSuccessful, error, result }</summary>
public sealed class ApiResponse<T>
{
    [JsonPropertyName("isSuccessful")]
    public bool IsSuccessful { get; set; }

    [JsonPropertyName("error")]
    public JsonElement? Error { get; set; }

    [JsonPropertyName("result")]
    public T? Result { get; set; }
}
