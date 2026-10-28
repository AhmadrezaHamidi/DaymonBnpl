using System.Text.Json.Serialization;

namespace DayaDiamond.Client.Models;

public sealed class Gateway
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
