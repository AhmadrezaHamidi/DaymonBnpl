using System.Text.Json.Serialization;

namespace DayaDiamond.Client.Models;

public static class Genders
{
    public const string Male = "Male";
    public const string Female = "Female";
}

public sealed class UpsertProfileInfoRequest
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("nationalCode")]
    public string NationalCode { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("fatherName")]
    public string FatherName { get; set; } = string.Empty;

    /// <summary>تاریخ تولد شمسی به فرمت yyyy/MM/dd، مثلا 1360/11/12</summary>
    [JsonPropertyName("persianBirthDate")]
    public string PersianBirthDate { get; set; } = string.Empty;

    /// <summary>Male یا Female (مقدار دوم در مستند نیامده؛ حدس زده شده).</summary>
    [JsonPropertyName("gender")]
    public string Gender { get; set; } = Genders.Male;

    /// <summary>شماره شبا بدون IR (۲۴ رقم)</summary>
    [JsonPropertyName("sheba")]
    public string Sheba { get; set; } = string.Empty;
}

public sealed class UpsertAddressRequest
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("province")]
    public string Province { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("street")]
    public string Street { get; set; } = string.Empty;

    [JsonPropertyName("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    [JsonPropertyName("telephoneNumber")]
    public string TelephoneNumber { get; set; } = string.Empty;
}
