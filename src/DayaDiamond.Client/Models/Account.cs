using System.Text.Json.Serialization;

namespace DayaDiamond.Client.Models;

public sealed class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResult
{
    // نام فیلد در مستند «accesToken» (با یک s) است.
    [JsonPropertyName("accesToken")]
    public string? AccesToken { get; set; }

    // در صورتی که سرور نام درست را برگرداند.
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonIgnore]
    public string? Token => AccesToken ?? AccessToken;
}
