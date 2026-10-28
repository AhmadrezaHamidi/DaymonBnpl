using System.Text.Json.Serialization;

namespace DayaDiamond.Client.Models;

public sealed class GetOrAddUserRequest
{
    /// <summary>شماره موبایل Encrypt شده (Base64)</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
}

public sealed class UserStatus
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("profileCompleted")]
    public bool ProfileCompleted { get; set; }

    [JsonPropertyName("addressCompleted")]
    public bool AddressCompleted { get; set; }
}
